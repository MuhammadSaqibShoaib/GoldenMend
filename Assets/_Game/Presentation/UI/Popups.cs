using System;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>Pause menu: one primary action (resume) and the ways out, stacked in the thumb-friendly middle.</summary>
    public sealed class PausePopup : UiView
    {
        public Action Resume;
        public Action Restart;
        public Action Settings;
        public Action Home;

        readonly RectTransform card;

        public PausePopup(UiRoot ui) : base(ui, "Pause", popup: true)
        {
            card = B.Card("Card", Root, Anchor.Center, Vector2.zero, new Vector2(760f, 860f));
            B.Text("Title", card, "Paused", T.DisplayFont, UiType.H1, T.Indigo, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -56f), new Vector2(640f, 100f));
            B.IconButton("Close", card, T.IconClose, ButtonStyle.Ghost, Anchor.TopRight, new Vector2(-20f, -20f), 100f, () => Resume?.Invoke());

            var size = new Vector2(600f, 128f);
            B.Button("Resume", card, "Resume", ButtonStyle.Primary, Anchor.Top, new Vector2(0f, -196f), new Vector2(600f, 144f), () => Resume?.Invoke(), T.IconPlay);
            B.Button("Restart", card, "Restart", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -364f), size, () => Restart?.Invoke(), T.IconRestart);
            B.Button("Settings", card, "Settings", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -516f), size, () => Settings?.Invoke(), T.IconSettings);
            B.Button("Home", card, "Home", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -668f), size, () => Home?.Invoke(), T.IconHome);
        }

        protected override void OnShow() => Ui.Pop(card);
    }

    /// <summary>Sound and haptics toggles, plus credits for the open assets the game uses.</summary>
    public sealed class SettingsPopup : UiView
    {
        public Action Done;
        public Action<bool> SoundChanged;
        public Action<bool> HapticsChanged;

        readonly RectTransform card;
        readonly Toggle sound;
        readonly Toggle haptics;

        sealed class Toggle
        {
            public Button Button;
            public Image Background;
            public Text Label;
            public bool On;
        }

        public SettingsPopup(UiRoot ui) : base(ui, "Settings", popup: true)
        {
            card = B.Card("Card", Root, Anchor.Center, Vector2.zero, new Vector2(800f, 860f));
            B.Text("Title", card, "Settings", T.DisplayFont, UiType.H1, T.Indigo, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -56f), new Vector2(640f, 100f));

            sound = Row("Sound", T.IconSoundOn, -196f, on => SoundChanged?.Invoke(on));
            haptics = Row("Haptics", T.IconHaptics, -336f, on => HapticsChanged?.Invoke(on));

            B.Image("Divider", card, T.Divider, UiBuilder.WithAlpha(T.Gold, 0.7f), Anchor.Top, new Vector2(0f, -476f), new Vector2(560f, 6f)).preserveAspect = false;
            B.Text("Credits", card, "Paintings: public domain, Art Institute of Chicago and The Met (CC0)\nUI art and sounds: Kenney (kenney.nl), CC0\nFont: Zen Maru Gothic, SIL Open Font License",
                T.BodyFont, 25, UiBuilder.WithAlpha(T.Ink, 0.6f), TextAnchor.UpperCenter, Anchor.Top, new Vector2(0f, -500f), new Vector2(720f, 110f));
            B.Text("Version", card, $"Golden Mend  {Application.version}", T.BodyFont, 24, UiBuilder.WithAlpha(T.Ink, 0.4f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -620f), new Vector2(700f, 36f));

            B.Button("Done", card, "Done", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 52f), new Vector2(520f, 136f), () => Done?.Invoke());
        }

        public void Bind(bool soundOn, bool hapticsOn)
        {
            Set(sound, soundOn);
            Set(haptics, hapticsOn);
        }

        protected override void OnShow() => Ui.Pop(card);

        Toggle Row(string label, Sprite icon, float y, Action<bool> changed)
        {
            var row = B.Node(label, card, Anchor.Top, new Vector2(0f, y), new Vector2(680f, 116f));
            B.Image("Icon", row, icon, T.Indigo, Anchor.Left, Vector2.zero, new Vector2(64f, 64f));
            B.Text("Label", row, label, T.TitleFont, 42, T.Ink, TextAnchor.MiddleLeft, Anchor.Left, new Vector2(96f, 0f), new Vector2(360f, 60f));

            var toggle = new Toggle();
            toggle.Button = B.Button("Toggle", row, "On", ButtonStyle.Primary, Anchor.Right, Vector2.zero, new Vector2(210f, 100f), () =>
            {
                Set(toggle, !toggle.On);
                changed(toggle.On);
            });
            toggle.Background = toggle.Button.GetComponent<Image>();
            toggle.Label = toggle.Button.GetComponentInChildren<Text>();
            return toggle;
        }

        void Set(Toggle toggle, bool on)
        {
            toggle.On = on;
            toggle.Background.color = on ? T.Gold : UiBuilder.WithAlpha(T.Indigo, 0.35f);
            toggle.Label.color = on ? T.Ink : T.Indigo;
            toggle.Label.text = on ? "On" : "Off";
        }
    }

    /// <summary>
    /// Level complete: a bottom sheet so the restored vessel stays visible above it. Stars, the coin reward,
    /// then Next as the main action and Home as a quiet one.
    /// </summary>
    public sealed class WinSheet : UiView
    {
        public Action Next;
        public Action Home;

        const float SheetHeight = 860f;
        static readonly Vector2 Rest = new Vector2(0f, -60f);

        readonly RectTransform sheet;
        readonly RectTransform stars;
        readonly Text subtitle;
        readonly Text reward;
        readonly Text stats;
        readonly Text nextLabel;

        public WinSheet(UiRoot ui) : base(ui, "Level Complete", popup: true)
        {
            Backdrop.color = UiBuilder.WithAlpha(T.Scrim, 0.3f);

            sheet = B.Card("Sheet", Root, Anchor.StretchBottom, Rest, new Vector2(0f, SheetHeight));
            stars = B.Node("Stars", sheet, Anchor.Top, new Vector2(0f, 96f), new Vector2(480f, 160f));

            B.Text("Title", sheet, "Restored!", T.DisplayFont, 96, T.Indigo, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -84f), new Vector2(900f, 120f));
            subtitle = B.Text("Subtitle", sheet, "", T.BodyFont, 36, UiBuilder.WithAlpha(T.Ink, 0.8f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -204f), new Vector2(900f, 56f));
            reward = B.CoinPill("Reward", sheet, UiBuilder.WithAlpha(T.Gold, 0.35f), "+0", T.Ink, 50, Anchor.Top, new Vector2(0f, -288f), new Vector2(300f, 104f));
            stats = B.Text("Stats", sheet, "", T.BodyFont, UiType.Caption, UiBuilder.WithAlpha(T.Ink, 0.55f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -414f), new Vector2(900f, 48f));

            var next = B.Button("Next", sheet, "Next vessel", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 212f), new Vector2(680f, 156f), () => Next?.Invoke(), T.IconPlay);
            nextLabel = next.GetComponentInChildren<Text>();
            B.Button("Home", sheet, "Home", ButtonStyle.Ghost, Anchor.Bottom, new Vector2(0f, 92f), new Vector2(360f, 100f), () => Home?.Invoke(), T.IconHome);
        }

        public void Bind(string vesselName, string byline, int starCount, int coins, bool firstClear, int benchPeak, int bestPeak, bool hasNext)
        {
            for (int i = stars.childCount - 1; i >= 0; i--) Release(stars.GetChild(i).gameObject);
            B.Stars(stars, starCount, Anchor.Center, Vector2.zero, 118f, raised: true);

            string piece = byline != null ? $"{vesselName} by {byline.Split(',')[0]}" : vesselName;
            subtitle.text = firstClear ? $"{piece} joins your museum." : $"{piece}, restored once more.";
            reward.text = $"+{coins}";
            stats.text = $"Busiest workbench  {benchPeak}   ·   best possible  {bestPeak}";
            nextLabel.text = hasNext ? "Next vessel" : "To the museum";
        }

        protected override void OnShow()
        {
            Ui.SlideIn(sheet, Rest, new Vector2(0f, -SheetHeight));
            Ui.Pop(stars, 0.25f);
        }

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// Workbench full: the continue offer (+2 slots for coins) as the main action, then retry and home.
    /// The offer greys out with a clear reason when the player can't afford it.
    /// </summary>
    public sealed class FailPopup : UiView
    {
        public Action Continue;
        public Action Retry;
        public Action Home;

        readonly RectTransform card;
        readonly Button continueButton;
        readonly Text price;
        readonly Text shortfall;

        public FailPopup(UiRoot ui) : base(ui, "Workbench Full", popup: true)
        {
            card = B.Card("Card", Root, Anchor.Center, new Vector2(0f, -20f), new Vector2(800f, 860f));

            var badge = B.Image("Badge", card, T.Circle, T.Clay, Anchor.Top, new Vector2(0f, 76f), new Vector2(152f, 152f));
            B.Image("Icon", badge.transform, T.IconWarning, T.Paper, Anchor.Center, Vector2.zero, new Vector2(84f, 84f));

            B.Text("Title", card, "Workbench full", T.DisplayFont, UiType.H1, T.Indigo, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -100f), new Vector2(740f, 100f));
            B.Text("Body", card, "No free slot for that shard.\nAdd two more and keep going?", T.BodyFont, 36, UiBuilder.WithAlpha(T.Ink, 0.8f),
                TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -206f), new Vector2(700f, 110f));

            continueButton = B.Button("Continue", card, "Continue", ButtonStyle.Primary, Anchor.Top, new Vector2(0f, -352f), new Vector2(640f, 150f), () => Continue?.Invoke());
            var label = continueButton.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(48f, label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax = new Vector2(-220f, label.rectTransform.offsetMax.y);
            B.Text("Slots", continueButton.transform, "+2 slots", T.BodyFont, 28, UiBuilder.WithAlpha(T.Ink, 0.7f), TextAnchor.MiddleLeft,
                Anchor.Left, new Vector2(250f, 5f), new Vector2(160f, 40f));
            price = B.CoinPill("Price", continueButton.transform, UiBuilder.WithAlpha(T.Ink, 0.14f), "0", T.Ink, 40,
                Anchor.Right, new Vector2(-28f, 5f), new Vector2(170f, 92f));

            shortfall = B.Text("Shortfall", card, "", T.BodyFont, 28, T.Clay, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -512f), new Vector2(700f, 44f));

            B.Button("Retry", card, "Try again", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -576f), new Vector2(640f, 128f), () => Retry?.Invoke(), T.IconRestart);
            B.Button("Home", card, "Home", ButtonStyle.Ghost, Anchor.Top, new Vector2(0f, -728f), new Vector2(360f, 100f), () => Home?.Invoke(), T.IconHome);
        }

        public void Bind(int cost, int coins)
        {
            price.text = cost.ToString();
            bool canAfford = coins >= cost;
            SetInteractable(continueButton, canAfford);
            shortfall.text = canAfford ? "" : $"Not enough coins (you have {coins})";
        }

        protected override void OnShow() => Ui.Pop(card);
    }

    /// <summary>A short announcement at the top of the board when a level starts.</summary>
    public sealed class LevelBanner
    {
        readonly UiRoot ui;
        readonly CanvasGroup group;
        readonly Text line1;
        readonly Text line2;
        Coroutine running;

        public LevelBanner(UiRoot ui)
        {
            this.ui = ui;
            var b = ui.Build;
            var t = ui.Theme;
            var rect = b.Image("Level Banner", ui.Screens, t.Badge, UiBuilder.WithAlpha(t.Indigo, 0.94f), Anchor.Center, new Vector2(0f, 300f), new Vector2(800f, 200f), sliced: true).rectTransform;
            group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            line1 = b.Text("Line 1", rect, "", t.DisplayFont, 64, t.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -26f), new Vector2(760f, 90f));
            line2 = b.Text("Line 2", rect, "", t.BodyFont, 32, t.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -124f), new Vector2(760f, 46f));
            rect.gameObject.SetActive(false);
        }

        public void Show(string title, string detail, float seconds = 1.8f)
        {
            line1.text = title;
            line2.text = detail;
            group.transform.SetAsLastSibling();
            if (running != null) ui.StopCoroutine(running);
            ui.Fade(group, true, 0.25f);
            group.blocksRaycasts = false;
            running = ui.Run(HideLater(seconds));
        }

        public void Hide() => ui.Fade(group, false, 0f);

        System.Collections.IEnumerator HideLater(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            ui.Fade(group, false, 0.35f);
            running = null;
        }
    }
}
