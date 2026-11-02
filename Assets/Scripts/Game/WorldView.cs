using System.Collections.Generic;
using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>
    /// Draws a run: a sunset backdrop in parallax layers, the ground, every obstacle and pickup
    /// near the runner (matched by id and pooled), and the runner itself, a rig of sprites that
    /// runs, tucks, flips, slides, dangles from the balloon and tumbles when it crashes. It also
    /// kicks up the running dust and reports each footstep so the director can play it.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        const float RunnerX = CameraRig.RunnerX;
        const float GroundTile = 8f;
        const float HillTile = 24f;

        sealed class Shown
        {
            public GameObject Root;
            public SpriteRenderer Main;
            public SpriteRenderer Extra;
            public SpriteRenderer Glow;
            public int Kind;
            public bool Seen;
        }

        sealed class Layer
        {
            public Transform[] Tiles;
            public float Factor;
            public float Width;
        }

        readonly Dictionary<int, Shown> obstacles = new Dictionary<int, Shown>();
        readonly Dictionary<int, Shown> pickups = new Dictionary<int, Shown>();
        readonly Dictionary<int, Stack<Shown>> pool = new Dictionary<int, Stack<Shown>>();
        readonly List<int> gone = new List<int>();
        readonly List<Layer> layers = new List<Layer>();
        readonly List<(Transform Cloud, float Speed)> clouds = new List<(Transform, float)>();
        readonly List<SpriteRenderer> streaks = new List<SpriteRenderer>();
        readonly float[] streakSpeed = new float[8];

        GameArt art;
        Transform sky;
        Transform sun;
        Transform objectsRoot;
        ParticleSystem dust;

        // The runner rig.
        Transform runnerRoot;
        Transform pose;
        SpriteRenderer body;
        Transform legFront;
        Transform legBack;
        Transform armFront;
        Transform armBack;
        readonly Transform[] scarf = new Transform[3];
        Transform balloon;
        Transform balloonString;
        SpriteRenderer shadow;
        SpriteRenderer shield;
        SpriteRenderer shieldGlow;
        SpriteRenderer magnetRing;

        float time;
        float phase;
        float squash;
        float flip;
        float travelled;
        float dustClock;
        float tumble;
        float tumbleVelocity;
        float tumbleHeight;
        bool wasOver;
        int lastStepSign = 1;

        /// <summary>True on a frame when a foot hit the ground.</summary>
        public bool Stepped { get; private set; }

        public int ObstaclesShown => obstacles.Count;

        public int PickupsShown => pickups.Count;

        /// <summary>The runner's feet in world space.</summary>
        public Vector2 Feet => runnerRoot != null ? (Vector2)runnerRoot.position : Vector2.zero;

        /// <summary>The middle of the runner in world space.</summary>
        public Vector2 Middle => Feet + new Vector2(0f, 0.75f);

        public void Build(GameArt gameArt)
        {
            art = gameArt;
            CreateBackdrop();
            objectsRoot = new GameObject("Track").transform;
            objectsRoot.SetParent(transform, false);
            CreateRunner();
            dust = Effects.Emitter("Dust", art != null ? art.Puff : null, 9);
            dust.transform.SetParent(transform, false);
            for (int i = 0; i < streakSpeed.Length; i++)
            {
                SpriteRenderer streak = Sprite("Speed Line", Shapes.Streak, Color.white.WithAlpha(0f), 25);
                streaks.Add(streak);
                ResetStreak(i, true);
            }
        }

        /// <summary>Converts a track position to world space.</summary>
        public static Vector2 ToWorld(Run run, float x, float y) => new Vector2(x - run.Distance + RunnerX, y);

        // ---- Backdrop ----

        void CreateBackdrop()
        {
            sky = Sprite("Sky", Shapes.Sky, Color.white, -60).transform;
            sky.localScale = new Vector3(40f, 16f, 1f);
            sun = Sprite("Sun", Shapes.SunDisc, Color.white, -58).transform;
            sun.localScale = Vector3.one * 9f;

            var random = new System.Random(3);
            for (int i = 0; i < 6; i++)
            {
                SpriteRenderer cloud = Sprite("Cloud", Shapes.CloudPuff, Palette.Cloud.WithAlpha(0.35f + (float)random.NextDouble() * 0.3f), -57);
                float scale = 0.8f + (float)random.NextDouble() * 1.2f;
                cloud.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
                cloud.transform.localPosition = new Vector3(-12f + i * 5f + (float)random.NextDouble() * 2f, 5f + (float)random.NextDouble() * 4f, 0f);
                clouds.Add((cloud.transform, 0.15f + (float)random.NextDouble() * 0.35f));
            }

            AddLayer("Far Hills", Shapes.Hills("FarHills", HillTile, 6.5f, Palette.FarHills, 11, 0.5f), 0.08f, -0.5f, -56);
            AddLayer("Mid Hills", Shapes.Hills("MidHills", HillTile, 5f, Palette.MidHills, 23, 0.8f), 0.22f, -0.8f, -55);
            AddLayer("Near Hills", Shapes.Hills("NearHills", HillTile, 3.6f, Palette.NearHills, 37, 1.2f), 0.5f, -1f, -54);
            AddLayer("Ground", Shapes.Ground, 1f, 0.02f, -10, GroundTile, 4);
        }

        void AddLayer(string name, Sprite sprite, float factor, float y, int order, float width = HillTile, int copies = 3)
        {
            var layer = new Layer { Factor = factor, Width = width, Tiles = new Transform[copies] };
            for (int i = 0; i < copies; i++)
            {
                Transform tile = Sprite(name, sprite, Color.white, order).transform;
                tile.localPosition = new Vector3(0f, y, 0f);
                layer.Tiles[i] = tile;
            }
            layers.Add(layer);
        }

        void ScrollBackdrop(float cameraY)
        {
            float left = -CameraRig.HalfWidth - 1.5f;
            foreach (Layer layer in layers)
            {
                float offset = BackgroundLoop.Scroll(travelled, layer.Factor, layer.Width);
                for (int i = 0; i < layer.Tiles.Length; i++)
                {
                    Vector3 p = layer.Tiles[i].localPosition;
                    layer.Tiles[i].localPosition = new Vector3(left + offset + i * layer.Width, p.y, 0f);
                }
            }
            sky.localPosition = new Vector3(0f, cameraY + 1.5f, 0f);
            sun.localPosition = new Vector3(3.2f, 4.6f + (cameraY - CameraRig.BaseY) * 0.7f, 0f);
        }

        void MoveClouds(float dt, float speed)
        {
            for (int i = 0; i < clouds.Count; i++)
            {
                (Transform cloud, float drift) = clouds[i];
                Vector3 p = cloud.localPosition;
                p.x -= (drift + speed * 0.04f) * dt;
                if (p.x < -14f)
                {
                    p.x += 30f;
                }
                cloud.localPosition = p;
            }
        }

        // ---- Runner ----

        void CreateRunner()
        {
            runnerRoot = new GameObject("Runner").transform;
            runnerRoot.SetParent(transform, false);
            shadow = Sprite("Shadow", Shapes.SoftDot, Palette.Ink.WithAlpha(0.35f), 8, transform);
            pose = new GameObject("Pose").transform;
            pose.SetParent(runnerRoot, false);

            legBack = Limb("Leg Back", new Vector3(-0.1f, 0.42f, 0f), 10, Palette.Ink * 0.8f);
            armBack = Limb("Arm Back", new Vector3(-0.05f, 0.95f, 0f), 10, Palette.Ink * 0.8f);
            body = Sprite("Body", Shapes.RunnerBody, Color.white, 12, pose);
            body.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            body.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            legFront = Limb("Leg Front", new Vector3(0.1f, 0.42f, 0f), 13, Palette.Ink);
            // Both arms sit behind the body, so they never cross the face.
            armFront = Limb("Arm Front", new Vector3(0.08f, 0.92f, 0f), 11, Palette.Ink);
            Transform neck = new GameObject("Neck").transform;
            neck.SetParent(pose, false);
            neck.localPosition = new Vector3(-0.3f, 1.0f, 0f);
            Transform parent = neck;
            for (int i = 0; i < scarf.Length; i++)
            {
                SpriteRenderer strip = Sprite("Scarf", Shapes.ScarfTail, Color.white, 11, parent);
                // The first strip turns to point backwards; the others hang off its end.
                strip.transform.localPosition = i == 0 ? Vector3.zero : new Vector3(0.42f, 0f, 0f);
                strip.transform.localScale = i == 0 ? Vector3.one : Vector3.one * 0.92f;
                scarf[i] = strip.transform;
                parent = strip.transform;
            }

            balloonString = Sprite("String", Shapes.Square, Palette.Text.WithAlpha(0.85f), 9, runnerRoot).transform;
            balloon = Sprite("Balloon", Shapes.Balloon, Color.white, 15, runnerRoot).transform;
            balloon.gameObject.SetActive(false);
            balloonString.gameObject.SetActive(false);

            shieldGlow = Sprite("Shield Glow", Shapes.SoftDot, Palette.Shield.WithAlpha(0f), 16, runnerRoot);
            shieldGlow.sharedMaterial = art != null && art.Glow != null ? art.Glow : shieldGlow.sharedMaterial;
            shieldGlow.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            shieldGlow.transform.localScale = Vector3.one * 2.6f;
            shield = Sprite("Shield", Shapes.Ring, Palette.Shield.WithAlpha(0f), 17, runnerRoot);
            shield.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            shield.transform.localScale = Vector3.one * 2.1f;
            magnetRing = Sprite("Magnet Range", Shapes.Ring, Palette.Magnet.WithAlpha(0f), 7, runnerRoot);
            magnetRing.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            magnetRing.transform.localScale = Vector3.one * Tuning.MagnetRadius * 2f;
        }

        Transform Limb(string name, Vector3 at, int order, Color color)
        {
            SpriteRenderer limb = Sprite(name, Shapes.Limb, color.WithAlpha(1f), order, pose);
            limb.transform.localPosition = at;
            return limb.transform;
        }

        /// <summary>A take-off or landing squash: positive stretches up, negative squashes down.</summary>
        public void Squash(float amount) => squash = Mathf.Clamp(amount, -0.45f, 0.45f);

        /// <summary>Starts the forward somersault of a double jump.</summary>
        public void Flip() => flip = 1f;

        void PoseRunner(Run run, float dt, bool live)
        {
            Runner runner = run.Runner;
            float y = runner.Y;
            if (run.IsOver)
            {
                if (!wasOver)
                {
                    // Knocked back off the obstacle into a tumble.
                    tumbleVelocity = run.EndReason == EndReason.Exploded ? 9f : 6f;
                    tumbleHeight = y;
                    tumble = 0f;
                }
                tumbleVelocity -= 30f * dt;
                tumbleHeight = Mathf.Max(0f, tumbleHeight + tumbleVelocity * dt);
                if (tumbleHeight <= 0f)
                {
                    tumbleVelocity = Mathf.Abs(tumbleVelocity) > 2f ? -tumbleVelocity * 0.35f : 0f;
                }
                tumble = Mathf.MoveTowards(tumble, 1f, dt * 1.6f);
                y = tumbleHeight;
            }
            wasOver = run.IsOver;
            runnerRoot.localPosition = new Vector3(RunnerX - (run.IsOver ? tumble * 1.2f : 0f), y, 0f);

            bool floating = runner.Floating && !run.IsOver;
            bool grounded = runner.Grounded && !run.IsOver;
            float speed = run.IsOver ? 0f : run.Speed;
            Stepped = false;
            if (grounded && !runner.Sliding && live)
            {
                phase += speed * dt * 1.05f;
                int sign = Mathf.Sin(phase) >= 0f ? 1 : -1;
                if (sign != lastStepSign)
                {
                    Stepped = true;
                    lastStepSign = sign;
                }
            }

            squash = Mathf.MoveTowards(squash, 0f, dt * 2.4f);
            flip = Mathf.MoveTowards(flip, 0f, dt / 0.38f);
            float bob = grounded && !runner.Sliding ? Mathf.Abs(Mathf.Sin(phase)) * 0.07f : 0f;
            float stretch = 1f + squash;
            float lean;
            float legSwing;
            float armSwing;
            if (run.IsOver)
            {
                lean = -tumble * 300f;
                legSwing = 50f;
                armSwing = 120f;
            }
            else if (runner.Sliding)
            {
                // Leaning right back, feet first.
                lean = -68f;
                legSwing = 20f;
                armSwing = -40f;
            }
            else if (floating)
            {
                lean = Mathf.Sin(time * 2.2f) * 5f;
                legSwing = Mathf.Sin(time * 3.1f) * 18f;
                armSwing = 160f;
            }
            else if (!grounded)
            {
                lean = Mathf.Clamp(-runner.VelocityY * 1.1f, -14f, 14f) - (1f - Mathf.Pow(1f - flip, 2f)) * 360f * (flip > 0f ? 1f : 0f);
                legSwing = 40f;
                armSwing = runner.VelocityY > 0f ? 150f : 70f;
            }
            else
            {
                lean = -6f - SpeedCurve.Fraction(run.Distance) * 8f;
                legSwing = Mathf.Sin(phase) * 48f;
                armSwing = -Mathf.Sin(phase) * 55f;
            }

            pose.localPosition = new Vector3(0f, bob + (runner.Sliding ? 0.05f : 0f), 0f);
            // Rotate around the hips so a somersault spins in place.
            pose.localRotation = Quaternion.Euler(0f, 0f, -lean);
            pose.localScale = new Vector3(1f / Mathf.Sqrt(stretch), stretch, 1f);
            if (runner.Sliding && !run.IsOver)
            {
                pose.localPosition = new Vector3(0.45f, 0.08f, 0f);
            }
            else if (flip > 0f && !grounded)
            {
                pose.localPosition += Quaternion.Euler(0f, 0f, -lean) * new Vector3(0f, -0.75f, 0f) + new Vector3(0f, 0.75f, 0f);
            }

            bool tucked = !grounded && !floating && !run.IsOver && flip <= 0f;
            legFront.localRotation = Quaternion.Euler(0f, 0f, legSwing);
            legBack.localRotation = Quaternion.Euler(0f, 0f, tucked ? legSwing - 30f : -legSwing);
            armFront.localRotation = Quaternion.Euler(0f, 0f, armSwing);
            armBack.localRotation = Quaternion.Euler(0f, 0f, floating ? armSwing - 15f : -armSwing);

            // The scarf trails straight back whatever the pose, and flutters faster with speed.
            float flutter = time * (6f + speed * 0.5f);
            for (int i = 0; i < scarf.Length; i++)
            {
                float angle = Mathf.Sin(flutter - i * 0.9f) * (8f + i * 6f) + (i == 0 ? 180f + lean + runner.VelocityY * 0.8f : 0f);
                scarf[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            balloon.gameObject.SetActive(floating);
            balloonString.gameObject.SetActive(floating);
            if (floating)
            {
                float sway = Mathf.Sin(time * 1.7f) * 0.18f;
                balloon.localPosition = new Vector3(0.1f + sway, 2.55f + Mathf.Sin(time * 2.3f) * 0.06f, 0f);
                balloon.localRotation = Quaternion.Euler(0f, 0f, -sway * 25f);
                // The balloon shrinks in quick beats when it is about to go.
                float warning = run.FloatLeft < 1.5f && Mathf.Repeat(time * 6f, 1f) < 0.35f ? 0.9f : 1f;
                balloon.localScale = Vector3.one * (0.95f + 0.04f * Mathf.Sin(time * 5f)) * warning;
                Vector3 hand = new Vector3(0.12f, 1.62f, 0f);
                Vector3 knot = balloon.localPosition + new Vector3(0f, 0.12f, 0f);
                Vector3 mid = (hand + knot) * 0.5f;
                balloonString.localPosition = mid;
                Vector3 along = knot - hand;
                balloonString.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
                balloonString.localScale = new Vector3(along.magnitude * 10f, 0.25f, 1f);
            }

            float height = Mathf.Max(0f, y);
            shadow.transform.localPosition = new Vector3(runnerRoot.localPosition.x, 0.02f, 0f);
            float shadowSize = Mathf.Lerp(1.3f, 0.4f, Mathf.Clamp01(height / 5f));
            shadow.transform.localScale = new Vector3(shadowSize, shadowSize * 0.22f, 1f);
            shadow.color = Palette.Ink.WithAlpha(0.35f * Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(height / 5f)));

            bool shielded = run.HasShield || run.GraceLeft > 0f;
            float blink = run.GraceLeft > 0f && Mathf.Repeat(time * 12f, 1f) < 0.5f ? 0.2f : 1f;
            shield.color = Palette.Shield.WithAlpha(shielded ? 0.85f * blink : 0f);
            shieldGlow.color = Palette.Shield.WithAlpha(shielded ? 0.25f * blink : 0f);
            shield.transform.localScale = Vector3.one * (2.1f + Mathf.Sin(time * 4f) * 0.05f);
            float pulse = Mathf.Repeat(time * 1.3f, 1f);
            magnetRing.color = Palette.Magnet.WithAlpha(run.MagnetLeft > 0f && !run.IsOver ? 0.5f * (1f - pulse) : 0f);
            magnetRing.transform.localScale = Vector3.one * Tuning.MagnetRadius * 2f * (0.6f + 0.4f * pulse);
        }

        void KickDust(Run run, float dt)
        {
            Runner runner = run.Runner;
            if (run.IsOver || !runner.Grounded || dust == null)
            {
                return;
            }
            float rate = runner.Sliding ? 0.018f : 0.07f;
            dustClock -= dt;
            if (dustClock > 0f)
            {
                return;
            }
            dustClock = rate * Random.Range(0.7f, 1.3f);
            Vector2 at = Feet + new Vector2(runner.Sliding ? 0.2f : -0.15f, 0.05f);
            var velocity = new Vector2(-run.Speed * Random.Range(0.55f, 0.85f), Random.Range(0.4f, runner.Sliding ? 2.2f : 1.4f));
            Color color = Color.Lerp(Palette.Dust, Palette.Hex("#e0a8a0"), Random.value).WithAlpha(runner.Sliding ? 0.85f : 0.6f);
            Effects.Puff(dust, at, velocity, color, Random.Range(0.18f, runner.Sliding ? 0.5f : 0.34f), Random.Range(0.3f, 0.55f));
        }

        // ---- Speed lines ----

        void ResetStreak(int i, bool anywhere)
        {
            Transform streak = streaks[i].transform;
            float x = anywhere ? Random.Range(-10f, 10f) : Random.Range(10f, 14f);
            streak.localPosition = new Vector3(x, Random.Range(0.4f, 8f), 0f);
            float length = Random.Range(0.8f, 2.2f);
            streak.localScale = new Vector3(length, Random.Range(0.6f, 1.2f), 1f);
            streakSpeed[i] = Random.Range(1.4f, 2.2f);
        }

        void MoveStreaks(Run run, float dt, float cameraY)
        {
            float fraction = run.IsOver ? 0f : Mathf.Clamp01((SpeedCurve.Fraction(run.Distance) - 0.3f) / 0.5f);
            if (run.Runner.Floating && !run.IsOver)
            {
                fraction = Mathf.Max(fraction, 0.5f);
            }
            for (int i = 0; i < streaks.Count; i++)
            {
                Transform streak = streaks[i].transform;
                Vector3 p = streak.localPosition;
                p.x -= run.Speed * streakSpeed[i] * dt;
                streak.localPosition = p;
                if (p.x < -14f)
                {
                    ResetStreak(i, false);
                    streak.localPosition += new Vector3(0f, cameraY - CameraRig.BaseY, 0f);
                }
                streaks[i].color = Color.white.WithAlpha(0.22f * fraction);
            }
        }

        // ---- Track objects ----

        /// <summary>Moves every sprite to the run's state; <paramref name="live"/> is false while paused.</summary>
        public void Sync(Run run, float dt, float cameraY, bool live)
        {
            if (!live)
            {
                dt = 0f;
            }
            time += dt;
            if (!run.IsOver)
            {
                travelled = run.Distance;
            }
            ScrollBackdrop(cameraY);
            MoveClouds(dt, run.IsOver ? 0f : run.Speed);
            MoveStreaks(run, dt, cameraY);
            SyncObstacles(run);
            SyncPickups(run);
            PoseRunner(run, dt, live);
            if (live)
            {
                KickDust(run, dt);
            }
        }

        void SyncObstacles(Run run)
        {
            foreach (Shown shown in obstacles.Values)
            {
                shown.Seen = false;
            }
            foreach (Obstacle obstacle in run.Obstacles)
            {
                Vector2 at = ToWorld(run, obstacle.X, obstacle.Y);
                if (at.x > CameraRig.HalfWidth + 6f || at.x + obstacle.Width < -CameraRig.HalfWidth - 6f)
                {
                    continue;
                }
                if (!obstacles.TryGetValue(obstacle.Id, out Shown shown))
                {
                    shown = Take((int)obstacle.Kind, () => MakeObstacle(obstacle.Kind));
                    obstacles[obstacle.Id] = shown;
                }
                shown.Seen = true;
                Transform root = shown.Root.transform;
                switch (obstacle.Kind)
                {
                    case ObstacleKind.Barrel:
                        root.localPosition = new Vector3(at.x + obstacle.Width * 0.5f, at.y + obstacle.Height * 0.5f, 0f);
                        // It rolls with the distance it travelled relative to the ground.
                        root.localRotation = Quaternion.Euler(0f, 0f, obstacle.X * 127f);
                        break;
                    case ObstacleKind.Bomb:
                        float bob = Mathf.Sin(time * 3f + obstacle.Id) * 0.06f;
                        root.localPosition = new Vector3(at.x + Tuning.BombRadius, at.y + Tuning.BombRadius + bob, 0f);
                        root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 2f + obstacle.Id) * 8f);
                        shown.Glow.color = Palette.Tangerine.WithAlpha(0.35f + 0.3f * Mathf.PerlinNoise(time * 12f, obstacle.Id));
                        break;
                    default:
                        root.localPosition = new Vector3(at.x, at.y, 0f);
                        break;
                }
            }
            Release(obstacles);
        }

        void SyncPickups(Run run)
        {
            foreach (Shown shown in pickups.Values)
            {
                shown.Seen = false;
            }
            foreach (Pickup pickup in run.Pickups)
            {
                Vector2 at = ToWorld(run, pickup.X, pickup.Y);
                if (at.x > CameraRig.HalfWidth + 6f || at.x < -CameraRig.HalfWidth - 6f)
                {
                    continue;
                }
                int key = 100 + (int)pickup.Kind;
                if (!pickups.TryGetValue(pickup.Id, out Shown shown))
                {
                    shown = Take(key, () => MakePickup(pickup.Kind));
                    pickups[pickup.Id] = shown;
                }
                shown.Seen = true;
                Transform root = shown.Root.transform;
                if (pickup.Kind == PickupKind.Coin)
                {
                    root.localPosition = new Vector3(at.x, at.y, 0f);
                    float spin = Mathf.Cos(time * 5f + pickup.X * 0.7f);
                    shown.Main.transform.localScale = new Vector3(Mathf.Max(0.12f, Mathf.Abs(spin)), 1f, 1f);
                    shown.Main.color = spin < 0f ? new Color(0.85f, 0.8f, 0.7f) : Color.white;
                }
                else
                {
                    root.localPosition = new Vector3(at.x, at.y + Mathf.Sin(time * 3f + pickup.Id) * 0.12f, 0f);
                    float pulse = 1f + Mathf.Sin(time * 6f) * 0.08f;
                    shown.Glow.transform.localScale = Vector3.one * 1.9f * pulse;
                }
            }
            Release(pickups);
        }

        Shown MakeObstacle(ObstacleKind kind)
        {
            var root = new GameObject(kind.ToString());
            root.transform.SetParent(objectsRoot, false);
            var shown = new Shown { Root = root, Kind = (int)kind };
            switch (kind)
            {
                case ObstacleKind.Crate:
                    shown.Main = Sprite("Crate", Shapes.Crate, Color.white, 2, root.transform);
                    break;
                case ObstacleKind.Stack:
                    shown.Main = Sprite("Crate", Shapes.Crate, Color.white, 2, root.transform);
                    shown.Extra = Sprite("Crate", Shapes.Crate, new Color(0.92f, 0.9f, 0.95f), 2, root.transform);
                    shown.Extra.transform.localPosition = new Vector3(0.04f, 1f, 0f);
                    shown.Extra.transform.localScale = new Vector3(0.92f, 1f, 1f);
                    break;
                case ObstacleKind.Spikes:
                    shown.Main = Sprite("Spikes", Shapes.Spikes, Color.white, 2, root.transform);
                    break;
                case ObstacleKind.Sign:
                    shown.Main = Sprite("Sign", Shapes.Sign, Color.white, 3, root.transform);
                    // Two chains up to the top of the screen.
                    foreach (float x in new[] { 0.3f, 1.3f })
                    {
                        SpriteRenderer chain = Sprite("Chain", Shapes.Square, Palette.Hex("#3b2d5c"), 1, root.transform);
                        chain.transform.localPosition = new Vector3(x, 2.2f + 6f, 0f);
                        chain.transform.localScale = new Vector3(0.6f, 120f, 1f);
                    }
                    break;
                case ObstacleKind.Barrel:
                    shown.Main = Sprite("Barrel", Shapes.Barrel, Color.white, 2, root.transform);
                    break;
                default:
                    shown.Glow = Sprite("Fuse Glow", Shapes.SoftDot, Palette.Tangerine.WithAlpha(0.5f), 3, root.transform);
                    if (art != null && art.Glow != null)
                    {
                        shown.Glow.sharedMaterial = art.Glow;
                    }
                    shown.Glow.transform.localPosition = new Vector3(0.33f, 0.56f, 0f);
                    shown.Glow.transform.localScale = Vector3.one * 0.7f;
                    shown.Main = Sprite("Bomb", Shapes.Bomb, Color.white, 4, root.transform);
                    break;
            }
            return shown;
        }

        Shown MakePickup(PickupKind kind)
        {
            var root = new GameObject(kind.ToString());
            root.transform.SetParent(objectsRoot, false);
            var shown = new Shown { Root = root, Kind = 100 + (int)kind };
            if (kind != PickupKind.Coin)
            {
                shown.Glow = Sprite("Glow", Shapes.SoftDot, Palette.Of(kind).WithAlpha(0.45f), 5, root.transform);
                if (art != null && art.Glow != null)
                {
                    shown.Glow.sharedMaterial = art.Glow;
                }
            }
            shown.Main = Sprite(kind.ToString(), Shapes.For(kind), Color.white, 6, root.transform);
            return shown;
        }

        Shown Take(int kind, System.Func<Shown> make)
        {
            if (pool.TryGetValue(kind, out Stack<Shown> stack) && stack.Count > 0)
            {
                Shown reused = stack.Pop();
                reused.Root.SetActive(true);
                return reused;
            }
            return make();
        }

        void Release(Dictionary<int, Shown> shownById)
        {
            gone.Clear();
            foreach (KeyValuePair<int, Shown> pair in shownById)
            {
                if (!pair.Value.Seen)
                {
                    gone.Add(pair.Key);
                }
            }
            foreach (int id in gone)
            {
                Shown shown = shownById[id];
                shown.Root.SetActive(false);
                if (!pool.TryGetValue(shown.Kind, out Stack<Shown> stack))
                {
                    pool[shown.Kind] = stack = new Stack<Shown>();
                }
                stack.Push(shown);
                shownById.Remove(id);
            }
        }

        /// <summary>Forgets the runner's tumble and the sprites of the last run (a new run starts).</summary>
        public void Reset()
        {
            wasOver = false;
            tumble = 0f;
            squash = 0f;
            flip = 0f;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Color color, int order, Transform parent = null)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent != null ? parent : transform, false);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
