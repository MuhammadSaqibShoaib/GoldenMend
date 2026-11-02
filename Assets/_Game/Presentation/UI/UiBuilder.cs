using System;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>Where a widget hangs from its parent. Positions are offsets from that point, in canvas units.</summary>
    public enum Anchor
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
        StretchTop,
        StretchBottom,
        Fill,
    }

    public enum ButtonStyle
    {
        /// <summary>Gold with ink text: the one main action on a screen.</summary>
        Primary,

        /// <summary>Indigo with cream text: other actions.</summary>
        Secondary,

        /// <summary>No background, indigo text: low-emphasis actions such as Home.</summary>
        Ghost,
    }

    /// <summary>Shared type scale and spacing, in canvas units of the 1080x1920 reference layout.</summary>
    public static class UiType
    {
        public const int Display = 104;
        public const int H1 = 72;
        public const int H2 = 48;
        public const int Body = 38;
        public const int Caption = 30;
        public const int Button = 48;

        public const float Margin = 48f;
        public const float Gap = 24f;
        public const float IconButton = 116f;
    }

    /// <summary>Builds themed uGUI widgets from code with consistent anchors, type and colours.</summary>
    public sealed class UiBuilder
    {
        readonly Action click;

        public UiTheme Theme { get; }

        public UiBuilder(UiTheme theme, Action click)
        {
            Theme = theme;
            this.click = click;
        }

        public RectTransform Node(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Place(rect, anchor, position, size);
            return rect;
        }

        /// <summary>
        /// Anchors and positions a rect. For stretch anchors, <paramref name="size"/> is the fixed height (top/bottom)
        /// or the inset from each edge (fill: x = sides, y = top and bottom).
        /// </summary>
        public static void Place(RectTransform rect, Anchor anchor, Vector2 position, Vector2 size)
        {
            switch (anchor)
            {
                case Anchor.StretchTop:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = position;
                    rect.sizeDelta = new Vector2(0f, size.y);
                    return;
                case Anchor.StretchBottom:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = position;
                    rect.sizeDelta = new Vector2(0f, size.y);
                    return;
                case Anchor.Fill:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(size.x, size.y) + position;
                    rect.offsetMax = new Vector2(-size.x, -size.y) + position;
                    return;
            }

            var point = AnchorPoint(anchor);
            rect.anchorMin = point;
            rect.anchorMax = point;
            rect.pivot = point;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static Vector2 AnchorPoint(Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft: return new Vector2(0f, 1f);
                case Anchor.Top: return new Vector2(0.5f, 1f);
                case Anchor.TopRight: return new Vector2(1f, 1f);
                case Anchor.Left: return new Vector2(0f, 0.5f);
                case Anchor.Right: return new Vector2(1f, 0.5f);
                case Anchor.BottomLeft: return new Vector2(0f, 0f);
                case Anchor.Bottom: return new Vector2(0.5f, 0f);
                case Anchor.BottomRight: return new Vector2(1f, 0f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        public Image Image(string name, Transform parent, Sprite sprite, Color color, Anchor anchor, Vector2 position, Vector2 size, bool sliced = false)
        {
            var image = Node(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sliced) image.type = UnityEngine.UI.Image.Type.Sliced;
            else image.preserveAspect = sprite != null;
            return image;
        }

        public Text Text(string name, Transform parent, string text, Font font, int size, Color color, TextAnchor alignment,
            Anchor anchor, Vector2 position, Vector2 box)
        {
            var label = Node(name, parent, anchor, position, box).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>A full-width text button. Returns the button; its label is the first <see cref="Text"/> child.</summary>
        public Button Button(string name, Transform parent, string label, ButtonStyle style, Anchor anchor, Vector2 position, Vector2 size,
            Action onClick, Sprite icon = null)
        {
            var rect = Node(name, parent, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Theme.Button;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = style == ButtonStyle.Primary ? Theme.Gold : style == ButtonStyle.Secondary ? Theme.Indigo : Color.clear;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                click();
                onClick();
            });

            var ink = style == ButtonStyle.Primary ? Theme.Ink : style == ButtonStyle.Secondary ? Theme.Cream : Theme.Indigo;
            float lift = style == ButtonStyle.Ghost ? 0f : 5f;
            int fontSize = Mathf.RoundToInt(Mathf.Min(UiType.Button, size.y * 0.36f));
            // Ghost buttons are text links; an icon pinned to the edge would float away from the word.
            if (icon != null && style != ButtonStyle.Ghost)
            {
                float iconSize = size.y * 0.42f;
                Image("Icon", rect, icon, ink, Anchor.Left, new Vector2(size.y * 0.32f, lift), new Vector2(iconSize, iconSize));
                Text("Label", rect, label, Theme.TitleFont, fontSize, ink, TextAnchor.MiddleCenter, Anchor.Fill, new Vector2(size.y * 0.22f, lift), new Vector2(size.y * 0.5f, 0f));
            }
            else
            {
                Text("Label", rect, label, Theme.TitleFont, fontSize, ink, TextAnchor.MiddleCenter, Anchor.Fill, new Vector2(0f, lift), new Vector2(16f, 0f));
            }
            return button;
        }

        public Button IconButton(string name, Transform parent, Sprite icon, ButtonStyle style, Anchor anchor, Vector2 position, float size, Action onClick)
        {
            var rect = Node(name, parent, anchor, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Theme.IconButton;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = style == ButtonStyle.Primary ? Theme.Gold : style == ButtonStyle.Secondary ? Theme.Indigo : Color.clear;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                click();
                onClick();
            });

            var ink = style == ButtonStyle.Primary ? Theme.Ink : style == ButtonStyle.Secondary ? Theme.Cream : Theme.Indigo;
            Image("Icon", rect, icon, ink, Anchor.Center, new Vector2(0f, style == ButtonStyle.Ghost ? 0f : 5f), new Vector2(size * 0.5f, size * 0.5f));
            return button;
        }

        /// <summary>A gold coin: a darker rim, a bright face and a small shine.</summary>
        public RectTransform Coin(string name, Transform parent, Anchor anchor, Vector2 position, float size)
        {
            var rim = Image(name, parent, Theme.Circle, Color.Lerp(Theme.Gold, Theme.Ink, 0.35f), anchor, position, new Vector2(size, size));
            Image("Face", rim.transform, Theme.Circle, Theme.Gold, Anchor.Center, Vector2.zero, new Vector2(size * 0.78f, size * 0.78f));
            Image("Shine", rim.transform, Theme.Circle, WithAlpha(Color.white, 0.55f), Anchor.Center, new Vector2(-size * 0.14f, size * 0.14f), new Vector2(size * 0.2f, size * 0.2f));
            return rim.rectTransform;
        }

        /// <summary>A rounded pill with a coin on the left and a label, for prices and rewards.</summary>
        public Text CoinPill(string name, Transform parent, Color background, string label, Color labelColor, int fontSize, Anchor anchor, Vector2 position, Vector2 size)
        {
            var text = Pill(name, parent, background, null, Color.clear, label, labelColor, fontSize, anchor, position, size);
            var rect = (RectTransform)text.transform.parent;
            float coin = size.y * 0.62f, pad = size.y * 0.2f;
            Coin("Coin", rect, Anchor.Left, new Vector2(pad, 0f), coin);
            text.rectTransform.offsetMin = new Vector2(coin + pad * 1.5f, 0f);
            text.rectTransform.offsetMax = new Vector2(-pad, 0f);
            return text;
        }

        /// <summary>A rounded pill: background plus an optional icon on the left and a label.</summary>
        public Text Pill(string name, Transform parent, Color background, Sprite icon, Color iconColor, string label, Color labelColor, int fontSize,
            Anchor anchor, Vector2 position, Vector2 size)
        {
            var rect = Node(name, parent, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Theme.Badge;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = background;
            image.raycastTarget = false;

            float pad = size.y * 0.2f;
            if (icon != null)
            {
                float iconSize = size.y * 0.62f;
                Image("Icon", rect, icon, iconColor, Anchor.Left, new Vector2(pad, 0f), new Vector2(iconSize, iconSize));
                return Text("Label", rect, label, Theme.TitleFont, fontSize, labelColor, TextAnchor.MiddleCenter, Anchor.Fill,
                    new Vector2(iconSize * 0.5f + pad * 0.5f, 0f), new Vector2(iconSize * 0.5f + pad, 0f));
            }
            return Text("Label", rect, label, Theme.TitleFont, fontSize, labelColor, TextAnchor.MiddleCenter, Anchor.Fill, Vector2.zero, new Vector2(pad, 0f));
        }

        /// <summary>A paper card for popups and lists.</summary>
        public RectTransform Card(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 size)
        {
            var rect = Node(name, parent, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Theme.Panel;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Theme.Paper;
            return rect;
        }

        /// <summary>Three stars; the first <paramref name="earned"/> are gold.</summary>
        public void Stars(Transform parent, int earned, Anchor anchor, Vector2 position, float size, bool raised)
        {
            for (int i = 0; i < 3; i++)
            {
                bool middle = i == 1;
                float s = raised && middle ? size * 1.3f : size;
                float y = raised && middle ? size * 0.22f : 0f;
                var color = i < earned ? Theme.Gold : WithAlpha(Theme.Ink, 0.16f);
                Image($"Star {i + 1}", parent, Theme.Star, color, anchor, position + new Vector2((i - 1) * size * 1.08f, y), new Vector2(s, s));
            }
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
