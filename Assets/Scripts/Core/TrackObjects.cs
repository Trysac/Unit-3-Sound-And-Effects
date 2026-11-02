namespace BalloonDash.Core
{
    /// <summary>An axis-aligned box in track coordinates (x along the track, y up from the ground).</summary>
    public readonly struct Box
    {
        public readonly float Left;
        public readonly float Bottom;
        public readonly float Right;
        public readonly float Top;

        public Box(float left, float bottom, float right, float top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        public float CenterX => (Left + Right) * 0.5f;
        public float CenterY => (Bottom + Top) * 0.5f;

        public bool Overlaps(Box other) => Left < other.Right && other.Left < Right && Bottom < other.Top && other.Bottom < Top;

        public Box Shrink(float amount) => new Box(Left + amount, Bottom + amount, Right - amount, Top - amount);

        /// <summary>True when a circle touches the box.</summary>
        public bool Touches(float x, float y, float radius)
        {
            float nx = x < Left ? Left : x > Right ? Right : x;
            float ny = y < Bottom ? Bottom : y > Top ? Top : y;
            float dx = x - nx;
            float dy = y - ny;
            return dx * dx + dy * dy < radius * radius;
        }
    }

    public enum ObstacleKind
    {
        /// <summary>A wooden crate: jump it.</summary>
        Crate,
        /// <summary>Two crates stacked: a full jump or a double jump.</summary>
        Stack,
        /// <summary>A long low row of spikes: jump far.</summary>
        Spikes,
        /// <summary>A hanging sign: slide under it.</summary>
        Sign,
        /// <summary>A barrel that rolls towards the runner.</summary>
        Barrel,
        /// <summary>A floating bomb (Challenge 3): anywhere in the air, it blows up on touch.</summary>
        Bomb,
    }

    public sealed class Obstacle
    {
        public int Id;
        /// <summary>The pattern it belongs to: obstacles of one pattern are laid out together.</summary>
        public int Group;
        public ObstacleKind Kind;
        /// <summary>Left edge along the track.</summary>
        public float X;
        public float Y;
        public float Width;
        public float Height;
        /// <summary>Extra speed towards the runner (rolling barrels).</summary>
        public float Roll;

        public Box Bounds => new Box(X, Y, X + Width, Y + Height);

        public float CenterX => X + Width * 0.5f;
        public float CenterY => Y + Height * 0.5f;

        /// <summary>The size and height every obstacle of a kind has (a bomb's y is its centre minus its radius).</summary>
        public static Obstacle Make(ObstacleKind kind, float x, float y = 0f)
        {
            var obstacle = new Obstacle { Kind = kind, X = x, Y = y };
            switch (kind)
            {
                case ObstacleKind.Crate:
                    obstacle.Width = 1f;
                    obstacle.Height = 1f;
                    break;
                case ObstacleKind.Stack:
                    obstacle.Width = 1f;
                    obstacle.Height = 2f;
                    break;
                case ObstacleKind.Spikes:
                    obstacle.Width = 2.4f;
                    obstacle.Height = 0.45f;
                    break;
                case ObstacleKind.Sign:
                    obstacle.Width = 1.6f;
                    obstacle.Y = 1.05f;
                    obstacle.Height = 2.2f;
                    break;
                case ObstacleKind.Barrel:
                    obstacle.Width = 0.9f;
                    obstacle.Height = 0.9f;
                    obstacle.Roll = Tuning.RollSpeed;
                    break;
                case ObstacleKind.Bomb:
                    obstacle.Width = Tuning.BombRadius * 2f;
                    obstacle.Height = Tuning.BombRadius * 2f;
                    break;
            }
            return obstacle;
        }
    }

    public enum PickupKind
    {
        Coin,
        /// <summary>Floats the runner under a balloon for a while (Challenge 3).</summary>
        Balloon,
        /// <summary>Pulls in nearby coins for a while.</summary>
        Magnet,
        /// <summary>Takes one hit.</summary>
        Shield,
    }

    public sealed class Pickup
    {
        public int Id;
        public PickupKind Kind;
        /// <summary>Centre along the track.</summary>
        public float X;
        /// <summary>Centre height.</summary>
        public float Y;
        /// <summary>A magnet is pulling it in.</summary>
        public bool Pulled;

        public float Radius => Kind == PickupKind.Coin ? Tuning.CoinRadius : Tuning.PowerRadius;

        public bool IsPowerUp => Kind != PickupKind.Coin;
    }
}
