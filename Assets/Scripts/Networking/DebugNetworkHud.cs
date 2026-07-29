using System.Text;
using RiverDeutsch.Networking.Dto;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace RiverDeutsch.Networking
{
    /// <summary>
    /// TEMPORARY test harness (plain IMGUI, no art) to manually exercise the
    /// host/client/server flow and the full ServerRpc surface before the real
    /// menu/table UI exists. Delete once GameView-equivalent screens land.
    /// </summary>
    public class DebugNetworkHud : MonoBehaviour
    {
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;
        [SerializeField] private string pseudo = "Player";

        private string lastAction = "(none)";
        private readonly StringBuilder log = new();
        private bool subscribedToSession;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += id => Log($"Connected as client {id}");
            }
        }

        private void Update()
        {
            if (NetworkGameSession.Instance != null && !subscribedToSession)
            {
                subscribedToSession = true;
                NetworkGameSession.Instance.OnGameStateReceived += HandleGameState;
                NetworkGameSession.Instance.OnLobbyStateReceived += count => Log($"Lobby: {count} player(s)");
                NetworkGameSession.Instance.OnDeutschCalled += name => Log($"{name} called SHUTDOWN!");
            }
        }

        private void HandleGameState(string action, GameStateDto dto)
        {
            lastAction = action;
            Log($"State update: {action} (turn: {dto.CurrentPlayerName}, round: {dto.RoundCount})");
        }

        private void Log(string message)
        {
            log.Insert(0, message + "\n");
            Debug.Log("[NetHud] " + message);
        }

        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return; // not initialized yet, or torn down (e.g. exiting Play Mode)

            GUILayout.BeginArea(new Rect(10, 10, 420, Screen.height - 20), GUI.skin.box);

            if (!nm.IsClient && !nm.IsServer)
            {
                GUILayout.Label("RiverDeutsch — Debug Network HUD");
                address = GUILayout.TextField(address);
                port = (ushort)EditorIntField("Port", port);

                if (GUILayout.Button("Start Host")) StartWithTransport(() => nm.StartHost());
                if (GUILayout.Button("Start Server")) StartWithTransport(() => nm.StartServer());
                if (GUILayout.Button("Start Client")) StartWithTransport(() => nm.StartClient());
            }
            else
            {
                GUILayout.Label(nm.IsHost ? "HOST" : nm.IsServer ? "SERVER" : "CLIENT");

                if (nm.IsClient)
                {
                    pseudo = GUILayout.TextField(pseudo);
                    if (GUILayout.Button("Connect (send pseudo)") && NetworkGameSession.Instance != null)
                    {
                        NetworkGameSession.Instance.ConnectServerRpc(pseudo);
                    }

                    var s = NetworkGameSession.Instance;
                    if (s != null)
                    {
                        if (GUILayout.Button("Start Match")) s.StartMatchServerRpc();
                        if (GUILayout.Button("Draw")) s.PlayerDrawServerRpc();
                        if (GUILayout.Button("Discard Pending")) s.DiscardPendingServerRpc();
                        if (GUILayout.Button("Call SHUTDOWN")) s.CallDeutschServerRpc();
                        if (GUILayout.Button("Start New Round")) s.StartNewRoundServerRpc();
                        if (GUILayout.Button("Clear Visible Cards")) s.ClearVisibleCardsServerRpc();
                    }
                }

                if (GUILayout.Button("Shutdown")) nm.Shutdown();
            }

            GUILayout.Space(10);
            GUILayout.Label($"Last action: {lastAction}");
            GUILayout.Label("Log:");
            GUILayout.Label(log.ToString());

            GUILayout.EndArea();
        }

        private static int EditorIntField(string label, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(40));
            string text = GUILayout.TextField(value.ToString());
            GUILayout.EndHorizontal();
            return int.TryParse(text, out int parsed) ? parsed : value;
        }

        private void StartWithTransport(System.Action start)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport?.SetConnectionData(address, port);
            start();
        }
    }
}
