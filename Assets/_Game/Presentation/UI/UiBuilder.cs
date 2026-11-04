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
        /// <summary>Candy pink: the one main action on a screen.</summary>
        Primary,

        /// <summary>Purple: other actions.</summary>
        Secondary,

        /// <summary>No background, muted text: low-emphasis actions such as Home.</summary>
        Ghost,

        /// <summary>Green: go ahead (resume, continue, buy).</summary>
        Confirm,

        /// <summary>Gold-yellow with dark text: boosters.</summary>
        Accent,

        /// <summary>Red: close.</summary>
        Danger,
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

        /// <summary>Adds a hard drop shadow under text, icons or stars: the chunky "lip" of the Berry Night look.</summary>
        public static Shadow Lip(Graphic graphic, Color color, float distance = 4f)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(0f, -distance);
            return shadow;
        }

        public static Color Darken(Color c, float amount) => new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);

        public Color StyleColor(ButtonStyle style)
        {
            switch (style)
            {
                case ButtonStyle.Primary: return Theme.Pink;
                case ButtonStyle.Secondary: return Theme.Indigo;
                case ButtonStyle.Confirm: return Theme.Green;
                case ButtonStyle.Accent: return Theme.Yellow;
                case ButtonStyle.Danger: return Theme.Clay;
                default: return Color.clear;
            }
        }

        Color InkFor(ButtonStyle style) => style == ButtonStyle.Accent ? Theme.Ink : style == ButtonStyle.Ghost ? Theme.Muted : Theme.Cream;

        /// <summary>
        /// A chunky button with an uppercase label. Returns the button; its label is the first <see cref="Text"/> child.
        /// With an icon, the icon sits at the left and the label centres in the rest.
        /// </summary>
        public Button Button(string name, Transform parent, string label, ButtonStyle style, Anchor anchor, Vector2 position, Vector2 size,
            Action onClick, Sprite icon = null)
        {
            var rect = Node(name, parent, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSkin.Chunky;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = StyleColor(style);
            if (style == ButtonStyle.Ghost) image.sprite = null;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                click();
                onClick();
            });

            var ink = InkFor(style);
            float lift = style == ButtonStyle.Ghost ? 0f : UiSkin.LipHeight / 2f;
            int fontSize = Mathf.RoundToInt(Mathf.Min(UiType.Button, size.y * 0.4f));
            Text text;
            if (icon != null && style != ButtonStyle.Ghost)
            {
                float iconSize = size.y * 0.42f;
                var glyph = Image("Icon", rect, icon, ink, Anchor.Left, new Vector2(size.y * 0.3f, lift), new Vector2(iconSize, iconSize));
                if (style != ButtonStyle.Accent) Lip(glyph, Darken(image.color, 0.45f), 3f);
                text = Text("Label", rect, label.ToUpperInvariant(), Theme.DisplayFont, fontSize, ink, TextAnchor.MiddleCenter, Anchor.Fill,
                    new Vector2(size.y * 0.24f, lift), new Vector2(size.y * 0.5f, 0f));
            }
            else
            {
                var font = style == ButtonStyle.Ghost ? Theme.TitleFont : Theme.DisplayFont;
                text = Text("Label", rect, style == ButtonStyle.Ghost ? label : label.ToUpperInvariant(), font, fontSize, ink, TextAnchor.MiddleCenter, Anchor.Fill,
                    new Vector2(0f, lift), new Vector2(16f, 0f));
            }
            if (style != ButtonStyle.Ghost && style != ButtonStyle.Accent) Lip(text, Darken(image.color, 0.45f), 4f);
            return button;
        }

        public Button IconButton(string name, Transform parent, Sprite icon, ButtonStyle style, Anchor anchor, Vector2 position, float size, Action onClick)
        {
            var rect = Node(name, parent, anchor, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = style == ButtonStyle.Ghost ? null : UiSkin.Chunky;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.4f;
            image.color = StyleColor(style);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                click();
                onClick();
            });

            var glyph = Image("Icon", rect, icon, InkFor(style), Anchor.Center, new Vector2(0f, style == ButtonStyle.Ghost ? 0f : UiSkin.LipHeight / 2.8f), new Vector2(size * 0.52f, size * 0.52f));
            if (style != ButtonStyle.Ghost && style != ButtonStyle.Accent) Lip(glyph, Darken(image.color, 0.45f), 3f);
            return button;
        }

        /// <summary>Makes a whole panel a button (with the click sound), for cards that open on tap.</summary>
        public Button Tappable(RectTransform rect, Action onClick)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponentInChildren<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                click();
                onClick();
            });
            return button;
        }

        /// <summary>A gold coin: a darker rim, a bright face and a small shine.</summary>
        public RectTransform Coin(string name, Transform parent, Anchor anchor, Vector2 position, float size)
        {
            var rim = Image(name, parent, Theme.Circle, Color.Lerp(Theme.Gold, Theme.Ink, 0.35f), anchor, position, new Vector2(size, size));
            Image("Face", rim.transform, Theme.Circle, Theme.Gold, Anchor.Center, Vector2.zero, new Vector2(size * 0.8f, size * 0.8f));
            Image("Ring", rim.transform, Theme.Circle, WithAlpha(Color.white, 0.5f), Anchor.Center, Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
            Image("Core", rim.transform, Theme.Circle, Theme.Gold, Anchor.Center, Vector2.zero, new Vector2(size * 0.38f, size * 0.38f));
            return rim.rectTransform;
        }

        /// <summary>A capsule with a coin on the left and a label, for prices and rewards.</summary>
        public Text CoinPill(string name, Transform parent, Color background, string label, Color labelColor, int fontSize, Anchor anchor, Vector2 position, Vector2 size)
        {
            var text = Pill(name, parent, background, null, Color.clear, label, labelColor, fontSize, anchor, position, size);
            var rect = (RectTransform)text.transform.parent;
            float coin = size.y * 0.72f, pad = size.y * 0.14f;
            Coin("Coin", rect, Anchor.Left, new Vector2(pad, 0f), coin);
            text.rectTransform.offsetMin = new Vector2(coin + pad * 2f, 0f);
            text.rectTransform.offsetMax = new Vector2(-pad * 1.5f, 0f);
            return text;
        }

        /// <summary>A capsule: background plus an optional icon on the left and a label.</summary>
        public Text Pill(string name, Transform parent, Color background, Sprite icon, Color iconColor, string label, Color labelColor, int fontSize,
            Anchor anchor, Vector2 position, Vector2 size)
        {
            var rect = Node(name, parent, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSkin.Pill;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 64f / size.y;
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

        /// <summary>
        /// A panel: a soft shadow, a purple gradient fill and a lighter edge. A glass card is see-through instead,
        /// for showcases on the night background. Content added to the returned rect draws on top.
        /// </summary>
        public RectTransform Card(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 size, bool glass = false, float radius = 1f)
        {
            var rect = Node(name, parent, anchor, position, size);
            if (!glass)
            {
                var shadow = Image("Shadow", rect, UiSkin.Shadow, WithAlpha(Color.black, 0.55f), Anchor.Fill, new Vector2(0f, -14f), new Vector2(-30f, -30f), sliced: true);
                shadow.pixelsPerUnitMultiplier = 0.8f;
            }
            var fill = Image("Fill", rect, glass ? UiSkin.Round : UiSkin.Panel, glass ? WithAlpha(Theme.PanelEdge, 0.14f) : Theme.Paper, Anchor.Fill, Vector2.zero, Vector2.zero, sliced: true);
            fill.pixelsPerUnitMultiplier = 0.6f / radius;
            fill.raycastTarget = true;
            var ring = Image("Edge", rect, UiSkin.Ring, glass ? WithAlpha(Theme.PanelEdge, 0.45f) : Theme.PanelEdge, Anchor.Fill, Vector2.zero, Vector2.zero, sliced: true);
            ring.pixelsPerUnitMultiplier = 0.6f / radius;
            return rect;
        }

        /// <summary>A sunken well inside a panel.</summary>
        public Image Inset(string name, Transform parent, Anchor anchor, Vector2 position, Vector2 size, float alpha = 0.55f)
        {
            var image = Image(name, parent, UiSkin.Inset, WithAlpha(Theme.Well, alpha), anchor, position, size, sliced: true);
            image.pixelsPerUnitMultiplier = 0.75f;
            return image;
        }

        /// <summary>
        /// A popup panel with a pink title ribbon across its top edge and, when <paramref name="close"/> is given, a red
        /// close button on the corner. Content goes into the returned panel, below the ribbon (about 150 units down).
        /// </summary>
        public RectTransform Popup(Transform parent, Vector2 size, Vector2 position, Action close, out Text title)
        {
            var card = Card("Card", parent, Anchor.Center, position, size);
            Inset("Inner", card, Anchor.Fill, new Vector2(0f, -60f), new Vector2(28f, 88f), 0.35f);

            // The ribbon stops short of the top-right corner so the close button never sits on it.
            var ribbon = Image("Ribbon", card, UiSkin.Chunky, Theme.Pink, Anchor.Top, new Vector2(0f, 64f), new Vector2(size.x - 290f, 150f), sliced: true);
            ribbon.pixelsPerUnitMultiplier = 0.9f;
            title = Text("Title", ribbon.transform, "", Theme.DisplayFont, 76, Theme.Cream, TextAnchor.MiddleCenter, Anchor.Fill, new Vector2(0f, UiSkin.LipHeight / 2f), new Vector2(28f, 0f));
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 40;
            title.resizeTextMaxSize = 76;
            Lip(title, Darken(Theme.Pink, 0.45f), 5f);

            if (close != null) IconButton("Close", card, Theme.IconClose, ButtonStyle.Danger, Anchor.TopRight, new Vector2(30f, 78f), 112f, close);
            return card;
        }

        /// <summary>Three stars; the first <paramref name="earned"/> are gold, the rest sunk into the panel.</summary>
        public void Stars(Transform parent, int earned, Anchor anchor, Vector2 position, float size, bool raised)
        {
            for (int i = 0; i < 3; i++)
            {
                bool middle = i == 1;
                float s = raised && middle ? size * 1.3f : size;
                float y = raised && middle ? size * 0.22f : 0f;
                bool on = i < earned;
                var star = Image($"Star {i + 1}", parent, Theme.Star, on ? Theme.Gold : new Color(0.16f, 0.09f, 0.38f, 1f), anchor,
                    position + new Vector2((i - 1) * size * 1.08f, y), new Vector2(s, s));
                if (on) Lip(star, Darken(Theme.Gold, 0.4f), Mathf.Max(3f, size * 0.07f));
            }
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
