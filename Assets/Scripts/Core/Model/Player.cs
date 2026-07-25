using System;
using System.Collections.Generic;
using RiverDeutsch.Core.Logic;

namespace RiverDeutsch.Core.Model
{
    [Serializable]
    public class Player
    {
        public string Name { get; }
        public List<Card> Hand { get; } = new();
        public bool HasCalledDeutsch { get; private set; }
        public PokerEvaluator.HandType NextRoundBonus { get; set; } = PokerEvaluator.HandType.None;

        public bool IsProtected { get; set; }
        public int TotalGamePoints { get; private set; }
        public int LastRoundScore { get; set; }

        public Player(string name)
        {
            Name = name;
        }

        public int HandSize => Hand.Count;

        public void AddGamePoints(int points) => TotalGamePoints += points;

        public void CallDeutsch() => HasCalledDeutsch = true;

        public void ResetDeutsch() => HasCalledDeutsch = false;

        public void AddCard(Card card)
        {
            if (card != null) Hand.Add(card);
        }

        public Card SwapCard(int index, Card newCard)
        {
            if (index >= 0 && index < Hand.Count)
            {
                Card oldCard = Hand[index];
                Hand[index] = newCard;
                return oldCard;
            }
            return null;
        }

        public override string ToString() => $"Joueur {Name} ({Hand.Count} cartes)";
    }
}
