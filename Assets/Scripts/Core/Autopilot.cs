namespace BalloonDash.Core
{
    /// <summary>
    /// A simple player that reads the track ahead and presses the buttons a person would: it runs
    /// the demo behind the title screen, and the tests use it to show every pattern can be cleared.
    /// It times each jump so its peak is over the obstacle, slides under signs, hops under a bomb,
    /// and steers the balloon between bombs.
    /// </summary>
    public sealed class Autopilot
    {
        /// <summary>Seconds from take-off to the top of a full jump.</summary>
        static float ToApex => Tuning.JumpVelocity / Tuning.Gravity;

        float releaseIn = -1f;
        float target = 3f;

        public void Drive(Run run, float dt)
        {
            if (run.IsOver)
            {
                return;
            }
            if (run.Runner.Floating)
            {
                Steer(run);
                return;
            }
            if (releaseIn >= 0f)
            {
                releaseIn -= dt;
                if (releaseIn < 0f)
                {
                    run.SetJumpHeld(false);
                }
            }

            Runner runner = run.Runner;
            float front = run.Distance + Tuning.StandWidth * 0.5f;
            Obstacle next = null;
            foreach (Obstacle obstacle in run.Obstacles)
            {
                bool grounded = obstacle.Kind != ObstacleKind.Bomb || obstacle.Y < 2f;
                if (!grounded || obstacle.X + obstacle.Width < run.Distance - Tuning.StandWidth * 0.5f)
                {
                    continue;
                }
                if (next == null || obstacle.X < next.X)
                {
                    next = obstacle;
                }
            }
            if (next == null || !runner.Grounded)
            {
                return;
            }

            float speed = run.Speed;
            float toCenter = next.CenterX - run.Distance;
            switch (next.Kind)
            {
                case ObstacleKind.Sign:
                    if (next.X - front < speed * 0.12f && !runner.Sliding)
                    {
                        run.PressSlide();
                    }
                    break;
                case ObstacleKind.Barrel:
                    if (toCenter < (speed + Tuning.RollSpeed) * ToApex)
                    {
                        Jump(run, -1f);
                    }
                    break;
                default:
                    if (Mined(run, next))
                    {
                        // A hop: let go early so the jump peaks between the crate and the bomb.
                        if (toCenter < speed * 0.22f)
                        {
                            Jump(run, 0.075f);
                        }
                    }
                    else if (toCenter < speed * ToApex)
                    {
                        Jump(run, -1f);
                    }
                    break;
            }
        }

        void Jump(Run run, float holdFor)
        {
            run.PressJump();
            run.SetJumpHeld(true);
            releaseIn = holdFor;
        }

        static bool Mined(Run run, Obstacle crate)
        {
            foreach (Obstacle obstacle in run.Obstacles)
            {
                if (obstacle.Kind == ObstacleKind.Bomb && obstacle.Y >= 2f && obstacle.CenterX > crate.X - 1f && obstacle.CenterX < crate.X + crate.Width + 1f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Under the balloon: for each height it could aim for, simulate the climb or sink towards
        /// it over the next second, measure how close that path passes the bombs coming up, aim
        /// for the height with the most room, and hold jump while below it.
        /// </summary>
        void Steer(Run run)
        {
            Runner runner = run.Runner;
            float bestRoom = float.MinValue;
            float best = target;
            for (float height = 0.3f; height <= Tuning.FloatCeiling; height += 0.25f)
            {
                float room = Room(run, height) - System.Math.Abs(height - target) * 0.01f;
                if (System.Math.Abs(height - target) < 0.13f)
                {
                    room += 0.15f; // Stick with the current plan unless another is clearly better.
                }
                if (room > bestRoom)
                {
                    bestRoom = room;
                    best = height;
                }
            }
            target = best;
            float predicted = runner.Y + runner.VelocityY * 0.1f;
            run.SetJumpHeld(predicted < target);
        }

        /// <summary>The smallest gap between the runner and a bomb on the way to a height (negative: a hit).</summary>
        static float Room(Run run, float height)
        {
            const float Step = 1f / 30f;
            float half = Tuning.StandHeight * 0.5f;
            float reach = Tuning.StandWidth * 0.5f + Tuning.BombRadius;
            float y = run.Runner.Y;
            float velocity = run.Runner.VelocityY;
            float room = 10f;
            for (float time = 0f; time < 1.1f; time += Step)
            {
                float x = run.Distance + run.Speed * time;
                foreach (Obstacle obstacle in run.Obstacles)
                {
                    if (obstacle.Kind != ObstacleKind.Bomb || System.Math.Abs(obstacle.CenterX - x) > reach)
                    {
                        continue;
                    }
                    float clearance = System.Math.Abs(obstacle.CenterY - (y + half)) - half - Tuning.BombRadius;
                    room = System.Math.Min(room, clearance);
                }
                bool rise = y + velocity * 0.1f < height;
                velocity += (rise ? Tuning.FloatLift : -Tuning.FloatGravity) * Step;
                velocity = velocity < -Tuning.FloatMaxFall ? -Tuning.FloatMaxFall : velocity > Tuning.FloatMaxRise ? Tuning.FloatMaxRise : velocity;
                y += velocity * Step;
                if (y > Tuning.FloatCeiling)
                {
                    y = Tuning.FloatCeiling;
                }
                if (y < 0f)
                {
                    y = 0f;
                    velocity = Tuning.FloatBounce;
                }
            }
            return room;
        }
    }
}
