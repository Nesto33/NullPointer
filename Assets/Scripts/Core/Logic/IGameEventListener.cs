using System.Collections.Generic;
using RiverDeutsch.Core.Model;

namespace RiverDeutsch.Core.Logic
{
    public interface IGameEventListener
    {
        void OnTurnChanged(Player newPlayer);
        void OnRoundStarted(int roundNumber, Dictionary<Player, List<Card>> cardsToReveal);
        void OnRoundEnded(List<Player> rankedPlayers);
        void OnGameEnded(Player winner);
        void OnGameMessage(string message, string type);
        void OnPowerExecuted(Card card, Player source, Player target);
        void OnCardsRevealed(List<Card> cards);
        void OnBackToMenu();
    }
}
