using System.Collections.Generic;
using NUnit.Framework;
using RiverDeutsch.Core.Logic;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Tests
{
    public class GameManagerTests
    {
        private Player player1;
        private Player player2;

        [SetUp]
        public void SetUp()
        {
            player1 = new Player("Player 1");
            player2 = new Player("Player 2");
        }

        [Test]
        public void TestAddPlayer()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            Assert.IsNotNull(gameManager.Players);
            Assert.AreEqual(2, gameManager.Players.Count);
            Assert.AreEqual("Player 1", gameManager.Players[0].Name);
            Assert.AreEqual("Player 2", gameManager.Players[1].Name);
        }

        [Test]
        public void TestStartNewGame()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            gameManager.StartNewGame();
            Assert.AreEqual(4, player1.HandSize);
            Assert.AreEqual(4, player2.HandSize);

            Assert.IsFalse(player1.Hand.Count == 0);
            Assert.IsFalse(player1.Hand[0].IsFaceUp);

            Assert.IsFalse(player2.Hand.Count == 0);
            Assert.IsFalse(player2.Hand[0].IsFaceUp);
        }

        [Test]
        public void TestNextTurn()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            gameManager.StartNewGame();

            Assert.AreEqual("Player 1", gameManager.CurrentPlayer.Name);
            gameManager.NextTurn();
            Assert.AreEqual("Player 2", gameManager.CurrentPlayer.Name);
            gameManager.NextTurn();
            Assert.AreEqual("Player 1", gameManager.CurrentPlayer.Name);
        }

        [Test]
        public void TestAddNullPlayer()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(null);
            Assert.IsTrue(gameManager.Players.Count == 0, "Adding a null player should not change the players list.");
        }

        [Test]
        public void TestAddMoreThanFourPlayers()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            gameManager.AddPlayer(new Player("Player 3"));
            gameManager.AddPlayer(new Player("Player 4"));
            gameManager.AddPlayer(new Player("Player 5")); // This should not be added

            Assert.AreEqual(4, gameManager.Players.Count, "Should not allow more than 4 players.");
        }

        [Test]
        public void TestGetCurrentPlayerWithNoPlayers()
        {
            var gameManager = new GameManager();
            Assert.IsNull(gameManager.CurrentPlayer, "CurrentPlayer should return null when there are no players.");
        }

        [Test]
        public void TestNextTurnWithNoPlayers()
        {
            var gameManager = new GameManager();
            Assert.DoesNotThrow(() => gameManager.NextTurn(), "NextTurn should not throw an exception when there are no players.");
        }

        [Test]
        public void TestStartNewGameWithNoPlayers()
        {
            var gameManager = new GameManager();
            Assert.DoesNotThrow(() => gameManager.StartNewGame(), "StartNewGame should not throw an exception when there are no players.");
        }

        [Test]
        public void TestGetInitialPeekCard()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.StartNewGame();
            List<Card> peekCard = gameManager.GetInitialPeekCard(player1, 1);
            Assert.IsNotNull(peekCard, "GetInitialPeekCard should return a card from the player's hand.");
            Assert.AreEqual(player1.Hand[0], peekCard[0], "GetInitialPeekCard should return the first card in the player's hand.");
        }

        [Test]
        public void TestApplyPeekRiverPower()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.StartNewGame();

            var cardWithPower = new Card(7, Card.Club); // PEEK_RIVER
            int targetRiverIndex = 0;
            Card expectedCard = gameManager.Board.River[targetRiverIndex];

            List<Card> returnedCards = gameManager.ApplyCardPower(cardWithPower, null, null, null, targetRiverIndex);
            Assert.AreEqual(1, returnedCards.Count);
            Assert.AreEqual(expectedCard, returnedCards[0]);
        }

        [Test]
        public void TestApplyPeekOpponentPower()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            gameManager.StartNewGame();

            Player opponent = gameManager.Players[1];
            var cardWithPower = new Card(9, Card.Club); // PEEK_OPPONENT

            List<Card> returnedCards = gameManager.ApplyCardPower(cardWithPower, null, 1, 0, null);

            Assert.AreEqual(1, returnedCards.Count);
            Assert.AreEqual(opponent.Hand[0], returnedCards[0]);
        }

        [Test]
        public void TestApplyPeekSelfPower()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.StartNewGame();
            Player currentPlayer = gameManager.CurrentPlayer;
            var cardWithPower = new Card(12, Card.Club); // PEEK_SELF (Dame)

            // Pour voir sa propre main, targetPlayerIndex = index du joueur courant (0)
            List<Card> returnedCards = gameManager.ApplyCardPower(cardWithPower, null, 0, 3, null);

            Assert.AreEqual(1, returnedCards.Count);
            Assert.AreEqual(currentPlayer.Hand[3], returnedCards[0]);
        }

        [Test]
        public void TestApplySwapPower()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            gameManager.StartNewGame();

            Player currentPlayer = gameManager.CurrentPlayer;
            Player opponent = gameManager.Players[1];

            Card myCard = currentPlayer.Hand[0];
            Card opponentCard = opponent.Hand[0];

            var cardWithPower = new Card(11, Card.Club); // SWAP (Valet)

            gameManager.ApplyCardPower(cardWithPower, 0, 1, 0, null);

            Assert.AreEqual(opponentCard, currentPlayer.Hand[0]);
            Assert.AreEqual(myCard, opponent.Hand[0]);
        }

        [Test]
        public void TestApplyAttackPower()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.AddPlayer(player2);
            gameManager.StartNewGame();

            Board board = gameManager.Board;
            int initialDeckSize = board.DeckSize;
            Player opponent = gameManager.Players[1];
            int initialOpponentHandSize = opponent.HandSize;

            var cardWithPower = new Card(1, Card.Club); // ATTACK (As)

            gameManager.ApplyCardPower(cardWithPower, null, 1, null, null);

            Assert.AreEqual(initialDeckSize - 1, board.DeckSize);
            Assert.AreEqual(initialOpponentHandSize + 1, opponent.HandSize);
            Assert.IsFalse(opponent.Hand[initialOpponentHandSize].IsFaceUp);
        }

        [Test]
        public void TestApplyPowerNone()
        {
            var gameManager = new GameManager();
            gameManager.AddPlayer(player1);
            gameManager.StartNewGame();
            var cardWithNoPower = new Card(5, Card.Heart); // No power

            List<Card> returnedCards = gameManager.ApplyCardPower(cardWithNoPower, null, null, null, null);

            Assert.IsTrue(returnedCards.Count == 0);
        }
    }
}
