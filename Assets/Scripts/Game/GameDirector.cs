using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game
{
    public enum GameScreen
    {
        Title,
        HowToPlay,
        Records,
        Settings,
        Playing,
        Paused,
        Results,
    }

    /// <summary>
    /// Runs the game: the menus, with the autopilot running a demo behind the title, and the runs
    /// themselves. It feeds the buttons to the engine-free <see cref="Run"/>, turns its events into
    /// sound, particles, squash and stretch, shake, hit-stop and banners, and keeps the settings
    /// and the high scores.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        const float ResultsDelay = 1.5f;
        const float MaxFrame = 1f / 20f;
        const float PromptSeconds = 5f;
        const float CoinStreakGap = 0.6f;

        /// <summary>Semitones of a major scale: coins collected in a row climb it.</summary>
        static readonly int[] CoinScale = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };

        [SerializeField] GameArt art;
        [SerializeField] Hud hud;
        [SerializeField] CameraRig cameraRig;

        WorldView world;
        AudioDirector sound;
        readonly GameInput input = new GameInput();
        Autopilot pilot;
        IRecordStore store;
        HighScores records;
        GameSettings settings;
        int seed;
        float hitStop;
        float overTime;
        bool recorded;
        bool floatHinted;
        float lastCoinTime = -10f;
        int coinStreak;
        int stepFoot;
        float poofCooldown;
        RecordUpdate lastRecord;

        public GameScreen Screen { get; private set; }
        public Run Run { get; private set; }
        public HighScores Records => records;
        public GameSettings Settings => settings;
        public Hud Hud => hud;
        public WorldView World => world;
        public AudioDirector Sound => sound;
        public GameArt Art => art;
        public RecordUpdate LastRecord => lastRecord;

        /// <summary>True while the run behind the menus is the autopilot's demo.</summary>
        public bool Demo { get; private set; }

        /// <summary>Tests and the screenshot hook drive the game instead of the devices.</summary>
        public bool ScriptedInput { get; set; }

        /// <summary>Holds the run still while the world keeps animating (the screenshot hook frames a moment).</summary>
        public bool Frozen { get; set; }

        /// <summary>Freezes the run for hit-stop; tests turn it off to keep timings exact.</summary>
        public bool HitStopEnabled { get; set; } = true;

        void Awake()
        {
            seed = System.Environment.TickCount;
            store = new PlayerPrefsStore();
            LoadOptions();
            sound = gameObject.AddComponent<AudioDirector>();
            sound.Init(settings.Volume / 10f, settings.Music / 10f, settings.Muted, art != null ? art.Wind : null);
            world = new GameObject("World").AddComponent<WorldView>();
            world.Build(art);
            hud.ItemClicked += OnItemClicked;
            hud.PauseClicked += () =>
            {
                if (Screen == GameScreen.Playing)
                {
                    TogglePause();
                }
            };
        }

        void Start()
        {
            ToTitle();
            UpdateSoundLabel();
        }

        void LoadOptions()
        {
            records = new HighScores(store);
            settings = GameSettings.Load(store);
            ApplyOptions();
        }

        void ApplyOptions()
        {
            if (cameraRig != null)
            {
                cameraRig.ShakeScale = settings.ScreenShake ? 1f : 0f;
            }
            if (sound != null)
            {
                sound.SetVolumes(settings.Volume / 10f, settings.Music / 10f);
                sound.SetMuted(settings.Muted);
            }
        }

        /// <summary>Keeps records and settings in memory only (tests and screenshots).</summary>
        public void UseMemoryStore()
        {
            store = new MemoryRecordStore();
            LoadOptions();
            UpdateSoundLabel();
        }

        /// <summary>Makes the next runs repeatable.</summary>
        public void Seed(int value) => seed = value;

        int NextSeed() => seed++;

        // ---- Flow ----

        public void ToTitle(int selected = 0)
        {
            Screen = GameScreen.Title;
            StartDemo();
            hud.ShowPlayHud(false);
            hud.HideBanner();
            hud.ClearFloaters();
            hud.SetSkyFrame(false);
            sound.PlayMusic(art.TitleMusic);
            sound.Muffled = false;
            sound.MusicPitch = 1f;
            sound.WindAmount = 0f;
            ShowTitleMenu(selected);
        }

        void StartDemo()
        {
            Demo = true;
            sound.Silent = true;
            Run = new Run(NextSeed());
            pilot = new Autopilot();
            world.Reset();
            cameraRig.Calm();
            hitStop = 0f;
        }

        /// <summary>Starts a new run.</summary>
        public void StartRun()
        {
            Demo = false;
            sound.Silent = false;
            Run = new Run(NextSeed());
            pilot = null;
            world.Reset();
            Screen = GameScreen.Playing;
            hitStop = 0f;
            overTime = 0f;
            recorded = false;
            floatHinted = false;
            coinStreak = 0;
            lastRecord = default;
            hud.HideMenu();
            hud.HideBanner();
            hud.ClearFloaters();
            hud.ShowPlayHud(true);
            hud.SetRun(records.Best);
            cameraRig.Calm();
            sound.PlayMusic(art.RunMusic);
            sound.Muffled = false;
            sound.MusicPitch = 1f;
            sound.WindAmount = 0f;
            sound.Play(art.Go, 0.8f, 1f, 0f);
            hud.Banner("GO!", null, 700);
        }

        public void Restart() => StartRun();

        public void TogglePause()
        {
            if (Screen == GameScreen.Playing)
            {
                Screen = GameScreen.Paused;
                sound.Muffled = true;
                sound.PlayUi(art.Pause);
                ShowPauseMenu(0);
            }
            else if (Screen == GameScreen.Paused)
            {
                Screen = GameScreen.Playing;
                sound.Muffled = false;
                sound.PlayUi(art.Pause);
                hud.HideMenu();
            }
        }

        void ShowResults()
        {
            Screen = GameScreen.Results;
            sound.Muffled = true;
            sound.Play(lastRecord.NewBest ? art.NewRecord : art.GameOver, 0.9f, 1f, 0f);
            ShowResultsMenu(0);
        }

        // ---- Buttons (the devices, the tests and the screenshot hook) ----

        public void PressJump()
        {
            if (Screen == GameScreen.Playing)
            {
                Run.PressJump();
            }
        }

        public void SetJumpHeld(bool held)
        {
            if (Screen == GameScreen.Playing)
            {
                Run.SetJumpHeld(held);
            }
        }

        public void PressSlide()
        {
            if (Screen == GameScreen.Playing)
            {
                Run.PressSlide();
            }
        }

        // ---- Frame ----

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrame);
            if (!ScriptedInput)
            {
                input.Read(point => hud.IsOverPauseButton(point) || hud.IsOverMenu(point));
                if (input.Mute)
                {
                    settings.Muted = !settings.Muted;
                    ApplyOptions();
                    settings.Save(store);
                    UpdateSoundLabel();
                    if (Screen == GameScreen.Settings)
                    {
                        ShowSettingsMenu(hud.Menu != null ? hud.Menu.Selected : 0);
                    }
                }
            }

            switch (Screen)
            {
                case GameScreen.Playing:
                    if (!ScriptedInput)
                    {
                        if (input.Pause)
                        {
                            TogglePause();
                            break;
                        }
                        if (input.JumpPressed)
                        {
                            PressJump();
                        }
                        SetJumpHeld(input.JumpHeld);
                        if (input.SlidePressed)
                        {
                            PressSlide();
                        }
                    }
                    if (!Frozen)
                    {
                        StepRun(dt);
                    }
                    break;
                case GameScreen.Paused:
                case GameScreen.Results:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    break;
                default:
                    if (!ScriptedInput)
                    {
                        MenuInput();
                    }
                    StepDemo(dt);
                    break;
            }

            if (Run != null)
            {
                bool live = Screen != GameScreen.Paused && !(Screen == GameScreen.Playing && hitStop > 0f);
                cameraRig.Target = Run.IsOver ? 0f : Run.Runner.Y;
                world.Sync(Run, dt, cameraRig.transform.position.y, live);
                if (world.Stepped && !Demo && Screen == GameScreen.Playing)
                {
                    // Footsteps alternate a little in pitch, like left and right feet.
                    stepFoot = 1 - stepFoot;
                    sound.Play(art.Step, 0.32f, stepFoot == 0 ? 1f : 0.88f, 0.05f);
                }
                if (!Demo)
                {
                    UpdateHud();
                }
            }
        }

        void StepDemo(float dt)
        {
            if (Run.IsOver)
            {
                overTime += dt;
                if (overTime > 1.2f)
                {
                    overTime = 0f;
                    StartDemo();
                }
                Run.ClearEvents();
                return;
            }
            pilot.Drive(Run, dt);
            Run.Step(dt);
            Run.ClearEvents();
        }

        void StepRun(float dt)
        {
            poofCooldown -= dt;
            if (Run.IsOver)
            {
                hitStop = Mathf.Max(0f, hitStop - dt);
                React();
                // Real time, so a slow frame cannot stretch the pause before the results.
                overTime += Time.unscaledDeltaTime;
                if (overTime >= ResultsDelay)
                {
                    ShowResults();
                }
                return;
            }
            if (!Consume(ref dt))
            {
                Run.Step(dt);
            }
            React();
            // The music climbs a little with the speed.
            sound.MusicPitch = 1f + 0.06f * SpeedCurve.Fraction(Run.Distance);
            sound.WindAmount = Run.Runner.Floating ? 1f : 0f;
        }

        /// <summary>Spends hit-stop time; true when the run should not move this frame.</summary>
        bool Consume(ref float dt)
        {
            if (hitStop <= 0f)
            {
                return false;
            }
            hitStop -= dt;
            if (hitStop > 0f)
            {
                return true;
            }
            dt = -hitStop;
            hitStop = 0f;
            return dt <= 0f;
        }

        void Freeze(float seconds)
        {
            if (HitStopEnabled)
            {
                hitStop = Mathf.Max(hitStop, seconds);
            }
        }

        // ---- Game feel ----

        void React()
        {
            foreach (RunEvent e in Run.Events)
            {
                Vector2 at = WorldView.ToWorld(Run, e.X, e.Y);
                Vector2 feet = world.Feet;
                float speed = Run.Speed;
                switch (e.Kind)
                {
                    case RunEventKind.Jumped:
                        sound.Play(art.Jump, 0.6f);
                        world.Squash(0.3f);
                        Effects.Burst(feet, new Vector2(-1f, 0.4f), art.Puff, Palette.Dust, Palette.Hex("#e0a8a0"), 8, 3f, 0.4f, 0.45f, 70f, -speed * 0.5f);
                        break;
                    case RunEventKind.DoubleJumped:
                        sound.Play(art.DoubleJump, 0.6f);
                        world.Flip();
                        Effects.Burst(feet + new Vector2(0f, 0.2f), Vector2.zero, art.Glow, Palette.Scarf, Color.white, 16, 4f, 0.22f, 0.35f, 70f, -speed * 0.3f);
                        break;
                    case RunEventKind.Landed:
                        float impact = Mathf.Clamp01(e.Value / Tuning.MaxFallSpeed);
                        sound.Play(art.Land, 0.25f + 0.6f * impact, 1.1f - 0.25f * impact);
                        world.Squash(-0.15f - 0.3f * impact);
                        Effects.Burst(feet, Vector2.up, art.Puff, Palette.Dust, Palette.Hex("#e0a8a0"), 6 + (int)(18 * impact), 2f + 4f * impact, 0.45f, 0.5f, 160f, -speed * 0.6f, 0.4f);
                        if (impact > 0.7f)
                        {
                            cameraRig.Shake(0.12f * impact);
                        }
                        break;
                    case RunEventKind.SlideStarted:
                        sound.Play(art.Slide, 0.55f);
                        world.Squash(-0.2f);
                        Effects.Burst(feet + new Vector2(0.3f, 0.05f), new Vector2(-1f, 0.5f), art.Glow, Palette.Hazard, Palette.Tangerine, 10, 5f, 0.12f, 0.3f, 40f, -speed * 0.3f, 1.5f);
                        break;
                    case RunEventKind.Dived:
                        sound.Play(art.Dive, 0.55f);
                        world.Squash(0.35f);
                        break;
                    case RunEventKind.Coin:
                        OnCoin(at);
                        break;
                    case RunEventKind.PowerUp:
                        OnPowerUp(e, at);
                        break;
                    case RunEventKind.PowerEnded:
                        OnPowerEnded(e);
                        break;
                    case RunEventKind.FloatBounced:
                        sound.Play(art.Boing, 0.7f);
                        world.Squash(-0.35f);
                        Effects.Burst(feet, Vector2.up, art.Puff, Palette.Dust, Palette.Grass, 14, 4f, 0.45f, 0.5f, 150f, -speed * 0.6f, 0.4f);
                        cameraRig.Shake(0.08f);
                        break;
                    case RunEventKind.Smashed:
                        sound.Play(art.ShieldBreak, 0.8f);
                        sound.Play(art.Smash, 0.9f);
                        Debris(at, e.Obstacle, 30);
                        Effects.Burst(at, Vector2.zero, art.Glow, Palette.Shield, Color.white, 40, 9f, 0.3f, 0.5f);
                        cameraRig.Shake(0.4f);
                        cameraRig.Kick(Vector2.left, 0.25f);
                        cameraRig.Punch(0.04f);
                        Freeze(0.08f);
                        hud.Float(at + new Vector2(0f, 1f), "SMASH!", Palette.Shield, 30f);
                        break;
                    case RunEventKind.Cleared:
                        if (poofCooldown <= 0f)
                        {
                            sound.Play(art.Poof, 0.6f);
                            poofCooldown = 0.1f;
                        }
                        Effects.Burst(at, Vector2.zero, art.Puff, Color.white, Palette.Cloud, 18, 3f, 0.7f, 0.6f);
                        break;
                    case RunEventKind.Crashed:
                        sound.Play(art.Crash, 1f, 1f, 0.02f);
                        Debris(at, e.Obstacle, 18);
                        Effects.Burst(at, Vector2.zero, art.Glow, Color.white, Palette.Hazard, 26, 7f, 0.3f, 0.35f);
                        cameraRig.Shake(0.7f);
                        cameraRig.Kick(Vector2.left, 0.45f);
                        cameraRig.Punch(0.06f);
                        Freeze(0.14f);
                        hud.Banner("OUCH!", "danger", 1100);
                        break;
                    case RunEventKind.Exploded:
                        sound.Play(art.Bomb, 1f, 1f, 0f);
                        Effects.Burst(at, Vector2.zero, art.Glow, Palette.Danger, Palette.Hazard, 90, 13f, 0.5f, 1.1f);
                        Effects.Burst(at, Vector2.zero, art.Glow, Color.white, Palette.Tangerine, 30, 5f, 0.7f, 0.6f);
                        Effects.Burst(at, Vector2.zero, art.Puff, Palette.Ink, Palette.Hex("#57306f"), 24, 3f, 1.2f, 1.2f);
                        cameraRig.Shake(1f);
                        cameraRig.Kick(Vector2.left, 0.5f);
                        cameraRig.Punch(0.08f);
                        Freeze(0.2f);
                        hud.Banner("BOOM!", "danger", 1300);
                        break;
                    case RunEventKind.Milestone:
                        sound.Play(art.Milestone, 0.6f, 1f, 0f);
                        hud.Banner($"{(int)e.Value} M", null, 650);
                        break;
                    case RunEventKind.Over:
                        OnOver();
                        break;
                }
            }
            Run.ClearEvents();
        }

        void OnCoin(Vector2 at)
        {
            float now = Run.Elapsed;
            coinStreak = now - lastCoinTime < CoinStreakGap ? Mathf.Min(coinStreak + 1, CoinScale.Length - 1) : 0;
            lastCoinTime = now;
            float pitch = Mathf.Pow(2f, CoinScale[coinStreak] / 12f);
            sound.Play(art.Coin, 0.5f, pitch, 0f);
            Effects.Burst(at, Vector2.zero, art.Glow, Palette.Gold, Color.white, 8, 3.5f, 0.18f, 0.3f, 70f, -Run.Speed * 0.3f);
        }

        void OnPowerUp(RunEvent e, Vector2 at)
        {
            Color color = Palette.Of(e.Pickup);
            Effects.Burst(at, Vector2.zero, art.Glow, color, Color.white, 46, 8f, 0.35f, 0.7f);
            cameraRig.Shake(0.18f);
            cameraRig.Punch(0.03f);
            switch (e.Pickup)
            {
                case PickupKind.Balloon:
                    sound.Play(art.BalloonUp, 0.9f, 1f, 0f);
                    sound.PlayMusic(art.SkyMusic);
                    hud.Banner("BALLOON!", "balloon", 900);
                    hud.SetSkyFrame(true);
                    break;
                case PickupKind.Magnet:
                    sound.Play(art.Magnet, 0.9f, 1f, 0f);
                    hud.Banner("MAGNET", "magnet", 900);
                    break;
                case PickupKind.Shield:
                    sound.Play(art.ShieldUp, 0.9f, 1f, 0f);
                    hud.Banner("SHIELD", "shield", 900);
                    break;
            }
        }

        void OnPowerEnded(RunEvent e)
        {
            if (e.Pickup == PickupKind.Balloon)
            {
                sound.Play(art.BalloonPop, 0.9f);
                Vector2 at = world.Feet + new Vector2(0.1f, 3.2f);
                Effects.Burst(at, Vector2.zero, art.Glow, Palette.Balloon, Palette.Hazard, 30, 7f, 0.22f, 0.6f, 70f, 0f, 0.6f);
                hud.SetSkyFrame(false);
                if (!Run.IsOver)
                {
                    sound.PlayMusic(art.RunMusic);
                }
            }
            else
            {
                sound.Play(art.PowerEnd, 0.7f);
            }
        }

        /// <summary>Splinters, shards or sparks, by what was hit.</summary>
        void Debris(Vector2 at, ObstacleKind kind, int count)
        {
            Color a = Palette.Wood;
            Color b = Palette.WoodDark;
            if (kind == ObstacleKind.Spikes)
            {
                a = Palette.Steel;
                b = Palette.Hex("#8f96b8");
            }
            else if (kind == ObstacleKind.Sign)
            {
                a = Palette.Hazard;
                b = Palette.Ink;
            }
            Effects.Burst(at, new Vector2(1f, 0.8f), art.Puff, a, b, count, 9f, 0.22f, 0.9f, 120f, 0f, 2.2f);
        }

        void OnOver()
        {
            sound.PlayMusic(null);
            sound.MusicPitch = 1f;
            sound.WindAmount = 0f;
            sound.Muffled = true;
            hud.SetSkyFrame(false);
            RecordResult();
            if (lastRecord.NewBest)
            {
                hud.SetBest(Run.Points);
            }
        }

        void RecordResult()
        {
            if (recorded)
            {
                return;
            }
            recorded = true;
            lastRecord = records.Submit(Run.Points, (int)Run.Distance, Run.Coins);
        }

        void UpdateHud()
        {
            hud.SetDistance(Run.Distance);
            hud.SetCoins(Run.Coins);
            hud.SetScore(Run.Points);
            hud.SetPowers(Run.FloatLeft, Run.MagnetLeft, Run.HasShield);
            hud.SetPauseButton(Screen == GameScreen.Playing);
            string prompt = null;
            if (Screen == GameScreen.Playing && !Run.IsOver)
            {
                if (Run.Runner.Floating && !floatHinted && Run.FloatLeft > Tuning.FloatSeconds - 3f)
                {
                    prompt = input.UsingTouch ? "HOLD THE RIGHT SIDE TO RISE  -  LET GO TO SINK" : "HOLD JUMP TO RISE  -  LET GO TO SINK";
                }
                else if (Run.Elapsed < PromptSeconds)
                {
                    prompt = input.UsingGamepad
                        ? "A TO JUMP (AGAIN IN THE AIR)  -  B TO SLIDE  -  START TO PAUSE"
                        : input.UsingTouch
                            ? "TAP THE RIGHT SIDE TO JUMP  -  TAP THE LEFT SIDE OR SWIPE DOWN TO SLIDE"
                            : "SPACE TO JUMP (AGAIN IN THE AIR)  -  DOWN TO SLIDE  -  ESC TO PAUSE";
                }
                if (Run.Runner.Floating && Run.FloatLeft <= Tuning.FloatSeconds - 3f)
                {
                    floatHinted = true;
                }
            }
            hud.SetPrompt(prompt);
        }

        // ---- Menus ----

        void ShowTitleMenu(int selected)
        {
            var menu = new MenuModel
            {
                Logo = true,
                Subtitle = "RUN  -  JUMP  -  SLIDE  -  FLOAT OVER THE BOMBS",
                Selected = selected,
                Hint = MenuHint(),
            };
            menu.Add("play", "PLAY");
            menu.Add("howto", "HOW TO PLAY");
            menu.Add("records", "HIGH SCORES");
            menu.Add("settings", "SETTINGS");
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                menu.Add("quit", "QUIT");
            }
            hud.ShowMenu(menu);
        }

        void ShowHowToPlay()
        {
            Screen = GameScreen.HowToPlay;
            var menu = new MenuModel { Title = "HOW TO PLAY", Subtitle = "KEYBOARD, MOUSE, GAMEPAD OR TOUCH", Hint = MenuHint() };
            menu.Body.Add(("JUMP", "SPACE / UP / CLICK / A", false));
            menu.Body.Add(("DOUBLE JUMP", "JUMP AGAIN IN THE AIR", false));
            menu.Body.Add(("SHORT HOP", "LET GO OF JUMP EARLY", false));
            menu.Body.Add(("SLIDE  (DIVE IN THE AIR)", "DOWN / RIGHT CLICK / B", false));
            menu.Body.Add(("CRATES, SPIKES, BARRELS", "JUMP THEM", false));
            menu.Body.Add(("HANGING SIGNS", "SLIDE UNDER", false));
            menu.Body.Add(("BALLOON", "7 S AFLOAT: HOLD JUMP TO RISE", false));
            menu.Body.Add(("MAGNET  /  SHIELD", "PULLS COINS  /  TAKES A HIT", false));
            menu.Body.Add(("SCORE", "1 PER METRE + 10 PER COIN", false));
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowRecordsMenu()
        {
            Screen = GameScreen.Records;
            var menu = new MenuModel { Title = "HIGH SCORES", Subtitle = "BEST RUNS ON THIS DEVICE", Hint = MenuHint() };
            menu.Body.Add(("BEST SCORE", records.Best.ToString(), false));
            menu.Body.Add(("LONGEST RUN", $"{records.BestDistance} M", false));
            menu.Body.Add(("MOST COINS IN A RUN", records.MostCoins.ToString(), false));
            menu.Body.Add(("COINS COLLECTED", records.TotalCoins.ToString(), false));
            menu.Body.Add(("RUNS", records.Runs.ToString(), false));
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowSettingsMenu(int selected)
        {
            Screen = GameScreen.Settings;
            var menu = new MenuModel { Title = "SETTINGS", Selected = selected, Hint = MenuHint(withChange: true) };
            menu.Add("volume", "EFFECTS VOLUME", settings.Volume.ToString());
            menu.Add("music", "MUSIC VOLUME", settings.Music.ToString());
            menu.Add("shake", "SCREEN SHAKE", settings.ScreenShake ? "ON" : "OFF");
            menu.Add("mute", "SOUND", settings.Muted ? "OFF" : "ON");
            menu.Add("back", "BACK");
            hud.ShowMenu(menu);
        }

        void ShowPauseMenu(int selected)
        {
            var menu = new MenuModel
            {
                Title = "PAUSED",
                Subtitle = $"{(int)Run.Distance} M  -  {Run.Coins} COINS",
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            menu.Add("resume", "RESUME");
            menu.Add("restart", "RESTART RUN");
            menu.Add("menu", "MAIN MENU");
            hud.ShowMenu(menu);
        }

        void ShowResultsMenu(int selected)
        {
            var menu = new MenuModel
            {
                Title = Run.EndReason == EndReason.Exploded ? "BOOM!" : "CRASHED!",
                TitleDanger = true,
                Subtitle = lastRecord.NewBest ? "A NEW BEST RUN!" : "SO CLOSE. ONE MORE?",
                Selected = selected,
                LightDim = true,
                Hint = MenuHint(),
            };
            menu.Body.Add(("SCORE", Run.Points + (lastRecord.NewBest ? "   NEW BEST!" : string.Empty), lastRecord.NewBest));
            menu.Body.Add(("BEST", records.Best.ToString(), false));
            menu.Body.Add(("DISTANCE", $"{(int)Run.Distance} M" + (lastRecord.NewBestDistance ? "   LONGEST!" : string.Empty), lastRecord.NewBestDistance));
            menu.Body.Add(("COINS", Run.Coins.ToString(), false));
            menu.Body.Add(("JUMPS", Run.Jumps.ToString(), false));
            menu.Body.Add(("POWER-UPS", Run.PowerUpsCollected.ToString(), false));
            menu.Add("again", "RUN AGAIN");
            menu.Add("menu", "MAIN MENU");
            hud.ShowMenu(menu);
        }

        string MenuHint(bool withChange = false)
        {
            if (input.UsingGamepad)
            {
                return withChange ? "D-PAD TO CHOOSE AND CHANGE  -  A TO SELECT  -  B TO GO BACK" : "D-PAD TO CHOOSE  -  A TO SELECT  -  B TO GO BACK";
            }
            if (input.UsingTouch)
            {
                return "TAP A ROW";
            }
            return withChange
                ? "ARROWS TO CHOOSE AND CHANGE  -  ENTER TO SELECT  -  ESC TO GO BACK"
                : "ARROWS TO CHOOSE  -  ENTER TO SELECT  -  ESC TO GO BACK";
        }

        void MenuInput()
        {
            MenuModel menu = hud.Menu;
            if (menu == null)
            {
                return;
            }
            int vertical = input.VerticalStep();
            if (vertical != 0 && menu.Items.Count > 0)
            {
                menu.Selected = (menu.Selected + vertical + menu.Items.Count) % menu.Items.Count;
                sound.PlayUi(art.Move, 0.6f);
                hud.RefreshMenu();
            }
            int horizontal = input.HorizontalStep();
            if (horizontal != 0 && menu.Items.Count > 0 && menu.Items[menu.Selected].Value != null)
            {
                Change(menu.Items[menu.Selected].Id, horizontal);
            }
            if (input.Confirm && menu.Items.Count > 0)
            {
                Activate(menu.Items[menu.Selected].Id);
            }
            else if (input.Back || (Screen == GameScreen.Paused && input.Pause))
            {
                GoBack();
            }
        }

        void OnItemClicked(int index)
        {
            MenuModel menu = hud.Menu;
            if (menu == null || index < 0 || index >= menu.Items.Count)
            {
                return;
            }
            menu.Selected = index;
            Activate(menu.Items[index].Id);
        }

        void GoBack()
        {
            switch (Screen)
            {
                case GameScreen.HowToPlay:
                case GameScreen.Records:
                case GameScreen.Settings:
                    Activate("back");
                    break;
                case GameScreen.Paused:
                    TogglePause();
                    break;
                case GameScreen.Results:
                    sound.PlayUi(art.Move, 0.6f);
                    ToTitle();
                    break;
            }
        }

        /// <summary>Runs a menu row by its id, as Enter, a click or a tap would.</summary>
        public void Activate(string id)
        {
            switch (id)
            {
                case "play":
                case "again":
                case "restart":
                    sound.PlayUi(art.Select);
                    StartRun();
                    return;
                case "howto":
                    sound.PlayUi(art.Select);
                    ShowHowToPlay();
                    return;
                case "records":
                    sound.PlayUi(art.Select);
                    ShowRecordsMenu();
                    return;
                case "settings":
                    sound.PlayUi(art.Select);
                    ShowSettingsMenu(0);
                    return;
                case "quit":
                    Application.Quit();
                    return;
                case "back":
                    sound.PlayUi(art.Move, 0.6f);
                    string from = Screen == GameScreen.HowToPlay ? "howto" : Screen == GameScreen.Records ? "records" : "settings";
                    Screen = GameScreen.Title;
                    ShowTitleMenu(0);
                    hud.Menu.Selected = Mathf.Max(0, hud.Menu.IndexOf(from));
                    hud.RefreshMenu();
                    return;
                case "resume":
                    TogglePause();
                    return;
                case "menu":
                    sound.PlayUi(art.Select);
                    ToTitle();
                    return;
                default:
                    Change(id, 1);
                    return;
            }
        }

        /// <summary>Steps a setting row left or right, saves it and redraws the settings.</summary>
        public void Change(string id, int step)
        {
            switch (id)
            {
                case "volume":
                    settings.StepVolume(step);
                    break;
                case "music":
                    settings.StepMusic(step);
                    break;
                case "shake":
                    settings.ScreenShake = !settings.ScreenShake;
                    break;
                case "mute":
                    settings.Muted = !settings.Muted;
                    UpdateSoundLabel();
                    break;
                default:
                    return;
            }
            settings.Save(store);
            ApplyOptions();
            if (id == "shake" && settings.ScreenShake)
            {
                cameraRig.Shake(0.4f);
            }
            sound.PlayUi(art.Move, 0.6f);
            ShowSettingsMenu(hud.Menu != null ? hud.Menu.Selected : 0);
        }

        void UpdateSoundLabel() => hud.SetSound(settings.Muted ? "SOUND OFF  (M)" : "SOUND ON  (M)");
    }
}
