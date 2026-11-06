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
        public Action<bool> MusicChanged;
        public Action<bool> PersonalizedAdsChanged;

        readonly RectTransform card;
        readonly Switch sound;
        readonly Switch haptics;
        readonly Switch music;
        readonly Switch personalizedAds;

        sealed class Switch
        {
            public Image Track;
            public RectTransform Knob;
            public bool On;
        }

        public SettingsPopup(UiRoot ui) : base(ui, "Settings", popup: true)
        {
            card = B.Popup(Root, new Vector2(920f, 1250f), Vector2.zero, () => Done?.Invoke(), out var title);
            title.text = "Settings";

            sound = Row("Sound", T.IconSoundOn, -196f, on => SoundChanged?.Invoke(on));
            music = Row("Music", T.IconMusic, -388f, on => MusicChanged?.Invoke(on));
            haptics = Row("Haptics", T.IconHaptics, -580f, on => HapticsChanged?.Invoke(on));
            personalizedAds = Row("Personal ads", T.IconPlay, -772f, on => PersonalizedAdsChanged?.Invoke(on));

            B.Text("Version", card, $"Golden Mend  {Application.version}", T.BodyFont, 26, UiBuilder.WithAlpha(T.Muted, 0.6f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -930f), new Vector2(700f, 40f));

            B.Button("Done", card, "Done", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 64f), new Vector2(560f, 160f), () => Done?.Invoke());
        }

        public void Bind(bool soundOn, bool musicOn, bool hapticsOn, bool personalizedAdsOn = true)
        {
            Set(sound, soundOn);
            Set(music, musicOn);
            Set(haptics, hapticsOn);
            Set(personalizedAds, personalizedAdsOn);
        }

        protected override void OnShow() => Ui.Pop(card);

        Switch Row(string label, Sprite icon, float y, Action<bool> changed)
        {
            var row = B.Card(label, card, Anchor.Top, new Vector2(0f, y), new Vector2(800f, 164f), glass: true, radius: 0.7f);
            var tile = B.Node("Icon Tile", row, Anchor.Left, new Vector2(24f, 0f), new Vector2(116f, 116f));
            B.GlassSurface(tile, T.Ribbon, 0.8f);
            var glyph = B.Image("Icon", tile, icon, T.OnAccent, Anchor.Center, Vector2.zero, new Vector2(64f, 64f));
            UiBuilder.Lip(glyph, UiBuilder.Darken(T.Ribbon, 0.4f), 2f);
            B.Text("Label", row, label, T.TitleFont, 54, T.Text, TextAnchor.MiddleLeft, Anchor.Left, new Vector2(164f, 0f), new Vector2(360f, 80f));

            var toggle = new Switch();
            toggle.Track = B.Image("Switch", row, UiSkin.Pill, T.Confirm, Anchor.Right, new Vector2(-32f, 0f), new Vector2(176f, 88f), sliced: true);
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
            toggle.Track.color = on ? T.Confirm : T.StarEmpty;
            UiBuilder.Place(toggle.Knob, on ? Anchor.Right : Anchor.Left, new Vector2(on ? -8f : 8f, 0f), new Vector2(72f, 72f));
        }
    }

    /// <summary>
    /// Level complete: a full-screen celebration. The title, the stars, the restored vessel, the coin reward, then the
    /// run's numbers and record line, with the next level's button (in its difficulty tone) as the main action and
    /// Home as a quiet one. After the last level the main action is the museum.
    /// </summary>
    public sealed class WinSheet : UiView
    {
        public Action Next;
        public Action Home;

        readonly RectTransform content;
        readonly RectTransform stars;
        int earnedStars;
        readonly RectTransform showcase;
        readonly Text subtitle;
        readonly Text reward;
        readonly Text stats;
        readonly Text record;
        readonly LevelButton nextLevel;
        readonly Button museumButton;
        RectTransform preview;

        public WinSheet(UiRoot ui) : base(ui, "Level Complete", popup: true)
        {
            Backdrop.sprite = UiSkin.VerticalGradient(T.SkyTop, T.SkyBottom);
            Backdrop.color = Color.white;

            content = B.Node("Content", Root, Anchor.Fill, Vector2.zero, Vector2.zero);
            B.Image("Glow", content, UiSkin.Glow, UiBuilder.WithAlpha(Color.white, 0.75f), Anchor.Top, new Vector2(0f, -40f), new Vector2(1400f, 1400f));
            var title = B.Text("Title", content, "Restored!", T.DisplayFont, 150, T.Title, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -100f), new Vector2(1060f, 200f));
            UiBuilder.Lip(title, T.TitleLip, 8f);
            stars = B.Node("Stars", content, Anchor.Top, new Vector2(0f, -330f), new Vector2(600f, 200f));
            showcase = B.Node("Vessel", content, Anchor.Top, new Vector2(0f, -540f), new Vector2(760f, 500f));

            subtitle = B.Text("Subtitle", content, "", T.TitleFont, 46, T.Text, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -1072f), new Vector2(980f, 64f));
            reward = B.CoinPill("Reward", content, T.Chip, "+0", T.Text, 64, Anchor.Top, new Vector2(0f, -1166f), new Vector2(360f, 128f));

            var box = B.Card("Stats", content, Anchor.Top, new Vector2(0f, -1336f), new Vector2(860f, 184f), glass: true, radius: 0.7f);
            stats = B.Text("Stats", box, "", T.BodyFont, 36, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -22f), new Vector2(820f, 56f));
            record = B.Text("Record", box, "", T.TitleFont, 38, T.Title, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -94f), new Vector2(820f, 64f));

            nextLevel = new LevelButton(B, content, Anchor.Bottom, new Vector2(0f, 150f), new Vector2(640f, 196f), () => Next?.Invoke());
            museumButton = B.Button("Museum", content, "Museum", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 150f), new Vector2(640f, 196f), () => Next?.Invoke(), T.IconMuseum);
            B.Button("Home", content, "Home", ButtonStyle.Ghost, Anchor.Bottom, new Vector2(0f, 30f), new Vector2(360f, 100f), () => Home?.Invoke());
        }

        /// <param name="recordLine">The personal-record line under the stats, for example "New personal best!"; may be empty.</param>
        /// <param name="level">Shown restored in the middle of the screen; omitted in tests.</param>
        /// <param name="nextNumber">The next level's number as shown to the player (1-based); used when <paramref name="hasNext"/>.</param>
        public void Bind(string vesselName, string byline, int starCount, int coins, bool firstClear, int benchPeak, int bestPeak, bool hasNext,
            string recordLine = "", BuiltLevel level = null, int nextNumber = 0, Difficulty nextDifficulty = Difficulty.Normal)
        {
            for (int i = stars.childCount - 1; i >= 0; i--) Release(stars.GetChild(i).gameObject);
            B.Stars(stars, starCount, Anchor.Center, Vector2.zero, 150f, raised: true);
            earnedStars = starCount;

            if (preview != null) Release(preview.gameObject);
            preview = level != null ? VesselPreview.Build(B, showcase, level, VesselLook.Restored, Anchor.Center, Vector2.zero, new Vector2(720f, 460f)) : null;

            string piece = byline != null ? $"{vesselName} by {byline.Split(',')[0]}" : vesselName;
            subtitle.text = firstClear ? $"{piece} joins your museum" : $"{piece}, restored once more";
            reward.text = $"+{coins}";
            stats.text = $"Busiest workbench {benchPeak}  ·  best possible {bestPeak}";
            record.text = recordLine ?? "";
            nextLevel.Rect.gameObject.SetActive(hasNext);
            museumButton.gameObject.SetActive(!hasNext);
            if (hasNext) nextLevel.Bind(nextNumber, nextDifficulty);
        }

        protected override bool PlaysOpenSound => false;

        protected override void OnShow()
        {
            // Stars land one by one, each with a ding a step higher.
            for (int i = 0; i < stars.childCount; i++)
            {
                float delay = 0.3f + 0.22f * i;
                Ui.Pop(stars.GetChild(i), delay);
                if (i < earnedStars) Ui.StarSound(i, delay);
            }
            Ui.Pop(reward.transform.parent, 0.3f + 0.22f * stars.childCount);
        }

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// Workbench full: the continue offer (+2 slots for coins) as the main action, then retry and home.
    /// The offer greys out with a clear reason when the player can't afford it, and is gone once it was used this
    /// attempt: one continue per try keeps the bench limit meaningful.
    /// </summary>
    public sealed class FailPopup : UiView
    {
        public Action Continue;
        public Action ContinueWithAd;
        public Action Retry;
        public Action Home;

        readonly RectTransform card;
        readonly Button continueButton;
        readonly Button adButton;
        readonly Text price;
        readonly Text shortfall;
        readonly Text body;
        readonly Text slots;
        readonly RectTransform retry;
        readonly RectTransform home;

        public FailPopup(UiRoot ui) : base(ui, "Workbench Full", popup: true)
        {
            card = B.Popup(Root, new Vector2(920f, 940f), new Vector2(0f, -20f), null, out var title);
            title.text = "Bench full!";

            body = B.Text("Body", card, "No free slot for that shard.\nAdd two more and keep going?", T.TitleFont, 44, T.Text,
                TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -184f), new Vector2(820f, 130f));

            continueButton = OfferButton(B, card, "Continue", new Vector2(0f, -352f), () => Continue?.Invoke(), out price);
            adButton = B.Button("Watch ad", card, "Watch an ad", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -572f), new Vector2(760f, 150f), () => ContinueWithAd?.Invoke(), T.IconPlay);
            slots = B.Text("Slots", card, "Adds 2 workbench slots", T.BodyFont, 34, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -566f), new Vector2(800f, 48f));
            shortfall = B.Text("Shortfall", card, "", T.TitleFont, 34, T.Danger, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -614f), new Vector2(800f, 48f));

            retry = (RectTransform)B.Button("Retry", card, "Retry", ButtonStyle.Secondary, Anchor.Top, new Vector2(-190f, -690f), new Vector2(360f, 152f), () => Retry?.Invoke(), T.IconRestart).transform;
            home = (RectTransform)B.Button("Home", card, "Home", ButtonStyle.Secondary, Anchor.Top, new Vector2(190f, -690f), new Vector2(360f, 152f), () => Home?.Invoke(), T.IconHome).transform;
        }

        /// <summary>A green glass buy button: the action on the left, the coin price in a capsule on the right.</summary>
        internal static Button OfferButton(UiBuilder b, Transform parent, string action, Vector2 position, Action onClick, out Text price)
        {
            var button = b.Button(action, parent, action, ButtonStyle.Confirm, Anchor.Top, position, new Vector2(760f, 200f), onClick);
            var label = button.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = 64;
            label.rectTransform.offsetMin = new Vector2(56f, label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax = new Vector2(-260f, label.rectTransform.offsetMax.y);
            price = b.CoinPill("Price", button.transform, UiBuilder.WithAlpha(UiBuilder.Darken(b.Theme.Confirm, 0.5f), 0.28f), "0", Color.white, 52,
                Anchor.Right, new Vector2(-32f, 0f), new Vector2(220f, 104f));
            return button;
        }

        public void Bind(int cost, int coins, bool canContinue = true, bool adReady = false)
        {
            bool showAd = canContinue && adReady;
            continueButton.gameObject.SetActive(canContinue);
            adButton.gameObject.SetActive(showAd);
            slots.gameObject.SetActive(canContinue);
            body.text = canContinue
                ? "No free slot for that shard.\nAdd two more and keep going?"
                : "No free slot for that shard.\nYou already used this try's continue.";
            // The ad option slots in under the coin button and pushes the rest down.
            float shift = showAd ? 170f : 0f;
            ((RectTransform)slots.transform).anchoredPosition = new Vector2(0f, -566f - shift);
            ((RectTransform)shortfall.transform).anchoredPosition = new Vector2(0f, -614f - shift);
            float buttonsY = canContinue ? -690f - shift : -400f;
            retry.anchoredPosition = new Vector2(-190f, buttonsY);
            home.anchoredPosition = new Vector2(190f, buttonsY);
            card.sizeDelta = new Vector2(card.sizeDelta.x, canContinue ? 940f + shift : 650f);

            price.text = cost.ToString();
            bool canAfford = coins >= cost;
            SetInteractable(continueButton, canAfford);
            shortfall.text = canContinue && !canAfford ? $"Not enough coins (you have {coins})" : "";
        }

        protected override void OnShow() => Ui.Pop(card);
    }

    /// <summary>
    /// Asked once, before any ad, where the law requires a choice first (see AdConsent). Two clear, equal choices and
    /// no close button; either answer can be changed later in Settings.
    /// </summary>
    public sealed class ConsentPopup : UiView
    {
        public Action<bool> Chosen;

        readonly RectTransform card;

        public ConsentPopup(UiRoot ui) : base(ui, "Ad Consent", popup: true)
        {
            card = B.Popup(Root, new Vector2(920f, 1020f), Vector2.zero, null, out var title);
            title.text = "Ads in Golden Mend";

            B.Text("Body", card,
                "Golden Mend is free and shows a few ads.\nMay our ad partners use data from this\ndevice to show ads that suit you better?",
                T.TitleFont, 40, T.Text, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -250f), new Vector2(840f, 220f));
            B.Text("Note", card, "Either way you get the same game.\nYou can change this any time in Settings.",
                T.BodyFont, 30, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -420f), new Vector2(800f, 90f));

            B.Button("Allow", card, "Personalized ads", ButtonStyle.Confirm, Anchor.Top, new Vector2(0f, -560f), new Vector2(760f, 168f), () => Chosen?.Invoke(true));
            B.Button("Decline", card, "General ads only", ButtonStyle.Secondary, Anchor.Top, new Vector2(0f, -760f), new Vector2(760f, 168f), () => Chosen?.Invoke(false));
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

            var halo = B.Image("Halo", card, UiSkin.Glow, UiBuilder.WithAlpha(T.Gold, 0.55f), Anchor.Top, new Vector2(0f, -140f), new Vector2(300f, 300f));
            var badge = B.Node("Badge", halo.transform, Anchor.Center, Vector2.zero, new Vector2(196f, 196f));
            B.GlassSurface(badge, T.Gold, 1.6f);
            icon = B.Image("Icon", badge, T.IconHint, T.OnAccent, Anchor.Center, Vector2.zero, new Vector2(112f, 112f));
            UiBuilder.Lip(icon, UiBuilder.Darken(T.Gold, 0.4f), 3f);

            body = B.Text("Body", card, "", T.TitleFont, 42, T.Text, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -420f), new Vector2(820f, 130f));
            buyButton = FailPopup.OfferButton(B, card, "Buy", new Vector2(0f, -584f), () => Buy?.Invoke(), out price);
            buyLabel = buyButton.GetComponentInChildren<Text>();
            shortfall = B.Text("Shortfall", card, "", T.TitleFont, 34, T.Danger, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -794f), new Vector2(800f, 48f));
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

    /// <summary>A short announcement over the board when a level starts: a glass ribbon in the level's difficulty tone.</summary>
    public sealed class LevelBanner
    {
        readonly UiRoot ui;
        readonly CanvasGroup group;
        readonly Text line1;
        readonly Text line2;
        readonly Image fill;
        readonly Shadow line1Shade;
        Coroutine running;

        public LevelBanner(UiRoot ui)
        {
            this.ui = ui;
            var b = ui.Build;
            var t = ui.Theme;
            var rect = b.Node("Level Banner", ui.Screens, Anchor.Center, new Vector2(0f, 300f), new Vector2(880f, 230f));
            fill = b.GlassSurface(rect, t.Primary, 1.4f);
            group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            line1 = b.Text("Line 1", rect, "", t.DisplayFont, 78, t.OnAccent, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -22f), new Vector2(840f, 104f));
            line1Shade = UiBuilder.Lip(line1, UiBuilder.Darken(t.Primary, 0.4f), 4f);
            line2 = b.Text("Line 2", rect, "", t.TitleFont, 36, UiBuilder.WithAlpha(t.OnAccent, 0.95f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -128f), new Vector2(840f, 52f));
            rect.gameObject.SetActive(false);
        }

        public void Show(string title, string detail, float seconds = 1.8f, Difficulty difficulty = Difficulty.Normal)
        {
            var tone = ui.Build.DifficultyColor(difficulty);
            fill.color = tone;
            line1Shade.effectColor = UiBuilder.Darken(tone, 0.4f);
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
