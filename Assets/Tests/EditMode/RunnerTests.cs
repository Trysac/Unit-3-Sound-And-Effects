using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>The runner's jump, double jump, jump buffer, short hop, slide, dive and balloon float.</summary>
    public class RunnerTests
    {
        const float Dt = 1f / 120f;

        readonly List<RunEvent> events = new List<RunEvent>();

        static IEnumerable<RunEventKind> Kinds(List<RunEvent> list) => list.Select(e => e.Kind);

        /// <summary>Steps until the runner lands (or a time limit), returning the highest feet height seen.</summary>
        float StepUntilLanded(Runner runner, float limit = 3f)
        {
            float peak = runner.Y;
            for (float t = 0f; t < limit; t += Dt)
            {
                runner.Step(Dt, 0f, events);
                peak = System.Math.Max(peak, runner.Y);
                if (runner.Grounded)
                {
                    break;
                }
            }
            return peak;
        }

        void Steps(Runner runner, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-5f; t += Dt)
            {
                runner.Step(Dt, 0f, events);
            }
        }

        [SetUp]
        public void Clear() => events.Clear();

        [Test]
        public void AFullJumpReachesTheTunedHeightAndLands()
        {
            var runner = new Runner();
            runner.PressJump();
            float peak = StepUntilLanded(runner);

            Assert.That(peak, Is.EqualTo(Tuning.JumpHeight).Within(0.1f));
            Assert.That(runner.Grounded, Is.True);
            Assert.That(runner.Y, Is.EqualTo(0f));
            Assert.That(Kinds(events), Is.EqualTo(new[] { RunEventKind.Jumped, RunEventKind.Landed }));
            Assert.That(events[1].Value, Is.GreaterThan(Tuning.JumpVelocity * 0.9f), "the landing reports its impact speed");
        }

        [Test]
        public void LettingGoEarlyMakesAShortHop()
        {
            var full = new Runner();
            full.PressJump();
            float fullPeak = StepUntilLanded(full);

            var hop = new Runner();
            hop.PressJump();
            hop.Step(Dt, 0f, events);
            hop.ReleaseJump();
            float hopPeak = StepUntilLanded(hop);

            Assert.That(hopPeak, Is.LessThan(fullPeak * 0.4f));
            Assert.That(hopPeak, Is.GreaterThan(0.3f));
        }

        [Test]
        public void ThereIsOneDoubleJumpPerJump()
        {
            var runner = new Runner();
            runner.PressJump();
            Steps(runner, 0.3f);
            float before = runner.Y;
            runner.PressJump();
            runner.Step(Dt, 0f, events);
            Assert.That(runner.VelocityY, Is.EqualTo(Tuning.DoubleJumpVelocity - Tuning.Gravity * Dt).Within(1e-3f));
            Assert.That(runner.AirJumpsUsed, Is.EqualTo(1));

            Steps(runner, 0.1f);
            float velocity = runner.VelocityY;
            runner.PressJump();
            runner.Step(Dt, 0f, events);
            Assert.That(runner.VelocityY, Is.LessThan(velocity), "a third press does nothing in the air");
            Assert.That(Kinds(events).Count(k => k == RunEventKind.DoubleJumped), Is.EqualTo(1));
            Assert.That(runner.Y, Is.GreaterThan(before));
        }

        [Test]
        public void AJumpPressedJustBeforeLandingFiresOnLanding()
        {
            var runner = new Runner();
            runner.PressJump();
            Steps(runner, 0.05f);
            runner.PressJump(); // the double jump
            // Fall until about 0.08 s before touching down, then press again.
            while (runner.Y + runner.VelocityY * 0.08f > 0f)
            {
                runner.Step(Dt, 0f, events);
            }
            runner.PressJump();
            StepUntilLanded(runner);
            runner.Step(Dt, 0f, events);

            Assert.That(runner.Grounded, Is.False, "the buffered jump took off again");
            Assert.That(Kinds(events).Count(k => k == RunEventKind.Jumped), Is.EqualTo(2));
        }

        [Test]
        public void AJumpPressedTooLongBeforeLandingIsDropped()
        {
            var runner = new Runner();
            runner.PressJump();
            Steps(runner, 0.05f);
            runner.PressJump();
            while (runner.Y + runner.VelocityY * 0.3f > 0f)
            {
                runner.Step(Dt, 0f, events);
            }
            runner.PressJump();
            StepUntilLanded(runner);
            Steps(runner, 0.1f);

            Assert.That(runner.Grounded, Is.True);
            Assert.That(Kinds(events).Count(k => k == RunEventKind.Jumped), Is.EqualTo(1));
        }

        [Test]
        public void ASlideLowersTheHitBoxForItsDuration()
        {
            var runner = new Runner();
            runner.PressSlide();
            runner.Step(Dt, 5f, events);

            Assert.That(runner.Sliding, Is.True);
            Assert.That(runner.Height, Is.EqualTo(Tuning.SlideHeight));
            Assert.That(runner.BoundsAt(5f).Top, Is.EqualTo(Tuning.SlideHeight));
            Assert.That(runner.BoundsAt(5f).Left, Is.EqualTo(5f - Tuning.SlideWidth / 2f).Within(1e-5f));

            Steps(runner, Tuning.SlideSeconds + 2f * Dt);
            Assert.That(runner.Sliding, Is.False);
            Assert.That(runner.Height, Is.EqualTo(Tuning.StandHeight));
            Assert.That(Kinds(events), Is.EqualTo(new[] { RunEventKind.SlideStarted, RunEventKind.SlideEnded }));
        }

        [Test]
        public void JumpingOutOfASlideStandsUp()
        {
            var runner = new Runner();
            runner.PressSlide();
            runner.Step(Dt, 0f, events);
            runner.PressJump();
            runner.Step(Dt, 0f, events);

            Assert.That(runner.Sliding, Is.False);
            Assert.That(runner.Grounded, Is.False);
        }

        [Test]
        public void SlidingInTheAirDivesAndSlidesOnLanding()
        {
            var runner = new Runner();
            runner.PressJump();
            Steps(runner, 0.2f);
            runner.PressSlide();
            runner.Step(Dt, 0f, events);
            Assert.That(runner.VelocityY, Is.LessThanOrEqualTo(-Tuning.DiveSpeed));

            StepUntilLanded(runner);
            Assert.That(runner.Sliding, Is.True);
            Assert.That(Kinds(events), Is.EqualTo(new[] { RunEventKind.Jumped, RunEventKind.Dived, RunEventKind.Landed, RunEventKind.SlideStarted }));
        }

        [Test]
        public void HoldingJumpUnderTheBalloonRisesAndLettingGoSinks()
        {
            var runner = new Runner();
            runner.StartFloating();
            runner.SetJumpHeld(true);
            Steps(runner, 1f);
            float high = runner.Y;
            Assert.That(high, Is.GreaterThan(4f));
            Assert.That(runner.VelocityY, Is.LessThanOrEqualTo(Tuning.FloatMaxRise));

            runner.SetJumpHeld(false);
            Steps(runner, 0.8f);
            Assert.That(runner.Y, Is.LessThan(high));
            Assert.That(runner.VelocityY, Is.GreaterThanOrEqualTo(-Tuning.FloatMaxFall));
        }

        [Test]
        public void TheBalloonStopsAtTheCeiling()
        {
            var runner = new Runner();
            runner.StartFloating();
            runner.SetJumpHeld(true);
            Steps(runner, 3f);

            Assert.That(runner.Y, Is.EqualTo(Tuning.FloatCeiling));
            Assert.That(Kinds(events), Has.Member(RunEventKind.FloatCeiling));
        }

        [Test]
        public void TheBalloonBouncesOffTheGroundInsteadOfLanding()
        {
            var runner = new Runner();
            runner.StartFloating();
            Steps(runner, 3f);

            Assert.That(Kinds(events), Has.Member(RunEventKind.FloatBounced));
            Assert.That(Kinds(events), Has.No.Member(RunEventKind.Landed));
            Assert.That(runner.Grounded, Is.False);
            Assert.That(runner.Y, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void AfterTheBalloonTheRunnerFallsAndCanDoubleJumpAgain()
        {
            var runner = new Runner();
            runner.StartFloating();
            runner.SetJumpHeld(true);
            Steps(runner, 0.6f);
            runner.SetJumpHeld(false);
            runner.StopFloating();
            runner.PressJump();
            runner.Step(Dt, 0f, events);
            Assert.That(Kinds(events), Has.Member(RunEventKind.DoubleJumped));

            StepUntilLanded(runner);
            Assert.That(runner.Grounded, Is.True);
        }
    }
}
