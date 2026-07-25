using System.Collections.Generic;
using System.Linq;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Logic
{
    public static class PokerEvaluator
    {
        public enum HandType
        {
            None,
            Pair,
            TwoPairs,
            Brelan,
            Full,
            Carre
        }

        public static readonly Dictionary<HandType, (int BonusValue, string PowerUp)> HandInfo = new()
        {
            [HandType.Carre] = (99, "GOD MODE : Immunisé aux attaques"),
            [HandType.Full] = (10, "MEGA REVEAL : Voit 3 cartes"),
            [HandType.Brelan] = (6, "REVEAL : Voit 2 cartes au lieu de 1"),
            [HandType.TwoPairs] = (4, "PROTECTED : Le joueur est protégé contre la prochaine attaque"),
            [HandType.Pair] = (2, "PING : Voit la prochaine carte rivière"),
            [HandType.None] = (0, "Aucun avantage"),
        };

        public static int BonusValue(this HandType handType) => HandInfo[handType].BonusValue;
        public static string GetPowerUp(this HandType handType) => HandInfo[handType].PowerUp;

        public static int CalculateFinalWeight(List<Card> hand, List<Card> river)
        {
            if (hand.Count == 0) return 0;

            HandType bestHand = DetectBestHand(hand, river);

            if (bestHand == HandType.Carre) return -99;

            int handWeight = hand.Sum(c => c.CardValue);

            return handWeight - bestHand.BonusValue();
        }

        public static int CompareEqualScores(List<Card> handP1, List<Card> handP2)
        {
            if (handP1.Count == 0 && handP2.Count == 0)
            {
                return 0; // Match nul
            }
            // on privilégie qui n'a plus de carte
            if (handP1.Count == 0) return 1;
            if (handP2.Count == 0) return 2;

            // je cherche la carte la plus basse de la main.
            int minP1 = handP1.Min(c => c.CardValue);
            int minP2 = handP2.Min(c => c.CardValue);

            if (minP1 < minP2) return 1;
            if (minP2 < minP1) return 2;
            return 1;
        }

        public static HandType DetectBestHand(List<Card> hand, List<Card> river)
        {
            var allCards = new List<Card>(hand);

            if (river != null && river.Count > 0)
            {
                allCards.AddRange(river);
            }

            // on fait un dictionnaire (la clé --> rang, la valeur --> nombre d'occurrences)
            var counts = allCards
                .GroupBy(c => c.Rank)
                .ToDictionary(g => g.Key, g => g.Count());

            // on trie la liste des occurrences en ordre décroissant pour faciliter la détection des mains
            var occurrences = counts.Values.OrderByDescending(v => v).ToList();

            if (occurrences.Contains(4)) return HandType.Carre;
            if (occurrences.Contains(3) && occurrences.Contains(2)) return HandType.Full;
            if (occurrences.Contains(3)) return HandType.Brelan;

            int pairCount = occurrences.Count(count => count >= 2);
            if (pairCount >= 2) return HandType.TwoPairs;
            if (pairCount == 1) return HandType.Pair;

            return HandType.None;
        }
    }
}
