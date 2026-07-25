using System.Collections.Generic;
using NUnit.Framework;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Tests
{
    public class DeckTests
    {
        private Deck deck;

        [SetUp]
        public void SetUp()
        {
            deck = new Deck();
        }

        [Test]
        public void TestDeckInitialization()
        {
            Assert.AreEqual(52, deck.Size);
        }

        [Test]
        public void TestShuffle()
        {
            var deckToShuffle = new Deck();
            var originalOrder = new List<Card>();
            for (int i = 0; i < 52; i++)
            {
                originalOrder.Add(deckToShuffle.Draw());
            }

            deckToShuffle = new Deck();
            deckToShuffle.Shuffle();
            var shuffledOrder = new List<Card>();
            for (int i = 0; i < 52; i++)
            {
                shuffledOrder.Add(deckToShuffle.Draw());
            }

            CollectionAssert.AreNotEqual(originalOrder, shuffledOrder, "Shuffling should change the order of cards.");
            Assert.AreEqual(52, shuffledOrder.Count);
        }

        [Test]
        public void TestDraw()
        {
            Card firstCard = deck.Draw();
            Assert.IsNotNull(firstCard);
            Assert.AreEqual(51, deck.Size);

            Card secondCard = deck.Draw();
            Assert.IsNotNull(secondCard);
            Assert.AreEqual(50, deck.Size);

            Assert.AreNotEqual(firstCard, secondCard);
        }

        [Test]
        public void TestDrawUntilEmpty()
        {
            for (int i = 0; i < 52; i++)
            {
                Assert.IsNotNull(deck.Draw());
            }
            Assert.AreEqual(0, deck.Size);
            Assert.IsNull(deck.Draw(), "Drawing from an empty deck should return null.");
        }
    }
}
