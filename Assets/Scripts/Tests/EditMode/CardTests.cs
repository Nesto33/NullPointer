using NUnit.Framework;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Tests
{
    public class CardTests
    {
        [Test]
        public void TestCardCreation()
        {
            var card = new Card(1, Card.Club);
            Assert.AreEqual(1, card.Rank);
            Assert.AreEqual(Card.Club, card.Suit);
            Assert.IsFalse(card.IsFaceUp);
        }

        [Test]
        public void TestIsRedAndIsBlack()
        {
            var redCard = new Card(10, Card.Heart);
            var blackCard = new Card(10, Card.Clover);
            Assert.IsTrue(redCard.IsRed);
            Assert.IsFalse(redCard.IsBlack);
            Assert.IsTrue(blackCard.IsBlack);
            Assert.IsFalse(blackCard.IsRed);
        }

        [Test]
        public void TestGetCardValue()
        {
            var seven = new Card(7, Card.Square);
            Assert.AreEqual(7, seven.CardValue);

            var jack = new Card(11, Card.Club);
            Assert.AreEqual(11, jack.CardValue);

            var redKing = new Card(13, Card.Heart);
            Assert.AreEqual(0, redKing.CardValue);

            var blackKing = new Card(13, Card.Club);
            Assert.AreEqual(25, blackKing.CardValue);
        }

        [Test]
        public void TestGetPower()
        {
            var ace = new Card(1, Card.Club);
            Assert.AreEqual(Card.PowerType.Attack, ace.Power);

            var seven = new Card(7, Card.Clover);
            Assert.AreEqual(Card.PowerType.PeekRiver, seven.Power);

            var ten = new Card(10, Card.Heart);
            Assert.AreEqual(Card.PowerType.PeekOpponent, ten.Power);

            var jack = new Card(11, Card.Square);
            Assert.AreEqual(Card.PowerType.Swap, jack.Power);

            var queen = new Card(12, Card.Club);
            Assert.AreEqual(Card.PowerType.Peek, queen.Power);

            var five = new Card(5, Card.Clover);
            Assert.AreEqual(Card.PowerType.None, five.Power);
        }

        [Test]
        public void TestToString()
        {
            var queenOfHearts = new Card(12, Card.Heart);
            Assert.AreEqual("Queen of Heart", queenOfHearts.ToString());

            var aceOfClub = new Card(1, Card.Club);
            Assert.AreEqual("Ace of Club", aceOfClub.ToString());
        }
    }
}
