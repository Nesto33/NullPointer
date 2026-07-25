using System;

namespace RiverDeutsch.Networking.Dto
{
    /// <summary>
    /// Wire representation of a card. When <see cref="Known"/> is false, Rank/Suit are
    /// withheld — the recipient isn't allowed to know this card's identity right now.
    /// </summary>
    [Serializable]
    public class CardDto
    {
        public bool Known;
        public int Rank;
        public string Suit;
        public bool IsFaceUp;
    }
}
