using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>The endless backdrop: the course's jump-back rule and the parallax layers built on it.</summary>
    public class BackgroundLoopTests
    {
        [Test]
        public void TheBackdropStaysPutUntilItHasMovedOneRepeatWidth()
        {
            Assert.That(BackgroundLoop.Wrap(10f, 10f, 30f), Is.EqualTo(10f));
            Assert.That(BackgroundLoop.Wrap(-19.9f, 10f, 30f), Is.EqualTo(-19.9f));
            Assert.That(BackgroundLoop.Wrap(-20f, 10f, 30f), Is.EqualTo(-20f));
        }

        [Test]
        public void TheBackdropJumpsBackToItsStartPastOneRepeatWidth()
        {
            Assert.That(BackgroundLoop.Wrap(-20.1f, 10f, 30f), Is.EqualTo(10f));
            Assert.That(BackgroundLoop.Wrap(-60f, 10f, 30f), Is.EqualTo(10f));
        }

        [Test]
        public void AParallaxLayerMovesAtItsFactorAndWrapsEveryTile()
        {
            Assert.That(BackgroundLoop.Scroll(0f, 0.5f, 24f), Is.EqualTo(0f));
            Assert.That(BackgroundLoop.Scroll(10f, 0.5f, 24f), Is.EqualTo(-5f).Within(1e-4f));
            Assert.That(BackgroundLoop.Scroll(50f, 0.5f, 24f), Is.EqualTo(-1f).Within(1e-4f), "25 m of layer is one tile and one metre");
            Assert.That(BackgroundLoop.Scroll(48f, 0.5f, 24f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(BackgroundLoop.Scroll(-6f, 1f, 8f), Is.EqualTo(-2f).Within(1e-4f), "moving back wraps too");
            Assert.That(BackgroundLoop.Scroll(5f, 1f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void TheParallaxOffsetIsAlwaysWithinOneTile()
        {
            for (float travelled = 0f; travelled < 5000f; travelled += 13.37f)
            {
                float offset = BackgroundLoop.Scroll(travelled, 0.35f, 24f);
                Assert.That(offset, Is.LessThanOrEqualTo(0f).And.GreaterThan(-24f));
            }
        }
    }
}
