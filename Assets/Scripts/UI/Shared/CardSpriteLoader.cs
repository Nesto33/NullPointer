using System.Collections.Generic;
using UnityEngine;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>
    /// Loads card/power artwork from Resources/ by convention, so no per-card Inspector
    /// wiring is needed. Card face filenames match the original assets exactly, e.g.
    /// "Resources/Cards/Heart/7 of Heart.png".
    /// </summary>
    public static class CardSpriteLoader
    {
        private static readonly Dictionary<string, Texture2D> Cache = new();

        public static Texture2D GetCardFace(int rank, string suit)
        {
            return Load($"Cards/{suit}/{rank} of {suit}");
        }

        public static Texture2D GetCardBack()
        {
            return Load("Cards/back");
        }

        /// <summary>Accepts a Card.PowerType name (e.g. "PeekRiver") and maps it to the
        /// original asset's SCREAMING_SNAKE_CASE filename (e.g. "PEEK_RIVER.png").</summary>
        public static Texture2D GetPowerIcon(string powerTypeName)
        {
            string fileName = powerTypeName switch
            {
                "PeekRiver" => "PEEK_RIVER",
                "PeekOpponent" => "PEEK_OPPONENT",
                "Peek" => "PEEK",
                "Swap" => "SWAP",
                "Attack" => "ATTACK",
                _ => powerTypeName,
            };
            return Load($"Powers/{fileName}");
        }

        public static Texture2D GetTableBackground()
        {
            return Load("Backgrounds/table_background");
        }

        private static Texture2D Load(string resourcePath)
        {
            if (Cache.TryGetValue(resourcePath, out Texture2D cached)) return cached;

            Texture2D loaded = Resources.Load<Texture2D>(resourcePath);
            Cache[resourcePath] = loaded;
            if (loaded == null)
            {
                Debug.LogWarning($"CardSpriteLoader: no texture found at Resources/{resourcePath}");
            }
            return loaded;
        }
    }
}
