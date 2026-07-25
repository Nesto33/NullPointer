using System;

namespace RiverDeutsch.Core.Model
{
    [Serializable]
    public class Card : IEquatable<Card>
    {
        public const string Heart = "Heart";
        public const string Square = "Square";
        public const string Club = "Club";
        public const string Clover = "Clover";

        public enum PowerType
        {
            None,          // Pas de pouvoir (2, 3, 4, 5, 6)
            PeekRiver,     // 7, 8 : Voir Rivière
            PeekOpponent,  // 9, 10 : Voir Adversaire
            Swap,          // Valet : Échanger
            Peek,          // Dame : Voir son jeu (ou autre)
            Attack         // As : Attaque
        }

        public int Rank { get; }
        public string Suit { get; }
        public bool IsFaceUp { get; private set; }

        public Card(int rank, string suit)
        {
            Rank = rank;
            Suit = suit;
            IsFaceUp = false;
        }

        public bool IsRed => Suit == Heart || Suit == Square;
        public bool IsBlack => Suit == Club || Suit == Clover;
        public bool IsFaceCard => Rank >= 11;

        public int CardValue
        {
            get
            {
                if (Rank == 13) return IsRed ? 0 : 25;
                return Rank;
            }
        }

        public void Flip() => IsFaceUp = !IsFaceUp;
        public void SetFaceUp(bool faceUp) => IsFaceUp = faceUp;

        public PowerType Power
        {
            get
            {
                if (Rank == 1) return PowerType.Attack;
                if (Rank == 7 || Rank == 8) return PowerType.PeekRiver;
                if (Rank == 9 || Rank == 10) return PowerType.PeekOpponent;
                if (Rank == 11) return PowerType.Swap;
                if (Rank == 12) return PowerType.Peek;
                return PowerType.None;
            }
        }

        public override string ToString()
        {
            string valueStr = Rank switch
            {
                1 => "Ace",
                11 => "Jack",
                12 => "Queen",
                13 => "King",
                _ => Rank.ToString()
            };
            return $"{valueStr} of {Suit}";
        }

        public bool Equals(Card other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Rank == other.Rank && Suit == other.Suit;
        }

        public override bool Equals(object obj) => Equals(obj as Card);

        public override int GetHashCode() => HashCode.Combine(Rank, Suit);
    }
}
