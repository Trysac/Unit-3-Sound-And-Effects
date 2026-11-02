using System.Collections.Generic;

namespace BalloonDash.Core
{
    /// <summary>
    /// The runner's vertical motion: jump with a buffer, a double jump, a short hop when jump is
    /// let go early, a slide, a dive from the air into a slide, and floating under the balloon.
    /// The runner never moves along the track by itself; <see cref="Run"/> scrolls the track.
    /// </summary>
    public sealed class Runner
    {
        bool slidePressed;
        bool diving;
        bool canCut;

        /// <summary>Height of the feet above the ground.</summary>
        public float Y { get; private set; }

        public float VelocityY { get; private set; }

        public bool Grounded { get; private set; } = true;

        public float SlideLeft { get; private set; }

        public bool Sliding => SlideLeft > 0f;

        /// <summary>Air jumps used since the last time the feet left the ground (one is allowed).</summary>
        public int AirJumpsUsed { get; private set; }

        /// <summary>Seconds a jump press stays buffered.</summary>
        public float BufferLeft { get; private set; }

        public bool JumpHeld { get; private set; }

        /// <summary>Under the balloon: holding jump rises, letting go sinks, the ground bounces.</summary>
        public bool Floating { get; private set; }

        public float Width => Sliding ? Tuning.SlideWidth : Tuning.StandWidth;

        public float Height => Sliding ? Tuning.SlideHeight : Tuning.StandHeight;

        /// <summary>The hit box with its feet centred on track position <paramref name="x"/>.</summary>
        public Box BoundsAt(float x) => new Box(x - Width * 0.5f, Y, x + Width * 0.5f, Y + Height);

        public void PressJump()
        {
            BufferLeft = Tuning.JumpBuffer;
            JumpHeld = true;
        }

        public void ReleaseJump() => JumpHeld = false;

        public void SetJumpHeld(bool held) => JumpHeld = held;

        public void PressSlide() => slidePressed = true;

        public void StartFloating()
        {
            Floating = true;
            Grounded = false;
            SlideLeft = 0f;
            diving = false;
            VelocityY = VelocityY < 4f ? 4f : VelocityY;
        }

        public void StopFloating()
        {
            Floating = false;
            AirJumpsUsed = 0;
            canCut = false;
        }

        public void Step(float dt, float x, List<RunEvent> events)
        {
            HandleSlide(dt, x, events);
            HandleJump(x, events);

            if (Floating)
            {
                VelocityY += (JumpHeld ? Tuning.FloatLift : -Tuning.FloatGravity) * dt;
                VelocityY = Clamp(VelocityY, -Tuning.FloatMaxFall, Tuning.FloatMaxRise);
            }
            else
            {
                if (canCut && !JumpHeld && VelocityY > Tuning.JumpCutVelocity)
                {
                    VelocityY = Tuning.JumpCutVelocity;
                    canCut = false;
                }
                if (!Grounded)
                {
                    VelocityY -= Tuning.Gravity * dt;
                    if (VelocityY < -Tuning.MaxFallSpeed && !diving)
                    {
                        VelocityY = -Tuning.MaxFallSpeed;
                    }
                }
            }

            Y += VelocityY * dt;
            if (Floating && Y > Tuning.FloatCeiling)
            {
                Y = Tuning.FloatCeiling;
                if (VelocityY > 0f)
                {
                    VelocityY = 0f;
                    events.Add(new RunEvent(RunEventKind.FloatCeiling, x, Y + Height));
                }
            }
            if (Y <= 0f)
            {
                Touchdown(x, events);
            }

            BufferLeft = BufferLeft > dt ? BufferLeft - dt : 0f;
        }

        void HandleSlide(float dt, float x, List<RunEvent> events)
        {
            if (slidePressed)
            {
                slidePressed = false;
                if (Grounded)
                {
                    if (!Sliding)
                    {
                        events.Add(new RunEvent(RunEventKind.SlideStarted, x, 0f));
                    }
                    SlideLeft = Tuning.SlideSeconds;
                }
                else if (VelocityY > -Tuning.DiveSpeed)
                {
                    VelocityY = -Tuning.DiveSpeed;
                    diving = !Floating;
                    canCut = false;
                    events.Add(new RunEvent(RunEventKind.Dived, x, Y));
                }
            }
            else if (Sliding)
            {
                SlideLeft -= dt;
                if (SlideLeft <= 0f)
                {
                    SlideLeft = 0f;
                    events.Add(new RunEvent(RunEventKind.SlideEnded, x, 0f));
                }
            }
        }

        void HandleJump(float x, List<RunEvent> events)
        {
            if (BufferLeft <= 0f || Floating)
            {
                return;
            }
            if (Grounded)
            {
                Grounded = false;
                VelocityY = Tuning.JumpVelocity;
                AirJumpsUsed = 0;
                canCut = true;
                BufferLeft = 0f;
                if (Sliding)
                {
                    SlideLeft = 0f;
                }
                events.Add(new RunEvent(RunEventKind.Jumped, x, 0f));
            }
            else if (AirJumpsUsed < 1)
            {
                AirJumpsUsed++;
                VelocityY = Tuning.DoubleJumpVelocity;
                canCut = false;
                diving = false;
                BufferLeft = 0f;
                events.Add(new RunEvent(RunEventKind.DoubleJumped, x, Y));
            }
        }

        void Touchdown(float x, List<RunEvent> events)
        {
            float impact = -VelocityY;
            Y = 0f;
            if (Floating)
            {
                VelocityY = Tuning.FloatBounce;
                events.Add(new RunEvent(RunEventKind.FloatBounced, x, 0f, impact));
                return;
            }
            VelocityY = 0f;
            if (Grounded)
            {
                return;
            }
            Grounded = true;
            AirJumpsUsed = 0;
            canCut = false;
            events.Add(new RunEvent(RunEventKind.Landed, x, 0f, impact));
            if (diving)
            {
                diving = false;
                SlideLeft = Tuning.SlideSeconds;
                events.Add(new RunEvent(RunEventKind.SlideStarted, x, 0f));
            }
        }

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
