using NUnit.Framework;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Tests
{
    public class PlayerTests
    {
        private Player player;
        private Card card1;
        private Card card2;

        [SetUp]
        public void SetUp()
        {
            player = new Player("Test Player");
            card1 = new Card(1, Card.Heart);
            card2 = new Card(2, Card.Club);
        }

        [Test]
        public void TestPlayerCreation()
        {
            Assert.AreEqual("Test Player", player.Name);
            Assert.IsTrue(player.Hand.Count == 0);
            Assert.AreEqual(0, player.HandSize);
            Assert.IsFalse(player.HasCalledDeutsch);
        }

        [Test]
        public void TestAddCard()
        {
            player.AddCard(card1);
            Assert.AreEqual(1, player.HandSize);
            Assert.IsTrue(player.Hand.Contains(card1));
        }

        [Test]
        public void TestAddNullCard()
        {
            player.AddCard(null);
            Assert.AreEqual(0, player.HandSize);
        }

        [Test]
        public void TestCallDeutsch()
        {
            Assert.IsFalse(player.HasCalledDeutsch);
            player.CallDeutsch();
            Assert.IsTrue(player.HasCalledDeutsch);
        }

        [Test]
        public void TestSwapCard()
        {
            player.AddCard(card1);
            Card oldCard = player.SwapCard(0, card2);
            Assert.AreEqual(card1, oldCard);
            Assert.AreEqual(card2, player.Hand[0]);
            Assert.AreEqual(1, player.HandSize);
        }

        [Test]
        public void TestSwapCardInvalidIndex()
        {
            player.AddCard(card1);
            Card returnedCard = player.SwapCard(1, card2);
            Assert.IsNull(returnedCard);
            Assert.AreEqual(card1, player.Hand[0]);
            Assert.AreEqual(1, player.HandSize);

            returnedCard = player.SwapCard(-1, card2);
            Assert.IsNull(returnedCard);
        }

        [Test]
        public void TestToString()
        {
            Assert.AreEqual("Joueur Test Player (0 cartes)", player.ToString());
            player.AddCard(card1);
            Assert.AreEqual("Joueur Test Player (1 cartes)", player.ToString());
        }
    }
}
