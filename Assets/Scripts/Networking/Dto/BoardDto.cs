using System;
using System.Collections.Generic;

namespace RiverDeutsch.Networking.Dto
{
    [Serializable]
    public class BoardDto
    {
        public List<CardDto> River;
        public CardDto DiscardTop;
        public int DeckSize;
        public int DiscardCount;
    }
}
