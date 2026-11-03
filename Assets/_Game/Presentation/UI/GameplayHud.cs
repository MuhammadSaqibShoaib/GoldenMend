using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// In-level HUD, kept to the top edge so the board stays clear: pause on the left, the level plate in the
    /// middle, and the two helpers on the right: pull (lift a buried piece, for coins) and hint (from the player's
    /// stock). A tip line under the plate teaches the first level.
    /// </summary>
    public sealed class GameplayHud : UiView
    {
        public Action Pause;
        public Action Hint;
        public Action Pull;

        readonly Text caption;
        readonly Text title;
        readonly CanvasGroup tip;
        readonly Text tipText;
        readonly Button hintButton;
        readonly Text hintCount;
        readonly Button pullButton;
        readonly Text pullPrice;

        public GameplayHud(UiRoot ui) : base(ui, "Gameplay HUD", popup: false)
        {
            float top = -32f, size = UiType.IconButton, gap = 20f;
            B.IconButton("Pause", Root, T.IconPause, ButtonStyle.Secondary, Anchor.TopLeft, new Vector2(UiType.Margin, top), size, () => Pause?.Invoke());

            hintButton = B.IconButton("Hint", Root, T.IconHint, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-UiType.Margin, top), size, () => Hint?.Invoke());
            var badge = B.Image("Count", hintButton.transform, T.Circle, T.Gold, Anchor.TopRight, new Vector2(16f, 16f), new Vector2(54f, 54f));
            badge.raycastTarget = false;
            hintCount = B.Text("Label", badge.transform, "", T.TitleFont, 32, T.Ink, TextAnchor.MiddleCenter, Anchor.Fill, Vector2.zero, Vector2.zero);

            // The pull icon is the play arrow turned to point up: the piece comes up out of the pile.
            pullButton = B.IconButton("Pull", Root, T.IconPlay, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-UiType.Margin - size - gap, top), size, () => Pull?.Invoke());
            pullButton.transform.Find("Icon").localRotation = Quaternion.Euler(0f, 0f, 90f);
            pullPrice = B.CoinPill("Price", pullButton.transform, T.Gold, "", T.Ink, 26, Anchor.Bottom, new Vector2(0f, -24f), new Vector2(104f, 44f));

            var plate = B.Image("Level Plate", Root, T.Badge, T.Indigo, Anchor.Top, new Vector2(-40f, -28f), new Vector2(440f, 124f), sliced: true);
            caption = B.Text("Caption", plate.transform, "", T.TitleFont, 28, T.Gold, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -12f), new Vector2(410f, 40f));
            title = B.Text("Title", plate.transform, "", T.TitleFont, 42, T.Cream, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -54f), new Vector2(410f, 56f));

            var tipRect = B.Node("Tip", Root, Anchor.Top, new Vector2(0f, -200f), new Vector2(900f, 110f));
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

        /// <summary>Hints in stock; at zero the badge shows "+" because a tap offers more.</summary>
        public void SetHints(int count) => hintCount.text = count > 0 ? count.ToString() : "+";

        public void SetPullPrice(int coins) => pullPrice.text = coins.ToString();

        /// <summary>Dims the pull button while nothing in the pile fits.</summary>
        public void SetPullAvailable(bool available) => SetInteractable(pullButton, available);

        /// <summary>A gentle bounce on the hint button after the player has been stuck for a while.</summary>
        public void NudgeHint() => Ui.Pop(hintButton.transform);

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
