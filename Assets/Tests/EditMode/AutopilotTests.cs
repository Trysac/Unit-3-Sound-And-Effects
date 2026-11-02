using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>
    /// The autopilot plays seeded runs from start to well past top speed. It only reads the track
    /// and presses buttons, so its runs show that the patterns, gaps and power-ups can be cleared.
    /// </summary>
    public class AutopilotTests
    {
        const float Dt = 1f / 60f;

        static Run Play(int seed, float metres, float limitSeconds = 400f)
        {
            var run = new Run(seed);
            var pilot = new Autopilot();
            for (float t = 0f; t < limitSeconds && !run.IsOver && run.Distance < metres; t += Dt)
            {
                pilot.Drive(run, Dt);
                run.Step(Dt);
                run.ClearEvents();
            }
            return run;
        }

        [Test]
        public void TheAutopilotRunsTwoKilometresOnEverySeed()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                Run run = Play(seed, 2000f);
                Assert.That(run.IsOver, Is.False, $"seed {seed} ended at {run.Distance:0} m: {run.EndReason}");
                Assert.That(run.Distance, Is.GreaterThanOrEqualTo(2000f));
            }
        }

        [Test]
        public void TheAutopilotCollectsCoinsAndPowerUpsOnTheWay()
        {
            Run run = Play(3, 2000f);
            Assert.That(run.Coins, Is.GreaterThan(20));
            Assert.That(run.PowerUpsCollected, Is.GreaterThan(0));
            Assert.That(run.Jumps, Is.GreaterThan(50));
        }

        [Test]
        public void WithoutInputTheFirstCrateEndsTheRun()
        {
            var run = new Run(3);
            for (int i = 0; i < 60 * 10 && !run.IsOver; i++)
            {
                run.Step(Dt);
            }
            Assert.That(run.IsOver, Is.True);
            Assert.That(run.EndReason, Is.EqualTo(EndReason.Crashed));
            Assert.That(run.Distance, Is.LessThan(Tuning.FirstObstacle + 1f));
        }
    }
}
