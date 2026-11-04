using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Everything the screens are drawn with: fonts, sprites, icons, sounds and the palette.
    /// The Berry Night look: Titan One and Baloo 2 type, Kenney Game Icons, and the generated shapes in UiSkin.
    /// Built by Golden Mend > Rebuild UI Theme.
    /// </summary>
    [CreateAssetMenu(menuName = "Golden Mend/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Fonts")]
        public Font BodyFont;
        public Font TitleFont;
        public Font DisplayFont;

        [Header("Sprites (9-sliced)")]
        public Sprite Panel;
        public Sprite Button;
        public Sprite IconButton;
        public Sprite Badge;
        public Sprite Divider;
        public Sprite Circle;

        [Header("Icons")]
        public Sprite IconRestart;
        public Sprite IconHint;
        public Sprite IconPrevious;
        public Sprite IconNext;
        public Sprite Star;
        public Sprite IconPause;
        public Sprite IconSettings;
        public Sprite IconHome;
        public Sprite IconMuseum;
        public Sprite IconLock;
        public Sprite IconClose;
        public Sprite IconWarning;
        public Sprite IconSoundOn;
        public Sprite IconSoundOff;
        public Sprite IconPlay;
        public Sprite IconTrophy;
        public Sprite IconHaptics;
        public Sprite IconPull;

        [Header("Sounds")]
        public AudioClip Click;

        [Header("Palette")]
        /// <summary>Light text on the night background.</summary>
        public Color Cream = Color.white;
        /// <summary>Popup and card panels.</summary>
        public Color Paper = Hex(0x5B33B8);
        /// <summary>Panel edges.</summary>
        public Color PanelEdge = Hex(0x9E7BFF);
        /// <summary>Secondary buttons.</summary>
        public Color Indigo = Hex(0x7357F5);
        public Color Gold = Hex(0xFFD54A);
        /// <summary>Dark text on gold and yellow.</summary>
        public Color Ink = Hex(0x3A1A00);
        /// <summary>Warnings and the close button.</summary>
        public Color Clay = Hex(0xF25C5C);
        /// <summary>The main action on every screen.</summary>
        public Color Pink = Hex(0xFF5FA2);
        /// <summary>Go-ahead actions: resume, continue, buy.</summary>
        public Color Green = Hex(0x4FD06A);
        /// <summary>The pull booster and other gold-yellow buttons.</summary>
        public Color Yellow = Hex(0xFFD34D);
        /// <summary>Secondary text on dark panels.</summary>
        public Color Muted = Hex(0xC9B6F2);
        /// <summary>Background gradient, top and bottom.</summary>
        public Color Night = Hex(0x4A2391);
        public Color NightDeep = Hex(0x1A0B3B);
        /// <summary>Sunken wells inside panels.</summary>
        public Color Well = Hex(0x120830);
        public Color Scrim = new Color(0.043f, 0.016f, 0.125f, 0.72f);

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
