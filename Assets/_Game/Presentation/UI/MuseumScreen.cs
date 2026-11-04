using System;
using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// The collection: only what the player has reached, the restored vessels and the one they are on, in a
    /// scrolling two-column grid. Nothing hints at how many vessels are still to come. Cards are built lazily: only
    /// the rows in view (plus one spare row each side) exist, and rows that scroll away are released, so the museum
    /// costs the same with 30 vessels or 300. It opens scrolled to the current vessel. Restored pieces show in full
    /// with their gold border and stars, the current one as a gold-edged teaser with a Restore button. Tapping a card
    /// opens its details.
    /// </summary>
    public sealed class MuseumScreen : UiView
    {
        public Action Back;
        public Action<int> PlayLevel;
        public Action<int> OpenDetails;

        const float HeaderHeight = 200f;
        const float Padding = 24f;
        const float Spacing = 20f;
        const int SpareRows = 1;
        static readonly Vector2 CardSize = new Vector2(480f, 440f);
        static float Stride => CardSize.y + Spacing;

        readonly RectTransform content;
        readonly RectTransform viewport;
        readonly ScrollRect scroll;
        readonly Dictionary<int, RectTransform> built = new Dictionary<int, RectTransform>();

        int count; // vessels shown: every unlocked one
        int firstRow = -1;
        int lastRow = -1;
        Func<int, BuiltLevel> levelAt;
        Func<int, bool> completed;
        Func<int, int> stars;

        int RowCount => (count + 1) / 2;

        public MuseumScreen(UiRoot ui) : base(ui, "Museum", popup: false)
        {
            var header = B.Node("Header", Root, Anchor.StretchTop, Vector2.zero, new Vector2(0f, HeaderHeight));
            B.IconButton("Back", header, T.IconPrevious, ButtonStyle.Secondary, Anchor.TopLeft, new Vector2(36f, -44f), 124f, () => Back?.Invoke());
            var title = B.Text("Title", header, "Museum", T.DisplayFont, 104, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -40f), new Vector2(640f, 130f));
            UiBuilder.Lip(title, UiTheme.Hex(0xA1501A), 8f);

            viewport = B.Node("Viewport", Root, Anchor.Fill, Vector2.zero, Vector2.zero);
            viewport.offsetMax = new Vector2(0f, -HeaderHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            content = B.Node("Content", viewport, Anchor.StretchTop, Vector2.zero, new Vector2(0f, 0f));

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.onValueChanged.AddListener(_ => RefreshVisible());
        }

        /// <param name="levelAt">Builds a level on demand; only levels whose cards come into view are asked for.</param>
        /// <param name="focus">The level the museum opens scrolled to: the current one.</param>
        public void Bind(int levelCount, Func<int, BuiltLevel> levelAt, Func<int, bool> unlocked, Func<int, bool> completed, Func<int, int> stars,
            int focus)
        {
            // Levels unlock in order, so the reached ones are a prefix of the list.
            count = 0;
            while (count < levelCount && unlocked(count)) count++;
            this.levelAt = levelAt;
            this.completed = completed;
            this.stars = stars;

            foreach (var card in built.Values) Release(card.gameObject);
            built.Clear();
            firstRow = lastRow = -1;

            float height = Padding * 2f + RowCount * CardSize.y + Mathf.Max(0, RowCount - 1) * Spacing;
            content.sizeDelta = new Vector2(0f, height);

            // Centre the current vessel's row in view, within the scroll range.
            scroll.StopMovement();
            int row = Mathf.Clamp(focus, 0, Mathf.Max(0, count - 1)) / 2;
            float view = viewport.rect.height;
            float target = Padding + row * Stride - (view - CardSize.y) / 2f;
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(target, 0f, Mathf.Max(0f, height - view)));
            RefreshVisible();
        }

        /// <summary>Builds the cards of rows in view and releases rows that have scrolled well away.</summary>
        void RefreshVisible()
        {
            if (count == 0) return;
            float top = content.anchoredPosition.y;
            float view = viewport.rect.height;
            int first = Mathf.Max(0, Mathf.FloorToInt((top - Padding) / Stride) - SpareRows);
            int last = Mathf.Min(RowCount - 1, Mathf.FloorToInt((top + view - Padding) / Stride) + SpareRows);
            if (first == firstRow && last == lastRow) return;
            firstRow = first;
            lastRow = last;

            var gone = new List<int>();
            foreach (var pair in built)
            {
                int r = pair.Key / 2;
                if (r < first || r > last) gone.Add(pair.Key);
            }
            foreach (int index in gone)
            {
                Release(built[index].gameObject);
                built.Remove(index);
            }

            for (int index = first * 2; index <= Mathf.Min(count - 1, last * 2 + 1); index++)
            {
                if (built.ContainsKey(index)) continue;
                built[index] = BuildCard(index, levelAt(index), completed(index), stars(index));
            }
        }

        RectTransform BuildCard(int index, BuiltLevel level, bool isCompleted, int starCount)
        {
            float x = (index % 2 == 0 ? -1f : 1f) * (CardSize.x + 24f) / 2f;
            float y = -(Padding + index / 2 * Stride);
            var card = B.Card($"Card {index + 1}", content, Anchor.Top, new Vector2(x, y), CardSize, radius: 0.75f);
            if (!isCompleted) card.Find("Edge").GetComponent<Image>().color = T.Gold;
            B.Tappable(card, () => OpenDetails?.Invoke(index));

            var niche = B.Inset("Niche", card, Anchor.Top, new Vector2(0f, -20f), new Vector2(CardSize.x - 48f, 228f));
            VesselPreview.Build(B, niche.transform, level, isCompleted ? VesselLook.Restored : VesselLook.Teaser, Anchor.Center, Vector2.zero, new Vector2(360f, 192f));

            B.Text("Name", card, VesselArt.DisplayName(level.Spec), T.TitleFont, 46, T.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -258f), new Vector2(440f, 60f));

            if (isCompleted) B.Stars(card, starCount, Anchor.Top, new Vector2(0f, -338f), 56f, raised: false);
            else B.Button("Restore", card, "Restore", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 24f), new Vector2(380f, 96f), () => PlayLevel?.Invoke(index));
            return card;
        }

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
