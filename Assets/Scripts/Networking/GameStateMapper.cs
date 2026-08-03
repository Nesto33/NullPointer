using System.Collections.Generic;
using System.Linq;
using RiverDeutsch.Core.Logic;
using RiverDeutsch.Core.Model;
using RiverDeutsch.Networking.Dto;

namespace RiverDeutsch.Networking
{
    /// <summary>
    /// Builds a per-recipient <see cref="GameStateDto"/> from the authoritative GameManager.
    /// A card is only revealed to a recipient if it is face-up (e.g. the discard pile) or it
    /// appears in that recipient's entry of GameManager.VisibleCardsMap (a power reveal, an
    /// initial peek, ...). Every other client sees the same card as an anonymous face-down slot.
    /// </summary>
    public static class GameStateMapper
    {
        public static GameStateDto ToDto(GameManager gameManager, string recipientName)
        {
            List<Card> revealedToRecipient = null;
            if (recipientName != null)
            {
                gameManager.VisibleCardsMap.TryGetValue(recipientName, out revealedToRecipient);
            }
            revealedToRecipient ??= new List<Card>();

            bool isRecipientCurrentPlayer = gameManager.CurrentPlayer != null
                && gameManager.CurrentPlayer.Name == recipientName;

            return new GameStateDto
            {
                Players = gameManager.Players.Select(p => MapPlayer(p, revealedToRecipient)).ToList(),
                Board = MapBoard(gameManager.Board, revealedToRecipient),
                CurrentPlayerName = gameManager.CurrentPlayer?.Name,
                PendingCard = (isRecipientCurrentPlayer && gameManager.PendingCard != null)
                    ? MapCard(gameManager.PendingCard, reveal: true)
                    : null,
                CurrentState = gameManager.CurrentState.ToString(),
                ActivePowerCard = gameManager.ActivePowerCard != null
                    ? MapCard(gameManager.ActivePowerCard, reveal: true)
                    : null,
                RoundCount = gameManager.RoundCount,
                DeutschCallerName = gameManager.DeutschCaller?.Name,
                VisibleCards = revealedToRecipient.Select(c => MapCard(c, reveal: true)).ToList(),
            };
        }

        private static PlayerDto MapPlayer(Player player, List<Card> revealedToRecipient) => new()
        {
            Name = player.Name,
            Hand = player.Hand.Select(c => MapCard(c, ShouldReveal(c, revealedToRecipient))).ToList(),
            HasCalledDeutsch = player.HasCalledDeutsch,
            IsProtected = player.IsProtected,
            TotalGamePoints = player.TotalGamePoints,
            LastRoundScore = player.LastRoundScore,
            NextRoundBonus = player.NextRoundBonus.ToString(),
        };

        private static BoardDto MapBoard(Board board, List<Card> revealedToRecipient) => new()
        {
            River = board.River.Select(c => MapCard(c, ShouldReveal(c, revealedToRecipient))).ToList(),
            DiscardTop = board.DiscardPile.Count > 0 ? MapCard(board.DiscardPile.Peek(), reveal: true) : null,
            DeckSize = board.DeckSize,
            DiscardCount = board.DiscardPile.Count,
        };

        private static bool ShouldReveal(Card card, List<Card> revealedToRecipient) =>
            card.IsFaceUp || revealedToRecipient.Contains(card);

        private static CardDto MapCard(Card card, bool reveal)
        {
            if (!reveal)
            {
                return new CardDto { Known = false, Rank = 0, Suit = null, IsFaceUp = card.IsFaceUp, Power = Card.PowerType.None.ToString() };
            }
            return new CardDto { Known = true, Rank = card.Rank, Suit = card.Suit, IsFaceUp = card.IsFaceUp, Power = card.Power.ToString() };
        }
    }
}
