namespace BalloonDash.Core
{
    public enum RunEventKind
    {
        Jumped,
        DoubleJumped,
        /// <summary>The runner touched down; Value is the falling speed at impact.</summary>
        Landed,
        SlideStarted,
        SlideEnded,
        /// <summary>Slide pressed in the air: a fast drop.</summary>
        Dived,
        /// <summary>A coin was collected; Value is the run's coin count.</summary>
        Coin,
        /// <summary>A power-up was collected (<see cref="RunEvent.Pickup"/> says which).</summary>
        PowerUp,
        /// <summary>A timed power-up ran out (<see cref="RunEvent.Pickup"/> says which).</summary>
        PowerEnded,
        /// <summary>The balloon touched the ground and bounced back up.</summary>
        FloatBounced,
        /// <summary>The balloon hit the ceiling.</summary>
        FloatCeiling,
        /// <summary>The shield took a hit and smashed the obstacle (<see cref="RunEvent.Obstacle"/>).</summary>
        Smashed,
        /// <summary>Ground obstacles ahead were cleared for the balloon; one event per obstacle.</summary>
        Cleared,
        /// <summary>The runner hit an obstacle and the run is over.</summary>
        Crashed,
        /// <summary>The runner touched a bomb and the run is over.</summary>
        Exploded,
        /// <summary>Another 100 m; Value is the distance.</summary>
        Milestone,
        Over,
    }

    public enum EndReason
    {
        None,
        Crashed,
        Exploded,
    }

    /// <summary>Something that happened in a step, for sound, particles and the HUD.</summary>
    public readonly struct RunEvent
    {
        public readonly RunEventKind Kind;
        /// <summary>Where it happened, in track coordinates.</summary>
        public readonly float X;
        public readonly float Y;
        public readonly float Value;
        public readonly ObstacleKind Obstacle;
        public readonly PickupKind Pickup;

        public RunEvent(RunEventKind kind, float x, float y, float value = 0f,
            ObstacleKind obstacle = ObstacleKind.Crate, PickupKind pickup = PickupKind.Coin)
        {
            Kind = kind;
            X = x;
            Y = y;
            Value = value;
            Obstacle = obstacle;
            Pickup = pickup;
        }
    }
}
