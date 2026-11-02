using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>
    /// A side-on orthographic view that keeps the runner and the track ahead on screen at any
    /// aspect ratio, rises a little to follow the balloon, and adds trauma-based shake, a kick
    /// away from a hit and a short zoom punch.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        /// <summary>Half the width of the view at the widest framing (16:9 at the base height).</summary>
        public const float HalfWidth = 8.9f;
        public const float HalfHeight = 5f;
        /// <summary>The camera's height when the runner is on the ground (the ground sits below the middle).</summary>
        public const float BaseY = 3.2f;
        /// <summary>The runner's place on screen: a third of the way in from the left.</summary>
        public const float RunnerX = -5f;

        Camera view;
        float trauma;
        Vector2 kick;
        float punch;
        float follow;

        /// <summary>Scales every shake (the "screen shake" setting turns it off with 0).</summary>
        public float ShakeScale { get; set; } = 1f;

        /// <summary>The runner's feet height, which the camera follows up while floating.</summary>
        public float Target { get; set; }

        public Camera View => view != null ? view : view = GetComponent<Camera>();

        void Awake()
        {
            view = GetComponent<Camera>();
            view.orthographic = true;
        }

        /// <summary>Adds screen shake; 1 is the strongest.</summary>
        public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount * ShakeScale);

        /// <summary>Nudges the view a little along <paramref name="direction"/>.</summary>
        public void Kick(Vector2 direction, float amount) => kick += direction.normalized * (amount * ShakeScale);

        /// <summary>Zooms in for a moment (a fraction of the view, decaying quickly).</summary>
        public void Punch(float amount) => punch = Mathf.Max(punch, amount * ShakeScale);

        public void Calm()
        {
            trauma = 0f;
            kick = Vector2.zero;
            punch = 0f;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            float size = Mathf.Max(HalfHeight, HalfWidth / Mathf.Max(View.aspect, 0.1f));
            punch = Mathf.MoveTowards(punch, 0f, dt * 0.25f);
            view.orthographicSize = size * (1f - punch);

            float wanted = Mathf.Max(0f, Target - 2.2f) * 0.55f;
            follow = Mathf.Lerp(follow, wanted, 1f - Mathf.Exp(-dt * 4f));

            trauma = Mathf.Max(trauma - dt * 1.6f, 0f);
            kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-dt * 14f));
            float strength = trauma * trauma;
            float t = Time.unscaledTime * 30f;
            var shake = new Vector2(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f) * (0.9f * strength);
            float roll = (Mathf.PerlinNoise(t * 0.6f, 4.2f) - 0.5f) * 3f * strength;
            Vector2 offset = shake + kick;
            // The ground stays at the same place on screen whatever the aspect ratio.
            float y = BaseY + (size - HalfHeight) * 0.6f + follow;
            transform.SetPositionAndRotation(new Vector3(offset.x, y + offset.y, -10f), Quaternion.Euler(0f, 0f, roll));
        }
    }
}
