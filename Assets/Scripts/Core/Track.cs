using System;
using System.Collections.Generic;

namespace BalloonDash.Core
{
    /// <summary>The obstacle patterns, unlocked as the run gets longer.</summary>
    public enum Pattern
    {
        Crate,
        CratePair,
        Spikes,
        Sign,
        Stack,
        Barrel,
        /// <summary>A crate with a bomb hovering above it: a low hop, not a full jump.</summary>
        MinedCrate,
        /// <summary>A sign followed closely by a crate: slide, then jump.</summary>
        SignCrate,
    }

    /// <summary>
    /// Lays out the track ahead of the runner from a seed: obstacle patterns separated by gaps that
    /// are always long enough to land and jump again at the speed the runner will have there,
    /// coin lines and arcs, a power-up every few hundred metres, and bombs with coin clouds
    /// while the balloon floats. The same seed always lays out the same track.
    /// </summary>
    public sealed class Track
    {
        /// <summary>Distance at which each pattern starts to appear.</summary>
        public static readonly IReadOnlyDictionary<Pattern, float> Unlocks = new Dictionary<Pattern, float>
        {
            { Pattern.Crate, 0f },
            { Pattern.CratePair, 120f },
            { Pattern.Spikes, 180f },
            { Pattern.Sign, 240f },
            { Pattern.Stack, 360f },
            { Pattern.Barrel, 480f },
            { Pattern.SignCrate, 640f },
            { Pattern.MinedCrate, 800f },
        };

        static readonly Pattern[] AllPatterns = (Pattern[])Enum.GetValues(typeof(Pattern));

        readonly Rng rng;
        int nextId = 1;
        float airUntil = float.NegativeInfinity;
        float groundFrom;
        int group;

        public Track(int seed)
        {
            rng = new Rng(seed);
            NextX = Tuning.FirstObstacle;
            NextPowerUp = Tuning.FirstPowerUp;
        }

        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public readonly List<Pickup> Pickups = new List<Pickup>();

        /// <summary>Where the next pattern starts.</summary>
        public float NextX { get; private set; }

        /// <summary>The distance after which the next power-up may appear.</summary>
        public float NextPowerUp { get; private set; }

        /// <summary>True while the stretch being laid out is for the balloon.</summary>
        public bool Airborne(float x) => x < airUntil;

        /// <summary>Lays out patterns until the track reaches <paramref name="upTo"/>.</summary>
        /// <param name="powerUpsAllowed">False while a power-up runs, so none is laid out.</param>
        public void Fill(float upTo, bool powerUpsAllowed = true)
        {
            while (NextX < upTo)
            {
                if (Airborne(NextX))
                {
                    LayAir();
                }
                else
                {
                    LayGround(powerUpsAllowed);
                }
            }
        }

        /// <summary>
        /// The balloon lifts the runner at <paramref name="from"/> until about <paramref name="until"/>:
        /// ground obstacles further than a short step ahead are cleared (and returned), bombs and
        /// coin clouds are laid out instead, and the ground resumes after a runway.
        /// </summary>
        public List<Obstacle> BeginAir(float from, float until)
        {
            var cleared = new List<Obstacle>();
            for (int i = Obstacles.Count - 1; i >= 0; i--)
            {
                if (Obstacles[i].X > from + 3f)
                {
                    cleared.Add(Obstacles[i]);
                    Obstacles.RemoveAt(i);
                }
            }
            Pickups.RemoveAll(p => p.X > from + 3f && p.IsPowerUp);
            airUntil = until;
            groundFrom = until + Tuning.AirRunway * SpeedCurve.At(until);
            // The first bomb comes after a moment to get used to the balloon.
            NextX = from + Math.Max(8f, SpeedCurve.At(from) * 0.9f);
            return cleared;
        }

        /// <summary>The balloon ran out early or late: the ground resumes after a runway from <paramref name="at"/>.</summary>
        public void EndAir(float at)
        {
            if (airUntil > at)
            {
                // The balloon went early: drop the bombs and the high coins laid out for it.
                float until = airUntil;
                Obstacles.RemoveAll(o => o.Kind == ObstacleKind.Bomb && o.X > at + 2f && o.X < until);
                Pickups.RemoveAll(p => p.X > at + 2f && p.X < until && p.Y > 2.2f);
                groundFrom = at + Tuning.AirRunway * SpeedCurve.At(at);
                NextX = Math.Min(NextX, groundFrom);
            }
            airUntil = float.NegativeInfinity;
        }

        /// <summary>Clears the track and lays it out again from <paramref name="x"/>.</summary>
        public void Restart(float x)
        {
            Obstacles.Clear();
            Pickups.Clear();
            airUntil = float.NegativeInfinity;
            groundFrom = x;
            NextX = x;
            if (NextPowerUp < x)
            {
                NextPowerUp = x + Tuning.PowerUpSpacingMin * 0.5f;
            }
        }

        public void RemoveBehind(float x)
        {
            Obstacles.RemoveAll(o => o.X + o.Width < x - Tuning.KeepBehind);
            Pickups.RemoveAll(p => p.X < x - Tuning.KeepBehind);
        }

        /// <summary>The patterns that may appear at a distance.</summary>
        public static IEnumerable<Pattern> Available(float distance)
        {
            foreach (Pattern pattern in AllPatterns)
            {
                if (distance >= Unlocks[pattern])
                {
                    yield return pattern;
                }
            }
        }

        // ---- Ground ----

        void LayGround(bool powerUpsAllowed)
        {
            if (NextX < groundFrom)
            {
                NextX = groundFrom;
            }
            float x = NextX;
            float speed = SpeedCurve.At(x);
            float gapBefore = Tuning.Clearance(speed);
            if (powerUpsAllowed && x >= NextPowerUp)
            {
                // A power-up floats at head height in the middle of its own clear stretch.
                PickupKind kind = (PickupKind)(1 + rng.Range(0, 3));
                AddPickup(kind, x + gapBefore * 0.25f, 1.3f);
                NextPowerUp = x + rng.Range(Tuning.PowerUpSpacingMin, Tuning.PowerUpSpacingMax);
                NextX = x + gapBefore * 0.5f;
                return;
            }

            Pattern pattern = Pick(x);
            group++;
            float length = LayPattern(pattern, x);
            float end = x + length;
            float endSpeed = SpeedCurve.At(end);
            float gap = Tuning.Clearance(endSpeed) + rng.Range(0f, 0.6f) * endSpeed;
            if (rng.Value() < 0.55f)
            {
                LayCoinRun(end, gap);
            }
            NextX = end + gap;
        }

        Pattern Pick(float x)
        {
            // Newer patterns get a little more weight than the plain crate, so the mix keeps changing.
            float total = 0f;
            foreach (Pattern pattern in Available(x))
            {
                total += Weight(pattern);
            }
            float roll = rng.Range(0f, total);
            foreach (Pattern pattern in Available(x))
            {
                roll -= Weight(pattern);
                if (roll < 0f)
                {
                    return pattern;
                }
            }
            return Pattern.Crate;
        }

        static float Weight(Pattern pattern) => pattern == Pattern.Crate ? 1.4f : 1f;

        /// <summary>Lays one pattern at <paramref name="x"/> and returns its length along the track.</summary>
        float LayPattern(Pattern pattern, float x)
        {
            switch (pattern)
            {
                case Pattern.Crate:
                    AddObstacle(ObstacleKind.Crate, x);
                    LayArc(x + 0.5f, 1.9f);
                    return 1f;
                case Pattern.CratePair:
                    AddObstacle(ObstacleKind.Crate, x);
                    AddObstacle(ObstacleKind.Crate, x + 1.9f);
                    LayArc(x + 1.45f, 2.3f);
                    return 2.9f;
                case Pattern.Spikes:
                    AddObstacle(ObstacleKind.Spikes, x);
                    LayArc(x + 1.2f, 2f);
                    return 2.4f;
                case Pattern.Sign:
                    AddObstacle(ObstacleKind.Sign, x);
                    for (int i = 0; i < 3; i++)
                    {
                        AddPickup(PickupKind.Coin, x + 0.2f + i * 0.6f, 0.4f);
                    }
                    return 1.6f;
                case Pattern.Stack:
                    AddObstacle(ObstacleKind.Stack, x);
                    AddPickup(PickupKind.Coin, x + 0.5f, 3.2f);
                    return 1f;
                case Pattern.Barrel:
                    // The barrel starts rolling towards the runner once it is on screen, so it is
                    // laid as far out as it can roll at the slowest speed.
                    AddObstacle(ObstacleKind.Barrel, x + RollShift(Tuning.StartSpeed));
                    return RollShift(Tuning.StartSpeed) + 0.9f;
                case Pattern.MinedCrate:
                    AddObstacle(ObstacleKind.Crate, x);
                    AddObstacle(ObstacleKind.Bomb, x + 0.5f - Tuning.BombRadius, 3.1f);
                    AddPickup(PickupKind.Coin, x + 0.5f, 1.55f);
                    return 1f;
                default:
                    // Sign then crate: the crate sits a slide's length after the sign.
                    AddObstacle(ObstacleKind.Sign, x);
                    float crateAt = x + 1.6f + SpeedCurve.At(x) * (Tuning.SlideSeconds * 0.5f + Tuning.Recovery) + 0.5f;
                    AddObstacle(ObstacleKind.Crate, crateAt);
                    return crateAt + 1f - x;
            }
        }

        /// <summary>Coins along the top of a jump over something centred at <paramref name="centerX"/>.</summary>
        void LayArc(float centerX, float peak)
        {
            if (rng.Value() > 0.6f)
            {
                return;
            }
            float speed = SpeedCurve.At(centerX);
            float half = speed * Tuning.JumpAirtime * 0.32f;
            for (int i = -2; i <= 2; i++)
            {
                float t = i / 2f;
                AddPickup(PickupKind.Coin, centerX + t * half, 0.9f + (peak - 0.9f) * (1f - t * t));
            }
        }

        /// <summary>A line of coins in the gap after a pattern, on the ground or up where a jump reaches.</summary>
        void LayCoinRun(float start, float gap)
        {
            int count = 3 + rng.Range(0, 4);
            float spacing = 0.75f;
            float from = start + (gap - spacing * (count - 1)) * 0.5f;
            float height = rng.Value() < 0.3f ? 2.2f : 0.55f;
            for (int i = 0; i < count; i++)
            {
                AddPickup(PickupKind.Coin, from + i * spacing, height);
            }
        }

        /// <summary>
        /// How far a barrel rolls between appearing <see cref="Tuning.RollStart"/> ahead of the
        /// runner and reaching it, at a running speed.
        /// </summary>
        public static float RollShift(float speed) => Tuning.RollSpeed * Tuning.RollStart / (speed + Tuning.RollSpeed);

        // ---- Air (the balloon) ----

        void LayAir()
        {
            float x = NextX;
            float speed = SpeedCurve.At(x);
            group++;
            // No bombs in the last moments of the balloon, so the fall back down is clear.
            bool bombsAllowed = x < airUntil - speed * 0.8f;
            if (bombsAllowed && rng.Value() < 0.5f)
            {
                // A bomb at a random height, sometimes a pair with a gap to fly through.
                float y = rng.Range(1.2f, 5.6f);
                AddObstacle(ObstacleKind.Bomb, x, y - Tuning.BombRadius);
                if (rng.Value() < 0.35f)
                {
                    // The second bomb goes above or below, but never down at the ground or past the ceiling.
                    float other = y > 3.4f ? y - AirPairGap : y + AirPairGap;
                    if (other >= 1.2f && other <= Tuning.FloatCeiling + 0.2f)
                    {
                        AddObstacle(ObstacleKind.Bomb, x, other - Tuning.BombRadius);
                    }
                }
            }
            else
            {
                // A small cloud of coins.
                float y = rng.Range(1.4f, 5.4f);
                int count = 3 + rng.Range(0, 3);
                for (int i = 0; i < count; i++)
                {
                    AddPickup(PickupKind.Coin, x + (i % 3) * 0.7f, y + (i / 3) * 0.7f);
                }
            }
            // Far enough apart in time to climb or sink between them.
            NextX = x + speed * rng.Range(0.4f, 0.6f);
        }

        /// <summary>Centre-to-centre height of a pair of bombs: the gap between them fits the runner with room to spare.</summary>
        public const float AirPairGap = 3.4f;

        // ---- Helpers ----

        void AddObstacle(ObstacleKind kind, float x, float y = 0f)
        {
            Obstacle obstacle = Obstacle.Make(kind, x, y);
            obstacle.Id = nextId++;
            obstacle.Group = group;
            Obstacles.Add(obstacle);
        }

        void AddPickup(PickupKind kind, float x, float y)
        {
            Pickups.Add(new Pickup { Id = nextId++, Kind = kind, X = x, Y = y });
        }
    }
}
