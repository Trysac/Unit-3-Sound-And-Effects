namespace BalloonDash.Core
{
    /// <summary>The runner's ground contact and game-over flag, as the 2021 PlayerController kept them.</summary>
    public sealed class RunnerState
    {
        public bool OnGround { get; private set; } = true;

        public bool GameOver { get; private set; }

        /// <summary>Takes off when the runner is on the ground and still alive; true if it jumped.</summary>
        public bool TryJump()
        {
            if (!OnGround || GameOver)
            {
                return false;
            }
            OnGround = false;
            return true;
        }

        /// <summary>The runner touches the ground; true when the running dust should start.</summary>
        public bool Land()
        {
            OnGround = true;
            return true;
        }

        public void Crash() => GameOver = true;
    }
}
