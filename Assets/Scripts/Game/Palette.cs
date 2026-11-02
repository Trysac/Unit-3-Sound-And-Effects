using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>The sunset colours shared by the backdrop, the runner, the track, the effects and the HUD.</summary>
    public static class Palette
    {
        public static readonly Color SkyTop = Hex("#2b1a5e");
        public static readonly Color SkyMiddle = Hex("#a8418f");
        public static readonly Color SkyHorizon = Hex("#ff9e5e");
        public static readonly Color Sun = Hex("#ffe08a");
        public static readonly Color FarHills = Hex("#7a3f8f");
        public static readonly Color MidHills = Hex("#57306f");
        public static readonly Color NearHills = Hex("#3a2352");
        public static readonly Color Cloud = Hex("#ffc6c0");
        public static readonly Color Grass = Hex("#58d68d");
        public static readonly Color GrassDark = Hex("#2f9e6b");
        public static readonly Color Dirt = Hex("#6b3d52");
        public static readonly Color DirtDark = Hex("#4a2840");
        public static readonly Color Dust = Hex("#f2c6a0");

        public static readonly Color Runner = Hex("#ff7a45");
        public static readonly Color RunnerDark = Hex("#c94f2a");
        public static readonly Color Scarf = Hex("#46c8ff");
        public static readonly Color Ink = Hex("#22163d");

        public static readonly Color Wood = Hex("#c98a4b");
        public static readonly Color WoodDark = Hex("#8a5530");
        public static readonly Color Steel = Hex("#dfe6f5");
        public static readonly Color Hazard = Hex("#ffd84a");
        public static readonly Color Bomb = Hex("#262236");

        public static readonly Color Gold = Hex("#ffd84a");
        public static readonly Color Balloon = Hex("#ff4f6d");
        public static readonly Color Magnet = Hex("#b06bff");
        public static readonly Color Shield = Hex("#5ef2ff");
        public static readonly Color Danger = Hex("#ff3b4f");
        public static readonly Color Text = Hex("#fff6ef");
        public static readonly Color Tangerine = Hex("#ffa53d");

        /// <summary>The colour of a power-up, for its badge, particles, chips and banners.</summary>
        public static Color Of(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Balloon: return Balloon;
                case PickupKind.Magnet: return Magnet;
                case PickupKind.Shield: return Shield;
                default: return Gold;
            }
        }

        public static Color WithAlpha(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
    }
}
