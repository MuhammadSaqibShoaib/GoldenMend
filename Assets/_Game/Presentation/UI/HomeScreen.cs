using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Main menu. Top: coins and settings. Middle: the gold title, the next level and its vessel on a glass card,
    /// kept alive by <see cref="ShowcaseMotion"/>. Bottom (thumb zone): progress, the big Play button and the museum.
    /// </summary>
    public sealed class HomeScreen : UiView
    {
        public Action Play;
        public Action OpenMuseum;
        public Action OpenSettings;

        readonly Text coins;
        readonly Text levelChip;
        readonly Text progressCaption;
        readonly RectTransform showcase;
        readonly RectTransform progressFill;
        readonly RectTransform playButton;
        RectTransform preview;
        readonly ShowcaseMotion motion;

        const float ProgressWidth = 600f;

        public HomeScreen(UiRoot ui) : base(ui, "Home", popup: false)
        {
            coins = B.CoinPill("Coins", Root, UiBuilder.WithAlpha(Color.black, 0.3f), "0", T.Cream, 50, Anchor.TopLeft, new Vector2(40f, -52f), new Vector2(312f, 104f));
            B.IconButton("Settings", Root, T.IconSettings, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-36f, -44f), 124f, () => OpenSettings?.Invoke());

            var title = B.Text("Title", Root, "Golden Mend", T.DisplayFont, 128, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -196f), new Vector2(1060f, 170f));
            UiBuilder.Lip(title, UiTheme.Hex(0xA1501A), 10f);
            levelChip = B.Pill("Level", Root, UiBuilder.WithAlpha(Color.white, 0.12f), null, Color.clear, "", T.Cream, 36, Anchor.Top, new Vector2(0f, -380f), new Vector2(520f, 84f));

            showcase = B.Card("Showcase", Root, Anchor.Top, new Vector2(0f, -512f), new Vector2(920f, 740f), glass: true);

            progressCaption = B.Text("Progress", Root, "", T.BodyFont, 40, T.Cream, TextAnchor.MiddleCenter, Anchor.Bottom, new Vector2(0f, 572f), new Vector2(800f, 56f));
            var track = B.Image("Progress Track", Root, UiSkin.Pill, UiBuilder.WithAlpha(Color.white, 0.12f), Anchor.Bottom, new Vector2(0f, 520f), new Vector2(ProgressWidth, 40f), sliced: true);
            track.pixelsPerUnitMultiplier = 64f / 40f;
            var fill = B.Image("Progress Fill", track.transform, UiSkin.Pill, T.Gold, Anchor.Left, Vector2.zero, new Vector2(0f, 40f), sliced: true);
            fill.pixelsPerUnitMultiplier = 64f / 40f;
            progressFill = fill.rectTransform;

            var play = B.Button("Play", Root, "Play", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 236f), new Vector2(700f, 216f), () => Play?.Invoke());
            play.GetComponentInChildren<Text>().fontSize = 100;
            playButton = (RectTransform)play.transform;

            B.Button("Museum", Root, "Museum", ButtonStyle.Secondary, Anchor.Bottom, new Vector2(0f, 56f), new Vector2(520f, 148f), () => OpenMuseum?.Invoke(), T.IconMuseum);

            motion = ShowcaseMotion.Attach(B, showcase, playButton);
        }

        public void Bind(int coinCount, BuiltLevel next, int nextIndex, int completed, int total, bool allComplete)
        {
            coins.text = coinCount.ToString();
            progressCaption.text = $"{completed} of {total} restored";
            float fraction = Mathf.Clamp01(total > 0 ? completed / (float)total : 0f);
            progressFill.sizeDelta = new Vector2(fraction > 0f ? Mathf.Max(40f, ProgressWidth * fraction) : 0f, progressFill.sizeDelta.y);

            if (preview != null) Release(preview.gameObject);
            preview = VesselPreview.Build(B, showcase, next, allComplete ? VesselLook.Restored : VesselLook.Teaser, Anchor.Center, Vector2.zero, new Vector2(700f, 600f));
            motion.Track(preview);

            string name = VesselArt.DisplayName(next.Spec).ToUpperInvariant();
            levelChip.text = allComplete ? "ALL RESTORED  ·  REPLAY ANY" : $"LEVEL {nextIndex + 1}  ·  {name}";
        }

        protected override void OnShow() => motion.Restart();

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
