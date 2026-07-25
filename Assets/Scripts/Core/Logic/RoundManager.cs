using System;
using System.Collections.Generic;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Logic
{
    [Serializable]
    public class RoundManager
    {
        /// <summary>Nettoie le plateau et les mains des joueurs pour une nouvelle manche</summary>
        public void ResetForNewRound(Board board, List<Player> players)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.Hand)
                {
                    board.AddToDiscard(card);
                }
                player.Hand.Clear();
                player.ResetDeutsch();
            }

            foreach (Card riverCard in board.River)
            {
                board.AddToDiscard(riverCard);
            }
            board.River.Clear();

            board.ReshuffleDiscardIntoDeck();
            board.DiscardPile.Clear();
        }

        /// <summary>Distribue les 4 cartes initiales à chaque joueur</summary>
        public void DistributeInitialCards(Board board, List<Player> players)
        {
            foreach (Player player in players)
            {
                for (int i = 0; i < 4; i++)
                {
                    Card c = board.DrawFromDeck();
                    if (c != null)
                    {
                        c.SetFaceUp(false);
                        player.AddCard(c);
                    }
                }
            }
        }

        /// <summary>Gère l'attribution des bonus de poker à la fin d'une manche</summary>
        public void EvaluateAndAssignBonuses(List<Player> players, Board board)
        {
            foreach (Player player in players)
            {
                PokerEvaluator.HandType handType = PokerEvaluator.DetectBestHand(player.Hand, board.River);
                player.NextRoundBonus = handType;
                // la j'ai rajouter le pouvoir PROTECTED pour le bonus TWO_PAIRS
                player.IsProtected = handType == PokerEvaluator.HandType.TwoPairs;
            }
        }

        /// <summary>Détermine combien de cartes un joueur peut voir au début selon son bonus</summary>
        public int GetPeekCount(Player player)
        {
            return player.NextRoundBonus switch
            {
                PokerEvaluator.HandType.Brelan => 2,
                PokerEvaluator.HandType.Full => 3,
                _ => 1
            };
        }

        /// <summary>Réinitialise les bonus de tous les joueurs</summary>
        public void ClearBonuses(List<Player> players)
        {
            foreach (Player p in players)
            {
                p.NextRoundBonus = PokerEvaluator.HandType.None;
            }
        }

        /// <summary>Logique spécifique pour le bonus PAIR (voir la première carte de la rivière)</summary>
        public Card GetBonusRiverCard(Player player, Board board)
        {
            if (player.NextRoundBonus == PokerEvaluator.HandType.Pair && board.River.Count > 0)
            {
                return board.River[0];
            }
            return null;
        }
    }
}
