namespace BalloonDash.Core
{
    /// <summary>
    /// A frame-driven replacement for InvokeRepeating: the first spawn is due after a delay and
    /// the next ones every interval, until the timer is stopped.
    /// </summary>
    public sealed class SpawnTimer
    {
        readonly float interval;
        float untilNext;

        public SpawnTimer(float delay, float interval)
        {
            this.interval = interval;
            untilNext = delay;
        }

        public bool Running { get; private set; } = true;

        /// <summary>Advances the timer and returns how many spawns fell due in this step.</summary>
        public int Tick(float deltaTime)
        {
            if (!Running)
            {
                return 0;
            }
            untilNext -= deltaTime;
            int due = 0;
            while (untilNext <= 0f)
            {
                due++;
                if (interval <= 0f)
                {
                    untilNext = 0f;
                    break;
                }
                untilNext += interval;
            }
            return due;
        }

        public void Stop() => Running = false;
    }
}
