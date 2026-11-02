using BalloonDash.Core;
using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>The rules extracted from the 2021 course scripts.</summary>
    public class CourseRulesTests
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
        public void TheFirstSpawnWaitsForTheDelayAndTheNextOnesForTheInterval()
        {
            var timer = new SpawnTimer(1f, 3f);
            Assert.That(timer.Tick(0.75f), Is.EqualTo(0));
            Assert.That(timer.Tick(0.25f), Is.EqualTo(1));
            Assert.That(timer.Tick(2.5f), Is.EqualTo(0));
            Assert.That(timer.Tick(0.5f), Is.EqualTo(1));
            Assert.That(timer.Tick(6f), Is.EqualTo(2), "a long frame catches up");
        }

        [Test]
        public void AStoppedTimerSpawnsNothing()
        {
            var timer = new SpawnTimer(0f, 1f);
            timer.Stop();
            Assert.That(timer.Running, Is.False);
            Assert.That(timer.Tick(10f), Is.EqualTo(0));
        }

        [Test]
        public void TheRunnerJumpsOnlyFromTheGround()
        {
            var runner = new RunnerState();
            Assert.That(runner.OnGround, Is.True);
            Assert.That(runner.TryJump(), Is.True);
            Assert.That(runner.OnGround, Is.False);
            Assert.That(runner.TryJump(), Is.False, "no jump in the air");
            Assert.That(runner.Land(), Is.True, "landing starts the dust");
            Assert.That(runner.OnGround, Is.True);
            Assert.That(runner.TryJump(), Is.True);
        }

        [Test]
        public void ACrashEndsTheRunAndStopsJumping()
        {
            var runner = new RunnerState();
            runner.Crash();
            Assert.That(runner.GameOver, Is.True);
            Assert.That(runner.TryJump(), Is.False);
            Assert.That(runner.OnGround, Is.True);
        }

        [Test]
        public void SceneGravityIsTheProjectGravityTimesTheModifierEveryTime()
        {
            Assert.That(WorldGravity.Scaled(1f), Is.EqualTo(-9.81f));
            Assert.That(WorldGravity.Scaled(1.5f), Is.EqualTo(-14.715f).Within(1e-4f));
            Assert.That(WorldGravity.Scaled(1.5f), Is.EqualTo(WorldGravity.Scaled(1.5f)));
        }
    }
}
