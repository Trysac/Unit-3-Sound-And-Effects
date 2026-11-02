namespace BalloonDash.Core
{
    /// <summary>The project's gravity and a scene's scaled copy of it.</summary>
    public static class WorldGravity
    {
        /// <summary>Vertical gravity in the physics settings, in units per second squared.</summary>
        public const float Project = -9.81f;

        /// <summary>
        /// The vertical gravity a scene wants: always the project gravity times the scene's modifier,
        /// however often scenes were loaded before it.
        /// </summary>
        public static float Scaled(float modifier) => Project * modifier;
    }
}
