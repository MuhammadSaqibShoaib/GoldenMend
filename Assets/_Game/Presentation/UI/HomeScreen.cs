using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Main menu. Top: coins and settings. Middle: the title and the next vessel waiting to be restored.
    /// Bottom (thumb zone): progress, the Play button and the museum.
    /// </summary>
    public sealed class HomeScreen : UiView
    {
        public Action Play;
        public Action OpenMuseum;
        public Action OpenSettings;

        readonly Text coins;
        readonly Text nextCaption;
        readonly Text progressCaption;
        readonly Text playLevel;
        readonly RectTransform showcase;
        readonly RectTransform progressFill;
        readonly RectTransform playButton;
        RectTransform preview;

        const float ProgressWidth = 560f;

        public HomeScreen(UiRoot ui) : base(ui, "Home", popup: false)
        {
            coins = B.CoinPill("Coins", Root, T.Indigo, "0", T.Cream, 44, Anchor.TopLeft, new Vector2(UiType.Margin, -40f), new Vector2(270f, 100f));
            B.IconButton("Settings", Root, T.IconSettings, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-UiType.Margin, -32f), UiType.IconButton, () => OpenSettings?.Invoke());

            B.Text("Title", Root, "Golden Mend", T.DisplayFont, 112, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -196f), new Vector2(1000f, 150f));
            B.Text("Subtitle", Root, "a kintsugi puzzle", T.BodyFont, 36, UiBuilder.WithAlpha(T.Cream, 0.62f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -340f), new Vector2(900f, 56f));

            showcase = B.Node("Showcase", Root, Anchor.Center, new Vector2(0f, 90f), new Vector2(760f, 640f));
            B.Image("Glow", showcase, T.Circle, UiBuilder.WithAlpha(T.Gold, 0.06f), Anchor.Center, Vector2.zero, new Vector2(700f, 700f));
            nextCaption = B.Text("Next", Root, "", T.BodyFont, 36, T.Cream, TextAnchor.MiddleCenter, Anchor.Center, new Vector2(0f, -284f), new Vector2(940f, 60f));

            progressCaption = B.Text("Progress", Root, "", T.BodyFont, UiType.Caption, UiBuilder.WithAlpha(T.Cream, 0.7f), TextAnchor.MiddleCenter, Anchor.Bottom, new Vector2(0f, 478f), new Vector2(800f, 44f));
            var track = B.Image("Progress Track", Root, T.Badge, UiBuilder.WithAlpha(T.Indigo, 0.9f), Anchor.Bottom, new Vector2(0f, 446f), new Vector2(ProgressWidth, 22f), sliced: true);
            var fill = B.Image("Progress Fill", track.transform, T.Badge, T.Gold, Anchor.Left, Vector2.zero, new Vector2(0f, 22f), sliced: true);
            progressFill = fill.rectTransform;

            var play = B.Button("Play", Root, "Play", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 228f), new Vector2(660f, 178f), () => Play?.Invoke());
            playButton = (RectTransform)play.transform;
            var playLabel = play.GetComponentInChildren<Text>();
            playLabel.fontSize = 64;
            playLabel.rectTransform.anchoredPosition += new Vector2(0f, 20f);
            playLevel = B.Text("Level", play.transform, "", T.TitleFont, UiType.Caption, UiBuilder.WithAlpha(T.Ink, 0.7f), TextAnchor.MiddleCenter, Anchor.Bottom, new Vector2(0f, 30f), new Vector2(500f, 40f));

            B.Button("Museum", Root, "Museum", ButtonStyle.Secondary, Anchor.Bottom, new Vector2(0f, 72f), new Vector2(440f, 124f), () => OpenMuseum?.Invoke(), T.IconMuseum);
        }

        public void Bind(int coinCount, BuiltLevel next, int nextIndex, int completed, int total, bool allComplete)
        {
            coins.text = coinCount.ToString();
            progressCaption.text = $"{completed} of {total} vessels restored";
            progressFill.sizeDelta = new Vector2(ProgressWidth * Mathf.Clamp01(total > 0 ? completed / (float)total : 0f), progressFill.sizeDelta.y);

            if (preview != null) Release(preview.gameObject);
            preview = VesselPreview.Build(B, showcase, next, allComplete ? VesselLook.Restored : VesselLook.Teaser, Anchor.Center, Vector2.zero, new Vector2(560f, 470f));

            string gold = ColorUtility.ToHtmlStringRGB(T.Gold);
            nextCaption.text = allComplete
                ? "Every vessel restored. Replay any piece from the museum."
                : $"Next to restore  ·  <color=#{gold}>{next.Spec.Name}</color>";
            playLevel.text = allComplete ? $"Replay level {nextIndex + 1}" : $"Level {nextIndex + 1}";
        }

        protected override void OnShow() => Ui.Pop(playButton, 0.1f);

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
