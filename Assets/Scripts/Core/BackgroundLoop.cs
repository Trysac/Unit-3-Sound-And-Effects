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

        /// <summary>
        /// The offset of a parallax layer of tiles <paramref name="tileWidth"/> wide after the camera
        /// travelled <paramref name="travelled"/> metres, for a layer that moves at
        /// <paramref name="factor"/> times the track: always in (-tileWidth, 0], so two or three
        /// tiles side by side cover the screen without a gap.
        /// </summary>
        public static float Scroll(float travelled, float factor, float tileWidth)
        {
            if (tileWidth <= 0f)
            {
                return 0f;
            }
            float moved = travelled * factor % tileWidth;
            if (moved < 0f)
            {
                moved += tileWidth;
            }
            return moved <= 0f ? 0f : -moved;
        }
    }
}
