namespace BalloonDash.Core
{
    /// <summary>
    /// Every number that shapes a run, in world units (one unit is one metre of track) and seconds.
    /// The runner stands at track position <see cref="Run.Distance"/> with its feet at height 0.
    /// </summary>
    public static class Tuning
    {
        // ---- Runner ----
        public const float Gravity = 42f;
        public const float MaxFallSpeed = 24f;
        public const float JumpVelocity = 15f;
        public const float DoubleJumpVelocity = 12.5f;
        /// <summary>Letting go of jump while still rising this fast cuts the jump short.</summary>
        public const float JumpCutVelocity = 6f;
        /// <summary>A jump pressed this long before landing still fires on landing.</summary>
        public const float JumpBuffer = 0.12f;
        public const float SlideSeconds = 0.65f;
        /// <summary>Pressing slide in the air dives at least this fast, then slides on landing.</summary>
        public const float DiveSpeed = 22f;
        public const float StandWidth = 0.8f;
        public const float StandHeight = 1.5f;
        public const float SlideWidth = 1.1f;
        public const float SlideHeight = 0.7f;
        /// <summary>Hit boxes are this much smaller on each side than they look, so close calls feel fair.</summary>
        public const float Forgiveness = 0.08f;

        // ---- Balloon (Challenge 3's float mode) ----
        public const float FloatSeconds = 7f;
        public const float FloatGravity = 14f;
        /// <summary>Upward acceleration while jump is held under the balloon.</summary>
        public const float FloatLift = 36f;
        public const float FloatMaxRise = 7f;
        public const float FloatMaxFall = 8f;
        public const float FloatCeiling = 6.2f;
        /// <summary>Touching the ground under the balloon bounces back up at this speed.</summary>
        public const float FloatBounce = 7f;

        // ---- Speed ----
        public const float StartSpeed = 9f;
        public const float MaxSpeed = 22f;
        /// <summary>The distance over which the gap to top speed shrinks by a factor of e.</summary>
        public const float SpeedRamp = 900f;

        // ---- Track ----
        /// <summary>How far ahead of the runner the track is laid out (beyond the right edge of the screen).</summary>
        public const float LayAhead = 30f;
        /// <summary>Objects this far behind the runner are removed.</summary>
        public const float KeepBehind = 10f;
        public const float FirstObstacle = 16f;
        /// <summary>Time to land and react before the next obstacle, on top of a full jump.</summary>
        public const float Recovery = 0.3f;
        public const float RollSpeed = 3f;
        /// <summary>A barrel starts rolling when it is this far ahead of the runner (on screen).</summary>
        public const float RollStart = 14f;
        public const float AirRunway = 1.6f;

        // ---- Pickups and power-ups ----
        public const int CoinValue = 10;
        public const float CoinRadius = 0.3f;
        public const float PowerRadius = 0.45f;
        public const float BombRadius = 0.45f;
        public const float MagnetSeconds = 8f;
        public const float MagnetRadius = 4f;
        public const float MagnetPull = 22f;
        public const float ShieldGrace = 1f;
        public const float FirstPowerUp = 280f;
        public const float PowerUpSpacingMin = 380f;
        public const float PowerUpSpacingMax = 560f;
        public const float Milestone = 100f;

        /// <summary>How long a full jump from the ground stays in the air.</summary>
        public static float JumpAirtime => 2f * JumpVelocity / Gravity;

        /// <summary>The height of a full jump from the ground.</summary>
        public static float JumpHeight => JumpVelocity * JumpVelocity / (2f * Gravity);

        /// <summary>The shortest gap between two obstacles at a speed: a full jump plus time to react.</summary>
        public static float Clearance(float speed) => speed * (JumpAirtime + Recovery);
    }
}
