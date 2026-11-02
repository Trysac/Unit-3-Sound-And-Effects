using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>Particle effects made in code: one-shot bursts, and a pooled system for the running dust.</summary>
    public static class Effects
    {
        /// <summary>A one-shot spray of particles that cleans itself up.</summary>
        /// <param name="direction">Main direction of the spray; zero sprays all around.</param>
        /// <param name="drift">Sideways speed added to every particle (the ground scrolling under it).</param>
        /// <param name="gravity">Pull on the particles, in multiples of a gentle 9.81.</param>
        public static ParticleSystem Burst(Vector2 position, Vector2 direction, Material material, Color colorA, Color colorB,
            int count, float speed, float size, float lifetime, float spread = 70f, float drift = 0f, float gravity = 0f, int order = 20)
        {
            if (material == null || count <= 0)
            {
                return null;
            }
            var host = new GameObject("Burst");
            host.transform.position = new Vector3(position.x, position.y, -0.2f);
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.1f;
            main.loop = false;
            main.useUnscaledTime = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = system.shape;
            if (direction.sqrMagnitude > 1e-4f)
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = spread * 0.5f;
                shape.radius = 0.05f;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // The cone points along +Z; turn it into the screen's plane along the direction.
                shape.rotation = new Vector3(-angle, 90f, 0f);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.1f;
            }

            if (Mathf.Abs(drift) > 1e-3f)
            {
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(drift);
                velocity.y = new ParticleSystem.MinMaxCurve(0f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);
            }

            ParticleSystem.LimitVelocityOverLifetimeModule drag = system.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 2.5f;

            ParticleSystem.SizeOverLifetimeModule shrink = system.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            system.Play();
            return system;
        }

        /// <summary>
        /// A long-lived system the runner emits single puffs into (the dust behind its feet), so a
        /// puff every few frames costs no new objects.
        /// </summary>
        public static ParticleSystem Emitter(string name, Material material, int order)
        {
            var host = new GameObject(name);
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = false;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystem.SizeOverLifetimeModule grow = system.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            system.Play();
            return system;
        }

        /// <summary>Emits one particle into an <see cref="Emitter"/>.</summary>
        public static void Puff(ParticleSystem system, Vector2 position, Vector2 velocity, Color color, float size, float lifetime)
        {
            if (system == null)
            {
                return;
            }
            var emit = new ParticleSystem.EmitParams
            {
                position = new Vector3(position.x, position.y, -0.1f),
                velocity = velocity,
                startColor = color,
                startSize = size,
                startLifetime = lifetime,
                applyShapeToPosition = false,
            };
            system.Emit(emit, 1);
        }
    }
}
