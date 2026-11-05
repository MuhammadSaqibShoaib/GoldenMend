using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Everything the screens are drawn with: fonts, sprites, icons, sounds and the palette.
    /// The Sky Breeze look: Fredoka type, Kenney Game Icons, and the generated glass shapes in UiSkin.
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
        public Sprite IconMusic;
        public Sprite IconPull;

        [Header("Sounds")]
        public AudioClip Click;

        [Tooltip("A popup card slides in.")]
        public AudioClip Popup;

        [Tooltip("A win star lands; played up the scale for each star.")]
        public AudioClip StarSound;

        [Header("Palette")]
        /// <summary>Body text on light surfaces.</summary>
        public Color Text = Hex(0x2B4A8B);
        /// <summary>Big titles ("Golden Mend", "Museum", "Restored!") and the HUD level caption.</summary>
        public Color Title = Hex(0x2350B5);
        /// <summary>The hard drop shadow under titles: a white lip that lifts them off the sky.</summary>
        public Color TitleLip = Color.white;
        /// <summary>Labels and icons on coloured buttons.</summary>
        public Color OnAccent = Color.white;
        /// <summary>Popup and card panels: white, slightly see-through.</summary>
        public Color Paper = new Color(1f, 1f, 1f, 0.9f);
        /// <summary>Glass showcases: more see-through than Paper.</summary>
        public Color Glass = new Color(1f, 1f, 1f, 0.5f);
        /// <summary>Panel rims.</summary>
        public Color PanelEdge = Color.white;
        /// <summary>Coin and price capsules.</summary>
        public Color Chip = new Color(1f, 1f, 1f, 0.75f);
        /// <summary>The main action: sunny orange. Also the Level button for a normal level.</summary>
        public Color Primary = Hex(0xFF9A1F);
        /// <summary>Secondary buttons: white glass with blue text.</summary>
        public Color Secondary = new Color(1f, 1f, 1f, 0.85f);
        /// <summary>Go-ahead actions: resume, continue, buy.</summary>
        public Color Confirm = Hex(0x34C759);
        /// <summary>The pull booster and the hint badge.</summary>
        public Color Accent = Hex(0xFF5F8F);
        /// <summary>Close buttons and warnings.</summary>
        public Color Danger = Hex(0xEF4A50);
        /// <summary>Popup title ribbons.</summary>
        public Color Ribbon = Hex(0x3F8CFF);
        /// <summary>Level button and tags for hard levels: ruby.</summary>
        public Color Hard = Hex(0xE8304F);
        /// <summary>Level button and tags for very hard levels: deep violet.</summary>
        public Color VeryHard = Hex(0x6234DB);
        public Color Gold = Hex(0xFFC93C);
        /// <summary>Dark text on gold and the accent colours.</summary>
        public Color Ink = Hex(0x3A2400);
        /// <summary>Secondary text.</summary>
        public Color Muted = Hex(0x5A77AE);
        /// <summary>Unearned stars.</summary>
        public Color StarEmpty = Hex(0xB9CCEB);
        /// <summary>Background gradient, top and bottom.</summary>
        public Color SkyTop = Hex(0xC9F0FF);
        public Color SkyBottom = Hex(0x8EC5FF);
        /// <summary>Sunken wells inside panels, empty vessel cells and silhouettes.</summary>
        public Color Well = Hex(0xDCE7F8);
        public Color Scrim = new Color(0.11f, 0.23f, 0.47f, 0.42f);

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
