using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Main menu. Top: coins and settings. Middle: the title, the next vessel's name and the vessel itself, floating on the sky,
    /// kept alive by <see cref="ShowcaseMotion"/>. Bottom (thumb zone): the level button and the museum. No
    /// progress count: players shouldn't see how many vessels are left.
    /// </summary>
    public sealed class HomeScreen : UiView
    {
        public Action Play;
        public Action OpenMuseum;
        public Action OpenSettings;

        readonly Text coins;
        readonly Text levelChip;
        readonly RectTransform showcase;
        readonly LevelButton levelButton;
        RectTransform preview;
        readonly ShowcaseMotion motion;

        public HomeScreen(UiRoot ui) : base(ui, "Home", popup: false)
        {
            coins = B.CoinPill("Coins", Root, T.Chip, "0", T.Text, 50, Anchor.TopLeft, new Vector2(40f, -52f), new Vector2(312f, 104f));
            B.IconButton("Settings", Root, T.IconSettings, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-36f, -44f), 124f, () => OpenSettings?.Invoke());

            var title = B.Text("Title", Root, "Golden Mend", T.DisplayFont, 128, T.Title, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -196f), new Vector2(1060f, 170f));
            UiBuilder.Lip(title, T.TitleLip, 8f);
            levelChip = B.Pill("Level", Root, T.Chip, null, Color.clear, "", T.Text, 36, Anchor.Top, new Vector2(0f, -380f), new Vector2(520f, 84f));

            // No panel behind the vessel: it floats on the sky, with the backdrop's clouds and bubbles drifting behind.
            showcase = B.Node("Showcase", Root, Anchor.Top, new Vector2(0f, -512f), new Vector2(920f, 860f));

            // The level button replaces a generic Play: its number and tone say what is next and how hard it is.
            levelButton = new LevelButton(B, Root, Anchor.Bottom, new Vector2(0f, 214f), new Vector2(640f, 196f), () => Play?.Invoke());

            B.Button("Museum", Root, "Museum", ButtonStyle.Secondary, Anchor.Bottom, new Vector2(0f, 56f), new Vector2(480f, 132f), () => OpenMuseum?.Invoke(), T.IconMuseum);

            motion = ShowcaseMotion.Attach(showcase, levelButton.Rect);
        }

        public void Bind(int coinCount, BuiltLevel next, int nextIndex, int completed, int total, bool allComplete)
        {
            coins.text = coinCount.ToString();

            if (preview != null) Release(preview.gameObject);
            preview = VesselPreview.Build(B, showcase, next, allComplete ? VesselLook.Restored : VesselLook.Teaser, Anchor.Center, Vector2.zero, new Vector2(720f, 700f));
            motion.Track(preview);

            levelChip.text = allComplete ? "ALL RESTORED  ·  REPLAY ANY" : VesselArt.DisplayName(next.Spec).ToUpperInvariant();
            levelButton.Bind(nextIndex + 1, next.Spec.Difficulty);
        }

        protected override void OnShow() => motion.Restart();

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
