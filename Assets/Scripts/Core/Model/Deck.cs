using System;
using System.Collections.Generic;

namespace RiverDeutsch.Core.Model
{
    [Serializable]
    public class Deck
    {
        private readonly List<Card> cards = new();
        private static readonly Random Rng = new();

        public Deck()
        {
            InitializeDeck();
        }

        private void InitializeDeck()
        {
            string[] suits = { Card.Heart, Card.Club, Card.Square, Card.Clover };
            foreach (string suit in suits)
            {
                for (int rank = 1; rank <= 13; rank++)
                {
                    cards.Add(new Card(rank, suit));
                }
            }
        }

        public void Shuffle()
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        public Card Draw()
        {
            if (cards.Count == 0) return null;
            Card top = cards[0];
            cards.RemoveAt(0);
            return top;
        }

        public int Size => cards.Count;

        public void AddCard(Card card)
        {
            if (card != null) cards.Add(card);
        }
    }
}
