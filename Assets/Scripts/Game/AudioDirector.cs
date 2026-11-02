using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>
    /// The game's sound: cross-fades between the title, run and sky themes, muffles the music
    /// through a low-pass filter while the game is paused or over, bends the music up a little
    /// as the run gets faster, loops wind under the balloon, and plays one-shot effects with a
    /// little random pitch so repeated sounds never feel mechanical.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        const float MusicLevel = 0.34f;
        const float WindLevel = 0.22f;
        const float OpenCutoff = 22000f;
        const float MuffledCutoff = 900f;

        AudioSource musicA;
        AudioSource musicB;
        AudioSource wind;
        AudioSource effects;
        AudioLowPassFilter filter;
        AudioClip wanted;
        bool aIsCurrent = true;
        float blend = 1f;

        /// <summary>Effects volume from 0 to 1.</summary>
        public float Volume { get; private set; } = 0.8f;

        /// <summary>Music volume from 0 to 1.</summary>
        public float MusicVolume { get; private set; } = 0.7f;

        public bool Muted { get; private set; }

        /// <summary>When true (the demo behind the title), effects are not played.</summary>
        public bool Silent { get; set; }

        /// <summary>The music bends to this pitch (a little higher as the run speeds up).</summary>
        public float MusicPitch { get; set; } = 1f;

        /// <summary>Muffles the music (pause and results).</summary>
        public bool Muffled { get; set; }

        /// <summary>How loud the wind under the balloon should be, 0..1.</summary>
        public float WindAmount { get; set; }

        /// <summary>The theme playing or fading in.</summary>
        public AudioClip CurrentMusic => wanted;

        public void Init(float volume, float musicVolume, bool muted, AudioClip windLoop)
        {
            var musicHost = new GameObject("Music");
            musicHost.transform.SetParent(transform, false);
            musicA = MusicSource(musicHost);
            musicB = MusicSource(musicHost);
            filter = musicHost.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = OpenCutoff;

            wind = gameObject.AddComponent<AudioSource>();
            wind.clip = windLoop;
            wind.loop = true;
            wind.playOnAwake = false;
            wind.volume = 0f;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            SetVolumes(volume, musicVolume);
            Muted = muted;
            Apply();
        }

        static AudioSource MusicSource(GameObject host)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            return source;
        }

        public void SetVolumes(float volume, float musicVolume)
        {
            Volume = Mathf.Clamp01(volume);
            MusicVolume = Mathf.Clamp01(musicVolume);
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            Apply();
        }

        void Apply() => AudioListener.volume = Muted ? 0f : 1f;

        /// <summary>Cross-fades to a theme; the new one starts where the old one was, so related themes stay in time.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == wanted || musicA == null)
            {
                return;
            }
            AudioSource from = aIsCurrent ? musicA : musicB;
            AudioSource to = aIsCurrent ? musicB : musicA;
            wanted = clip;
            aIsCurrent = !aIsCurrent;
            blend = 0f;
            to.clip = clip;
            if (clip != null)
            {
                // Themes on the same tempo whose lengths are whole multiples of each other line up bar for bar.
                bool related = false;
                if (from.clip != null && from.isPlaying)
                {
                    float ratio = Mathf.Max(from.clip.length, clip.length) / Mathf.Max(0.01f, Mathf.Min(from.clip.length, clip.length));
                    related = Mathf.Abs(ratio - Mathf.Round(ratio)) < 0.01f;
                }
                to.time = related ? Mathf.Repeat(from.time, clip.length) : 0f;
                to.Play();
            }
        }

        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchJitter = 0.04f)
        {
            if (clip == null || effects == null || Silent)
            {
                return;
            }
            effects.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            effects.PlayOneShot(clip, volume * Volume);
        }

        /// <summary>Menu sounds play even during the attract demo.</summary>
        public void PlayUi(AudioClip clip, float volume = 0.8f)
        {
            if (clip == null || effects == null)
            {
                return;
            }
            effects.pitch = 1f;
            effects.PlayOneShot(clip, volume * Volume);
        }

        void Update()
        {
            if (musicA == null)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            blend = Mathf.MoveTowards(blend, 1f, dt * 1.6f);
            AudioSource current = aIsCurrent ? musicA : musicB;
            AudioSource old = aIsCurrent ? musicB : musicA;
            float level = MusicLevel * MusicVolume * (Muffled ? 0.6f : 1f);
            current.volume = level * Mathf.Sqrt(blend);
            old.volume = level * Mathf.Sqrt(1f - blend);
            if (blend >= 1f && old.isPlaying)
            {
                old.Stop();
            }
            current.pitch = old.pitch = Mathf.MoveTowards(current.pitch, MusicPitch, dt * 0.5f);
            float cutoff = Muffled ? MuffledCutoff : OpenCutoff;
            filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, cutoff, 1f - Mathf.Exp(-dt * 6f));

            float windLevel = WindLevel * Volume * Mathf.Clamp01(WindAmount) * (Silent ? 0f : 1f);
            wind.volume = Mathf.MoveTowards(wind.volume, windLevel, dt * 0.6f);
            if (wind.volume > 0.001f && !wind.isPlaying && wind.clip != null)
            {
                wind.Play();
            }
            else if (wind.volume <= 0.001f && wind.isPlaying)
            {
                wind.Stop();
            }
        }
    }
}
