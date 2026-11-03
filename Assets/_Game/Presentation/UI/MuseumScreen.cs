using System;
using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// The collection: every vessel as a card in a scrolling two-column grid. Restored pieces show in full with
    /// their gold border, the next one as a teaser, later ones as locked silhouettes. Tapping an unlocked card
    /// opens its details.
    /// </summary>
    public sealed class MuseumScreen : UiView
    {
        public Action Back;
        public Action<int> PlayLevel;
        public Action<int> OpenDetails;

        const float HeaderHeight = 250f;
        static readonly Vector2 CardSize = new Vector2(460f, 610f);

        readonly Text subtitle;
        readonly RectTransform content;
        readonly ScrollRect scroll;

        public MuseumScreen(UiRoot ui) : base(ui, "Museum", popup: false)
        {
            var header = B.Node("Header", Root, Anchor.StretchTop, Vector2.zero, new Vector2(0f, HeaderHeight));
            B.IconButton("Back", header, T.IconPrevious, ButtonStyle.Secondary, Anchor.TopLeft, new Vector2(UiType.Margin, -32f), UiType.IconButton, () => Back?.Invoke());
            B.Text("Title", header, "Museum", T.DisplayFont, UiType.H1, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -36f), new Vector2(640f, 100f));
            subtitle = B.Text("Subtitle", header, "", T.BodyFont, UiType.Caption, UiBuilder.WithAlpha(T.Cream, 0.7f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -140f), new Vector2(760f, 48f));
            B.Image("Divider", header, T.Divider, UiBuilder.WithAlpha(T.Gold, 0.55f), Anchor.Bottom, new Vector2(0f, 24f), new Vector2(420f, 6f), sliced: false).preserveAspect = false;

            // Scroll view below the header.
            var viewport = B.Node("Viewport", Root, Anchor.Fill, Vector2.zero, Vector2.zero);
            viewport.offsetMax = new Vector2(0f, -HeaderHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            content = B.Node("Content", viewport, Anchor.StretchTop, Vector2.zero, new Vector2(0f, 0f));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CardSize;
            grid.spacing = new Vector2(32f, 32f);
            grid.padding = new RectOffset(48, 48, 24, 80);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
        }

        public void Bind(IReadOnlyList<BuiltLevel> levels, Func<int, bool> unlocked, Func<int, bool> completed, Func<int, int> stars)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Release(content.GetChild(i).gameObject);

            int done = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                if (completed(i)) done++;
                BuildCard(i, levels[i], unlocked(i), completed(i), stars(i));
            }
            subtitle.text = $"{done} of {levels.Count} vessels restored";

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition = 1f;
        }

        void BuildCard(int index, BuiltLevel level, bool isUnlocked, bool isCompleted, int starCount)
        {
            var card = B.Card($"Card {index + 1}", content, Anchor.TopLeft, Vector2.zero, CardSize);
            if (isUnlocked) B.Tappable(card, () => OpenDetails?.Invoke(index));

            var niche = B.Image("Niche", card, T.Badge, Color.Lerp(T.Indigo, T.Ink, 0.45f), Anchor.Top, new Vector2(0f, -22f), new Vector2(CardSize.x - 44f, 300f), sliced: true);
            var look = isCompleted ? VesselLook.Restored : isUnlocked ? VesselLook.Teaser : VesselLook.Silhouette;
            VesselPreview.Build(B, niche.transform, level, look, Anchor.Center, Vector2.zero, new Vector2(330f, 250f));
            if (!isUnlocked) B.Image("Lock", niche.transform, T.IconLock, UiBuilder.WithAlpha(T.Cream, 0.85f), Anchor.Center, Vector2.zero, new Vector2(92f, 92f));

            string byline = isUnlocked ? VesselArt.Byline(level.Spec) : null;
            B.Text("Number", card, byline ?? $"No. {index + 1}", T.TitleFont, 26, UiBuilder.WithAlpha(T.Ink, 0.5f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -336f), new Vector2(400f, 34f));
            B.Text("Name", card, isUnlocked ? VesselArt.DisplayName(level.Spec) : "???", T.TitleFont, 40, T.Ink, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -368f), new Vector2(420f, 54f));

            if (isCompleted)
            {
                B.Stars(card, starCount, Anchor.Top, new Vector2(0f, -432f), 46f, raised: false);
                B.Button("Details", card, "Details", ButtonStyle.Secondary, Anchor.Bottom, new Vector2(0f, 22f), new Vector2(380f, 90f), () => OpenDetails?.Invoke(index));
            }
            else if (isUnlocked)
            {
                B.Text("State", card, "Waiting for repair", T.BodyFont, 28, UiBuilder.WithAlpha(T.Ink, 0.55f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -434f), new Vector2(420f, 40f));
                B.Button("Restore", card, "Restore", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 22f), new Vector2(380f, 90f), () => PlayLevel?.Invoke(index));
            }
            else
            {
                B.Text("State", card, "Locked", T.BodyFont, 28, UiBuilder.WithAlpha(T.Ink, 0.45f), TextAnchor.MiddleCenter, Anchor.Bottom, new Vector2(0f, 52f), new Vector2(420f, 40f));
            }
        }

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
