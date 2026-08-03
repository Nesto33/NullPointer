using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RiverDeutsch.Core.Logic;
using RiverDeutsch.Core.Model;
using RiverDeutsch.Networking.Dto;
using Unity.Netcode;
using UnityEngine;

namespace RiverDeutsch.Networking
{
    /// <summary>
    /// Server-authoritative session controller. A single instance is spawned by the host/server
    /// and owns the one true <see cref="GameManager"/>. Clients never run game rules locally —
    /// they send intents via ServerRpc and receive a personalized state snapshot via ClientRpc.
    ///
    /// This mirrors the original GameServer/ClientHandler protocol (action + payload broadcast
    /// after every message) but replaces "broadcast the whole mutable object" with a filtered,
    /// per-client DTO snapshot built by <see cref="GameStateMapper"/>.
    /// </summary>
    public class NetworkGameSession : NetworkBehaviour
    {
        public static NetworkGameSession Instance { get; private set; }

        /// <summary>The pseudo this client connected with — set client-side right before
        /// ConnectServerRpc, so UI layers can tell which player/hand in a GameStateDto is "us".</summary>
        public string LocalPlayerName { get; set; }

        public event Action<string, GameStateDto> OnGameStateReceived;
        public event Action<int> OnLobbyStateReceived;
        public event Action<string> OnDeutschCalled;
        public event Action<long> OnPong;

        private GameManager gameManager;
        private readonly Dictionary<ulong, string> clientPlayerNames = new();

        // Mirrors GameServer's small state machine around round transitions.
        private bool roundFinished;
        private bool awaitingNewRound;
        private bool gameStarted;
        private bool gameEnded;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            if (IsServer)
            {
                gameManager = new GameManager();
                gameManager.SetGameEventListener(new ServerEventRelay(this));
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!clientPlayerNames.TryGetValue(clientId, out string name)) return;
            clientPlayerNames.Remove(clientId);

            if (!gameStarted)
            {
                gameManager.RemovePlayer(name);
                LobbyStateClientRpc(gameManager.Players.Count);
            }
        }

        private Player GetSenderPlayer(ServerRpcParams rpcParams)
        {
            if (!clientPlayerNames.TryGetValue(rpcParams.Receive.SenderClientId, out string name)) return null;
            return gameManager.Players.FirstOrDefault(p => p.Name == name);
        }

        // ── CLIENT -> SERVER ────────────────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        public void ConnectServerRpc(string pseudo, ServerRpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            if (clientPlayerNames.ContainsKey(senderId)) return;

            if (string.IsNullOrWhiteSpace(pseudo)) pseudo = $"Player{senderId}";
            clientPlayerNames[senderId] = pseudo;
            gameManager.AddPlayer(new Player(pseudo));

            LobbyStateClientRpc(gameManager.Players.Count);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PingServerRpc(long clientTimestamp, ServerRpcParams rpcParams = default)
        {
            PongClientRpc(clientTimestamp, ToSingleClient(rpcParams.Receive.SenderClientId));
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartMatchServerRpc(ServerRpcParams rpcParams = default)
        {
            if (gameManager.Players.Count < 2) return;

            gameStarted = true;
            gameManager.StartNewGame();
            BroadcastStateToAll("GAME_STARTED");
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartNewRoundServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!awaitingNewRound) return;

            awaitingNewRound = false;
            gameManager.StartNewRound();
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerDrawServerRpc(ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender != null && ReferenceEquals(sender, gameManager.CurrentPlayer))
            {
                gameManager.PlayerDrawsFromDeck(sender);
            }
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SwapPendingCardServerRpc(int cardIndex, ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender == null || !ReferenceEquals(sender, gameManager.CurrentPlayer)) return;

            gameManager.ReplaceCardWithPending(cardIndex);
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DiscardPendingServerRpc(ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender == null || !ReferenceEquals(sender, gameManager.CurrentPlayer)) return;

            gameManager.DiscardPendingCard();
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CallDeutschServerRpc(ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender == null || !ReferenceEquals(sender, gameManager.CurrentPlayer) || gameManager.DeutschCaller != null)
            {
                return;
            }

            DeutschCalledClientRpc(sender.Name);
            gameManager.DeclareDeutsch(sender);
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PowerTargetPlayerServerRpc(int targetPlayerIndex, int targetCardIndex, ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender == null || !ReferenceEquals(sender, gameManager.CurrentPlayer)) return;

            gameManager.HandleTargetSelected(targetPlayerIndex, targetCardIndex, null);
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PowerTargetRiverServerRpc(int riverIndex, ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender == null || !ReferenceEquals(sender, gameManager.CurrentPlayer)) return;

            gameManager.HandleTargetSelected(null, null, riverIndex);
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AttemptSnapServerRpc(int cardIndex, ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender != null) gameManager.CheckSnap(sender, cardIndex);
            FinalizeAction(shouldBroadcast: true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearVisibleCardsServerRpc(ServerRpcParams rpcParams = default)
        {
            Player sender = GetSenderPlayer(rpcParams);
            if (sender != null) gameManager.VisibleCardsMap.Remove(sender.Name);
            FinalizeAction(shouldBroadcast: !roundFinished);
        }

        // ── SERVER -> CLIENTS ───────────────────────────────────────────────

        private void FinalizeAction(bool shouldBroadcast)
        {
            if (gameEnded || !shouldBroadcast) return;

            bool powerPending = gameManager.CurrentState == GameManager.GameState.WaitingForPowerTarget;
            if (roundFinished && !powerPending)
            {
                BroadcastStateToAll("ROUND_ENDED");
                roundFinished = false;
                awaitingNewRound = true;
            }
            else
            {
                BroadcastStateToAll("UPDATE_BOARD");
            }
        }

        private void BroadcastStateToAll(string action)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SendStateToClient(clientId, action);
            }
        }

        private void SendStateToClient(ulong clientId, string action)
        {
            string recipientName = clientPlayerNames.TryGetValue(clientId, out string name) ? name : null;
            GameStateDto dto = GameStateMapper.ToDto(gameManager, recipientName);
            string json = JsonConvert.SerializeObject(dto);

            GameStateUpdateClientRpc(action, json, ToSingleClient(clientId));
        }

        private static ClientRpcParams ToSingleClient(ulong clientId) => new()
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        [ClientRpc]
        private void GameStateUpdateClientRpc(string action, string json, ClientRpcParams rpcParams = default)
        {
            GameStateDto dto = JsonConvert.DeserializeObject<GameStateDto>(json);
            OnGameStateReceived?.Invoke(action, dto);
        }

        [ClientRpc]
        private void LobbyStateClientRpc(int playerCount)
        {
            OnLobbyStateReceived?.Invoke(playerCount);
        }

        [ClientRpc]
        private void DeutschCalledClientRpc(string callerName)
        {
            OnDeutschCalled?.Invoke(callerName);
        }

        [ClientRpc]
        private void PongClientRpc(long clientTimestamp, ClientRpcParams rpcParams = default)
        {
            OnPong?.Invoke(clientTimestamp);
        }

        /// <summary>Server-side relay from GameManager's rule-engine callbacks to the network layer.</summary>
        private class ServerEventRelay : IGameEventListener
        {
            private readonly NetworkGameSession session;

            public ServerEventRelay(NetworkGameSession session) => this.session = session;

            public void OnTurnChanged(Player newPlayer) { }
            public void OnRoundStarted(int roundNumber, Dictionary<Player, List<Card>> cardsToReveal) { }
            public void OnGameMessage(string message, string type) { }
            public void OnPowerExecuted(Card card, Player source, Player target) { }
            public void OnCardsRevealed(List<Card> cards) { }
            public void OnBackToMenu() { }

            public void OnRoundEnded(List<Player> rankedPlayers) => session.roundFinished = true;

            public void OnGameEnded(Player winner)
            {
                session.gameEnded = true;
                session.BroadcastStateToAll("GAME_ENDED");
            }
        }
    }
}
