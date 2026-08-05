using System.Collections;
using System.Collections.Generic;
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
    /// to matter perf-wise. The one exception is card reveals: those get compared
    /// against the previous state (by hand/river position) so a card flipping from
    /// hidden to known can play a flip animation instead of instantly swapping.
    ///
    /// Deferred for a follow-up pass: the round-end score screen and the game-end
    /// victory screen. For now the board just keeps rendering through those
    /// transitions.
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
        private VisualElement powerPopupOverlay;
        private VisualElement powerPopupIcon;
        private Label powerPopupNameLabel;
        private Label powerPopupDescriptionLabel;
        private VisualElement countdownOverlay;
        private VisualElement countdownPlayers;
        private Label countdownNumberLabel;
        private VisualElement powerBadge;
        private VisualElement powerBadgeIcon;
        private Label powerBadgeNameLabel;

        private GameStateDto latestState;
        private string lastToastKey;
        private bool subscribedToSession;
        private bool uiReady;
        private string pendingAction;
        private GameStateDto pendingState;
        private bool pendingCardOverlayWasShown;
        private int powerPopupToken;

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
            powerPopupOverlay = root.Q<VisualElement>("power-popup-overlay");
            powerPopupIcon = root.Q<VisualElement>("power-popup-icon");
            powerPopupNameLabel = root.Q<Label>("power-popup-name");
            powerPopupDescriptionLabel = root.Q<Label>("power-popup-description");
            countdownOverlay = root.Q<VisualElement>("countdown-overlay");
            countdownPlayers = root.Q<VisualElement>("countdown-players");
            countdownNumberLabel = root.Q<Label>("countdown-number");
            powerBadge = root.Q<VisualElement>("power-badge");
            powerBadgeIcon = root.Q<VisualElement>("power-badge-icon");
            powerBadgeNameLabel = root.Q<Label>("power-badge-name");

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

            GameStateDto previous = latestState;
            latestState = dto;

            if (dto.CurrentPlayerName != null && dto.CurrentPlayerName != previous?.CurrentPlayerName)
            {
                ShowToast(dto.CurrentPlayerName == LocalPlayerName ? "A VOUS DE JOUER" : $"TOUR DE {dto.CurrentPlayerName.ToUpper()}");
            }

            RenderBoard(dto, previous);
            MaybeShowPowerPopup(dto, previous);

            if (action == "GAME_STARTED")
            {
                StartCoroutine(PlayStartCountdown(dto));
            }
        }

        private void HandleDeutschCalled(string callerName)
        {
            ShowToast($"{callerName.ToUpper()} A LANCE SHUTDOWN ! DERNIER TOUR !");
        }

        // ── RENDERING ────────────────────────────────────────────────────────

        private void RenderBoard(GameStateDto dto, GameStateDto previous)
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
            RenderLocalHand(dto, previous, localPlayer, localName, localIndex, isMyTurn, powerWaiting, activePower, hasPending);
            RenderOpponents(dto, previous, localName, isMyTurn, powerWaiting, activePower);
            RenderRiver(dto, previous, isMyTurn, powerWaiting, activePower);
            RenderPiles(dto);
            RenderPendingCardOverlay(dto, isMyTurn);
            RenderShutdownButton(isMyTurn, hasPending, powerWaiting, dto);
            RenderPowerBadge(dto, powerWaiting);
        }

        private void RenderTurnIndicator(GameStateDto dto, bool isMyTurn)
        {
            turnPlayerLabel.text = dto.CurrentPlayerName != null ? dto.CurrentPlayerName.ToUpper() : "";
            turnPlayerLabel.style.color = isMyTurn ? new Color(0.91f, 0.64f, 0.24f) : new Color(0.96f, 0.92f, 0.85f);
        }

        private void RenderLocalHand(GameStateDto dto, GameStateDto previous, PlayerDto localPlayer, string localName, int localIndex, bool isMyTurn, bool powerWaiting, string activePower, bool hasPending)
        {
            localNameLabel.text = localPlayer.Name.ToUpper() + " (VOUS)";
            localHandRow.Clear();

            List<CardDto> previousHand = FindHand(previous, localName);

            // Balatro-style fan: cards toward the edges of the hand rest at a small
            // outward rotation and dip slightly, instead of all sitting dead flat.
            float fanCenter = (localPlayer.Hand.Count - 1) / 2f;

            for (int i = 0; i < localPlayer.Hand.Count; i++)
            {
                int cardIndex = i;
                CardDto card = localPlayer.Hand[i];
                CardDto previousCard = SameLength(previousHand, localPlayer.Hand) ? previousHand[i] : null;
                float fanOffset = i - fanCenter;
                float baseRotation = fanOffset * 6f;
                float baseOffsetY = Mathf.Abs(fanOffset) * 5f;
                VisualElement slot = CreateCardSlot(card, previousCard, large: false, sway: true, swayPhase: i * 0.9f, baseRotation: baseRotation, baseOffsetY: baseOffsetY);

                if (powerWaiting && isMyTurn && IsHandTargetable(activePower, isOpponent: false))
                {
                    MarkTargetable(slot);
                    slot.RegisterCallback<ClickEvent>(_ => OnCardClicked(slot, () => NetworkGameSession.Instance.PowerTargetPlayerServerRpc(localIndex, cardIndex)));
                }
                else if (isMyTurn && hasPending && !powerWaiting)
                {
                    MarkSelectable(slot);
                    slot.RegisterCallback<ClickEvent>(_ => OnCardClicked(slot, () => NetworkGameSession.Instance.SwapPendingCardServerRpc(cardIndex)));
                }
                else if (dto.CurrentState == "Normal" && !hasPending)
                {
                    slot.AddToClassList("card-slot--clickable");
                    slot.RegisterCallback<ClickEvent>(_ => OnCardClicked(slot, () => NetworkGameSession.Instance.AttemptSnapServerRpc(cardIndex)));
                }

                localHandRow.Add(slot);
            }
        }

        private void RenderOpponents(GameStateDto dto, GameStateDto previous, string localName, bool isMyTurn, bool powerWaiting, string activePower)
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
                column.AddToClassList("opponent-zone");

                var nameLabel = new Label(player.Name.ToUpper());
                nameLabel.AddToClassList("player-label");
                column.Add(nameLabel);

                var row = new VisualElement();
                row.AddToClassList("card-row");

                List<CardDto> previousHand = FindHand(previous, player.Name);

                // Attack doesn't target a specific card — it targets the player — so the
                // whole zone becomes the click target instead of highlighting every card,
                // which used to read as "pick one of these" when it wasn't the case.
                bool wholeZoneTargetable = powerWaiting && isMyTurn && activePower == "Attack";
                bool perCardTargetable = powerWaiting && isMyTurn && IsHandTargetable(activePower, isOpponent: true) && activePower != "Attack";

                int capturedPlayerIndex = playerIndex;
                for (int i = 0; i < player.Hand.Count; i++)
                {
                    int cardIndex = i;
                    CardDto card = player.Hand[i];
                    CardDto previousCard = SameLength(previousHand, player.Hand) ? previousHand[i] : null;
                    VisualElement slot = CreateCardSlot(card, previousCard, large: false, smaller: true);

                    if (perCardTargetable)
                    {
                        MarkTargetable(slot);
                        slot.RegisterCallback<ClickEvent>(_ => OnCardClicked(slot, () => NetworkGameSession.Instance.PowerTargetPlayerServerRpc(capturedPlayerIndex, cardIndex)));
                    }

                    row.Add(slot);
                }

                if (wholeZoneTargetable)
                {
                    MarkZoneTargetable(column);
                    column.RegisterCallback<ClickEvent>(_ => OnCardClicked(column, () => NetworkGameSession.Instance.PowerTargetPlayerServerRpc(capturedPlayerIndex, 0)));
                }

                column.Add(row);
                container.Add(column);
            }
        }

        private void RenderRiver(GameStateDto dto, GameStateDto previous, bool isMyTurn, bool powerWaiting, string activePower)
        {
            riverRow.Clear();
            bool riverTargetable = powerWaiting && isMyTurn && (activePower == "PeekRiver" || activePower == "Peek");
            List<CardDto> previousRiver = previous?.Board?.River;

            for (int i = 0; i < dto.Board.River.Count; i++)
            {
                int riverIndex = i;
                CardDto previousCard = SameLength(previousRiver, dto.Board.River) ? previousRiver[i] : null;
                VisualElement slot = CreateCardSlot(dto.Board.River[i], previousCard, large: false);

                if (riverTargetable)
                {
                    MarkTargetable(slot);
                    slot.RegisterCallback<ClickEvent>(_ => OnCardClicked(slot, () => NetworkGameSession.Instance.PowerTargetRiverServerRpc(riverIndex)));
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

            if (show)
            {
                ApplyCardVisual(pendingCardSlot, dto.PendingCard);
                if (!pendingCardOverlayWasShown) StartCoroutine(DealInRoutine(pendingCardSlot));
            }
            pendingCardOverlayWasShown = show;
        }

        private void RenderShutdownButton(bool isMyTurn, bool hasPending, bool powerWaiting, GameStateDto dto)
        {
            bool visible = isMyTurn && !hasPending && !powerWaiting && dto.CurrentState == "Normal" && dto.DeutschCallerName == null;
            shutdownButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Small persistent reminder (icon + name) docked next to the turn
        /// indicator for as long as a power is actually active — unlike the intro
        /// popup below, this is purely state-driven so it can never linger or vanish
        /// out of sync with the real WaitingForPowerTarget state.</summary>
        private void RenderPowerBadge(GameStateDto dto, bool powerWaiting)
        {
            bool show = powerWaiting && dto.ActivePowerCard != null;
            powerBadge.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            string power = dto.ActivePowerCard.Power;
            Texture2D icon = CardSpriteLoader.GetPowerIcon(power);
            if (icon != null) powerBadgeIcon.style.backgroundImage = new StyleBackground(icon);
            powerBadgeNameLabel.text = GetPowerName(power);
        }

        // ── POWER POPUP ──────────────────────────────────────────────────────

        private void MaybeShowPowerPopup(GameStateDto dto, GameStateDto previous)
        {
            bool wasWaiting = previous != null && previous.CurrentState == "WaitingForPowerTarget";
            bool isWaiting = dto.CurrentState == "WaitingForPowerTarget";
            if (isWaiting && !wasWaiting && dto.ActivePowerCard != null)
            {
                ShowPowerPopup(dto.ActivePowerCard.Power);
            }
        }

        private void ShowPowerPopup(string power)
        {
            Texture2D icon = CardSpriteLoader.GetPowerIcon(power);
            if (icon != null) powerPopupIcon.style.backgroundImage = new StyleBackground(icon);
            powerPopupNameLabel.text = GetPowerName(power);
            powerPopupDescriptionLabel.text = GetPowerDescription(power);

            powerPopupOverlay.style.display = DisplayStyle.Flex;
            powerPopupOverlay.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f));
            StartCoroutine(ScaleRoutine(powerPopupOverlay, 0.9f, 1f, 0.15f, clearAtEnd: true));

            // Brief intro flash only — the docked power-badge (state-driven, see
            // RenderPowerBadge) is what stays on screen for the rest of the targeting.
            int token = ++powerPopupToken;
            StartCoroutine(HidePowerPopupAfterDelay(token));
        }

        private IEnumerator HidePowerPopupAfterDelay(int token)
        {
            yield return new WaitForSecondsRealtime(1.8f);
            if (token == powerPopupToken) powerPopupOverlay.style.display = DisplayStyle.None;
        }

        private static string GetPowerName(string power) => power switch
        {
            "PeekRiver" => "VISION RIVIERE",
            "PeekOpponent" => "VISION ADVERSAIRE",
            "Peek" => "VISION LIBRE",
            "Swap" => "ECHANGE",
            "Attack" => "ATTAQUE",
            _ => "POUVOIR",
        };

        private static string GetPowerDescription(string power) => power switch
        {
            "PeekRiver" => "Regardez une carte face cachee de la Riviere.",
            "PeekOpponent" => "Regardez une carte face cachee d'un adversaire.",
            "Peek" => "Regardez n'importe quelle carte : votre jeu, un adversaire ou la Riviere.",
            "Swap" => "Echangez une de vos cartes avec celle d'un adversaire.\nCliquez sur une de vos cartes, puis sur celle d'un adversaire.",
            "Attack" => "Un adversaire de votre choix pioche une carte supplementaire.",
            _ => "",
        };

        // ── START COUNTDOWN ──────────────────────────────────────────────────

        private IEnumerator PlayStartCountdown(GameStateDto dto)
        {
            countdownPlayers.Clear();
            foreach (PlayerDto p in dto.Players)
            {
                bool isMe = p.Name == LocalPlayerName;
                var label = new Label(p.Name.ToUpper() + (isMe ? " (VOUS)" : ""));
                label.AddToClassList("countdown-player-label");
                if (isMe) label.style.color = new Color(0.91f, 0.64f, 0.24f);
                countdownPlayers.Add(label);
            }

            countdownOverlay.style.display = DisplayStyle.Flex;

            for (int i = 5; i >= 1; i--)
            {
                countdownNumberLabel.text = i.ToString();
                yield return WaitRealtime(1f);
            }

            countdownOverlay.style.display = DisplayStyle.None;
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, StepSeconds);
                yield return null;
            }
        }

        // ── HELPERS ──────────────────────────────────────────────────────────

        private static List<CardDto> FindHand(GameStateDto state, string playerName)
        {
            if (state == null || playerName == null) return null;
            foreach (PlayerDto p in state.Players)
            {
                if (p.Name == playerName) return p.Hand;
            }
            return null;
        }

        private static bool SameLength(List<CardDto> a, List<CardDto> b) => a != null && b != null && a.Count == b.Count;

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

        private VisualElement CreateCardSlot(CardDto card, CardDto previousCard, bool large, bool smaller = false, bool sway = false, float swayPhase = 0f, float baseRotation = 0f, float baseOffsetY = 0f)
        {
            var slot = new VisualElement();
            slot.AddToClassList("card-slot");
            if (large) slot.AddToClassList("card-slot--large");
            if (smaller)
            {
                slot.style.width = 64;
                slot.style.height = 92;
            }

            bool justRevealed = previousCard != null && previousCard.Known != card.Known;
            ApplyCardVisual(slot, justRevealed ? previousCard : card);
            if (justRevealed) StartCoroutine(FlipRoutine(slot, card));
            if (sway) StartCoroutine(CardMotionRoutine(slot, swayPhase, baseRotation, baseOffsetY));

            return slot;
        }

        /// <summary>Idle rotate/bob loop around the card's fan resting pose when nobody's
        /// interacting with it; when the pointer is over the card this instead drives a
        /// tilt/lift toward the cursor (a 2D stand-in for the video's 3D hover rotation).
        /// The two never touch style.rotate/translate on the same frame, so they can't
        /// fight each other for control of those properties.</summary>
        private static IEnumerator CardMotionRoutine(VisualElement slot, float phase, float baseRotation, float baseOffsetY)
        {
            bool hovering = false;
            float pointerX = 0f;
            float pointerY = 0f;

            slot.RegisterCallback<PointerEnterEvent>(_ => hovering = true);
            slot.RegisterCallback<PointerLeaveEvent>(_ => hovering = false);
            slot.RegisterCallback<PointerMoveEvent>(evt =>
            {
                Rect layout = slot.layout;
                if (layout.width <= 0f || layout.height <= 0f) return;
                pointerX = Mathf.Clamp((evt.localPosition.x / layout.width) * 2f - 1f, -1f, 1f);
                pointerY = Mathf.Clamp((evt.localPosition.y / layout.height) * 2f - 1f, -1f, 1f);
            });

            float t = phase;
            while (slot.panel != null)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, StepSeconds);

                if (hovering)
                {
                    float tilt = baseRotation - pointerX * 6f;
                    float lift = baseOffsetY - 10f - pointerY * 4f;
                    slot.style.rotate = new Rotate(tilt);
                    slot.style.translate = new Translate(pointerX * 3f, lift);
                }
                else
                {
                    float angle = baseRotation + Mathf.Sin(t * 1.1f) * 1.2f;
                    float bob = baseOffsetY + Mathf.Sin(t * 1.4f + 1f) * 2f;
                    slot.style.rotate = new Rotate(angle);
                    slot.style.translate = new Translate(0, bob);
                }

                yield return null;
            }
        }

        private static void ApplyCardVisual(VisualElement slot, CardDto card)
        {
            Texture2D tex = card is { Known: true }
                ? CardSpriteLoader.GetCardFace(card.Rank, card.Suit)
                : CardSpriteLoader.GetCardBack();

            if (tex != null) slot.style.backgroundImage = new StyleBackground(tex);
        }

        private void MarkTargetable(VisualElement slot)
        {
            slot.AddToClassList("card-slot--targetable");
            slot.AddToClassList("card-slot--clickable");
            StartCoroutine(PulseRoutine(slot, "card-slot--targetable", 1.07f));
        }

        private void MarkSelectable(VisualElement slot)
        {
            slot.AddToClassList("card-slot--selectable");
            slot.AddToClassList("card-slot--clickable");
            StartCoroutine(PulseRoutine(slot, "card-slot--selectable", 1.07f));
        }

        private void MarkZoneTargetable(VisualElement zone)
        {
            zone.AddToClassList("opponent-zone--targetable");
            StartCoroutine(PulseRoutine(zone, "opponent-zone--targetable", 1.03f));
        }

        /// <summary>Breathing scale loop while an element carries requiredClass (a valid
        /// power target, a selectable swap card, ...); stops on its own once the element
        /// leaves the panel (next render's Clear()) or loses that class.</summary>
        private static IEnumerator PulseRoutine(VisualElement element, string requiredClass, float peakScale)
        {
            while (element.panel != null && element.ClassListContains(requiredClass))
            {
                yield return ScaleRoutine(element, 1f, peakScale, 0.5f);
                if (element.panel == null) yield break;
                yield return ScaleRoutine(element, peakScale, 1f, 0.5f);
            }
        }

        // ── ACTIONS ──────────────────────────────────────────────────────────

        private void OnCardClicked(VisualElement slot, System.Action sendRpc)
        {
            StartCoroutine(PunchRoutine(slot));
            sendRpc();
        }

        private void OnDeckClicked(ClickEvent evt)
        {
            if (latestState == null || NetworkGameSession.Instance == null) return;
            bool isMyTurn = latestState.CurrentPlayerName == LocalPlayerName;
            bool drawAuthorized = isMyTurn && latestState.PendingCard == null && latestState.CurrentState == "Normal";
            if (!drawAuthorized) return;

            StartCoroutine(PunchRoutine(deckCardSlot));
            NetworkGameSession.Instance.PlayerDrawServerRpc();
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
            toast.style.scale = new Scale(new Vector3(0f, 0f, 1f));
            toastLayer.Add(toast);

            StartCoroutine(ToastRoutine(toast));
        }

        private static IEnumerator ToastRoutine(VisualElement toast)
        {
            yield return ScaleRoutine(toast, 0f, 1.15f, 0.16f);
            yield return ScaleRoutine(toast, 1.15f, 1f, 0.1f);
            yield return new WaitForSecondsRealtime(2.0f);
            yield return ScaleRoutine(toast, 1f, 0f, 0.15f);
            toast.RemoveFromHierarchy();
        }

        // ── JUICE ────────────────────────────────────────────────────────────

        private const float StepSeconds = 0.05f;

        /// <summary>Quick scale bump for click feedback, ahead of the server round-trip.</summary>
        private static IEnumerator PunchRoutine(VisualElement element)
        {
            yield return ScaleRoutine(element, 1f, 1.18f, 0.08f);
            yield return ScaleRoutine(element, 1.18f, 1f, 0.1f, clearAtEnd: true);
        }

        /// <summary>Squashes a card to a sliver, swaps its face at the midpoint, then
        /// expands back out — a 2D stand-in for a 3D flip.</summary>
        private static IEnumerator FlipRoutine(VisualElement slot, CardDto revealedCard)
        {
            yield return ScaleXRoutine(slot, 1f, 0.04f, 0.09f);
            ApplyCardVisual(slot, revealedCard);
            yield return ScaleXRoutine(slot, 0.04f, 1f, 0.12f, clearAtEnd: true);
        }

        /// <summary>Card sliding/flipping in from the deck when drawn.</summary>
        private static IEnumerator DealInRoutine(VisualElement slot)
        {
            slot.style.scale = new Scale(new Vector3(0.05f, 0.4f, 1f));
            slot.style.translate = new Translate(0, -30);
            yield return ScaleRoutine(slot, 0.05f, 1f, 0.22f, clearAtEnd: true);
            slot.style.translate = StyleKeyword.Null;
        }

        /// <summary>Animates style.scale from "from" to "to". When clearAtEnd is true the
        /// inline override is cleared instead of pinned at "to", so USS rules (like a
        /// :hover scale) can take back over — only safe when "to" is the CSS's own
        /// resting value (1), so only pass it on the last step of a sequence.</summary>
        private static IEnumerator ScaleRoutine(VisualElement element, float from, float to, float duration, bool clearAtEnd = false)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, StepSeconds);
                float t = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Lerp(from, to, t);
                element.style.scale = new Scale(new Vector3(s, s, 1f));
                yield return null;
            }
            element.style.scale = clearAtEnd ? StyleKeyword.Null : new Scale(new Vector3(to, to, 1f));
        }

        private static IEnumerator ScaleXRoutine(VisualElement element, float from, float to, float duration, bool clearAtEnd = false)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, StepSeconds);
                float t = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Lerp(from, to, t);
                element.style.scale = new Scale(new Vector3(s, 1f, 1f));
                yield return null;
            }
            element.style.scale = clearAtEnd ? StyleKeyword.Null : new Scale(new Vector3(to, 1f, 1f));
        }
    }
}
