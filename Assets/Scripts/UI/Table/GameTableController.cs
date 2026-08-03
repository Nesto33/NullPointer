using System.Collections;
using RiverDeutsch.Networking;
using RiverDeutsch.Networking.Dto;
using RiverDeutsch.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Table
{
    /// <summary>
    /// Renders the authoritative GameStateDto onto the table and turns clicks into
    /// ServerRpc calls. Like the original GameView.renderBoard(), this rebuilds the
    /// dynamic rows (hands, river) from scratch on every state update rather than
    /// diffing — simpler to keep correct, and the table isn't big enough for that
    /// to matter perf-wise.
    ///
    /// Deferred for a follow-up pass: round-end score screen, game-end victory
    /// screen, the power-activation popup, and the pre-round countdown. For now the
    /// board just keeps rendering through those transitions.
    /// </summary>
    public class GameTableController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private VisualElement tableRoot;
        private VisualElement opponentTop;
        private VisualElement opponentLeft;
        private VisualElement opponentRight;
        private VisualElement riverRow;
        private Label deckLabel;
        private VisualElement deckCardSlot;
        private VisualElement discardCardSlot;
        private VisualElement localHandRow;
        private Label localNameLabel;
        private Label turnPlayerLabel;
        private Button shutdownButton;
        private VisualElement toastLayer;
        private VisualElement pendingCardOverlay;
        private VisualElement pendingCardSlot;
        private Button discardPendingButton;

        private GameStateDto latestState;
        private string lastToastKey;
        private bool subscribedToSession;
        private bool uiReady;
        private string pendingAction;
        private GameStateDto pendingState;

        private string LocalPlayerName => NetworkGameSession.Instance != null ? NetworkGameSession.Instance.LocalPlayerName : null;

        private void OnEnable()
        {
            if (document == null)
            {
                Debug.LogError("GameTableController: 'Document' is not assigned in the inspector.");
                return;
            }

            VisualElement root = document.rootVisualElement;

            tableRoot = root.Q<VisualElement>("table-root");
            opponentTop = root.Q<VisualElement>("opponent-top");
            opponentLeft = root.Q<VisualElement>("opponent-left");
            opponentRight = root.Q<VisualElement>("opponent-right");
            riverRow = root.Q<VisualElement>("river-row");
            deckLabel = root.Q<Label>("deck-label");
            deckCardSlot = root.Q<VisualElement>("deck-card-slot");
            discardCardSlot = root.Q<VisualElement>("discard-card-slot");
            localHandRow = root.Q<VisualElement>("local-hand-row");
            localNameLabel = root.Q<Label>("local-name-label");
            turnPlayerLabel = root.Q<Label>("turn-player-label");
            shutdownButton = root.Q<Button>("shutdown-button");
            toastLayer = root.Q<VisualElement>("toast-layer");
            pendingCardOverlay = root.Q<VisualElement>("pending-card-overlay");
            pendingCardSlot = root.Q<VisualElement>("pending-card-slot");
            discardPendingButton = root.Q<Button>("discard-pending-button");

            Texture2D tableBg = CardSpriteLoader.GetTableBackground();
            if (tableBg != null) tableRoot.style.backgroundImage = new StyleBackground(tableBg);

            Texture2D back = CardSpriteLoader.GetCardBack();
            if (back != null) deckCardSlot.style.backgroundImage = new StyleBackground(back);

            deckCardSlot.RegisterCallback<ClickEvent>(OnDeckClicked);
            shutdownButton.clicked += OnShutdownClicked;
            discardPendingButton.clicked += OnDiscardPendingClicked;

            uiReady = true;

            // If HandleGameState was called (e.g. by the login->table handoff) before
            // this ran, apply it now instead of leaving the table blank.
            if (pendingState != null)
            {
                string action = pendingAction;
                GameStateDto dto = pendingState;
                pendingAction = null;
                pendingState = null;
                HandleGameState(action, dto);
            }
        }

        private void OnDisable()
        {
            uiReady = false;

            if (deckCardSlot != null) deckCardSlot.UnregisterCallback<ClickEvent>(OnDeckClicked);
            if (shutdownButton != null) shutdownButton.clicked -= OnShutdownClicked;
            if (discardPendingButton != null) discardPendingButton.clicked -= OnDiscardPendingClicked;

            if (NetworkGameSession.Instance != null)
            {
                NetworkGameSession.Instance.OnGameStateReceived -= HandleGameState;
                NetworkGameSession.Instance.OnDeutschCalled -= HandleDeutschCalled;
            }
            subscribedToSession = false;
        }

        private void Update()
        {
            if (!subscribedToSession && NetworkGameSession.Instance != null)
            {
                subscribedToSession = true;
                NetworkGameSession.Instance.OnGameStateReceived += HandleGameState;
                NetworkGameSession.Instance.OnDeutschCalled += HandleDeutschCalled;
            }
        }

        /// <summary>Public so the screen-transition orchestrator can feed the very first
        /// GAME_STARTED payload directly — this controller's own OnGameStateReceived
        /// subscription only starts once it's enabled, which would otherwise be one
        /// event too late and leave the table blank until the next update.</summary>
        public void HandleGameState(string action, GameStateDto dto)
        {
            if (!uiReady)
            {
                // OnEnable hasn't finished querying the UXML yet — remember this and
                // apply it as soon as it does, instead of hitting null UI references.
                pendingAction = action;
                pendingState = dto;
                return;
            }

            string previousCurrentPlayer = latestState?.CurrentPlayerName;
            latestState = dto;

            if (dto.CurrentPlayerName != null && dto.CurrentPlayerName != previousCurrentPlayer)
            {
                ShowToast(dto.CurrentPlayerName == LocalPlayerName ? "A VOUS DE JOUER" : $"TOUR DE {dto.CurrentPlayerName.ToUpper()}");
            }

            RenderBoard(dto);
        }

        private void HandleDeutschCalled(string callerName)
        {
            ShowToast($"{callerName.ToUpper()} A LANCE SHUTDOWN ! DERNIER TOUR !");
        }

        // ── RENDERING ────────────────────────────────────────────────────────

        private void RenderBoard(GameStateDto dto)
        {
            string localName = LocalPlayerName;
            PlayerDto localPlayer = null;
            int localIndex = -1;
            for (int i = 0; i < dto.Players.Count; i++)
            {
                if (dto.Players[i].Name == localName)
                {
                    localPlayer = dto.Players[i];
                    localIndex = i;
                    break;
                }
            }
            if (localPlayer == null) return; // not seated in this game (yet)

            bool isMyTurn = dto.CurrentPlayerName == localName;
            bool powerWaiting = dto.CurrentState == "WaitingForPowerTarget";
            string activePower = dto.ActivePowerCard != null ? dto.ActivePowerCard.Power : "None";
            bool hasPending = dto.PendingCard != null;

            RenderTurnIndicator(dto, isMyTurn);
            RenderLocalHand(dto, localPlayer, localIndex, isMyTurn, powerWaiting, activePower, hasPending);
            RenderOpponents(dto, localName, isMyTurn, powerWaiting, activePower);
            RenderRiver(dto, isMyTurn, powerWaiting, activePower);
            RenderPiles(dto);
            RenderPendingCardOverlay(dto, isMyTurn);
            RenderShutdownButton(isMyTurn, hasPending, powerWaiting, dto);
        }

        private void RenderTurnIndicator(GameStateDto dto, bool isMyTurn)
        {
            turnPlayerLabel.text = dto.CurrentPlayerName != null ? dto.CurrentPlayerName.ToUpper() : "";
            turnPlayerLabel.style.color = isMyTurn ? new Color(0.91f, 0.64f, 0.24f) : new Color(0.96f, 0.92f, 0.85f);
        }

        private void RenderLocalHand(GameStateDto dto, PlayerDto localPlayer, int localIndex, bool isMyTurn, bool powerWaiting, string activePower, bool hasPending)
        {
            localNameLabel.text = localPlayer.Name.ToUpper() + " (VOUS)";
            localHandRow.Clear();

            for (int i = 0; i < localPlayer.Hand.Count; i++)
            {
                int cardIndex = i;
                CardDto card = localPlayer.Hand[i];
                VisualElement slot = CreateCardSlot(card, large: false);

                if (powerWaiting && isMyTurn && IsHandTargetable(activePower, isOpponent: false))
                {
                    MarkTargetable(slot);
                    slot.RegisterCallback<ClickEvent>(_ => NetworkGameSession.Instance.PowerTargetPlayerServerRpc(localIndex, cardIndex));
                }
                else if (isMyTurn && hasPending && !powerWaiting)
                {
                    MarkSelectable(slot);
                    slot.RegisterCallback<ClickEvent>(_ => NetworkGameSession.Instance.SwapPendingCardServerRpc(cardIndex));
                }
                else if (dto.CurrentState == "Normal" && !hasPending)
                {
                    slot.AddToClassList("card-slot--clickable");
                    slot.RegisterCallback<ClickEvent>(_ => NetworkGameSession.Instance.AttemptSnapServerRpc(cardIndex));
                }

                localHandRow.Add(slot);
            }
        }

        private void RenderOpponents(GameStateDto dto, string localName, bool isMyTurn, bool powerWaiting, string activePower)
        {
            opponentTop.Clear();
            opponentLeft.Clear();
            opponentRight.Clear();

            VisualElement[] slots = { opponentTop, opponentLeft, opponentRight };
            int slotIndex = 0;

            for (int playerIndex = 0; playerIndex < dto.Players.Count; playerIndex++)
            {
                PlayerDto player = dto.Players[playerIndex];
                if (player.Name == localName) continue;
                if (slotIndex >= slots.Length) break;

                VisualElement container = slots[slotIndex];
                slotIndex++;

                var column = new VisualElement();
                column.style.alignItems = Align.Center;

                var nameLabel = new Label(player.Name.ToUpper());
                nameLabel.AddToClassList("player-label");
                column.Add(nameLabel);

                var row = new VisualElement();
                row.AddToClassList("card-row");

                int capturedPlayerIndex = playerIndex;
                for (int i = 0; i < player.Hand.Count; i++)
                {
                    int cardIndex = i;
                    CardDto card = player.Hand[i];
                    VisualElement slot = CreateCardSlot(card, large: false, smaller: true);

                    if (powerWaiting && isMyTurn && IsHandTargetable(activePower, isOpponent: true))
                    {
                        MarkTargetable(slot);
                        slot.RegisterCallback<ClickEvent>(_ => NetworkGameSession.Instance.PowerTargetPlayerServerRpc(capturedPlayerIndex, cardIndex));
                    }

                    row.Add(slot);
                }

                column.Add(row);
                container.Add(column);
            }
        }

        private void RenderRiver(GameStateDto dto, bool isMyTurn, bool powerWaiting, string activePower)
        {
            riverRow.Clear();
            bool riverTargetable = powerWaiting && isMyTurn && (activePower == "PeekRiver" || activePower == "Peek");

            for (int i = 0; i < dto.Board.River.Count; i++)
            {
                int riverIndex = i;
                VisualElement slot = CreateCardSlot(dto.Board.River[i], large: false);

                if (riverTargetable)
                {
                    MarkTargetable(slot);
                    slot.RegisterCallback<ClickEvent>(_ => NetworkGameSession.Instance.PowerTargetRiverServerRpc(riverIndex));
                }

                riverRow.Add(slot);
            }
        }

        private void RenderPiles(GameStateDto dto)
        {
            deckLabel.text = $"PIOCHE ({dto.Board.DeckSize})";
            ApplyCardVisual(discardCardSlot, dto.Board.DiscardTop);
        }

        private void RenderPendingCardOverlay(GameStateDto dto, bool isMyTurn)
        {
            bool show = dto.PendingCard != null && isMyTurn;
            pendingCardOverlay.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show) ApplyCardVisual(pendingCardSlot, dto.PendingCard);
        }

        private void RenderShutdownButton(bool isMyTurn, bool hasPending, bool powerWaiting, GameStateDto dto)
        {
            bool visible = isMyTurn && !hasPending && !powerWaiting && dto.CurrentState == "Normal" && dto.DeutschCallerName == null;
            shutdownButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── HELPERS ──────────────────────────────────────────────────────────

        private static bool IsHandTargetable(string activePower, bool isOpponent)
        {
            return activePower switch
            {
                "PeekOpponent" => isOpponent,
                "Attack" => isOpponent,
                "Swap" => true,
                "Peek" => true,
                _ => false,
            };
        }

        private static VisualElement CreateCardSlot(CardDto card, bool large, bool smaller = false)
        {
            var slot = new VisualElement();
            slot.AddToClassList("card-slot");
            if (large) slot.AddToClassList("card-slot--large");
            if (smaller)
            {
                slot.style.width = 50;
                slot.style.height = 72;
            }
            ApplyCardVisual(slot, card);
            return slot;
        }

        private static void ApplyCardVisual(VisualElement slot, CardDto card)
        {
            Texture2D tex = card is { Known: true }
                ? CardSpriteLoader.GetCardFace(card.Rank, card.Suit)
                : CardSpriteLoader.GetCardBack();

            if (tex != null) slot.style.backgroundImage = new StyleBackground(tex);
        }

        private static void MarkTargetable(VisualElement slot)
        {
            slot.AddToClassList("card-slot--targetable");
            slot.AddToClassList("card-slot--clickable");
        }

        private static void MarkSelectable(VisualElement slot)
        {
            slot.AddToClassList("card-slot--selectable");
            slot.AddToClassList("card-slot--clickable");
        }

        // ── ACTIONS ──────────────────────────────────────────────────────────

        private void OnDeckClicked(ClickEvent evt)
        {
            if (latestState == null || NetworkGameSession.Instance == null) return;
            bool isMyTurn = latestState.CurrentPlayerName == LocalPlayerName;
            bool drawAuthorized = isMyTurn && latestState.PendingCard == null && latestState.CurrentState == "Normal";
            if (drawAuthorized) NetworkGameSession.Instance.PlayerDrawServerRpc();
        }

        private void OnShutdownClicked()
        {
            NetworkGameSession.Instance?.CallDeutschServerRpc();
        }

        private void OnDiscardPendingClicked()
        {
            NetworkGameSession.Instance?.DiscardPendingServerRpc();
        }

        // ── TOASTS ───────────────────────────────────────────────────────────

        private void ShowToast(string message)
        {
            if (message == lastToastKey) return;
            lastToastKey = message;

            var toast = new Label(message);
            toast.AddToClassList("toast");
            toastLayer.Add(toast);

            StartCoroutine(RemoveToastAfterDelay(toast));
        }

        private static IEnumerator RemoveToastAfterDelay(VisualElement toast)
        {
            yield return new WaitForSecondsRealtime(2.2f);
            toast.RemoveFromHierarchy();
        }
    }
}
