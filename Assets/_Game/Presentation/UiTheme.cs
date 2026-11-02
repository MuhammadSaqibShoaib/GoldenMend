using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Everything the HUD and popups are drawn with: fonts, sprites, sounds and the palette.
    /// Built from the Kenney UI Pack and Zen Maru Gothic by Golden Mend > Rebuild Game Scene.
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

        [Header("Icons")]
        public Sprite IconRestart;
        public Sprite IconHint;
        public Sprite IconPrevious;
        public Sprite IconNext;
        public Sprite Star;

        [Header("Sounds")]
        public AudioClip Click;

        [Header("Palette")]
        public Color Cream = new Color(0.96f, 0.92f, 0.84f, 1f);
        public Color Paper = new Color(1f, 0.98f, 0.93f, 1f);
        public Color Indigo = new Color(0.22f, 0.27f, 0.45f, 1f);
        public Color Gold = new Color(0.97f, 0.77f, 0.33f, 1f);
        public Color Ink = new Color(0.16f, 0.13f, 0.12f, 1f);
        public Color Clay = new Color(0.80f, 0.38f, 0.30f, 1f);
        public Color Scrim = new Color(0.05f, 0.04f, 0.04f, 0.62f);
    }
}
