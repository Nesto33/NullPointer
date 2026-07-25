using System;
using System.Collections.Generic;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Logic
{
    [Serializable]
    public class PowerEngine
    {
        /// <summary>7-8 : Voir une carte de la Rivière</summary>
        public List<Card> ApplyPeekRiverPower(Board board, int riverIndex)
        {
            if (riverIndex < 0 || riverIndex >= board.River.Count)
            {
                Console.WriteLine("Index rivière invalide.");
                return new List<Card>();
            }
            return new List<Card> { board.River[riverIndex] };
        }

        /// <summary>9-10 : Voir une carte d'un adversaire
        /// (peut aussi servir à regarder son propre jeu avec la dame --> appel de la méthode)</summary>
        public List<Card> ApplyPeekOpponentPower(List<Player> players, int targetPlayerIndex, int targetCardIndex)
        {
            if (targetPlayerIndex < 0 || targetPlayerIndex >= players.Count) return new List<Card>();

            Player target = players[targetPlayerIndex];
            if (target == null || targetCardIndex < 0 || targetCardIndex >= target.HandSize)
            {
                return new List<Card>();
            }
            return new List<Card> { target.Hand[targetCardIndex] };
        }

        /// <summary>Dame : Voir une carte (Rivière, Adversaire ou Soi-même)</summary>
        public List<Card> ApplyPeekPower(List<Player> players, Board board, int? targetPlayerIndex, int? targetCardIndex, int? riverIndex)
        {
            if (riverIndex.HasValue)
            {
                return ApplyPeekRiverPower(board, riverIndex.Value);
            }
            // Ici, si l'index du target c'est le currentplayer alors on regarde sa main, sinon on regarde la main d'un adversaire
            if (targetPlayerIndex.HasValue && targetCardIndex.HasValue)
            {
                return ApplyPeekOpponentPower(players, targetPlayerIndex.Value, targetCardIndex.Value);
            }

            return new List<Card>();
        }

        /// <summary>Valet : Échange de cartes (Swap)</summary>
        public void ApplySwapPower(Player currentPlayer, List<Player> players, int myCardIndex, int targetPlayerIndex, int targetCardIndex)
        {
            if (currentPlayer == null || myCardIndex < 0 || myCardIndex >= currentPlayer.HandSize) return;
            if (targetPlayerIndex < 0 || targetPlayerIndex >= players.Count) return;

            Player target = players[targetPlayerIndex];
            if (target == null || target.Hand.Count == 0 || targetCardIndex < 0 || targetCardIndex >= target.HandSize)
            {
                return;
            }

            Card myCard = currentPlayer.Hand[myCardIndex];
            Card oppCard = target.Hand[targetCardIndex];

            currentPlayer.Hand[myCardIndex] = oppCard;
            target.Hand[targetCardIndex] = myCard;

            Console.WriteLine($"SWAP effectué entre {currentPlayer.Name} et {target.Name}");
        }

        /// <summary>As : Attaque (piocher une carte et la donner à un adversaire)</summary>
        public List<Card> ApplyAttackPower(Board board, List<Player> players, int targetPlayerIndex)
        {
            if (targetPlayerIndex < 0 || targetPlayerIndex >= players.Count) return new List<Card>();

            Player target = players[targetPlayerIndex];
            if (target == null) return new List<Card>();

            if (target.IsProtected)
            {
                Console.WriteLine($"BOUCLIER ACTIVÉ ! L'attaque contre {target.Name} est bloquée.");
                target.IsProtected = false; // Le bouclier est consommé
                return new List<Card>(); // On arrête ici, aucune carte n'est piochée
            }

            if (target.NextRoundBonus == PokerEvaluator.HandType.Carre)
            {
                Console.WriteLine($"GOD MODE ! {target.Name} est immunisé contre tout.");
                return new List<Card>();
            }

            if (board.IsDeckEmpty)
            {
                board.ReshuffleDiscardIntoDeck();
            }

            Card drawn = board.DrawFromDeck();
            if (drawn != null)
            {
                drawn.SetFaceUp(false);
                target.AddCard(drawn);
                return new List<Card> { drawn };
            }
            return new List<Card>();
        }

        /// <summary>Activation des pouvoirs</summary>
        public List<Card> HandlePower(Card card, Player currentPlayer, List<Player> players, Board board,
            int? myCardIndex, int? targetPlayerIndex, int? targetCardIndex, int? riverIndex)
        {
            if (card == null) return new List<Card>();

            return card.Power switch
            {
                Card.PowerType.PeekRiver => ApplyPeekRiverPower(board, riverIndex ?? -1),
                Card.PowerType.PeekOpponent => ApplyPeekOpponentPower(players, targetPlayerIndex ?? -1, targetCardIndex ?? -1),
                Card.PowerType.Peek => ApplyPeekPower(players, board, targetPlayerIndex, targetCardIndex, riverIndex),
                Card.PowerType.Swap => SwapAndReturnEmpty(currentPlayer, players, myCardIndex ?? -1, targetPlayerIndex ?? -1, targetCardIndex ?? -1),
                Card.PowerType.Attack => ApplyAttackPower(board, players, targetPlayerIndex ?? -1),
                _ => new List<Card>()
            };
        }

        private List<Card> SwapAndReturnEmpty(Player currentPlayer, List<Player> players, int myCardIndex, int targetPlayerIndex, int targetCardIndex)
        {
            ApplySwapPower(currentPlayer, players, myCardIndex, targetPlayerIndex, targetCardIndex);
            return new List<Card>();
        }
    }
}
