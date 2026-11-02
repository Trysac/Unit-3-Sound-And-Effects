using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>The generated particle materials and the synthesized audio, wired up by ProjectBuilder.</summary>
    public sealed class GameArt : ScriptableObject
    {
        [Header("Materials")]
        [Tooltip("Additive soft dots: sparkles, flashes and explosions.")]
        public Material Glow;
        [Tooltip("Alpha-blended soft dots: dust, smoke and debris.")]
        public Material Puff;

        [Header("Music")]
        public AudioClip TitleMusic;
        public AudioClip RunMusic;
        public AudioClip SkyMusic;
        public AudioClip Wind;

        [Header("Runner")]
        public AudioClip Step;
        public AudioClip Jump;
        public AudioClip DoubleJump;
        public AudioClip Land;
        public AudioClip Slide;
        public AudioClip Dive;

        [Header("Pickups")]
        public AudioClip Coin;
        public AudioClip BalloonUp;
        public AudioClip BalloonPop;
        public AudioClip Boing;
        public AudioClip Magnet;
        public AudioClip ShieldUp;
        public AudioClip ShieldBreak;
        public AudioClip PowerEnd;

        [Header("Hits")]
        public AudioClip Smash;
        public AudioClip Crash;
        public AudioClip Bomb;
        public AudioClip Poof;

        [Header("Run")]
        public AudioClip Milestone;
        public AudioClip NewRecord;
        public AudioClip GameOver;
        public AudioClip Go;

        [Header("Menus")]
        public AudioClip Move;
        public AudioClip Select;
        public AudioClip Pause;
    }
}
