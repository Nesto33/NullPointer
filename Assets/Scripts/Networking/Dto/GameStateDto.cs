using System;
using System.Collections.Generic;

namespace RiverDeutsch.Networking.Dto
{
    /// <summary>
    /// Snapshot of the authoritative GameManager, personalized for one recipient:
    /// hidden cards are stripped of their identity before this leaves the server.
    /// </summary>
    [Serializable]
    public class GameStateDto
    {
        public List<PlayerDto> Players;
        public BoardDto Board;
        public string CurrentPlayerName;
        public CardDto PendingCard;
        public string CurrentState;
        public CardDto ActivePowerCard;
        public int RoundCount;
        public string DeutschCallerName;
        public List<CardDto> VisibleCards;
    }
}
