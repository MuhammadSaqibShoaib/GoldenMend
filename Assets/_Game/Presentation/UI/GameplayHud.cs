using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// In-level HUD, kept to the top edge so the board stays clear: pause on the left, the level plate in the
    /// middle, hint on the right. A tip line under the plate teaches the first level.
    /// </summary>
    public sealed class GameplayHud : UiView
    {
        public Action Pause;
        public Action Hint;

        readonly Text caption;
        readonly Text title;
        readonly CanvasGroup tip;
        readonly Text tipText;

        public GameplayHud(UiRoot ui) : base(ui, "Gameplay HUD", popup: false)
        {
            B.IconButton("Pause", Root, T.IconPause, ButtonStyle.Secondary, Anchor.TopLeft, new Vector2(UiType.Margin, -32f), UiType.IconButton, () => Pause?.Invoke());
            B.IconButton("Hint", Root, T.IconHint, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-UiType.Margin, -32f), UiType.IconButton, () => Hint?.Invoke());

            var plate = B.Image("Level Plate", Root, T.Badge, T.Indigo, Anchor.Top, new Vector2(0f, -28f), new Vector2(520f, 124f), sliced: true);
            caption = B.Text("Caption", plate.transform, "", T.TitleFont, 28, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -12f), new Vector2(480f, 40f));
            title = B.Text("Title", plate.transform, "", T.TitleFont, 42, T.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -54f), new Vector2(480f, 56f));

            var tipRect = B.Node("Tip", Root, Anchor.Top, new Vector2(0f, -180f), new Vector2(900f, 110f));
            tip = tipRect.gameObject.AddComponent<CanvasGroup>();
            tip.blocksRaycasts = false;
            tipText = B.Text("Text", tipRect, "", T.BodyFont, 34, T.Cream, TextAnchor.MiddleCenter, Anchor.Fill, Vector2.zero, Vector2.zero);
            tip.alpha = 0f;
        }

        public void Bind(int levelIndex, LevelSpec spec)
        {
            string clay = ColorUtility.ToHtmlStringRGB(T.Clay);
            caption.text = spec.Hard ? $"LEVEL {levelIndex + 1}   <color=#{clay}>HARD</color>" : $"LEVEL {levelIndex + 1}";
            title.text = VesselArt.DisplayName(spec);
        }

        /// <summary>Shows a teaching line under the level plate until <see cref="HideTip"/>.</summary>
        public void ShowTip(string text)
        {
            tipText.text = text;
            Ui.Fade(tip, true, 0.3f);
            tip.blocksRaycasts = false;
        }

        public void HideTip()
        {
            if (tip.alpha > 0f || tip.gameObject.activeSelf) Ui.Fade(tip, false, 0.25f);
        }
    }
}
