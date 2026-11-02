using System;
using System.Collections;
using System.IO;
using BalloonDash.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BalloonDash.Game
{
    /// <summary>
    /// Opt-in capture of README screenshots: <c>BalloonDash.exe -screenshot &lt;folder&gt; -scenario title,run,slide,balloon,powers,crash,pause,howto,records,settings</c>.
    /// Without the -screenshot argument nothing is created, so normal runs and WebGL builds
    /// (which have no command line) are unaffected. The same hook runs inside the editor
    /// (ProjectBuilder.CaptureScreenshots), which renders the camera and the HUD off screen and
    /// blends them, for machines that cannot run the player.
    /// </summary>
    public sealed class ScreenshotScenarios : MonoBehaviour
    {
        const string DefaultScenarios = "title,run,slide,balloon,powers,crash,pause,howto,records,settings";
        const int Width = 1280;
        const int Height = 720;

        string folder;
        string[] scenarios;
        RenderTexture sceneTarget;
        RenderTexture uiTarget;
        GameDirector director;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            string target = Argument("-screenshot");
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            var host = new GameObject(nameof(ScreenshotScenarios)).AddComponent<ScreenshotScenarios>();
            host.folder = target;
            host.scenarios = (Argument("-scenario") ?? DefaultScenarios).Split(',');
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            director = FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore();
            if (Application.isEditor)
            {
                RenderOffscreen();
            }
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (string raw in scenarios)
            {
                string scenario = raw.Trim();
                director.Seed(StableSeed(scenario));
                yield return Stage(scenario);
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(folder, scenario + ".png"), shot.EncodeToPNG());
                Destroy(shot);
                Debug.Log($"Saved screenshot {scenario}.png");
                director.ScriptedInput = false;
                director.Frozen = false;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        IEnumerator Stage(string scenario)
        {
            switch (scenario)
            {
                case "title":
                    director.ToTitle();
                    yield return new WaitForSecondsRealtime(3.5f);
                    break;
                case "howto":
                    director.ToTitle();
                    director.Activate("howto");
                    yield return new WaitForSecondsRealtime(1.2f);
                    break;
                case "records":
                    director.Records.Submit(2380, 1655, 71);
                    director.Records.Submit(1915, 1302, 61);
                    director.Records.Submit(964, 701, 26);
                    director.ToTitle();
                    director.Activate("records");
                    yield return new WaitForSecondsRealtime(1.2f);
                    break;
                case "settings":
                    director.ToTitle();
                    director.Activate("settings");
                    director.Hud.Menu.Selected = 1;
                    director.Hud.RefreshMenu();
                    yield return new WaitForSecondsRealtime(1.2f);
                    break;
                case "run":
                    yield return Begin(430f, 37);
                    Run run = director.Run;
                    run.Place(ObstacleKind.Crate, 3.6f);
                    run.Place(ObstacleKind.Spikes, 10.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        float t = (i - 2) / 2f;
                        run.Place(PickupKind.Coin, 11.7f + t * 2.2f, 0.9f + 1.4f * (1f - t * t));
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        run.Place(PickupKind.Coin, 5.8f + i * 0.75f, 0.55f);
                    }
                    director.Run.Laying = false;
                    director.PressJump();
                    director.SetJumpHeld(true);
                    yield return Advance(0.26f);
                    break;
                case "slide":
                    yield return Begin(760f, 58);
                    director.Run.Place(ObstacleKind.Sign, 1.4f);
                    director.Run.Place(ObstacleKind.Stack, 11f);
                    director.Run.Place(PickupKind.Coin, 11.5f, 3.2f);
                    for (int i = 0; i < 3; i++)
                    {
                        director.Run.Place(PickupKind.Coin, 1.6f + i * 0.6f, 0.4f);
                    }
                    director.PressSlide();
                    yield return Advance(0.1f);
                    break;
                case "balloon":
                    yield return Begin(905f, 64);
                    director.Run.Activate(PickupKind.Balloon, director.Run.Distance + 1f, 1.3f);
                    director.Run.ClearTrack();
                    director.SetJumpHeld(true);
                    yield return Advance(0.75f);
                    director.SetJumpHeld(false);
                    director.Run.ClearTrack();
                    director.Run.Place(ObstacleKind.Bomb, 4.5f, 5.2f);
                    director.Run.Place(ObstacleKind.Bomb, 8.5f, 1.4f);
                    director.Run.Place(ObstacleKind.Bomb, 8.5f, 4.8f);
                    director.Run.Place(ObstacleKind.Bomb, 12.5f, 2.6f);
                    for (int i = 0; i < 5; i++)
                    {
                        director.Run.Place(PickupKind.Coin, 6f + (i % 3) * 0.7f, 3.3f + (i / 3) * 0.7f);
                    }
                    yield return Advance(0.05f);
                    break;
                case "powers":
                    yield return Begin(1240f, 92);
                    director.Run.Activate(PickupKind.Magnet, director.Run.Distance, 1f);
                    yield return Advance(1.1f);
                    director.Run.ClearTrack();
                    for (int i = 0; i < 6; i++)
                    {
                        director.Run.Place(PickupKind.Coin, 2.2f + i * 0.75f, 2.8f - i * 0.25f);
                    }
                    director.Run.Place(ObstacleKind.Barrel, 9f);
                    director.Run.Place(ObstacleKind.Crate, 13f);
                    yield return Advance(0.08f);
                    break;
                case "crash":
                    yield return Begin(560f, 43);
                    director.Run.Place(ObstacleKind.Barrel, 3.5f);
                    float waited = 0f;
                    director.Frozen = false;
                    while (director.Screen != GameScreen.Results && waited < 8f)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(0.8f);
                    break;
                case "pause":
                    yield return Begin(320f, 24);
                    director.Run.Place(ObstacleKind.Crate, 6f);
                    director.Run.Place(ObstacleKind.Sign, 13f);
                    yield return Advance(0.1f);
                    director.Frozen = false;
                    director.TogglePause();
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                default:
                    Debug.LogWarning($"Unknown screenshot scenario '{scenario}'.");
                    break;
            }
        }

        /// <summary>Starts a run partway in, with nothing on the track and some coins already collected.</summary>
        IEnumerator Begin(float metres, int coins)
        {
            director.StartRun();
            director.ScriptedInput = true;
            director.HitStopEnabled = true;
            Run run = director.Run;
            run.Skip(metres);
            run.AddCoins(coins);
            run.Laying = false;
            run.ClearTrack();
            director.Frozen = false;
            director.Hud.HideBanner();
            yield return new WaitForSecondsRealtime(0.5f);
            director.Hud.HideBanner();
        }

        /// <summary>Lets the run play for a moment, then holds it still for the picture.</summary>
        IEnumerator Advance(float seconds)
        {
            director.Frozen = false;
            yield return new WaitForSecondsRealtime(seconds);
            director.Frozen = true;
            yield return new WaitForSecondsRealtime(0.15f);
        }

        /// <summary>Points the camera and the HUD panel at render textures of a fixed size.</summary>
        void RenderOffscreen()
        {
            sceneTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = sceneTarget;
            var document = director.Hud.GetComponent<UIDocument>();
            PanelSettings panel = Instantiate(document.panelSettings);
            panel.targetTexture = uiTarget;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
        }

        /// <summary>Blends the HUD over the world, like the screen would show them.</summary>
        Texture2D CaptureOffscreen()
        {
            Color[] scene = Read(sceneTarget);
            Color[] ui = Read(uiTarget);
            for (int i = 0; i < scene.Length; i++)
            {
                Color over = ui[i];
                Color result = Color.Lerp(scene[i], new Color(over.r, over.g, over.b), over.a);
                result.a = 1f;
                scene[i] = result;
            }
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.SetPixels(scene);
            shot.Apply();
            return shot;
        }

        static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Destroy(texture);
            return pixels;
        }

        static int StableSeed(string text)
        {
            int seed = 17;
            foreach (char c in text)
            {
                seed = seed * 31 + c;
            }
            return seed & 0xffff;
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
