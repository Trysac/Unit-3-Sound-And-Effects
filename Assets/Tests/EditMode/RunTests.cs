using System.Linq;
using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>A whole run: speed and distance, coins and score, crashes, bombs and the power-ups.</summary>
    public class RunTests
    {
        const float Dt = 1f / 60f;

        /// <summary>A run with nothing on the track, so each test places what it needs.</summary>
        static Run Empty()
        {
            var run = new Run(3) { Laying = false };
            run.ClearTrack();
            return run;
        }

        static void Steps(Run run, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-5f; t += Dt)
            {
                run.Step(Dt);
            }
        }

        static bool Happened(Run run, RunEventKind kind) => run.Events.Any(e => e.Kind == kind);

        [Test]
        public void SpeedStartsSlowAndRisesTowardsTheTopSpeed()
        {
            Assert.That(SpeedCurve.At(0f), Is.EqualTo(Tuning.StartSpeed));
            float previous = 0f;
            for (float d = 0f; d <= 10000f; d += 250f)
            {
                float speed = SpeedCurve.At(d);
                Assert.That(speed, Is.GreaterThan(previous));
                Assert.That(speed, Is.LessThan(Tuning.MaxSpeed));
                previous = speed;
            }
            Assert.That(SpeedCurve.At(900f), Is.EqualTo(Tuning.MaxSpeed - (Tuning.MaxSpeed - Tuning.StartSpeed) / 2.7182818f).Within(0.01f));
            Assert.That(SpeedCurve.Fraction(0f), Is.EqualTo(0f));
            Assert.That(SpeedCurve.Fraction(10000f), Is.GreaterThan(0.99f));
        }

        [Test]
        public void PointsAreOnePerMetreAndTenPerCoin()
        {
            Assert.That(Scoring.Points(0f, 0), Is.EqualTo(0));
            Assert.That(Scoring.Points(123.9f, 0), Is.EqualTo(123));
            Assert.That(Scoring.Points(123.9f, 7), Is.EqualTo(193));
        }

        [Test]
        public void TheRunnerCoversDistanceAtTheCurrentSpeed()
        {
            Run run = Empty();
            Steps(run, 2f);

            Assert.That(run.Distance, Is.EqualTo(Tuning.StartSpeed * 2f).Within(0.5f));
            Assert.That(run.Elapsed, Is.EqualTo(2f).Within(0.02f));
            Assert.That(run.Points, Is.EqualTo((int)run.Distance));
            Assert.That(run.IsOver, Is.False);
        }

        [Test]
        public void EveryHundredMetresIsAMilestone()
        {
            Run run = Empty();
            int milestones = 0;
            float last = 0f;
            for (int i = 0; i < 60 * 25; i++)
            {
                run.Step(Dt);
                foreach (RunEvent e in run.Events.Where(e => e.Kind == RunEventKind.Milestone))
                {
                    milestones++;
                    last = e.Value;
                }
                run.ClearEvents();
            }
            Assert.That(milestones, Is.EqualTo((int)(run.Distance / 100f)));
            Assert.That(last, Is.EqualTo(milestones * 100f));
        }

        [Test]
        public void RunningIntoACrateEndsTheRun()
        {
            Run run = Empty();
            run.Place(ObstacleKind.Crate, 3f);
            Steps(run, 1f);

            Assert.That(run.IsOver, Is.True);
            Assert.That(run.EndReason, Is.EqualTo(EndReason.Crashed));
            RunEvent crash = run.Events.First(e => e.Kind == RunEventKind.Crashed);
            Assert.That(crash.Obstacle, Is.EqualTo(ObstacleKind.Crate));
            Assert.That(run.Events.Last().Kind, Is.EqualTo(RunEventKind.Over));

            float distance = run.Distance;
            run.PressJump();
            Steps(run, 0.5f);
            Assert.That(run.Distance, Is.EqualTo(distance), "nothing moves after the run is over");
            Assert.That(run.Runner.Grounded, Is.True);
        }

        [Test]
        public void JumpingOverACrateKeepsTheRunGoing()
        {
            Run run = Empty();
            run.Place(ObstacleKind.Crate, 3f);
            run.PressJump();
            Steps(run, 1.2f);

            Assert.That(run.IsOver, Is.False);
            Assert.That(run.Jumps, Is.EqualTo(1));
        }

        [Test]
        public void SlidingUnderASignKeepsTheRunGoingAndStandingDoesNot()
        {
            Run sliding = Empty();
            sliding.Place(ObstacleKind.Sign, 1.5f);
            sliding.PressSlide();
            Steps(sliding, 0.6f);
            Assert.That(sliding.IsOver, Is.False);

            Run standing = Empty();
            standing.Place(ObstacleKind.Sign, 1.5f);
            Steps(standing, 0.6f);
            Assert.That(standing.IsOver, Is.True);
        }

        [Test]
        public void ABarrelRollsTowardsTheRunnerOnceItIsOnScreen()
        {
            Run run = Empty();
            Obstacle far = run.Place(ObstacleKind.Barrel, 25f);
            Obstacle near = run.Place(ObstacleKind.Barrel, 12f);
            float farX = far.X;
            float nearX = near.X;
            run.Step(0.1f);

            Assert.That(far.X, Is.EqualTo(farX));
            Assert.That(near.X, Is.EqualTo(nearX - Tuning.RollSpeed * 0.1f).Within(1e-4f));
        }

        [Test]
        public void TouchingABombBlowsUpTheRun()
        {
            Run run = Empty();
            run.Place(ObstacleKind.Bomb, 2f, 0.3f);
            Steps(run, 0.5f);

            Assert.That(run.EndReason, Is.EqualTo(EndReason.Exploded));
            Assert.That(Happened(run, RunEventKind.Exploded), Is.True);
            Assert.That(run.Obstacles, Is.Empty, "the bomb is gone");
        }

        [Test]
        public void CoinsAreCollectedAndScoreTenPointsEach()
        {
            Run run = Empty();
            for (int i = 0; i < 5; i++)
            {
                run.Place(PickupKind.Coin, 1f + i * 0.7f, 0.6f);
            }
            run.Place(PickupKind.Coin, 2f, 3.5f);
            Steps(run, 0.6f);

            Assert.That(run.Coins, Is.EqualTo(5), "the high coin is out of reach");
            Assert.That(run.Points, Is.EqualTo((int)run.Distance + 50));
            Assert.That(run.Events.Where(e => e.Kind == RunEventKind.Coin).Select(e => (int)e.Value), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(run.Pickups.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheShieldTakesOneHitThenGivesAMomentOfGrace()
        {
            Run run = Empty();
            run.Place(PickupKind.Shield, 1f, 1f);
            run.Place(ObstacleKind.Crate, 4f);
            run.Place(ObstacleKind.Crate, 5.2f);
            Steps(run, 0.6f);
            Assert.That(run.HasShield, Is.False, "used on the first crate");
            Assert.That(run.IsOver, Is.False, "the second crate came during the grace");
            Assert.That(run.ObstaclesSmashed, Is.EqualTo(1));
            Assert.That(Happened(run, RunEventKind.Smashed), Is.True);
            Assert.That(run.Events.First(e => e.Kind == RunEventKind.PowerUp).Pickup, Is.EqualTo(PickupKind.Shield));

            run.ClearEvents();
            Steps(run, Tuning.ShieldGrace);
            run.Place(ObstacleKind.Crate, 2f);
            Steps(run, 0.5f);
            Assert.That(run.IsOver, Is.True);
        }

        [Test]
        public void TheMagnetPullsInCoinsNearbyForItsDuration()
        {
            Run run = Empty();
            run.Activate(PickupKind.Magnet, 0f, 1f);
            run.Place(PickupKind.Coin, 3f, 3.4f);
            run.Place(PickupKind.Coin, 2f, -0f + 3.8f);
            Steps(run, 0.5f);
            Assert.That(run.Coins, Is.EqualTo(2), "coins above a jump are pulled in");
            Assert.That(run.MagnetLeft, Is.EqualTo(Tuning.MagnetSeconds - 0.5f).Within(0.02f));

            Steps(run, Tuning.MagnetSeconds);
            Assert.That(run.MagnetLeft, Is.EqualTo(0f));
            Assert.That(run.Events.Any(e => e.Kind == RunEventKind.PowerEnded && e.Pickup == PickupKind.Magnet), Is.True);
            run.Place(PickupKind.Coin, 3f, 3.4f);
            Steps(run, 0.5f);
            Assert.That(run.Coins, Is.EqualTo(2), "no pull once it ran out");
        }

        [Test]
        public void TheBalloonFloatsTheRunnerForItsDurationThenLetsGo()
        {
            var run = new Run(9);
            run.Skip(300f);
            run.Laying = false;
            run.ClearTrack();
            run.ClearEvents();
            run.Activate(PickupKind.Balloon, run.Distance, 1f);
            Assert.That(run.Runner.Floating, Is.True);
            Assert.That(run.FloatLeft, Is.EqualTo(Tuning.FloatSeconds));
            Assert.That(run.PowerUpRunning, Is.True);
            Assert.That(run.Track.Airborne(run.Distance + 10f), Is.True);

            run.SetJumpHeld(true);
            Steps(run, 2f);
            Assert.That(run.Runner.Y, Is.GreaterThan(3f));
            run.SetJumpHeld(false);
            Steps(run, Tuning.FloatSeconds);
            Assert.That(run.Runner.Floating, Is.False);
            Assert.That(run.Events.Any(e => e.Kind == RunEventKind.PowerEnded && e.Pickup == PickupKind.Balloon), Is.True);
        }

        [Test]
        public void NoPowerUpIsLaidOutWhileOneRunsOrTheShieldIsUp()
        {
            foreach (PickupKind power in new[] { PickupKind.Magnet, PickupKind.Shield })
            {
                var run = new Run(4);
                run.Activate(power, 0f, 1f);
                run.Skip(Tuning.FirstPowerUp - 20f);
                Steps(run, 1.1f);
                Assert.That(run.IsOver, Is.False);
                Assert.That(run.Track.NextX, Is.GreaterThan(Tuning.FirstPowerUp), "the track was laid past the first power-up");
                Assert.That(run.Pickups.Any(p => p.IsPowerUp), Is.False, power.ToString());
            }

            var free = new Run(4);
            free.Skip(Tuning.FirstPowerUp - 20f);
            Steps(free, 1.1f);
            Assert.That(free.Pickups.Any(p => p.IsPowerUp), Is.True, "one is laid once nothing runs");
        }

        [Test]
        public void TheSameSeedAndInputsPlayTheSameRun()
        {
            string Play(int seed)
            {
                var run = new Run(seed);
                for (int i = 0; i < 60 * 30 && !run.IsOver; i++)
                {
                    if (i % 47 == 0)
                    {
                        run.PressJump();
                    }
                    if (i % 47 == 20)
                    {
                        run.SetJumpHeld(false);
                    }
                    run.Step(Dt);
                }
                return $"{run.Distance:0.000} {run.Coins} {run.IsOver} {run.EndReason}";
            }

            Assert.That(Play(21), Is.EqualTo(Play(21)));
        }
    }
}
