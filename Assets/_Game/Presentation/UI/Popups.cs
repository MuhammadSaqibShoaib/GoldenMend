using System;
using GoldenMend.Data;
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
            card = B.Popup(Root, new Vector2(920f, 1080f), Vector2.zero, () => Resume?.Invoke(), out var title);
            title.text = "Paused";

            var size = new Vector2(720f, 168f);
            B.Button("Resume", card, "Resume", ButtonStyle.Confirm, Anchor.Top, new Vector2(0f, -196f), new Vector2(720f, 192f), () => Resume?.Invoke(), T.IconPlay);
            B.Button("Restart", card, "Restart", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -428f), size, () => Restart?.Invoke(), T.IconRestart);
            B.Button("Settings", card, "Settings", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -632f), size, () => Settings?.Invoke(), T.IconSettings);
            B.Button("Home", card, "Home", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -836f), size, () => Home?.Invoke(), T.IconHome);
        }

        protected override void OnShow() => Ui.Pop(card);
    }

    /// <summary>Sound and haptics switches, plus credits for the open assets the game uses.</summary>
    public sealed class SettingsPopup : UiView
    {
        public Action Done;
        public Action<bool> SoundChanged;
        public Action<bool> HapticsChanged;

        readonly RectTransform card;
        readonly Switch sound;
        readonly Switch haptics;

        sealed class Switch
        {
            public Image Track;
            public RectTransform Knob;
            public bool On;
        }

        public SettingsPopup(UiRoot ui) : base(ui, "Settings", popup: true)
        {
            card = B.Popup(Root, new Vector2(920f, 1120f), Vector2.zero, () => Done?.Invoke(), out var title);
            title.text = "Settings";

            sound = Row("Sound", T.IconSoundOn, -196f, on => SoundChanged?.Invoke(on));
            haptics = Row("Haptics", T.IconHaptics, -388f, on => HapticsChanged?.Invoke(on));

            B.Text("Credits", card, "Paintings: public domain, Art Institute of Chicago and The Met (CC0)\nIcons and sounds: Kenney (kenney.nl), CC0\nFonts: Titan One and Baloo 2, SIL Open Font License",
                T.BodyFont, 28, T.Muted, TextAnchor.UpperCenter, Anchor.Top, new Vector2(0f, -600f), new Vector2(820f, 180f));
            B.Text("Version", card, $"Golden Mend  {Application.version}", T.BodyFont, 26, UiBuilder.WithAlpha(T.Muted, 0.6f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -800f), new Vector2(700f, 40f));

            B.Button("Done", card, "Done", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 64f), new Vector2(560f, 160f), () => Done?.Invoke());
        }

        public void Bind(bool soundOn, bool hapticsOn)
        {
            Set(sound, soundOn);
            Set(haptics, hapticsOn);
        }

        protected override void OnShow() => Ui.Pop(card);

        Switch Row(string label, Sprite icon, float y, Action<bool> changed)
        {
            var row = B.Image(label, card, UiSkin.Round, UiBuilder.WithAlpha(Color.white, 0.07f), Anchor.Top, new Vector2(0f, y), new Vector2(800f, 164f), sliced: true).rectTransform;
            var tile = B.Image("Icon Tile", row, UiSkin.Chunky, T.Indigo, Anchor.Left, new Vector2(24f, 0f), new Vector2(116f, 116f), sliced: true);
            tile.pixelsPerUnitMultiplier = 1.4f;
            B.Image("Icon", tile.transform, icon, T.Cream, Anchor.Center, new Vector2(0f, 5f), new Vector2(64f, 64f));
            B.Text("Label", row, label, T.TitleFont, 54, T.Cream, TextAnchor.MiddleLeft, Anchor.Left, new Vector2(164f, 0f), new Vector2(360f, 80f));

            var toggle = new Switch();
            toggle.Track = B.Image("Switch", row, UiSkin.Pill, T.Green, Anchor.Right, new Vector2(-32f, 0f), new Vector2(176f, 88f), sliced: true);
            toggle.Track.pixelsPerUnitMultiplier = 64f / 88f;
            toggle.Track.raycastTarget = true;
            var knob = B.Image("Knob", toggle.Track.transform, T.Circle, Color.white, Anchor.Left, new Vector2(8f, 0f), new Vector2(72f, 72f));
            UiBuilder.Lip(knob, UiBuilder.WithAlpha(Color.black, 0.3f), 3f);
            toggle.Knob = knob.rectTransform;
            B.Tappable(row, () =>
            {
                Set(toggle, !toggle.On);
                changed(toggle.On);
            });
            return toggle;
        }

        void Set(Switch toggle, bool on)
        {
            toggle.On = on;
            toggle.Track.color = on ? T.Green : UiBuilder.WithAlpha(T.Well, 0.8f);
            UiBuilder.Place(toggle.Knob, on ? Anchor.Right : Anchor.Left, new Vector2(on ? -8f : 8f, 0f), new Vector2(72f, 72f));
        }
    }

    /// <summary>
    /// Level complete: a full-screen celebration. The gold title, the stars, the restored vessel, the coin reward,
    /// then the run's numbers and record line, with Next as the main action and Home as a quiet one.
    /// </summary>
    public sealed class WinSheet : UiView
    {
        public Action Next;
        public Action Home;

        readonly RectTransform content;
        readonly RectTransform stars;
        readonly RectTransform showcase;
        readonly Text subtitle;
        readonly Text reward;
        readonly Text stats;
        readonly Text record;
        readonly Text nextLabel;
        RectTransform preview;

        public WinSheet(UiRoot ui) : base(ui, "Level Complete", popup: true)
        {
            Backdrop.sprite = UiSkin.VerticalGradient(T.Night, T.NightDeep);
            Backdrop.color = Color.white;

            content = B.Node("Content", Root, Anchor.Fill, Vector2.zero, Vector2.zero);
            B.Image("Glow", content, UiSkin.Glow, UiBuilder.WithAlpha(T.Gold, 0.4f), Anchor.Top, new Vector2(0f, -40f), new Vector2(1400f, 1400f));
            var title = B.Text("Title", content, "Restored!", T.DisplayFont, 150, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -100f), new Vector2(1060f, 200f));
            UiBuilder.Lip(title, UiTheme.Hex(0xA1501A), 12f);
            stars = B.Node("Stars", content, Anchor.Top, new Vector2(0f, -330f), new Vector2(600f, 200f));
            showcase = B.Node("Vessel", content, Anchor.Top, new Vector2(0f, -540f), new Vector2(760f, 500f));

            subtitle = B.Text("Subtitle", content, "", T.TitleFont, 46, T.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -1072f), new Vector2(980f, 64f));
            reward = B.CoinPill("Reward", content, UiTheme.Hex(0x6A22C8), "+0", T.Cream, 64, Anchor.Top, new Vector2(0f, -1166f), new Vector2(360f, 128f));

            var box = B.Card("Stats", content, Anchor.Top, new Vector2(0f, -1336f), new Vector2(860f, 184f), glass: true, radius: 0.7f);
            stats = B.Text("Stats", box, "", T.BodyFont, 36, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -22f), new Vector2(820f, 56f));
            record = B.Text("Record", box, "", T.TitleFont, 38, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -94f), new Vector2(820f, 64f));

            var next = B.Button("Next", content, "Next", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 150f), new Vector2(700f, 192f), () => Next?.Invoke(), T.IconPlay);
            nextLabel = next.GetComponentInChildren<Text>();
            nextLabel.fontSize = 76;
            B.Button("Home", content, "Home", ButtonStyle.Ghost, Anchor.Bottom, new Vector2(0f, 30f), new Vector2(360f, 100f), () => Home?.Invoke());
        }

        /// <param name="recordLine">The personal-record line under the stats, for example "New personal best!"; may be empty.</param>
        /// <param name="level">Shown restored in the middle of the screen; omitted in tests.</param>
        public void Bind(string vesselName, string byline, int starCount, int coins, bool firstClear, int benchPeak, int bestPeak, bool hasNext,
            string recordLine = "", BuiltLevel level = null)
        {
            for (int i = stars.childCount - 1; i >= 0; i--) Release(stars.GetChild(i).gameObject);
            B.Stars(stars, starCount, Anchor.Center, Vector2.zero, 150f, raised: true);

            if (preview != null) Release(preview.gameObject);
            preview = level != null ? VesselPreview.Build(B, showcase, level, VesselLook.Restored, Anchor.Center, Vector2.zero, new Vector2(720f, 460f)) : null;

            string piece = byline != null ? $"{vesselName} by {byline.Split(',')[0]}" : vesselName;
            subtitle.text = firstClear ? $"{piece} joins your museum" : $"{piece}, restored once more";
            reward.text = $"+{coins}";
            stats.text = $"Busiest workbench {benchPeak}  ·  best possible {bestPeak}";
            record.text = recordLine ?? "";
            nextLabel.text = hasNext ? "NEXT" : "MUSEUM";
        }

        protected override void OnShow()
        {
            Ui.Pop(stars, 0.25f);
            Ui.Pop(reward.transform.parent, 0.4f);
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
            card = B.Popup(Root, new Vector2(920f, 940f), new Vector2(0f, -20f), null, out var title);
            title.text = "Bench full!";

            B.Text("Body", card, "No free slot for that shard.\nAdd two more and keep going?", T.TitleFont, 44, T.Cream,
                TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -184f), new Vector2(820f, 130f));

            continueButton = OfferButton(B, card, "Continue", new Vector2(0f, -352f), () => Continue?.Invoke(), out price);
            B.Text("Slots", card, "Adds 2 workbench slots", T.BodyFont, 34, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -566f), new Vector2(800f, 48f));
            shortfall = B.Text("Shortfall", card, "", T.TitleFont, 34, UiTheme.Hex(0xFF9A9A), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -614f), new Vector2(800f, 48f));

            B.Button("Retry", card, "Retry", ButtonStyle.Secondary, Anchor.Top, new Vector2(-190f, -690f), new Vector2(360f, 152f), () => Retry?.Invoke(), T.IconRestart);
            B.Button("Home", card, "Home", ButtonStyle.Secondary, Anchor.Top, new Vector2(190f, -690f), new Vector2(360f, 152f), () => Home?.Invoke(), T.IconHome);
        }

        /// <summary>A green buy button: the action on the left, the coin price in a dark capsule on the right.</summary>
        internal static Button OfferButton(UiBuilder b, Transform parent, string action, Vector2 position, Action onClick, out Text price)
        {
            var button = b.Button(action, parent, action, ButtonStyle.Confirm, Anchor.Top, position, new Vector2(760f, 200f), onClick);
            var label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = 64;
            label.rectTransform.offsetMin = new Vector2(56f, label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax = new Vector2(-260f, label.rectTransform.offsetMax.y);
            price = b.CoinPill("Price", button.transform, UiBuilder.WithAlpha(Color.black, 0.25f), "0", Color.white, 52,
                Anchor.Right, new Vector2(-32f, UiSkin.LipHeight / 2f), new Vector2(220f, 104f));
            return button;
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

    /// <summary>
    /// Offer for a helper the player is about to pay for (a hint once the free ones are gone, or the pull
    /// booster): coins as the main action, a rewarded ad when one is ready, and the close button.
    /// </summary>
    public sealed class HelpOffer : UiView
    {
        public Action Buy;
        public Action WatchAd;
        public Action Close;

        readonly RectTransform card;
        readonly Text title;
        readonly Text body;
        readonly Image icon;
        readonly Button buyButton;
        readonly Text buyLabel;
        readonly Text price;
        readonly Button adButton;
        readonly Text shortfall;

        public HelpOffer(UiRoot ui) : base(ui, "Help Offer", popup: true)
        {
            card = B.Popup(Root, new Vector2(920f, 1050f), new Vector2(0f, -20f), () => Close?.Invoke(), out title);

            var halo = B.Image("Halo", card, T.Circle, UiBuilder.WithAlpha(T.Gold, 0.25f), Anchor.Top, new Vector2(0f, -160f), new Vector2(240f, 240f));
            var badge = B.Image("Badge", halo.transform, T.Circle, T.Yellow, Anchor.Center, Vector2.zero, new Vector2(196f, 196f));
            UiBuilder.Lip(badge, UiBuilder.Darken(T.Yellow, 0.4f), 8f);
            icon = B.Image("Icon", badge.transform, T.IconHint, T.Ink, Anchor.Center, Vector2.zero, new Vector2(116f, 116f));

            body = B.Text("Body", card, "", T.TitleFont, 42, T.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -420f), new Vector2(820f, 130f));
            buyButton = FailPopup.OfferButton(B, card, "Buy", new Vector2(0f, -584f), () => Buy?.Invoke(), out price);
            buyLabel = buyButton.GetComponentInChildren<Text>();
            shortfall = B.Text("Shortfall", card, "", T.TitleFont, 34, UiTheme.Hex(0xFF9A9A), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -794f), new Vector2(800f, 48f));
            adButton = B.Button("Watch ad", card, "Watch an ad", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -852f), new Vector2(760f, 150f), () => WatchAd?.Invoke(), T.IconPlay);
        }

        /// <param name="glyph">The helper's icon (the hint bulb when omitted).</param>
        public void Bind(string heading, string description, string action, int cost, int coins, bool adReady, Sprite glyph = null)
        {
            title.text = heading;
            body.text = description;
            buyLabel.text = action.ToUpperInvariant();
            icon.sprite = glyph != null ? glyph : T.IconHint;
            price.text = cost.ToString();
            bool canAfford = coins >= cost;
            SetInteractable(buyButton, canAfford);
            shortfall.text = canAfford ? "" : $"Not enough coins (you have {coins})";
            adButton.gameObject.SetActive(adReady);
            // Without an ad to offer, the panel ends under the buy button.
            card.sizeDelta = new Vector2(card.sizeDelta.x, adReady ? 1050f : 880f);
        }

        protected override void OnShow() => Ui.Pop(card);
    }

    /// <summary>A short announcement over the board when a level starts: a pink ribbon with the level's name.</summary>
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
            var ribbon = b.Image("Level Banner", ui.Screens, UiSkin.Chunky, t.Pink, Anchor.Center, new Vector2(0f, 300f), new Vector2(880f, 230f), sliced: true);
            ribbon.pixelsPerUnitMultiplier = 0.8f;
            var rect = ribbon.rectTransform;
            group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            line1 = b.Text("Line 1", rect, "", t.DisplayFont, 78, t.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -22f), new Vector2(840f, 104f));
            UiBuilder.Lip(line1, UiBuilder.Darken(t.Pink, 0.45f), 5f);
            line2 = b.Text("Line 2", rect, "", t.TitleFont, 36, UiBuilder.WithAlpha(t.Cream, 0.92f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -128f), new Vector2(840f, 52f));
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
