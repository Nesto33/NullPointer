using System;
using System.Collections.Generic;
using System.Linq;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Logic
{
    [Serializable]
    public class GameManager
    {
        public enum GameState
        {
            Normal,
            WaitingForPowerTarget
        }

        private readonly Board board;
        private readonly List<Player> players;
        private readonly PowerEngine powerEngine;
        private readonly RoundManager roundManager;

        [NonSerialized] private IGameEventListener gameEventListener;

        private int currentPlayerIndex;
        private bool snapEnabled;
        private Card pendingCard;
        private Player deutschCaller;
        private bool isFinalTurn;
        private int roundCount;
        private int? firstSelectedCardIndex;

        private readonly Dictionary<string, List<Card>> visibleCardsMap = new();

        public GameManager()
        {
            board = new Board();
            players = new List<Player>();
            powerEngine = new PowerEngine();
            roundManager = new RoundManager();
            currentPlayerIndex = 0;
        }

        public GameState CurrentState { get; private set; } = GameState.Normal;
        public Card ActivePowerCard { get; private set; }

        // GETTERS
        public Board Board => board;
        public List<Player> Players => players;
        public Player CurrentPlayer => players.Count == 0 ? null : players[currentPlayerIndex];
        public Card PendingCard => pendingCard;
        public Dictionary<string, List<Card>> VisibleCardsMap => visibleCardsMap;
        public int RoundCount => roundCount;
        public Player DeutschCaller => deutschCaller;

        public void SetGameEventListener(IGameEventListener listener) => gameEventListener = listener;

        public void AddPlayer(Player player)
        {
            if (player != null && players.Count < 4) players.Add(player);
        }

        public void RemovePlayer(string name)
        {
            players.RemoveAll(p => p.Name == name);
        }

        public void StartNewGame()
        {
            roundCount = 0;
            currentPlayerIndex = 0;
            snapEnabled = false;
            deutschCaller = null;
            isFinalTurn = false;
            pendingCard = null;

            foreach (Player p in players)
            {
                p.AddGamePoints(-p.TotalGamePoints);
                p.NextRoundBonus = PokerEvaluator.HandType.None;
            }

            board.InitializeBoard();
            StartNewRound();
        }

        public void NextTurn()
        {
            if (players.Count == 0) return;
            int nextIndex = (currentPlayerIndex + 1) % players.Count;
            Player nextPlayer = players[nextIndex];

            if (isFinalTurn && ReferenceEquals(nextPlayer, deutschCaller))
            {
                CheckVictory();
                return;
            }
            currentPlayerIndex = nextIndex;

            gameEventListener?.OnTurnChanged(CurrentPlayer);
        }

        public List<Card> GetInitialPeekCard(Player player, int count)
        {
            if (player == null || player.Hand.Count == 0) return new List<Card>();
            int actualCount = Math.Min(count, player.Hand.Count);
            return player.Hand.GetRange(0, actualCount);
        }

        public List<Card> ApplyCardPower(Card card, int? myCardIndex, int? targetPlayerIndex, int? targetCardIndex, int? riverIndex)
        {
            bool targetWasProtected = false;
            bool targetHasGodMode = false;
            Player target = null;
            if (targetPlayerIndex.HasValue && targetPlayerIndex.Value >= 0 && targetPlayerIndex.Value < players.Count)
            {
                target = players[targetPlayerIndex.Value];
                targetWasProtected = target.IsProtected;
                targetHasGodMode = target.NextRoundBonus == PokerEvaluator.HandType.Carre;
            }

            List<Card> result = powerEngine.HandlePower(card, CurrentPlayer, players, board,
                myCardIndex, targetPlayerIndex, targetCardIndex, riverIndex);

            if (gameEventListener != null)
            {
                target = targetPlayerIndex.HasValue ? players[targetPlayerIndex.Value] : null;
                gameEventListener.OnPowerExecuted(card, CurrentPlayer, target);
                if (card.Power == Card.PowerType.Attack && targetHasGodMode)
                {
                    gameEventListener.OnGameMessage($"GOD MOD ! {target.Name} est intouchable !", "WARNING");
                }
                else if (targetWasProtected && target != null && !target.IsProtected)
                {
                    gameEventListener.OnGameMessage($"BOUCLIER ! L'attaque contre {target.Name} est bloquée.", "SUCCESS");
                }
                if (card.Power == Card.PowerType.Swap)
                {
                    gameEventListener.OnGameMessage($"ÉCHANGE ! {CurrentPlayer.Name} a volé une carte à {target?.Name}", "INFO");
                }
            }
            return result;
        }

        public void StartNewRound()
        {
            if (roundCount >= 5)
            {
                EndGame();
                return;
            }

            snapEnabled = false;
            deutschCaller = null;
            isFinalTurn = false;
            pendingCard = null;
            CurrentState = GameState.Normal;

            roundManager.ResetForNewRound(board, players);
            board.InitializeBoard();
            roundManager.DistributeInitialCards(board, players);

            visibleCardsMap.Clear();
            var cardsToReveal = new Dictionary<Player, List<Card>>();

            foreach (Player player in players)
            {
                var cardsToShow = new List<Card>();
                if (player.NextRoundBonus == PokerEvaluator.HandType.Brelan) cardsToShow.AddRange(GetInitialPeekCard(player, 2));
                else if (player.NextRoundBonus == PokerEvaluator.HandType.Full) cardsToShow.AddRange(GetInitialPeekCard(player, 3));
                else cardsToShow.AddRange(GetInitialPeekCard(player, 1));

                if (player.NextRoundBonus == PokerEvaluator.HandType.Pair && board.River.Count > 0)
                {
                    cardsToShow.Add(board.River[0]);
                }
                if (player.NextRoundBonus == PokerEvaluator.HandType.TwoPairs) player.IsProtected = true;

                visibleCardsMap[player.Name] = cardsToShow;
                cardsToReveal[player] = cardsToShow;
                player.NextRoundBonus = PokerEvaluator.HandType.None;
            }

            if (gameEventListener != null)
            {
                gameEventListener.OnRoundStarted(roundCount, cardsToReveal);
                gameEventListener.OnTurnChanged(CurrentPlayer);
            }
            roundCount++;
        }

        public Card PlayerDrawsFromDeck(Player player)
        {
            if (!ReferenceEquals(player, CurrentPlayer) || pendingCard != null || CurrentState != GameState.Normal) return null;
            snapEnabled = false;
            Card drawn = SafeDraw();
            if (drawn != null)
            {
                pendingCard = drawn;
                snapEnabled = true;
            }
            return drawn;
        }

        private Card SafeDraw()
        {
            if (board.IsDeckEmpty) board.ReshuffleDiscardIntoDeck();
            Card drawn = board.DrawFromDeck();
            if (drawn == null)
            {
                var rng = new Random();
                int randomRank = rng.Next(1, 14);
                string[] suits = { Card.Heart, Card.Square, Card.Club, Card.Clover };
                drawn = new Card(randomRank, suits[rng.Next(4)]);
            }
            return drawn;
        }

        public void DiscardPendingCard()
        {
            if (pendingCard == null) return;
            board.AddToDiscard(pendingCard);
            snapEnabled = true;
            pendingCard = null;
            NextTurn();
        }

        public void ReplaceCardWithPending(int cardIndexInHand)
        {
            if (pendingCard == null) return;
            Player p = CurrentPlayer;
            Card oldCard = p.Hand[cardIndexInHand];
            p.Hand[cardIndexInHand] = pendingCard;
            board.AddToDiscard(oldCard);
            snapEnabled = true;
            pendingCard = null;

            if (oldCard.Power != Card.PowerType.None)
            {
                CurrentState = GameState.WaitingForPowerTarget;
                ActivePowerCard = oldCard;
                if (gameEventListener != null)
                {
                    gameEventListener.OnPowerExecuted(oldCard, p, null);
                    gameEventListener.OnGameMessage("CHOISISSEZ UNE CIBLE POUR LE POUVOIR", "INFO");
                }
            }
            else
            {
                NextTurn();
            }
        }

        public void HandleTargetSelected(int? targetPlayerIndex, int? targetCardIndex, int? riverIndex)
        {
            if (CurrentState != GameState.WaitingForPowerTarget || ActivePowerCard == null) return;
            Card.PowerType power = ActivePowerCard.Power;

            if (power == Card.PowerType.Swap)
            {
                if (firstSelectedCardIndex == null)
                {
                    if (targetPlayerIndex.HasValue && targetPlayerIndex.Value == currentPlayerIndex)
                    {
                        firstSelectedCardIndex = targetCardIndex;
                        gameEventListener?.OnGameMessage("CARTE SÉLECTIONNÉE. MAINTENANT, CHOISISSEZ LA CIBLE !", "INFO");
                    }
                    else
                    {
                        gameEventListener?.OnGameMessage("CHOISISSEZ D'ABORD UNE DE VOS CARTES !", "WARNING");
                    }
                    return;
                }
                if (targetPlayerIndex.HasValue && targetPlayerIndex.Value != currentPlayerIndex)
                {
                    ApplyCardPower(ActivePowerCard, firstSelectedCardIndex, targetPlayerIndex, targetCardIndex, null);
                    FinalizePower();
                    NextTurn();
                }
                else
                {
                    gameEventListener?.OnGameMessage("CLIQUEZ SUR UN ADVERSAIRE !", "WARNING");
                }
                return;
            }

            var revealedCards = new List<Card>();
            if (power == Card.PowerType.PeekRiver && riverIndex.HasValue)
            {
                revealedCards.Add(board.River[riverIndex.Value]);
            }
            else if (power == Card.PowerType.PeekOpponent && targetPlayerIndex.HasValue && targetCardIndex.HasValue)
            {
                revealedCards.Add(players[targetPlayerIndex.Value].Hand[targetCardIndex.Value]);
            }
            else if (power == Card.PowerType.Peek)
            {
                if (riverIndex.HasValue) revealedCards.Add(board.River[riverIndex.Value]);
                else if (targetPlayerIndex.HasValue && targetCardIndex.HasValue) revealedCards.Add(players[targetPlayerIndex.Value].Hand[targetCardIndex.Value]);
            }
            else
            {
                ApplyCardPower(ActivePowerCard, null, targetPlayerIndex, targetCardIndex, riverIndex);
            }

            if (revealedCards.Count > 0)
            {
                if (!visibleCardsMap.TryGetValue(CurrentPlayer.Name, out var list))
                {
                    list = new List<Card>();
                    visibleCardsMap[CurrentPlayer.Name] = list;
                }
                list.AddRange(revealedCards);
                gameEventListener?.OnCardsRevealed(revealedCards);
            }
            FinalizePower();
            NextTurn();
        }

        private void FinalizePower()
        {
            CurrentState = GameState.Normal;
            ActivePowerCard = null;
            firstSelectedCardIndex = null;
        }

        public bool CheckSnap(Player player, int cardIndex)
        {
            if (!snapEnabled || CurrentState != GameState.Normal) return false;
            if (cardIndex < 0 || cardIndex >= player.HandSize) return false;
            Card playerCard = player.Hand[cardIndex];
            Card topDiscard = board.DiscardPile.Peek();

            if (playerCard.Rank == topDiscard.Rank)
            {
                player.Hand.RemoveAt(cardIndex);
                board.AddToDiscard(playerCard);
                gameEventListener?.OnGameMessage($"SNAP ! {player.Name} est rapide !", "SUCCESS");
                return true;
            }

            for (int i = 0; i < 2; i++)
            {
                Card penalty = SafeDraw();
                if (penalty != null) player.AddCard(penalty);
            }
            gameEventListener?.OnGameMessage($"RATÉ ! +2 Cartes pour {player.Name}", "WARNING");
            return false;
        }

        public void DeclareDeutsch(Player player)
        {
            if (deutschCaller != null || !ReferenceEquals(player, CurrentPlayer)) return;
            player.CallDeutsch();
            deutschCaller = player;
            isFinalTurn = true;
            NextTurn();
        }

        private readonly struct PlayerScoreEntry
        {
            public Player Player { get; }
            public int Score { get; }
            public PlayerScoreEntry(Player player, int score) { Player = player; Score = score; }
        }

        public void CheckVictory()
        {
            var scores = new List<PlayerScoreEntry>();

            foreach (Player p in players)
            {
                int roundScore = PokerEvaluator.CalculateFinalWeight(p.Hand, board.River);
                scores.Add(new PlayerScoreEntry(p, roundScore));
                PokerEvaluator.HandType bonus = PokerEvaluator.DetectBestHand(p.Hand, board.River);
                p.NextRoundBonus = bonus;
                p.LastRoundScore = roundScore;
            }

            var sortedScores = scores
                .OrderBy(s => s, Comparer<PlayerScoreEntry>.Create((a, b) =>
                {
                    if (a.Score != b.Score) return a.Score.CompareTo(b.Score);
                    try
                    {
                        int cmp = PokerEvaluator.CompareEqualScores(a.Player.Hand, b.Player.Hand);
                        if (cmp == 1) return -1;
                        if (cmp == -1) return 1;
                        return 0;
                    }
                    catch
                    {
                        return 0;
                    }
                }))
                .ToList();

            int[] rankPoints = { 0, 5, 10, 15 };
            for (int i = 0; i < sortedScores.Count; i++)
            {
                Player p = sortedScores[i].Player;
                int pointsToAdd = i < rankPoints.Length ? rankPoints[i] : 15;
                p.AddGamePoints(pointsToAdd);
            }

            Player winner = sortedScores[0].Player;
            if (deutschCaller != null && !ReferenceEquals(deutschCaller, winner))
            {
                deutschCaller.LastRoundScore += 10;
                deutschCaller.AddGamePoints(10);
            }

            gameEventListener?.OnRoundEnded(sortedScores.Select(s => s.Player).ToList());
        }

        public void EndGame()
        {
            if (players.Count == 0) return;
            Player winner = players.OrderBy(p => p.TotalGamePoints).FirstOrDefault() ?? players[0];

            gameEventListener?.OnGameEnded(winner);
        }

        public void ConcludePowerVision()
        {
            FinalizePower();
            NextTurn();
        }
    }
}
