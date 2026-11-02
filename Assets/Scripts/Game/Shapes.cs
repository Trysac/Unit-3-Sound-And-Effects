using System;
using System.Collections.Generic;
using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>
    /// Every sprite in the game, drawn in code at start-up from distance functions, so the game
    /// ships without image files. Sprites are authored in world units (one unit is one metre of
    /// track) at <see cref="Detail"/> pixels per unit, with the pivot where the game places them.
    /// </summary>
    public static class Shapes
    {
        const int Detail = 96;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // ---- Basics (one unit, centred) ----

        public static Sprite Square => Get("Square", 0.1f, 0.1f, Center, 80, (x, y) => Color.white);

        public static Sprite SoftDot => Get("SoftDot", 1f, 1f, Center, 64, (x, y) =>
        {
            float a = Mathf.Clamp01(1f - Len(x - 0.5f, y - 0.5f) * 2f);
            return White(a * a);
        });

        public static Sprite Circle => Get("Circle", 1f, 1f, Center, 64, (x, y) => White(Coverage(0.49f - Len(x - 0.5f, y - 0.5f), 0.02f)));

        public static Sprite Ring => Get("Ring", 1f, 1f, Center, 128, (x, y) => White(Coverage(0.025f - Mathf.Abs(Len(x - 0.5f, y - 0.5f) - 0.46f), 0.012f)));

        /// <summary>A soft streak for the speed lines.</summary>
        public static Sprite Streak => Get("Streak", 2f, 0.1f, Center, 32, (x, y) =>
        {
            float along = Mathf.Clamp01(1f - Mathf.Abs(x - 1f));
            float across = Mathf.Clamp01(1f - Mathf.Abs(y - 0.05f) / 0.05f);
            return White(along * along * across);
        });

        static Vector2 Center => new Vector2(0.5f, 0.5f);
        static Vector2 BottomCenter => new Vector2(0.5f, 0f);
        static Vector2 BottomLeft => Vector2.zero;

        // ---- The runner ----

        /// <summary>A bean-shaped runner facing right, feet at the pivot.</summary>
        public static Sprite RunnerBody => Get("RunnerBody", 1f, 1.25f, BottomCenter, Detail, (x, y) =>
        {
            float d = RoundedBox(x - 0.5f, y - 0.62f, 0.42f, 0.6f, 0.4f);
            float body = Coverage(-d, 0.02f);
            if (body <= 0f)
            {
                return Color.clear;
            }
            float light = Mathf.Clamp01(0.55f + 0.6f * (-(x - 0.5f) * 0.4f + (y - 0.6f) * 0.7f));
            Color color = Color.Lerp(Palette.RunnerDark, Color.Lerp(Palette.Runner, Color.white, 0.12f), light);
            color = Color.Lerp(color, Palette.RunnerDark * 0.8f, Coverage(d + 0.05f, 0.02f));
            // A paler belly on the front.
            float belly = Coverage(0.24f - Len((x - 0.62f) / 0.8f, (y - 0.45f) / 1.3f), 0.03f);
            color = Color.Lerp(color, Palette.Hex("#ffd0b0"), belly * 0.8f);
            // A big eye looking ahead, a pupil and a glint.
            float eye = Coverage(0.15f - Len(x - 0.66f, y - 0.88f), 0.02f);
            color = Color.Lerp(color, Color.white, eye);
            float pupil = Coverage(0.075f - Len(x - 0.71f, y - 0.87f), 0.02f);
            color = Color.Lerp(color, Palette.Ink, pupil);
            color = Color.Lerp(color, Color.white, Coverage(0.025f - Len(x - 0.735f, y - 0.905f), 0.012f));
            // A rosy cheek.
            float cheek = Coverage(0.07f - Len((x - 0.8f) / 1.3f, y - 0.7f), 0.03f);
            color = Color.Lerp(color, Palette.Hex("#ff5f7e"), cheek * 0.55f);
            color.a = body;
            return color;
        });

        /// <summary>An arm or a leg, hanging from its pivot at the top.</summary>
        public static Sprite Limb => Get("Limb", 0.22f, 0.5f, new Vector2(0.5f, 0.9f), Detail, (x, y) =>
        {
            float d = Capsule(x - 0.11f, y, 0.06f, 0.4f, 0.075f);
            return Palette.Ink.WithAlpha(Coverage(-d, 0.015f));
        });

        /// <summary>One strip of the scarf, attached at its left end.</summary>
        public static Sprite ScarfTail => Get("ScarfTail", 0.55f, 0.2f, new Vector2(0.05f, 0.5f), Detail, (x, y) =>
        {
            float taper = 0.075f - x * 0.05f;
            float d = Mathf.Abs(y - 0.1f) - taper;
            float cover = Coverage(-d, 0.015f) * Coverage(0.53f - x, 0.02f);
            Color color = Color.Lerp(Palette.Scarf, Palette.Hex("#1f8fd0"), Coverage(0.05f - Mathf.Abs(x - 0.3f), 0.02f));
            return color.WithAlpha(cover);
        });

        /// <summary>The red balloon the runner hangs from, tied at the pivot.</summary>
        public static Sprite Balloon => Get("Balloon", 1f, 1.3f, BottomCenter, Detail, (x, y) =>
        {
            float cx = x - 0.5f;
            float cy = y - 0.72f;
            float d = (Len(cx / 0.42f, cy / 0.5f) - 1f) * 0.42f;
            float knot = Mathf.Max(Coverage(0.07f - Mathf.Abs(cx) - (y - 0.12f) * 0.35f, 0.02f) * (y > 0.12f && y < 0.26f ? 1f : 0f), 0f);
            float body = Coverage(-d, 0.02f);
            if (body <= 0f && knot <= 0f)
            {
                return Color.clear;
            }
            float light = Mathf.Clamp01(0.5f + 0.7f * (-cx * 0.6f + cy * 0.6f));
            Color color = Color.Lerp(Palette.Balloon * 0.65f, Color.Lerp(Palette.Balloon, Color.white, 0.15f), light);
            float shine = Coverage(0.07f - Len((cx + 0.18f) / 0.6f, cy - 0.2f), 0.03f);
            color = Color.Lerp(color, Color.white, shine * 0.85f);
            color.a = 1f;
            Color knotColor = (Palette.Balloon * 0.6f).WithAlpha(knot);
            return Over(knotColor, color.WithAlpha(body));
        });

        // ---- Obstacles (pivot bottom-left, the size of their hit box) ----

        public static Sprite Crate => Get("Crate", 1f, 1f, BottomLeft, Detail, (x, y) =>
        {
            float frame = Mathf.Max(Coverage(0.12f - Mathf.Min(Mathf.Min(x, 1f - x), Mathf.Min(y, 1f - y)), 0.015f), 0f);
            float brace = Coverage(0.06f - Mathf.Abs(x - y), 0.015f) + Coverage(0.06f - Mathf.Abs(x + y - 1f), 0.015f);
            float plank = Mathf.Abs(Mathf.Repeat(y * 4f, 1f) - 0.5f) > 0.46f ? 1f : 0f;
            Color color = Color.Lerp(Palette.Wood, Palette.Hex("#e0a866"), Mathf.Clamp01(y * 0.5f + 0.25f));
            color = Color.Lerp(color, Palette.WoodDark, plank * 0.6f);
            color = Color.Lerp(color, Palette.Hex("#a96a38"), Mathf.Clamp01(brace));
            color = Color.Lerp(color, Palette.WoodDark, frame);
            float nails = 0f;
            foreach (float nx in new[] { 0.06f, 0.94f })
            {
                foreach (float ny in new[] { 0.06f, 0.94f })
                {
                    nails = Mathf.Max(nails, Coverage(0.025f - Len(x - nx, y - ny), 0.01f));
                }
            }
            color = Color.Lerp(color, Palette.Steel, nails);
            float edge = Coverage(Mathf.Min(Mathf.Min(x, 1f - x), Mathf.Min(y, 1f - y)), 0.01f);
            return color.WithAlpha(edge);
        });

        public static Sprite Spikes => Get("Spikes", 2.4f, 0.55f, BottomLeft, Detail, (x, y) =>
        {
            float baseplate = y < 0.12f ? 1f : 0f;
            float local = Mathf.Repeat(x, 0.3f) - 0.15f;
            float spike = Coverage((0.13f - Mathf.Abs(local)) * 3.4f - (y - 0.12f), 0.015f) * (y < 0.55f ? 1f : 0f);
            float cover = Mathf.Max(baseplate, spike) * Coverage(Mathf.Min(x, 2.4f - x), 0.01f);
            float light = Mathf.Clamp01(0.5f - local * 3f);
            Color color = Color.Lerp(Palette.Hex("#8f96b8"), Palette.Steel, light);
            color = baseplate > 0f ? Palette.Hex("#4a4466") : color;
            return color.WithAlpha(cover);
        });

        /// <summary>A hanging hazard board with arrows pointing down: slide under it.</summary>
        public static Sprite Sign => Get("Sign", 1.6f, 2.2f, BottomLeft, Detail, (x, y) =>
        {
            float d = RoundedBox(x - 0.8f, y - 1.1f, 0.8f, 1.1f, 0.12f);
            float cover = Coverage(-d, 0.015f);
            if (cover <= 0f)
            {
                return Color.clear;
            }
            float border = Coverage(d + 0.13f, 0.015f);
            float stripes = Mathf.Repeat((x + y) * 3.2f, 1f) < 0.5f ? 1f : 0f;
            Color color = Palette.Hex("#2d2147");
            color = Color.Lerp(color, Color.Lerp(Palette.Ink, Palette.Hazard, stripes), border);
            // Two chevrons pointing down in the middle of the board.
            for (int i = 0; i < 2; i++)
            {
                float cy = 1.35f - i * 0.55f;
                float chevron = Coverage(0.07f - Mathf.Abs((y - cy) - Mathf.Abs(x - 0.8f) * 0.8f + 0.1f), 0.015f) * Coverage(0.42f - Mathf.Abs(x - 0.8f), 0.015f);
                color = Color.Lerp(color, Palette.Hazard, chevron * (1f - border));
            }
            return color.WithAlpha(cover);
        });

        public static Sprite Barrel => Get("Barrel", 0.9f, 0.9f, Center, Detail, (x, y) =>
        {
            float cx = x - 0.45f;
            float cy = y - 0.45f;
            float r = Len(cx, cy);
            float cover = Coverage(0.44f - r, 0.015f);
            if (cover <= 0f)
            {
                return Color.clear;
            }
            Color color = Color.Lerp(Palette.WoodDark, Palette.Wood, Mathf.Clamp01(0.6f + (-cx + cy) * 0.6f));
            // Staves seen from the end, a steel hoop and a dark hub.
            float angle = Mathf.Atan2(cy, cx);
            float stave = Mathf.Abs(Mathf.Sin(angle * 4f)) < 0.08f && r < 0.36f ? 1f : 0f;
            color = Color.Lerp(color, Palette.WoodDark * 0.8f, stave);
            color = Color.Lerp(color, Palette.Hex("#9aa3c2"), Coverage(0.04f - Mathf.Abs(r - 0.39f), 0.012f));
            color = Color.Lerp(color, Palette.Ink, Coverage(0.07f - r, 0.015f));
            color.a = cover;
            return color;
        });

        /// <summary>A round black bomb with a warning band, a fuse and a spark (centred on its hit circle).</summary>
        public static Sprite Bomb => Get("Bomb", 1.3f, 1.3f, Center, Detail, (x, y) =>
        {
            float cx = x - 0.65f;
            float cy = y - 0.65f;
            float d = Len(cx, cy);
            float body = Coverage(Tuning.BombRadius - d, 0.015f);
            float cap = Box(cx, cy - 0.45f, 0.11f, 0.07f);
            float fuse = Coverage(0.03f - Mathf.Abs(Len(cx - 0.17f, cy - 0.46f) - 0.17f), 0.015f) * (cx > 0.03f && cy > 0.48f ? 1f : 0f);
            float spark = Coverage(0.08f - Len(cx - 0.33f, cy - 0.56f), 0.03f);
            float sparkGlow = Mathf.Clamp01(1f - Len(cx - 0.33f, cy - 0.56f) / 0.2f);
            Color color = Color.clear;
            color = Over(color, Palette.Hex("#ffb84a").WithAlpha(sparkGlow * 0.6f));
            color = Over(color, Palette.Hex("#8a7a60").WithAlpha(fuse));
            color = Over(color, Palette.Hex("#6b6880").WithAlpha(cap));
            if (body > 0f)
            {
                float light = Mathf.Clamp01(0.5f + 1.3f * (-cx * 0.6f + cy * 0.8f));
                Color skin = Color.Lerp(Palette.Bomb * 0.6f, Palette.Hex("#4a4466"), light);
                skin = Color.Lerp(skin, Palette.Danger, Coverage(0.04f - Mathf.Abs(d - 0.38f), 0.015f));
                skin = Color.Lerp(skin, Color.white, Coverage(0.11f - Ellipse(cx + 0.17f, cy - 0.18f, 0.13f, 0.07f, 0.6f), 0.03f) * 0.6f);
                color = Over(color, skin.WithAlpha(body));
            }
            return Over(color, Palette.Hex("#fff1b8").WithAlpha(spark));
        });

        // ---- Pickups (centred) ----

        public static Sprite Coin => Get("Coin", 0.64f, 0.64f, Center, Detail, (x, y) =>
        {
            float cx = x - 0.32f;
            float cy = y - 0.32f;
            float d = Len(cx, cy);
            float cover = Coverage(0.3f - d, 0.015f);
            if (cover <= 0f)
            {
                return Color.clear;
            }
            float light = Mathf.Clamp01(0.5f + 1.4f * (-cx * 0.5f + cy * 0.8f));
            Color color = Color.Lerp(Palette.Hex("#e8930f"), Palette.Hex("#fff3a6"), light);
            color = Color.Lerp(color, Palette.Hex("#c86f0a"), Coverage(0.03f - Mathf.Abs(d - 0.24f), 0.012f));
            float star = Coverage(-Star(cx, cy, 0.15f, 0.45f), 0.015f);
            color = Color.Lerp(color, Palette.Hex("#ffe66b"), star);
            color = Color.Lerp(color, Palette.Hex("#c86f0a"), Coverage(0.012f - Mathf.Abs(Star(cx, cy, 0.15f, 0.45f)), 0.008f));
            color.a = cover;
            return color;
        });

        public static Sprite For(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Balloon: return Get("BadgeBalloon", 0.9f, 0.9f, Center, Detail, BalloonBadge);
                case PickupKind.Magnet: return Get("BadgeMagnet", 0.9f, 0.9f, Center, Detail, MagnetBadge);
                case PickupKind.Shield: return Get("BadgeShield", 0.9f, 0.9f, Center, Detail, ShieldBadge);
                default: return Coin;
            }
        }

        static Color Badge(float cx, float cy, Color tint, out float inside)
        {
            float d = Len(cx, cy);
            float disk = Coverage(0.44f - d, 0.015f);
            inside = Coverage(0.37f - d, 0.015f);
            if (disk <= 0f)
            {
                return Color.clear;
            }
            Color rim = Color.Lerp(tint, Color.white, 0.4f);
            Color face = Color.Lerp(Palette.Hex("#241646"), tint * 0.45f, Mathf.Clamp01(0.5f + cy));
            Color color = Color.Lerp(rim, face, inside);
            color.a = disk;
            return color;
        }

        static Color BalloonBadge(float x, float y)
        {
            float cx = x - 0.45f, cy = y - 0.45f;
            Color color = Badge(cx, cy, Palette.Balloon, out float inside);
            if (color.a <= 0f)
            {
                return color;
            }
            float balloon = Coverage(-(Len(cx / 0.17f, (cy - 0.07f) / 0.21f) - 1f) * 0.17f, 0.012f);
            float stringLine = Coverage(0.012f - Mathf.Abs(cx + Mathf.Sin(cy * 30f) * 0.02f), 0.008f) * (cy < -0.13f && cy > -0.28f ? 1f : 0f);
            color = Color.Lerp(color, Palette.Balloon, balloon * inside);
            color = Color.Lerp(color, Color.white, Coverage(0.03f - Len(cx + 0.06f, cy - 0.15f), 0.012f) * inside);
            color = Color.Lerp(color, Palette.Steel, stringLine * inside);
            return color;
        }

        static Color MagnetBadge(float x, float y)
        {
            float cx = x - 0.45f, cy = y - 0.4f;
            Color color = Badge(x - 0.45f, y - 0.45f, Palette.Magnet, out float inside);
            if (color.a <= 0f)
            {
                return color;
            }
            float arc = Coverage(0.05f - Mathf.Abs(Len(cx, cy) - 0.13f), 0.012f) * (cy < 0f ? 1f : 0f);
            float legs = Mathf.Max(Box(cx - 0.13f, cy - 0.08f, 0.05f, 0.08f), Box(cx + 0.13f, cy - 0.08f, 0.05f, 0.08f));
            float magnet = Mathf.Max(arc, legs);
            bool tip = cy > 0.09f;
            return Color.Lerp(color, tip ? Palette.Steel : Palette.Magnet, magnet * inside);
        }

        static Color ShieldBadge(float x, float y)
        {
            float cx = x - 0.45f, cy = y - 0.45f;
            Color color = Badge(cx, cy, Palette.Shield, out float inside);
            if (color.a <= 0f)
            {
                return color;
            }
            // A heater shield: a box on top, a point at the bottom.
            float top = Box(cx, cy - 0.06f, 0.15f, 0.1f);
            float point = Coverage(0.15f - Mathf.Abs(cx) - (-0.04f - cy) * 0.9f, 0.012f) * (cy < -0.03f && cy > -0.24f ? 1f : 0f);
            float shield = Mathf.Max(top, point);
            color = Color.Lerp(color, Palette.Shield, shield * inside);
            float cross = Mathf.Max(Box(cx, cy - 0.01f, 0.025f, 0.12f), Box(cx, cy + 0.0f, 0.1f, 0.025f));
            return Color.Lerp(color, Color.white, cross * shield * inside);
        }

        // ---- Backdrop ----

        public static Sprite Sky => Get("Sky", 1f, 1f, Center, 64, (x, y) =>
        {
            Color low = Color.Lerp(Palette.SkyHorizon, Palette.SkyMiddle, Mathf.Clamp01(y / 0.45f));
            return Color.Lerp(low, Palette.SkyTop, Mathf.Clamp01((y - 0.45f) / 0.55f));
        });

        /// <summary>A big soft sun with a glow.</summary>
        public static Sprite SunDisc => Get("Sun", 1f, 1f, Center, 128, (x, y) =>
        {
            float d = Len(x - 0.5f, y - 0.5f);
            float disc = Coverage(0.2f - d, 0.01f);
            float glow = Mathf.Clamp01(1f - d / 0.5f);
            Color color = Palette.Sun.WithAlpha(glow * glow * 0.45f);
            // Retro bands across the lower half of the disc.
            float band = y < 0.48f && Mathf.Repeat(y * 22f, 1f) < 0.3f + (0.48f - y) * 1.5f ? 0f : 1f;
            return Over(color, Color.Lerp(Palette.Sun, Palette.SkyHorizon, Mathf.Clamp01(0.55f - y) * 1.5f).WithAlpha(disc * band));
        });

        public static Sprite CloudPuff => Get("Cloud", 3f, 1.2f, Center, 48, (x, y) =>
        {
            float a = Blob(x - 1.0f, y - 0.5f, 0.55f);
            a = Mathf.Max(a, Blob(x - 1.6f, y - 0.62f, 0.6f));
            a = Mathf.Max(a, Blob(x - 2.2f, y - 0.48f, 0.45f));
            a *= Coverage(y - 0.32f, 0.08f);
            return White(a);
        });

        /// <summary>
        /// A seamless strip of hills <paramref name="width"/> units wide: the same height at both
        /// ends, so copies side by side scroll without a seam.
        /// </summary>
        public static Sprite Hills(string name, float width, float height, Color color, int seed, float roughness)
        {
            var random = new System.Random(seed);
            var waves = new (float Frequency, float Phase, float Amplitude)[4];
            for (int i = 0; i < waves.Length; i++)
            {
                int cycles = 1 + i * 2 + random.Next(0, 2);
                waves[i] = (cycles * Mathf.PI * 2f / width, (float)random.NextDouble() * Mathf.PI * 2f, (0.5f / (i + 1)) * (i == 0 ? 1f : roughness));
            }
            return Get(name, width, height, BottomLeft, 24, (x, y) =>
            {
                float top = height * 0.6f;
                foreach ((float frequency, float phase, float amplitude) in waves)
                {
                    top += Mathf.Sin(x * frequency + phase) * amplitude * height * 0.4f;
                }
                // Never dip to the bottom of the strip, or the sky would show through under it.
                top = Mathf.Max(top, height * 0.3f);
                float cover = Coverage(top - y, 0.04f);
                Color shade = Color.Lerp(color * 0.85f, color, Mathf.Clamp01(y / top));
                return shade.WithAlpha(cover);
            });
        }

        /// <summary>A seamless strip of the ground: a grass edge over packed earth with stones.</summary>
        public static Sprite Ground => Get("Ground", 8f, 3f, new Vector2(0f, 1f), 48, (x, y) =>
        {
            float depth = 3f - y;
            float blades = 0.05f * Mathf.Abs(Mathf.Sin(x * 9.7f)) + 0.04f * Mathf.Abs(Mathf.Sin(x * 23.3f + 1f));
            float grassLine = 0.22f + blades;
            if (depth < 0.04f - blades * 0.8f)
            {
                return Color.clear;
            }
            if (depth < grassLine)
            {
                Color grass = Color.Lerp(Palette.Grass, Palette.GrassDark, Mathf.Clamp01(depth / grassLine));
                return grass;
            }
            Color dirt = Color.Lerp(Palette.Dirt, Palette.DirtDark, Mathf.Clamp01((depth - grassLine) / 2.2f));
            // A darker band of stripes moving with the ground shows the speed.
            float stripe = Mathf.Repeat(x - depth * 0.3f, 1f) < 0.08f && depth < 1.2f ? 0.18f : 0f;
            dirt = Color.Lerp(dirt, Palette.DirtDark * 0.85f, stripe);
            float stones = 0f;
            for (int i = 0; i < 9; i++)
            {
                float sx = Mathf.Repeat(i * 2.73f + 0.4f, 8f);
                float sy = 0.6f + Mathf.Repeat(i * 0.97f, 2f);
                stones = Mathf.Max(stones, Coverage(0.07f + (i % 3) * 0.025f - Len((x - sx) / 1.4f, depth - sy), 0.02f));
            }
            return Color.Lerp(dirt, Palette.Hex("#8a5a6e"), stones * 0.7f);
        });

        // ---- Helpers ----

        static Sprite Get(string name, float width, float height, Vector2 pivot, int pixelsPerUnit, Func<float, float, Color> paint)
        {
            if (cache.TryGetValue(name, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
            int w = Mathf.Max(1, Mathf.RoundToInt(width * pixelsPerUnit));
            int h = Mathf.Max(1, Mathf.RoundToInt(height * pixelsPerUnit));
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[w * h];
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    pixels[py * w + px] = paint((px + 0.5f) / w * width, (py + 0.5f) / h * height);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, w, h), pivot, w / width);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            cache[name] = sprite;
            return sprite;
        }

        static Color White(float alpha) => new Color(1f, 1f, 1f, alpha);

        static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

        /// <summary>0 outside, 1 inside, with a soft edge <paramref name="width"/> wide.</summary>
        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static float Box(float x, float y, float halfWidth, float halfHeight) =>
            Mathf.Min(Coverage(halfWidth - Mathf.Abs(x), 0.012f), Coverage(halfHeight - Mathf.Abs(y), 0.012f));

        static float Blob(float x, float y, float radius)
        {
            float a = Mathf.Clamp01(1f - Len(x, y) / radius);
            return Mathf.Clamp01(a * 2.2f);
        }

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>Distance to a vertical capsule from <paramref name="bottom"/> to <paramref name="top"/>.</summary>
        static float Capsule(float x, float y, float bottom, float top, float radius)
        {
            float cy = Mathf.Clamp(y, bottom, top);
            return Len(x, y - cy) - radius;
        }

        static float Ellipse(float x, float y, float rx, float ry, float radians)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            float u = (c * x + s * y) / rx;
            float v = (-s * x + c * y) / ry;
            return (Mathf.Sqrt(u * u + v * v) - 1f) * Mathf.Min(rx, ry);
        }

        /// <summary>Signed distance to a five-pointed star (negative inside).</summary>
        static float Star(float x, float y, float radius, float innerRatio)
        {
            float angle = Mathf.Atan2(x, y);
            float sector = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(angle + sector / 2f, sector) - sector / 2f;
            float d = Len(x, y);
            var p = new Vector2(Mathf.Abs(Mathf.Sin(a)) * d, Mathf.Cos(a) * d);
            var tip = new Vector2(0f, radius);
            var valley = new Vector2(Mathf.Sin(sector / 2f), Mathf.Cos(sector / 2f)) * (radius * innerRatio);
            Vector2 edge = valley - tip;
            float t = Mathf.Clamp01(Vector2.Dot(p - tip, edge) / edge.sqrMagnitude);
            float distance = (p - (tip + edge * t)).magnitude;
            float side = edge.x * (p.y - tip.y) - edge.y * (p.x - tip.x);
            return side < 0f ? -distance : distance;
        }

        static Color Over(Color below, Color above)
        {
            float a = above.a + below.a * (1f - above.a);
            if (a <= 1e-5f)
            {
                return Color.clear;
            }
            Color mixed = (above * above.a + below * below.a * (1f - above.a)) / a;
            mixed.a = a;
            return mixed;
        }
    }
}
