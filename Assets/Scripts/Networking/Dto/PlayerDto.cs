using System;
using System.Collections.Generic;

namespace RiverDeutsch.Networking.Dto
{
    [Serializable]
    public class PlayerDto
    {
        public string Name;
        public List<CardDto> Hand;
        public bool HasCalledDeutsch;
        public bool IsProtected;
        public int TotalGamePoints;
        public int LastRoundScore;
        public string NextRoundBonus;
    }
}
