using System.Linq;
using NUnit.Framework;

namespace BalloonDash.Core.Tests
{
    /// <summary>The seeded track: repeatable, fair gaps, patterns unlocked by distance, power-ups and the balloon's sky.</summary>
    public class TrackTests
    {
        static string Layout(Track track) =>
            string.Join(";", track.Obstacles.Select(o => $"{o.Kind}@{o.X:0.00},{o.Y:0.00}")) + "|" +
            string.Join(";", track.Pickups.Select(p => $"{p.Kind}@{p.X:0.00},{p.Y:0.00}"));

        [Test]
        public void TheSameSeedLaysTheSameTrack()
        {
            var a = new Track(42);
            var b = new Track(42);
            var c = new Track(43);
            a.Fill(3000f);
            b.Fill(3000f);
            c.Fill(3000f);

            Assert.That(Layout(a), Is.EqualTo(Layout(b)));
            Assert.That(Layout(a), Is.Not.EqualTo(Layout(c)));
        }

        [Test]
        public void TheFirstObstacleLeavesTimeToGetReady()
        {
            var track = new Track(1);
            track.Fill(200f);
            Assert.That(track.Obstacles.Min(o => o.X), Is.GreaterThanOrEqualTo(Tuning.FirstObstacle));
        }

        [Test]
        public void EveryGapBetweenPatternsIsLongEnoughToLandAndJumpAgain()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var track = new Track(seed);
                track.Fill(4000f);
                var groups = track.Obstacles
                    .Select(o => (o, Left: o.Kind == ObstacleKind.Barrel ? o.X - Track.RollShift(SpeedCurve.At(o.X)) : o.X))
                    .GroupBy(entry => entry.o.Group)
                    .Select(g => (Left: g.Min(e => e.Left), Right: g.Max(e => e.o.X + e.o.Width)))
                    .OrderBy(g => g.Left)
                    .ToList();
                for (int i = 1; i < groups.Count; i++)
                {
                    float gap = groups[i].Left - groups[i - 1].Right;
                    float needed = Tuning.Clearance(SpeedCurve.At(groups[i - 1].Right));
                    Assert.That(gap, Is.GreaterThanOrEqualTo(needed - 1e-3f), $"seed {seed}, gap before {groups[i].Left:0.0} m");
                }
            }
        }

        [Test]
        public void HarderPatternsAppearOnlyAfterTheirDistance()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var track = new Track(seed);
                track.Fill(3000f);
                foreach (Obstacle obstacle in track.Obstacles)
                {
                    if (obstacle.Kind == ObstacleKind.Sign)
                    {
                        Assert.That(obstacle.X, Is.GreaterThanOrEqualTo(Track.Unlocks[Pattern.Sign]));
                    }
                    if (obstacle.Kind == ObstacleKind.Stack)
                    {
                        Assert.That(obstacle.X, Is.GreaterThanOrEqualTo(Track.Unlocks[Pattern.Stack]));
                    }
                    if (obstacle.Kind == ObstacleKind.Barrel)
                    {
                        Assert.That(obstacle.X, Is.GreaterThanOrEqualTo(Track.Unlocks[Pattern.Barrel]));
                    }
                    if (obstacle.Kind == ObstacleKind.Bomb)
                    {
                        Assert.That(obstacle.X, Is.GreaterThanOrEqualTo(Track.Unlocks[Pattern.MinedCrate]));
                    }
                }
            }
            Assert.That(Track.Available(0f), Is.EqualTo(new[] { Pattern.Crate }));
            Assert.That(Track.Available(5000f).Count(), Is.EqualTo(8));
        }

        [Test]
        public void EveryPatternShowsUpOverALongRun()
        {
            var track = new Track(7);
            track.Fill(6000f);
            var kinds = track.Obstacles.Select(o => o.Kind).Distinct().ToList();
            Assert.That(kinds, Is.EquivalentTo(new[]
            {
                ObstacleKind.Crate, ObstacleKind.Stack, ObstacleKind.Spikes, ObstacleKind.Sign, ObstacleKind.Barrel, ObstacleKind.Bomb,
            }));
            Assert.That(track.Pickups.Count(p => p.Kind == PickupKind.Coin), Is.GreaterThan(100));
        }

        [Test]
        public void PowerUpsComeAFewHundredMetresApart()
        {
            var track = new Track(11);
            track.Fill(5000f);
            var powers = track.Pickups.Where(p => p.IsPowerUp).OrderBy(p => p.X).ToList();

            Assert.That(powers.Count, Is.InRange(7, 13));
            Assert.That(powers[0].X, Is.GreaterThanOrEqualTo(Tuning.FirstPowerUp));
            for (int i = 1; i < powers.Count; i++)
            {
                Assert.That(powers[i].X - powers[i - 1].X, Is.GreaterThanOrEqualTo(Tuning.PowerUpSpacingMin - 1f));
            }
            Assert.That(powers.Select(p => p.Kind).Distinct().Count(), Is.EqualTo(3), "balloon, magnet and shield all appear");
        }

        [Test]
        public void NoPowerUpIsLaidWhileOneRuns()
        {
            var track = new Track(11);
            track.Fill(5000f, powerUpsAllowed: false);
            Assert.That(track.Pickups.Any(p => p.IsPowerUp), Is.False);
        }

        [Test]
        public void TheBalloonClearsTheGroundAheadAndLaysBombsAndCoinsInTheSky()
        {
            var track = new Track(5);
            track.Fill(330f);
            int groundAhead = track.Obstacles.Count(o => o.X > 303f);
            var cleared = track.BeginAir(300f, 360f);
            track.Fill(420f);

            Assert.That(cleared.Count, Is.EqualTo(groundAhead));
            var sky = track.Obstacles.Where(o => o.X > 303f && o.X < 360f).ToList();
            Assert.That(sky, Is.Not.Empty);
            Assert.That(sky.All(o => o.Kind == ObstacleKind.Bomb), Is.True);
            Assert.That(sky.All(o => o.CenterY >= 1.2f - 1e-3f && o.CenterY <= Tuning.FloatCeiling + 2f), Is.True);
            Assert.That(track.Pickups.Count(p => p.X > 303f && p.X < 360f && p.Y > 1.3f), Is.GreaterThan(0), "coin clouds");
        }

        [Test]
        public void TheGroundResumesAfterARunwayBehindTheBalloon()
        {
            var track = new Track(5);
            track.Fill(330f);
            track.BeginAir(300f, 360f);
            track.Fill(600f);

            float runway = Tuning.AirRunway * SpeedCurve.At(360f);
            var ground = track.Obstacles.Where(o => o.Kind != ObstacleKind.Bomb && o.X > 303f).ToList();
            Assert.That(ground, Is.Not.Empty);
            Assert.That(ground.Min(o => o.X), Is.GreaterThanOrEqualTo(360f + runway - 1e-3f));
        }

        [Test]
        public void ABalloonThatEndsEarlyTakesItsBombsWithIt()
        {
            var track = new Track(5);
            track.Fill(330f);
            track.BeginAir(300f, 360f);
            track.Fill(360f);
            track.EndAir(320f);

            Assert.That(track.Obstacles.Any(o => o.Kind == ObstacleKind.Bomb && o.X > 322f && o.X < 360f), Is.False);
            Assert.That(track.Airborne(330f), Is.False);
        }
    }
}
