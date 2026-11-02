using System.Collections.Generic;

namespace BalloonDash.Core
{
    /// <summary>
    /// One run, without the engine: the track scrolls past the runner faster and faster, the
    /// runner jumps, slides and floats, coins and power-ups are collected, and the first obstacle
    /// or bomb touched without a shield ends it. Everything that happened in a step is reported in
    /// <see cref="Events"/> for the sounds, particles and HUD.
    /// </summary>
    public sealed class Run
    {
        readonly List<RunEvent> events = new List<RunEvent>();
        readonly List<Obstacle> hits = new List<Obstacle>();
        float nextMilestone = Tuning.Milestone;
        int placedId = 1000000;

        public Run(int seed)
        {
            Seed = seed;
            Track = new Track(seed);
            Track.Fill(Tuning.LayAhead);
        }

        public int Seed { get; }

        public Runner Runner { get; } = new Runner();

        public Track Track { get; }

        public IReadOnlyList<Obstacle> Obstacles => Track.Obstacles;

        public IReadOnlyList<Pickup> Pickups => Track.Pickups;

        public IReadOnlyList<RunEvent> Events => events;

        /// <summary>Metres run.</summary>
        public float Distance { get; private set; }

        /// <summary>Current scrolling speed in metres per second.</summary>
        public float Speed => SpeedCurve.At(Distance);

        public float Elapsed { get; private set; }

        public int Coins { get; private set; }

        public int Points => Scoring.Points(Distance, Coins);

        public float FloatLeft { get; private set; }

        public float MagnetLeft { get; private set; }

        public bool HasShield { get; private set; }

        /// <summary>Seconds of safety left after the shield took a hit.</summary>
        public float GraceLeft { get; private set; }

        public int PowerUpsCollected { get; private set; }

        public int Jumps { get; private set; }

        public int ObstaclesSmashed { get; private set; }

        public bool IsOver { get; private set; }

        public EndReason EndReason { get; private set; }

        /// <summary>False keeps the track empty (tests and the screenshot hook place things themselves).</summary>
        public bool Laying { get; set; } = true;

        public bool PowerUpRunning => FloatLeft > 0f || MagnetLeft > 0f;

        // ---- Input ----

        public void PressJump()
        {
            if (!IsOver)
            {
                Runner.PressJump();
            }
        }

        public void SetJumpHeld(bool held) => Runner.SetJumpHeld(held && !IsOver);

        public void PressSlide()
        {
            if (!IsOver)
            {
                Runner.PressSlide();
            }
        }

        // ---- Step ----

        public void Step(float dt)
        {
            if (IsOver || dt <= 0f)
            {
                return;
            }
            Elapsed += dt;
            Distance += Speed * dt;

            TickPowers(dt);
            int before = events.Count;
            Runner.Step(dt, Distance, events);
            for (int i = before; i < events.Count; i++)
            {
                if (events[i].Kind == RunEventKind.Jumped || events[i].Kind == RunEventKind.DoubleJumped)
                {
                    Jumps++;
                }
            }

            if (Laying)
            {
                Track.Fill(Distance + Tuning.LayAhead, !PowerUpRunning && !HasShield);
            }
            Roll(dt);
            Pull(dt);
            Collect();
            Collide();
            Track.RemoveBehind(Distance);

            while (!IsOver && Distance >= nextMilestone)
            {
                events.Add(new RunEvent(RunEventKind.Milestone, Distance, 0f, nextMilestone));
                nextMilestone += Tuning.Milestone;
            }
        }

        void TickPowers(float dt)
        {
            if (FloatLeft > 0f)
            {
                FloatLeft -= dt;
                if (FloatLeft <= 0f)
                {
                    FloatLeft = 0f;
                    Runner.StopFloating();
                    Track.EndAir(Distance);
                    events.Add(new RunEvent(RunEventKind.PowerEnded, Distance, Runner.Y + Runner.Height, 0f, pickup: PickupKind.Balloon));
                }
            }
            if (MagnetLeft > 0f)
            {
                MagnetLeft -= dt;
                if (MagnetLeft <= 0f)
                {
                    MagnetLeft = 0f;
                    events.Add(new RunEvent(RunEventKind.PowerEnded, Distance, Runner.Y + 0.75f, 0f, pickup: PickupKind.Magnet));
                }
            }
            if (GraceLeft > 0f)
            {
                GraceLeft = GraceLeft > dt ? GraceLeft - dt : 0f;
            }
        }

        /// <summary>Barrels roll towards the runner once they are on screen.</summary>
        void Roll(float dt)
        {
            foreach (Obstacle obstacle in Track.Obstacles)
            {
                if (obstacle.Roll > 0f && obstacle.X - Distance < Tuning.RollStart)
                {
                    obstacle.X -= obstacle.Roll * dt;
                }
            }
        }

        /// <summary>The magnet pulls in coins near the runner.</summary>
        void Pull(float dt)
        {
            if (MagnetLeft <= 0f)
            {
                return;
            }
            float cx = Distance;
            float cy = Runner.Y + Runner.Height * 0.5f;
            foreach (Pickup pickup in Track.Pickups)
            {
                if (pickup.Kind != PickupKind.Coin)
                {
                    continue;
                }
                float dx = cx - pickup.X;
                float dy = cy - pickup.Y;
                float distanceSquared = dx * dx + dy * dy;
                if (!pickup.Pulled && distanceSquared < Tuning.MagnetRadius * Tuning.MagnetRadius && dx < 1f)
                {
                    pickup.Pulled = true;
                }
                if (pickup.Pulled)
                {
                    float length = (float)System.Math.Sqrt(distanceSquared);
                    float move = (Tuning.MagnetPull + Speed) * dt;
                    if (length <= move || length < 1e-4f)
                    {
                        pickup.X = cx;
                        pickup.Y = cy;
                    }
                    else
                    {
                        pickup.X += dx / length * move;
                        pickup.Y += dy / length * move;
                    }
                }
            }
        }

        void Collect()
        {
            Box body = Runner.BoundsAt(Distance);
            for (int i = Track.Pickups.Count - 1; i >= 0; i--)
            {
                Pickup pickup = Track.Pickups[i];
                if (!body.Touches(pickup.X, pickup.Y, pickup.Radius))
                {
                    continue;
                }
                Track.Pickups.RemoveAt(i);
                if (pickup.Kind == PickupKind.Coin)
                {
                    Coins++;
                    events.Add(new RunEvent(RunEventKind.Coin, pickup.X, pickup.Y, Coins));
                }
                else
                {
                    Activate(pickup.Kind, pickup.X, pickup.Y);
                }
            }
        }

        /// <summary>Starts a power-up as if it had just been collected at a point.</summary>
        public void Activate(PickupKind kind, float x, float y)
        {
            PowerUpsCollected++;
            switch (kind)
            {
                case PickupKind.Balloon:
                    bool already = FloatLeft > 0f;
                    FloatLeft = Tuning.FloatSeconds;
                    Runner.StartFloating();
                    if (!already)
                    {
                        float until = Distance + Tuning.FloatSeconds * Speed;
                        foreach (Obstacle cleared in Track.BeginAir(Distance, until))
                        {
                            events.Add(new RunEvent(RunEventKind.Cleared, cleared.CenterX, cleared.CenterY, 0f, cleared.Kind));
                        }
                    }
                    break;
                case PickupKind.Magnet:
                    MagnetLeft = Tuning.MagnetSeconds;
                    break;
                case PickupKind.Shield:
                    HasShield = true;
                    break;
                default:
                    return;
            }
            events.Add(new RunEvent(RunEventKind.PowerUp, x, y, 0f, pickup: kind));
        }

        void Collide()
        {
            Box body = Runner.BoundsAt(Distance).Shrink(Tuning.Forgiveness);
            hits.Clear();
            foreach (Obstacle obstacle in Track.Obstacles)
            {
                bool hit = obstacle.Kind == ObstacleKind.Bomb
                    ? body.Touches(obstacle.CenterX, obstacle.CenterY, Tuning.BombRadius - Tuning.Forgiveness)
                    : body.Overlaps(obstacle.Bounds.Shrink(Tuning.Forgiveness));
                if (hit)
                {
                    hits.Add(obstacle);
                }
            }
            foreach (Obstacle obstacle in hits)
            {
                if (GraceLeft > 0f)
                {
                    continue;
                }
                if (HasShield)
                {
                    HasShield = false;
                    GraceLeft = Tuning.ShieldGrace;
                    ObstaclesSmashed++;
                    Track.Obstacles.Remove(obstacle);
                    events.Add(new RunEvent(RunEventKind.Smashed, obstacle.CenterX, obstacle.CenterY, 0f, obstacle.Kind));
                    continue;
                }
                bool bomb = obstacle.Kind == ObstacleKind.Bomb;
                if (bomb)
                {
                    Track.Obstacles.Remove(obstacle);
                }
                End(bomb ? EndReason.Exploded : EndReason.Crashed, obstacle);
                return;
            }
        }

        void End(EndReason reason, Obstacle obstacle)
        {
            IsOver = true;
            EndReason = reason;
            FloatLeft = 0f;
            MagnetLeft = 0f;
            Runner.StopFloating();
            float x = reason == EndReason.Exploded ? obstacle.CenterX : obstacle.X;
            float y = reason == EndReason.Exploded ? obstacle.CenterY : Runner.Y + Runner.Height * 0.5f;
            events.Add(new RunEvent(reason == EndReason.Exploded ? RunEventKind.Exploded : RunEventKind.Crashed, x, y, 0f, obstacle.Kind));
            events.Add(new RunEvent(RunEventKind.Over, Distance, Runner.Y));
        }

        public void ClearEvents() => events.Clear();

        // ---- Staging (tests and the screenshot hook) ----

        /// <summary>Puts an obstacle at a distance ahead of the runner (a bomb at a height).</summary>
        public Obstacle Place(ObstacleKind kind, float ahead, float y = 0f)
        {
            Obstacle obstacle = Obstacle.Make(kind, Distance + ahead, y);
            obstacle.Id = placedId++;
            Track.Obstacles.Add(obstacle);
            return obstacle;
        }

        /// <summary>Puts a coin or a power-up at a distance ahead of the runner and a height.</summary>
        public Pickup Place(PickupKind kind, float ahead, float y)
        {
            var pickup = new Pickup { Id = placedId++, Kind = kind, X = Distance + ahead, Y = y };
            Track.Pickups.Add(pickup);
            return pickup;
        }

        public void ClearTrack()
        {
            Track.Obstacles.Clear();
            Track.Pickups.Clear();
        }

        /// <summary>
        /// Moves the run forward (the screenshot hook and tests jump ahead): the track is cleared
        /// and laid out again from a short way in front of the runner.
        /// </summary>
        public void Skip(float metres)
        {
            Distance += metres;
            while (Distance >= nextMilestone)
            {
                nextMilestone += Tuning.Milestone;
            }
            Track.Restart(Distance + Tuning.FirstObstacle);
        }

        public void AddCoins(int count) => Coins += count;
    }
}
