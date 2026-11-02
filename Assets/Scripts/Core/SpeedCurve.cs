using System;

namespace BalloonDash.Core
{
    /// <summary>
    /// How fast the track scrolls: it starts at <see cref="Tuning.StartSpeed"/> and eases towards
    /// <see cref="Tuning.MaxSpeed"/> as the distance grows, quickly at first and then slower.
    /// Speed depends on distance only, so a track laid out ahead knows the speed it will be run at.
    /// </summary>
    public static class SpeedCurve
    {
        public static float At(float distance)
        {
            if (distance <= 0f)
            {
                return Tuning.StartSpeed;
            }
            float gap = Tuning.MaxSpeed - Tuning.StartSpeed;
            return Tuning.MaxSpeed - gap * (float)Math.Exp(-distance / Tuning.SpeedRamp);
        }

        /// <summary>The speed as a 0..1 fraction of the way from the start speed to the top speed.</summary>
        public static float Fraction(float distance) =>
            (At(distance) - Tuning.StartSpeed) / (Tuning.MaxSpeed - Tuning.StartSpeed);
    }

    /// <summary>A run's points: one per metre and <see cref="Tuning.CoinValue"/> per coin.</summary>
    public static class Scoring
    {
        public static int Points(float distance, int coins) => (int)Math.Floor(Math.Max(distance, 0f)) + coins * Tuning.CoinValue;
    }
}
