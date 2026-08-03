using System.Collections;
using RiverDeutsch.Networking;
using RiverDeutsch.Networking.Dto;
using RiverDeutsch.UI.Table;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Login
{
    /// <summary>
    /// Wires the LoginScreen UXML to the network layer: HEBERGER starts a host and
    /// REJOINDRE starts a client against the given address/port, then both send
    /// ConnectServerRpc with the chosen pseudo once the session object is ready.
    /// Also owns the one-way handoff to the game table once GAME_STARTED arrives.
    /// </summary>
    public class LoginScreenController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private Font retroFont;

        [Header("Game table handoff")]
        [SerializeField] private GameObject gameTableRoot;
        [SerializeField] private GameTableController gameTableController;

        private TextField pseudoField;
        private TextField addressField;
        private TextField portField;
        private Button hostButton;
        private Button joinButton;
        private Button quitButton;
        private Button startMatchButton;
        private Label statusLabel;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;

            if (retroFont != null)
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(retroFont));
            }

            pseudoField = root.Q<TextField>("pseudo-field");
            addressField = root.Q<TextField>("address-field");
            portField = root.Q<TextField>("port-field");
            hostButton = root.Q<Button>("host-button");
            joinButton = root.Q<Button>("join-button");
            quitButton = root.Q<Button>("quit-button");
            startMatchButton = root.Q<Button>("start-match-button");
            statusLabel = root.Q<Label>("status-label");

            startMatchButton.SetEnabled(false);

            hostButton.clicked += OnHostClicked;
            joinButton.clicked += OnJoinClicked;
            quitButton.clicked += OnQuitClicked;
            startMatchButton.clicked += OnStartMatchClicked;
        }

        private void OnDisable()
        {
            hostButton.clicked -= OnHostClicked;
            joinButton.clicked -= OnJoinClicked;
            quitButton.clicked -= OnQuitClicked;
            startMatchButton.clicked -= OnStartMatchClicked;

            if (NetworkGameSession.Instance != null)
            {
                NetworkGameSession.Instance.OnLobbyStateReceived -= HandleLobbyState;
                NetworkGameSession.Instance.OnGameStateReceived -= HandleGameStateForHandoff;
            }
        }

        private void OnHostClicked()
        {
            if (!ushort.TryParse(portField.value, out ushort port))
            {
                SetStatus("Port invalide.");
                return;
            }

            SetInteractable(false);
            SetStatus("Demarrage de l'hebergement...");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport?.SetConnectionData("0.0.0.0", port, "0.0.0.0");

            if (!NetworkManager.Singleton.StartHost())
            {
                SetStatus("Impossible de demarrer l'hebergement (port deja utilise ?).");
                SetInteractable(true);
                return;
            }

            StartCoroutine(ConnectOnceSessionReady());
        }

        private void OnJoinClicked()
        {
            if (!ushort.TryParse(portField.value, out ushort port))
            {
                SetStatus("Port invalide.");
                return;
            }

            SetInteractable(false);
            SetStatus($"Connexion a {addressField.value}:{port}...");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport?.SetConnectionData(addressField.value, port);

            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnectedWhileConnecting;

            if (!NetworkManager.Singleton.StartClient())
            {
                SetStatus("Impossible de demarrer le client.");
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnectedWhileConnecting;
                SetInteractable(true);
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnectedWhileConnecting;
            StartCoroutine(ConnectOnceSessionReady());
        }

        private void HandleClientDisconnectedWhileConnecting(ulong clientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnectedWhileConnecting;
            SetStatus("Connexion refusee ou serveur injoignable.");
            SetInteractable(true);
        }

        private IEnumerator ConnectOnceSessionReady()
        {
            SetStatus("Connexion etablie, synchronisation...");
            yield return new WaitUntil(() => NetworkGameSession.Instance != null);

            string pseudo = string.IsNullOrWhiteSpace(pseudoField.value) ? "Player" : pseudoField.value.Trim();
            NetworkGameSession.Instance.LocalPlayerName = pseudo;
            NetworkGameSession.Instance.OnLobbyStateReceived += HandleLobbyState;
            NetworkGameSession.Instance.OnGameStateReceived += HandleGameStateForHandoff;
            NetworkGameSession.Instance.ConnectServerRpc(pseudo);
        }

        private void HandleLobbyState(int playerCount)
        {
            SetStatus($"Connecte. {playerCount} joueur(s) dans le lobby.");
            startMatchButton.SetEnabled(playerCount >= 2);
        }

        private void OnStartMatchClicked()
        {
            NetworkGameSession.Instance?.StartMatchServerRpc();
        }

        /// <summary>Watches for the match actually starting, then swaps this screen out
        /// for the game table and forwards it the very state update that started it —
        /// GameTableController's own subscription only begins once it's enabled, which
        /// would otherwise miss this first payload.</summary>
        private void HandleGameStateForHandoff(string action, GameStateDto dto)
        {
            if (action != "GAME_STARTED") return;
            NetworkGameSession.Instance.OnGameStateReceived -= HandleGameStateForHandoff;

            if (gameTableRoot != null) gameTableRoot.SetActive(true);
            if (gameTableController != null) gameTableController.HandleGameState(action, dto);

            gameObject.SetActive(false);
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }

        private void SetInteractable(bool interactable)
        {
            hostButton.SetEnabled(interactable);
            joinButton.SetEnabled(interactable);
        }
    }
}
