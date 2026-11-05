using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// In-level HUD, kept to the top edge so the board stays clear: pause on the left, the level plate in the
    /// middle, and the two helpers on the right: pull (lift a buried piece, for coins, price underneath) and hint
    /// (from the player's stock, count on a gold badge). A tip line under the plate teaches the first level.
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
            float top = -44f, size = 124f;
            B.IconButton("Pause", Root, T.IconPause, ButtonStyle.Secondary, Anchor.TopLeft, new Vector2(36f, top), size, () => Pause?.Invoke());

            hintButton = B.IconButton("Hint", Root, T.IconHint, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-36f, top), size, () => Hint?.Invoke());
            var badge = B.Image("Count", hintButton.transform, T.Circle, T.Accent, Anchor.TopRight, new Vector2(22f, 22f), new Vector2(64f, 64f));
            badge.raycastTarget = false;
            var ring = B.Image("Ring", badge.transform, T.Circle, Color.white, Anchor.Center, Vector2.zero, new Vector2(74f, 74f));
            ring.transform.SetAsFirstSibling();
            hintCount = B.Text("Label", badge.transform, "", T.TitleFont, 38, T.OnAccent, TextAnchor.MiddleCenter, Anchor.Fill, new Vector2(0f, 2f), Vector2.zero);

            // The pull button lifts a piece up out of the pile; its price sits under it.
            pullButton = B.IconButton("Pull", Root, T.IconPull, ButtonStyle.Accent, Anchor.TopRight, new Vector2(-36f - size - 32f, top), size, () => Pull?.Invoke());
            pullPrice = B.CoinPill("Price", pullButton.transform, T.Chip, "", T.Text, 30, Anchor.Bottom, new Vector2(0f, -64f), new Vector2(150f, 56f));

            var plate = B.Card("Level Plate", Root, Anchor.Top, new Vector2(-60f, -44f), new Vector2(380f, 124f), glass: true, radius: 0.7f);
            caption = B.Text("Caption", plate, "", T.DisplayFont, 50, T.Title, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -8f), new Vector2(370f, 66f));
            UiBuilder.Lip(caption, T.TitleLip, 3f);
            title = B.Text("Title", plate, "", T.BodyFont, 32, T.Text, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -68f), new Vector2(370f, 44f));

            var tipRect = B.Node("Tip", Root, Anchor.Top, new Vector2(0f, -250f), new Vector2(940f, 120f));
            tip = tipRect.gameObject.AddComponent<CanvasGroup>();
            tip.blocksRaycasts = false;
            tipText = B.Text("Text", tipRect, "", T.TitleFont, 40, T.Title, TextAnchor.MiddleCenter, Anchor.Fill, Vector2.zero, Vector2.zero);
            UiBuilder.Lip(tipText, T.TitleLip, 3f);
            tip.alpha = 0f;
        }

        public void Bind(int levelIndex, LevelSpec spec)
        {
            // The difficulty rides on the name line, in its tone: the big caption has no room once levels reach two digits.
            caption.text = $"LEVEL {levelIndex + 1}";
            string tier = UiBuilder.DifficultyName(spec.Difficulty).ToUpperInvariant();
            string tone = ColorUtility.ToHtmlStringRGB(B.DifficultyColor(spec.Difficulty));
            title.text = tier.Length > 0 ? $"<color=#{tone}>{tier}</color> · {VesselArt.DisplayName(spec)}" : VesselArt.DisplayName(spec);
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
