using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BalloonDash.Game
{
    /// <summary>One row of a menu: an action, or a setting with a value that left/right changes.</summary>
    public sealed class MenuItem
    {
        public string Id;
        public string Label;
        public string Value;
    }

    /// <summary>Everything a menu screen shows.</summary>
    public sealed class MenuModel
    {
        public bool Logo;
        public string Title;
        public bool TitleDanger;
        public string Subtitle;
        public readonly List<(string Label, string Value, bool Highlight)> Body = new List<(string, string, bool)>();
        public readonly List<MenuItem> Items = new List<MenuItem>();
        public int Selected;
        public string Hint;
        /// <summary>A light dim lets the run show through (pause, results), a heavy one hides it.</summary>
        public bool LightDim;

        public MenuItem Add(string id, string label, string value = null)
        {
            var item = new MenuItem { Id = id, Label = label, Value = value };
            Items.Add(item);
            return item;
        }

        public int IndexOf(string id) => Items.FindIndex(item => item.Id == id);
    }

    /// <summary>
    /// Binds the UI Toolkit HUD (Hud.uxml): distance, coins, score and best, power-up chips with
    /// their time left, banners and floating text, plus a generic menu renderer for the title,
    /// settings, records, pause and results screens. Menu rows never take keyboard focus; the
    /// director reads the keys, and the mouse or a finger clicks rows.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Hud : MonoBehaviour
    {
        const float FloaterLifetime = 0.9f;

        sealed class Floater
        {
            public Label Label;
            public Vector2 Position;
            public float Age;
            public float Rise;
        }

        VisualElement root;
        VisualElement playHud;
        VisualElement skyFrame;
        Label distance;
        Label coins;
        Label score;
        Label best;
        VisualElement pauseButton;
        VisualElement powers;
        Label prompt;
        Label banner;
        VisualElement floaters;
        VisualElement menu;
        VisualElement menuDim;
        VisualElement logo;
        Label menuTitle;
        Label menuSubtitle;
        VisualElement menuBody;
        VisualElement menuItems;
        Label menuHint;
        Label sound;
        IVisualElementScheduledItem bannerHide;
        readonly List<Floater> active = new List<Floater>();
        readonly List<VisualElement> rows = new List<VisualElement>();
        int shownScore = -1;
        int shownCoins = -1;
        int shownMetres = -1;

        /// <summary>A menu row was clicked or tapped (its index).</summary>
        public event Action<int> ItemClicked;

        /// <summary>The on-screen pause button was clicked or tapped.</summary>
        public event Action PauseClicked;

        /// <summary>The menu currently shown, or null.</summary>
        public MenuModel Menu { get; private set; }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            playHud = root.Q("play-hud");
            skyFrame = root.Q("sky-frame");
            distance = root.Q<Label>("distance");
            coins = root.Q<Label>("coins");
            score = root.Q<Label>("score");
            best = root.Q<Label>("best");
            pauseButton = root.Q("pause-button");
            powers = root.Q("powers");
            prompt = root.Q<Label>("prompt");
            banner = root.Q<Label>("banner");
            floaters = root.Q("floaters");
            menu = root.Q("menu");
            menuDim = root.Q("menu-dim");
            logo = root.Q("logo");
            menuTitle = root.Q<Label>("menu-title");
            menuSubtitle = root.Q<Label>("menu-subtitle");
            menuBody = root.Q("menu-body");
            menuItems = root.Q("menu-items");
            menuHint = root.Q<Label>("menu-hint");
            sound = root.Q<Label>("sound");
            root.Q("coin-icon").style.backgroundImage = new StyleBackground(Shapes.Coin);
            pauseButton.AddManipulator(new Clickable(() => PauseClicked?.Invoke()));
        }

        // ---- Run HUD ----

        public void ShowPlayHud(bool visible) => SetVisible(playHud, visible);

        /// <summary>Sets up the HUD for a run.</summary>
        public void SetRun(int bestScore)
        {
            best.text = bestScore.ToString();
            shownScore = -1;
            shownCoins = -1;
            shownMetres = -1;
            SetScore(0);
            SetCoins(0);
            SetDistance(0f);
            SetPowers(0f, 0f, false);
            SetSkyFrame(false);
        }

        public void SetScore(int value)
        {
            if (shownScore == value)
            {
                return;
            }
            shownScore = value;
            score.text = value.ToString();
        }

        public void SetCoins(int value)
        {
            if (shownCoins == value)
            {
                return;
            }
            bool pop = shownCoins >= 0 && value > shownCoins;
            shownCoins = value;
            coins.text = value.ToString();
            if (pop)
            {
                coins.AddToClassList("coins--pop");
                coins.schedule.Execute(() => coins.RemoveFromClassList("coins--pop")).ExecuteLater(110);
            }
        }

        public void SetDistance(float metres)
        {
            int whole = Mathf.FloorToInt(Mathf.Max(metres, 0f));
            if (whole == shownMetres)
            {
                return;
            }
            shownMetres = whole;
            distance.text = $"{whole} M";
        }

        public void SetBest(int value) => best.text = value.ToString();

        /// <summary>Chips for the running power-ups with the seconds left.</summary>
        public void SetPowers(float floatLeft, float magnetLeft, bool shield)
        {
            int count = 0;
            if (floatLeft > 0f)
            {
                Chip(count++, $"BALLOON  {Mathf.CeilToInt(floatLeft)}", Palette.Balloon);
            }
            if (magnetLeft > 0f)
            {
                Chip(count++, $"MAGNET  {Mathf.CeilToInt(magnetLeft)}", Palette.Magnet);
            }
            if (shield)
            {
                Chip(count++, "SHIELD", Palette.Shield);
            }
            while (powers.childCount > count)
            {
                powers.RemoveAt(powers.childCount - 1);
            }
        }

        void Chip(int index, string text, Color color)
        {
            Label chip;
            if (index < powers.childCount)
            {
                chip = (Label)powers[index];
            }
            else
            {
                chip = new Label { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("chip");
                powers.Add(chip);
            }
            chip.text = text;
            chip.style.color = color;
            chip.style.borderTopColor = chip.style.borderBottomColor = chip.style.borderLeftColor = chip.style.borderRightColor = color;
        }

        public IEnumerable<string> PowerChipTexts
        {
            get
            {
                for (int i = 0; i < powers.childCount; i++)
                {
                    yield return ((Label)powers[i]).text;
                }
            }
        }

        /// <summary>Hides the pause button without moving the best score next to it.</summary>
        public void SetPauseButton(bool visible) => pauseButton.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;

        /// <summary>A soft frame around the screen while the balloon floats.</summary>
        public void SetSkyFrame(bool on) => skyFrame.EnableInClassList("sky-frame--on", on);

        public void SetPrompt(string text)
        {
            prompt.text = text ?? string.Empty;
            SetVisible(prompt, !string.IsNullOrEmpty(text));
        }

        /// <summary>Shows a big centred message for a moment; style is null, "danger", "balloon", "magnet" or "shield".</summary>
        public void Banner(string text, string style = null, int milliseconds = 900)
        {
            banner.text = text;
            banner.EnableInClassList("banner--danger", style == "danger");
            banner.EnableInClassList("banner--balloon", style == "balloon");
            banner.EnableInClassList("banner--magnet", style == "magnet");
            banner.EnableInClassList("banner--shield", style == "shield");
            banner.AddToClassList("banner--show");
            bannerHide?.Pause();
            bannerHide = banner.schedule.Execute(() => banner.RemoveFromClassList("banner--show"));
            bannerHide.ExecuteLater(milliseconds);
        }

        public void HideBanner()
        {
            bannerHide?.Pause();
            banner.RemoveFromClassList("banner--show");
        }

        public string BannerText => banner.ClassListContains("banner--show") ? banner.text : null;

        /// <summary>Text that rises from a point of the world and fades.</summary>
        public void Float(Vector2 world, string text, Color color, float size = 26f)
        {
            Camera view = Camera.main;
            if (root?.panel == null || view == null || active.Count > 24)
            {
                return;
            }
            // Through the viewport, so it also lines up when the camera renders to a texture.
            Vector3 viewport = view.WorldToViewportPoint(world);
            var position = new Vector2(viewport.x * root.layout.width, (1f - viewport.y) * root.layout.height);
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("display");
            label.AddToClassList("floater");
            label.style.color = color;
            label.style.fontSize = size;
            floaters.Add(label);
            var floater = new Floater { Label = label, Position = position, Rise = 60f };
            active.Add(floater);
            Place(floater);
        }

        public IEnumerable<string> FloaterTexts
        {
            get
            {
                foreach (Floater floater in active)
                {
                    yield return floater.Label.text;
                }
            }
        }

        public void ClearFloaters()
        {
            foreach (Floater floater in active)
            {
                floater.Label.RemoveFromHierarchy();
            }
            active.Clear();
        }

        public void SetSound(string text) => sound.text = text;

        /// <summary>True when a screen point (pixels, origin bottom-left) is over the pause button.</summary>
        public bool IsOverPauseButton(Vector2 screen)
        {
            if (root?.panel == null || playHud.ClassListContains("hidden"))
            {
                return false;
            }
            Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
            Rect bounds = pauseButton.worldBound;
            bounds.xMin -= 8f;
            bounds.yMin -= 8f;
            bounds.xMax += 8f;
            bounds.yMax += 8f;
            return bounds.Contains(panelPoint);
        }

        /// <summary>True when a screen point is over a menu row (presses there are not jumps).</summary>
        public bool IsOverMenu(Vector2 screen)
        {
            if (Menu == null || root?.panel == null)
            {
                return false;
            }
            Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
            return menuItems.worldBound.Contains(panelPoint);
        }

        // ---- Menus ----

        public void ShowMenu(MenuModel model)
        {
            Menu = model;
            SetVisible(menu, true);
            menuDim.EnableInClassList("menu__dim--light", model.LightDim);
            SetVisible(logo, model.Logo);
            menuTitle.text = model.Title ?? string.Empty;
            SetVisible(menuTitle, !string.IsNullOrEmpty(model.Title));
            menuTitle.EnableInClassList("menu__title--danger", model.TitleDanger);
            menuSubtitle.text = model.Subtitle ?? string.Empty;
            SetVisible(menuSubtitle, !string.IsNullOrEmpty(model.Subtitle));

            menuBody.Clear();
            foreach ((string label, string value, bool highlight) in model.Body)
            {
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList("body-line");
                var left = new Label(label) { pickingMode = PickingMode.Ignore };
                left.AddToClassList("body-line__label");
                var right = new Label(value) { pickingMode = PickingMode.Ignore };
                right.AddToClassList("body-line__value");
                right.EnableInClassList("body-line__value--new", highlight);
                line.Add(left);
                line.Add(right);
                menuBody.Add(line);
            }
            SetVisible(menuBody, model.Body.Count > 0);

            menuItems.Clear();
            rows.Clear();
            for (int i = 0; i < model.Items.Count; i++)
            {
                int index = i;
                var row = new VisualElement();
                row.AddToClassList("item");
                var label = new Label { name = "label", pickingMode = PickingMode.Ignore };
                label.AddToClassList("item__label");
                var value = new Label { name = "value", pickingMode = PickingMode.Ignore };
                value.AddToClassList("item__value");
                row.Add(label);
                row.Add(value);
                row.AddManipulator(new Clickable(() => ItemClicked?.Invoke(index)));
                menuItems.Add(row);
                rows.Add(row);
            }
            menuHint.text = model.Hint ?? string.Empty;
            RefreshMenu();
        }

        /// <summary>Redraws the rows' text and the highlight after a value or the selection changed.</summary>
        public void RefreshMenu()
        {
            if (Menu == null)
            {
                return;
            }
            for (int i = 0; i < rows.Count && i < Menu.Items.Count; i++)
            {
                MenuItem item = Menu.Items[i];
                rows[i].Q<Label>("label").text = item.Label;
                rows[i].Q<Label>("value").text = item.Value != null ? $"<  {item.Value}  >" : string.Empty;
                rows[i].EnableInClassList("item--selected", i == Menu.Selected);
            }
        }

        public void HideMenu()
        {
            Menu = null;
            SetVisible(menu, false);
        }

        // ---- Each frame ----

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Floater floater = active[i];
                floater.Age += Time.unscaledDeltaTime;
                if (floater.Age >= FloaterLifetime)
                {
                    floater.Label.RemoveFromHierarchy();
                    active.RemoveAt(i);
                    continue;
                }
                Place(floater);
            }
        }

        static void Place(Floater floater)
        {
            float t = floater.Age / FloaterLifetime;
            floater.Label.style.left = floater.Position.x;
            floater.Label.style.top = floater.Position.y - floater.Rise * Mathf.Sqrt(t);
            floater.Label.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            float pop = t < 0.12f ? 1f + (0.12f - t) * 4f : 1f;
            floater.Label.style.scale = new Scale(new Vector3(pop, pop, 1f));
        }

        static void SetVisible(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);
    }
}
