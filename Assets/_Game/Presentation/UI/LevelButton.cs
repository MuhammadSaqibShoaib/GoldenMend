using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// The button that starts a level, in place of a generic Play: "Level 17" on one line inside a glossy glass pill
    /// with a thick light rim. Its tone shows the difficulty: sunny orange for a normal level, ruby for hard, deep
    /// violet for very hard, and the hard tiers add a tab on the top edge naming it. The tab's space is always reserved,
    /// so the layout around the button never shifts between levels.
    /// </summary>
    public sealed class LevelButton
    {
        const float TabHeight = 64f;

        readonly UiBuilder b;
        readonly Image glow;
        readonly Image rim;
        readonly Image body;
        readonly Text label;
        readonly Shadow labelShade;
        readonly RectTransform tab;
        readonly Image tabFill;
        readonly Text tabLabel;

        public RectTransform Rect { get; }
        public Button Button { get; }

        /// <param name="size">The pill's size; the tab sits above it, inside the returned rect's top margin.</param>
        public LevelButton(UiBuilder b, Transform parent, Anchor anchor, Vector2 position, Vector2 size, Action onClick)
        {
            this.b = b;
            Rect = b.Node("Level Button", parent, anchor, position, size + new Vector2(0f, TabHeight * 0.5f));
            var pill = b.Node("Pill", Rect, Anchor.Bottom, Vector2.zero, size);

            glow = b.Image("Glow", pill, UiSkin.Shadow, Color.clear, Anchor.Fill, new Vector2(0f, -16f), new Vector2(-30f, -26f), sliced: true);
            glow.pixelsPerUnitMultiplier = 0.7f;
            rim = b.Image("Rim", pill, UiSkin.Glass, Color.white, Anchor.Fill, Vector2.zero, Vector2.zero, sliced: true);
            rim.pixelsPerUnitMultiplier = 128f / size.y;
            rim.raycastTarget = true;

            float inset = Mathf.Round(size.y * 0.075f);
            var core = b.Node("Body", pill, Anchor.Fill, Vector2.zero, new Vector2(inset, inset));
            body = b.GlassSurface(core, Color.white, (size.y - inset * 2f) / 128f, glow: false);

            label = b.Text("Label", pill, "Level 1", b.Theme.DisplayFont, Mathf.RoundToInt(size.y * 0.44f), b.Theme.OnAccent,
                TextAnchor.MiddleCenter, Anchor.Fill, new Vector2(0f, 2f), new Vector2(24f, 0f));
            labelShade = UiBuilder.Lip(label, Color.black, 4f);

            tab = b.Node("Difficulty Tab", Rect, Anchor.Top, Vector2.zero, new Vector2(220f, TabHeight));
            var outline = b.Image("Outline", tab, UiSkin.Pill, Color.white, Anchor.Fill, Vector2.zero, new Vector2(-6f, -6f), sliced: true);
            outline.pixelsPerUnitMultiplier = 64f / (TabHeight + 12f);
            tabFill = b.Image("Fill", tab, UiSkin.Glass, Color.white, Anchor.Fill, Vector2.zero, Vector2.zero, sliced: true);
            tabFill.pixelsPerUnitMultiplier = 128f / TabHeight;
            tabLabel = b.Text("Label", tab, "", b.Theme.DisplayFont, 34, b.Theme.OnAccent, TextAnchor.MiddleCenter, Anchor.Fill, new Vector2(0f, 1f), new Vector2(20f, 0f));

            Button = Rect.gameObject.AddComponent<Button>();
            Button.targetGraphic = body;
            Button.onClick.AddListener(() =>
            {
                b.PlayClick();
                onClick?.Invoke();
            });
        }

        public void Bind(int levelNumber, Difficulty difficulty)
        {
            var tone = b.DifficultyColor(difficulty);
            var deep = UiBuilder.Darken(tone, 0.4f);
            body.color = tone;
            rim.color = Color.Lerp(tone, Color.white, 0.78f);
            glow.color = UiBuilder.WithAlpha(deep, 0.45f);
            label.text = $"Level {levelNumber}";
            labelShade.effectColor = UiBuilder.WithAlpha(deep, 0.85f);

            string name = UiBuilder.DifficultyName(difficulty);
            tab.gameObject.SetActive(name.Length > 0);
            tabLabel.text = name;
            tabFill.color = deep;
            tab.sizeDelta = new Vector2(Mathf.Max(170f, 46f + name.Length * 22f), TabHeight);
        }
    }
}
