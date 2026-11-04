using System;
using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// The collection: only what the player has reached, the restored vessels and the one they are on, six per page
    /// with previous / next arrows when there is more than one page. Nothing hints at how many vessels are still to
    /// come. Only the visible page is built, so the museum costs the same with 30 vessels or 300. It opens on the page
    /// holding the current vessel. Restored pieces show in full with their gold border and stars, the current one as
    /// a gold-edged teaser with a Restore button. Tapping a card opens its details.
    /// </summary>
    public sealed class MuseumScreen : UiView
    {
        public Action Back;
        public Action<int> PlayLevel;
        public Action<int> OpenDetails;

        const float HeaderHeight = 200f;
        const float FooterHeight = 190f;
        public const int PageSize = 6;
        static readonly Vector2 CardSize = new Vector2(480f, 440f);

        readonly RectTransform content;
        readonly ScrollRect scroll;
        readonly Button previous;
        readonly Button next;

        int page;
        int count; // vessels shown: every unlocked one
        Func<int, BuiltLevel> levelAt;
        Func<int, bool> unlocked;
        Func<int, bool> completed;
        Func<int, int> stars;

        int PageCount => Mathf.Max(1, (count + PageSize - 1) / PageSize);

        public MuseumScreen(UiRoot ui) : base(ui, "Museum", popup: false)
        {
            var header = B.Node("Header", Root, Anchor.StretchTop, Vector2.zero, new Vector2(0f, HeaderHeight));
            B.IconButton("Back", header, T.IconPrevious, ButtonStyle.Secondary, Anchor.TopLeft, new Vector2(36f, -44f), 124f, () => Back?.Invoke());
            var title = B.Text("Title", header, "Museum", T.DisplayFont, 104, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -40f), new Vector2(640f, 130f));
            UiBuilder.Lip(title, UiTheme.Hex(0xA1501A), 8f);

            // Page turner pinned to the bottom.
            var footer = B.Node("Footer", Root, Anchor.StretchBottom, Vector2.zero, new Vector2(0f, FooterHeight));
            previous = B.IconButton("Previous Page", footer, T.IconPrevious, ButtonStyle.Secondary, Anchor.Left, new Vector2(60f, 10f), 132f, () => TurnPage(-1));
            next = B.IconButton("Next Page", footer, T.IconNext, ButtonStyle.Secondary, Anchor.Right, new Vector2(-60f, 10f), 132f, () => TurnPage(1));

            // The page's cards between header and footer; scrolls only if a short screen can't fit three rows.
            var viewport = B.Node("Viewport", Root, Anchor.Fill, Vector2.zero, Vector2.zero);
            viewport.offsetMax = new Vector2(0f, -HeaderHeight);
            viewport.offsetMin = new Vector2(0f, FooterHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            content = B.Node("Content", viewport, Anchor.StretchTop, Vector2.zero, new Vector2(0f, 0f));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CardSize;
            grid.spacing = new Vector2(24f, 20f);
            grid.padding = new RectOffset(48, 48, 24, 24);
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

        /// <param name="levelAt">Builds a level on demand; only the shown page's levels are asked for.</param>
        /// <param name="focus">The level whose page opens first: the current one.</param>
        public void Bind(int levelCount, Func<int, BuiltLevel> levelAt, Func<int, bool> unlocked, Func<int, bool> completed, Func<int, int> stars,
            int focus)
        {
            // Levels unlock in order, so the reached ones are a prefix of the list.
            count = 0;
            while (count < levelCount && unlocked(count)) count++;
            this.levelAt = levelAt;
            this.unlocked = unlocked;
            this.completed = completed;
            this.stars = stars;

            ShowPage(Mathf.Clamp(focus, 0, Mathf.Max(0, count - 1)) / PageSize);
        }

        public int Page => page;

        public void ShowPage(int index)
        {
            page = Mathf.Clamp(index, 0, PageCount - 1);
            for (int i = content.childCount - 1; i >= 0; i--) Release(content.GetChild(i).gameObject);

            int first = page * PageSize;
            for (int i = first; i < Mathf.Min(first + PageSize, count); i++)
                BuildCard(i, levelAt(i), unlocked(i), completed(i), stars(i));

            previous.gameObject.SetActive(PageCount > 1);
            next.gameObject.SetActive(PageCount > 1);
            SetInteractable(previous, page > 0);
            SetInteractable(next, page < PageCount - 1);

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition = 1f;
        }

        void TurnPage(int step)
        {
            int target = Mathf.Clamp(page + step, 0, PageCount - 1);
            if (target == page) return;
            ShowPage(target);
            Ui.SlideIn(content, new Vector2(0f, content.anchoredPosition.y), new Vector2(step * 160f, 0f));
        }

        void BuildCard(int index, BuiltLevel level, bool isUnlocked, bool isCompleted, int starCount)
        {
            var card = B.Card($"Card {index + 1}", content, Anchor.TopLeft, Vector2.zero, CardSize, radius: 0.75f);
            bool teaser = isUnlocked && !isCompleted;
            var edge = card.Find("Edge").GetComponent<Image>();
            if (teaser) edge.color = T.Gold;
            else if (!isUnlocked) edge.color = UiBuilder.WithAlpha(T.PanelEdge, 0.4f);
            if (isUnlocked) B.Tappable(card, () => OpenDetails?.Invoke(index));

            var niche = B.Inset("Niche", card, Anchor.Top, new Vector2(0f, -20f), new Vector2(CardSize.x - 48f, 228f));
            var look = isCompleted ? VesselLook.Restored : isUnlocked ? VesselLook.Teaser : VesselLook.Silhouette;
            VesselPreview.Build(B, niche.transform, level, look, Anchor.Center, Vector2.zero, new Vector2(360f, 192f));
            if (!isUnlocked) B.Image("Lock", niche.transform, T.IconLock, UiBuilder.WithAlpha(T.Cream, 0.85f), Anchor.Center, Vector2.zero, new Vector2(84f, 84f));

            string name = isUnlocked ? VesselArt.DisplayName(level.Spec) : $"No. {index + 1}";
            B.Text("Name", card, name, T.TitleFont, 46, isUnlocked ? T.Cream : T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -258f), new Vector2(440f, 60f));

            if (isCompleted)
            {
                B.Stars(card, starCount, Anchor.Top, new Vector2(0f, -338f), 56f, raised: false);
            }
            else if (isUnlocked)
            {
                B.Button("Restore", card, "Restore", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 24f), new Vector2(380f, 96f), () => PlayLevel?.Invoke(index));
            }
            else
            {
                B.Text("State", card, "Locked", T.BodyFont, 34, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -330f), new Vector2(420f, 48f));
            }
        }

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
