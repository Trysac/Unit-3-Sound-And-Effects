using System.Collections;
using System.Linq;
using BalloonDash.Core;
using BalloonDash.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BalloonDash.Tests
{
    /// <summary>
    /// The game's flow in the real scene: title, start, jumping, coins, the balloon, a crash and
    /// the results, a new run, pause and settings, with what the HUD and the music do at each step.
    /// </summary>
    public class RunFlowTests
    {
        GameDirector director;
        VisualElement ui;

        IEnumerator Load()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
            director = Object.FindAnyObjectByType<GameDirector>();
            Assert.That(director, Is.Not.Null);
            director.UseMemoryStore();
            director.Seed(1);
            director.HitStopEnabled = false;
            director.ScriptedInput = true;
            ui = director.Hud.GetComponent<UIDocument>().rootVisualElement;
        }

        /// <summary>Starts a run with nothing on the track, so the test places every obstacle.</summary>
        IEnumerator StartQuiet()
        {
            director.Activate("play");
            director.Run.Laying = false;
            director.Run.ClearTrack();
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
        }

        /// <summary>Runs into a crate and waits for the results screen.</summary>
        IEnumerator Crash()
        {
            director.Run.Place(ObstacleKind.Crate, 2f);
            float waited = 0f;
            while (director.Screen != GameScreen.Results && waited < 6f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Results));
        }

        string Text(string name) => ui.Q<Label>(name).text;

        static bool Visible(VisualElement element) => !element.ClassListContains("hidden");

        [UnityTest]
        public IEnumerator TheGameOpensOnTheTitleWithTheAutopilotRunningBehindIt()
        {
            yield return Load();
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));
            Assert.That(director.Demo, Is.True);
            Assert.That(Visible(ui.Q("menu")), Is.True);
            Assert.That(Visible(ui.Q("logo")), Is.True);
            Assert.That(Visible(ui.Q("play-hud")), Is.False);
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id).Take(4), Is.EqualTo(new[] { "play", "howto", "records", "settings" }));
            Assert.That(director.Sound.CurrentMusic, Is.EqualTo(director.Art.TitleMusic));

            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(director.Run.Distance, Is.GreaterThan(15f), "the demo runs");
            Assert.That(director.Run.IsOver, Is.False, "the autopilot clears the first obstacles");
            Assert.That(director.World.ObstaclesShown + director.World.PickupsShown, Is.GreaterThan(0));
            Assert.That(director.Records.Runs, Is.EqualTo(0), "the demo is not recorded");
        }

        [UnityTest]
        public IEnumerator PlayStartsARunWithAnEmptyHudAndTheRunMusic()
        {
            yield return Load();
            director.Activate("play");
            yield return null;

            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(director.Demo, Is.False);
            Assert.That(Visible(ui.Q("menu")), Is.False);
            Assert.That(Visible(ui.Q("play-hud")), Is.True);
            Assert.That(Text("coins"), Is.EqualTo("0"));
            Assert.That(Text("best"), Is.EqualTo("0"));
            Assert.That(Text("prompt"), Does.Contain("JUMP"));
            Assert.That(director.Hud.BannerText, Is.EqualTo("GO!"));
            Assert.That(director.Sound.CurrentMusic, Is.EqualTo(director.Art.RunMusic));

            yield return new WaitForSecondsRealtime(1f);
            Assert.That(director.Run.Distance, Is.GreaterThan(5f));
            Assert.That(Text("distance"), Is.EqualTo($"{(int)director.Run.Distance} M"));
            Assert.That(Text("score"), Is.EqualTo(director.Run.Points.ToString()));
        }

        [UnityTest]
        public IEnumerator JumpingClearsACrate()
        {
            yield return Load();
            yield return StartQuiet();
            director.Run.Place(ObstacleKind.Crate, 3f);
            director.PressJump();
            director.SetJumpHeld(true);
            yield return null;
            Assert.That(director.Run.Runner.Grounded, Is.False);

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(director.Run.IsOver, Is.False);
            Assert.That(director.Run.Jumps, Is.EqualTo(1));
            Assert.That(director.Run.Runner.Grounded, Is.True, "landed again");
        }

        [UnityTest]
        public IEnumerator CoinsCountOnTheHudAndTheBalloonSwitchesToTheSkyTheme()
        {
            yield return Load();
            yield return StartQuiet();
            for (int i = 0; i < 4; i++)
            {
                director.Run.Place(PickupKind.Coin, 1f + i * 0.7f, 0.6f);
            }
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(director.Run.Coins, Is.EqualTo(4));
            Assert.That(Text("coins"), Is.EqualTo("4"));
            Assert.That(Text("score"), Is.EqualTo(director.Run.Points.ToString()));

            director.Run.Place(PickupKind.Balloon, 1.2f, 1f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(director.Run.Runner.Floating, Is.True);
            Assert.That(director.Hud.BannerText, Is.EqualTo("BALLOON!"));
            Assert.That(director.Sound.CurrentMusic, Is.EqualTo(director.Art.SkyMusic));
            Assert.That(director.Hud.PowerChipTexts.Single(), Does.StartWith("BALLOON"));
            Assert.That(director.Sound.WindAmount, Is.EqualTo(1f));
            Assert.That(Text("prompt"), Does.Contain("RISE"));
        }

        [UnityTest]
        public IEnumerator ACrashShowsTheResultsAndRecordsTheRun()
        {
            yield return Load();
            yield return StartQuiet();
            director.Run.AddCoins(5);
            yield return Crash();

            Run run = director.Run;
            Assert.That(run.IsOver, Is.True);
            Assert.That(run.EndReason, Is.EqualTo(EndReason.Crashed));
            Assert.That(Visible(ui.Q("menu")), Is.True);
            Assert.That(Text("menu-title"), Is.EqualTo("CRASHED!"));
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id), Is.EqualTo(new[] { "again", "menu" }));
            Assert.That(director.LastRecord.NewBest, Is.True);
            Assert.That(director.Records.Best, Is.EqualTo(run.Points));
            Assert.That(director.Records.Runs, Is.EqualTo(1));
            Assert.That(director.Records.TotalCoins, Is.EqualTo(5));
            Assert.That(Text("best"), Is.EqualTo(run.Points.ToString()));
            Assert.That(director.Sound.Muffled, Is.True);
            Assert.That(director.Sound.CurrentMusic, Is.Null, "the music stops on a crash");
            Label score = ui.Q("menu-body").Query<Label>(className: "body-line__value").First();
            Assert.That(score.text, Is.EqualTo($"{run.Points}   NEW BEST!"));
        }

        [UnityTest]
        public IEnumerator RunAgainStartsAFreshRunAndShowsTheNewBest()
        {
            yield return Load();
            yield return StartQuiet();
            director.Run.AddCoins(3);
            yield return Crash();
            int best = director.Run.Points;

            director.Activate("again");
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(director.Run.IsOver, Is.False);
            Assert.That(director.Run.Coins, Is.EqualTo(0));
            Assert.That(director.Run.Distance, Is.LessThan(1f));
            Assert.That(Visible(ui.Q("menu")), Is.False);
            Assert.That(Text("coins"), Is.EqualTo("0"));
            Assert.That(Text("best"), Is.EqualTo(best.ToString()));
            Assert.That(director.Sound.Muffled, Is.False);
            Assert.That(director.Sound.CurrentMusic, Is.EqualTo(director.Art.RunMusic));

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(director.Run.Distance, Is.GreaterThan(2f), "the new run moves");
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheRunAndResumeCarriesOn()
        {
            yield return Load();
            yield return StartQuiet();
            yield return new WaitForSecondsRealtime(0.3f);

            director.TogglePause();
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Paused));
            Assert.That(Text("menu-title"), Is.EqualTo("PAUSED"));
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id), Is.EqualTo(new[] { "resume", "restart", "menu" }));
            Assert.That(director.Sound.Muffled, Is.True);
            float distance = director.Run.Distance;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(director.Run.Distance, Is.EqualTo(distance), "nothing moves while paused");

            director.Activate("resume");
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Playing));
            Assert.That(Visible(ui.Q("menu")), Is.False);
            Assert.That(director.Sound.Muffled, Is.False);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(director.Run.Distance, Is.GreaterThan(distance));

            director.TogglePause();
            director.Activate("menu");
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));
            Assert.That(director.Records.Runs, Is.EqualTo(0), "a run left from the pause menu is not recorded");
        }

        [UnityTest]
        public IEnumerator SettingsChangeTheVolumesAndTheShake()
        {
            yield return Load();
            director.Activate("settings");
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Settings));
            Assert.That(director.Hud.Menu.Items.Select(item => item.Id), Is.EqualTo(new[] { "volume", "music", "shake", "mute", "back" }));

            director.Change("volume", -1);
            director.Change("music", -3);
            director.Change("shake", 1);
            yield return null;
            Assert.That(director.Settings.Volume, Is.EqualTo(7));
            Assert.That(director.Settings.Music, Is.EqualTo(4));
            Assert.That(director.Settings.ScreenShake, Is.False);
            Assert.That(director.Sound.Volume, Is.EqualTo(0.7f).Within(1e-4f));
            Assert.That(director.Sound.MusicVolume, Is.EqualTo(0.4f).Within(1e-4f));
            Assert.That(director.Hud.Menu.Items.Select(item => item.Value).Take(3), Is.EqualTo(new[] { "7", "4", "OFF" }));

            director.Change("mute", 1);
            yield return null;
            Assert.That(director.Sound.Muted, Is.True);
            Assert.That(AudioListener.volume, Is.EqualTo(0f));
            Assert.That(Text("sound"), Is.EqualTo("SOUND OFF  (M)"));
            director.Change("mute", 1);

            director.Activate("back");
            yield return null;
            Assert.That(director.Screen, Is.EqualTo(GameScreen.Title));
            Assert.That(director.Hud.Menu.Items[director.Hud.Menu.Selected].Id, Is.EqualTo("settings"));
        }
    }
}
