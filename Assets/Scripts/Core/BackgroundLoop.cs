namespace BalloonDash.Core
{
    /// <summary>The rule that makes a scrolling backdrop look endless.</summary>
    public static class BackgroundLoop
    {
        /// <summary>
        /// Where a backdrop scrolling left should be: back at <paramref name="startX"/> once it has
        /// travelled one <paramref name="repeatWidth"/> (half its width, for a picture that repeats
        /// itself halfway), otherwise where it is.
        /// </summary>
        public static float Wrap(float x, float startX, float repeatWidth) => x < startX - repeatWidth ? startX : x;
    }
}
