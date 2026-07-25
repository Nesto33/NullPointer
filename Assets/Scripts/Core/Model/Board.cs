using System;
using System.Collections.Generic;

namespace RiverDeutsch.Core.Model
{
    [Serializable]
    public class Board
    {
        private readonly Deck deck;
        private readonly List<Card> river;
        private readonly Stack<Card> discardPile;

        public Board()
        {
            deck = new Deck();
            deck.Shuffle();
            river = new List<Card>();
            discardPile = new Stack<Card>();
        }

        public void InitializeBoard()
        {
            river.Clear();

            // rivière ici
            if (deck.Size >= 2)
            {
                river.Add(deck.Draw());
                river.Add(deck.Draw());
            }

            // défausse ici
            if (deck.Size > 0)
            {
                Card firstDiscard = deck.Draw();
                firstDiscard.SetFaceUp(true); // On la retourne IMMÉDIATEMENT
                discardPile.Push(firstDiscard);
            }
        }

        public Card DrawFromDeck()
        {
            if (deck.Size == 0)
            {
                ReshuffleDiscardIntoDeck();
            }

            Card drawn = deck.Draw();
            if (drawn == null)
            {
                Console.WriteLine("ERREUR CRITIQUE : Plus aucune carte dans le deck ET la défausse.");
            }
            return drawn;
        }

        public void AddToDiscard(Card card)
        {
            card.SetFaceUp(true);
            discardPile.Push(card);
        }

        public List<Card> River => river;

        public bool IsDeckEmpty => deck.Size == 0;

        public Stack<Card> DiscardPile => discardPile;

        public int DeckSize => deck.Size;

        public void ReshuffleDiscardIntoDeck()
        {
            if (discardPile.Count <= 1)
            {
                Console.WriteLine("Défausse trop courte pour être recyclée.");
                return;
            }

            Console.WriteLine("Système : Recyclage de la défausse (conservation de la carte active)...");

            // on garde la carte du dessus
            Card activeCard = discardPile.Pop();

            // on vide dans le deck
            while (discardPile.Count > 0)
            {
                Card c = discardPile.Pop();
                c.SetFaceUp(false); // On cache pour la nouvelle pioche
                deck.AddCard(c);
            }

            // mélanger
            deck.Shuffle();

            // remettre la carte active sur la défausse
            discardPile.Push(activeCard);

            Console.WriteLine("Deck reconstitué. Carte active : " + activeCard);
        }
    }
}
