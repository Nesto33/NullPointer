using NUnit.Framework;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Tests
{
    public class BoardTests
    {
        private Board board;

        [SetUp]
        public void SetUp()
        {
            board = new Board();
            board.InitializeBoard(); // remplit rivière (2 cartes) + défausse (1 carte)
        }

        [Test]
        public void TestBoardInitialization()
        {
            Assert.IsNotNull(board);
            Assert.IsNotNull(board.River);
            Assert.IsNotNull(board.DiscardPile);
            Assert.AreEqual(1, board.DiscardPile.Count);
            Assert.AreEqual(2, board.River.Count);
            Assert.AreEqual(49, board.DeckSize); // 52 cards minus 2 in river and 1 in discard
        }

        [Test]
        public void TestDrawFromDeck()
        {
            int initialDeckSize = board.DeckSize; // 52 cards minus 2 in river and 1 in discard --> 49
            Assert.IsFalse(board.IsDeckEmpty);
            Card drawnCard = board.DrawFromDeck();
            Assert.IsNotNull(drawnCard);
            Assert.AreEqual(initialDeckSize - 1, board.DeckSize, "La taille du deck doit diminuer de 1 après la pioche.");
        }

        [Test]
        public void TestAddToDiscard()
        {
            var card = new Card(10, Card.Heart);
            int initialDiscardSize = board.DiscardPile.Count;
            board.AddToDiscard(card);
            Assert.AreEqual(initialDiscardSize + 1, board.DiscardPile.Count);
            Assert.AreEqual(card, board.DiscardPile.Peek());
        }

        [Test]
        public void TestFaceUpRiverCards()
        {
            foreach (Card card in board.River)
            {
                Assert.IsFalse(card.IsFaceUp, "River cards shouldn't be face up.");
            }
        }

        [Test]
        public void TestFaceUpDiscardCard()
        {
            Assert.IsTrue(board.DiscardPile.Peek().IsFaceUp, "Discard pile's top card should be face up.");
            var card = new Card(10, Card.Heart);
            board.AddToDiscard(card);
            Assert.IsTrue(board.DiscardPile.Peek().IsFaceUp, "Newly added discard card should be face up.");
        }
    }
}
