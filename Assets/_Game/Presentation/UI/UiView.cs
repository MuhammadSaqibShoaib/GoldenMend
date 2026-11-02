using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Base for a full screen or a popup. A view owns one root under the screens or popups layer and fades
    /// in and out. Popups add a dimmed backdrop that also blocks taps on the board.
    /// </summary>
    public abstract class UiView
    {
        protected readonly UiRoot Ui;
        protected readonly UiBuilder B;
        protected readonly UiTheme T;
        protected readonly RectTransform Root;
        protected readonly UnityEngine.UI.Image Backdrop;
        readonly CanvasGroup group;

        protected UiView(UiRoot ui, string name, bool popup)
        {
            Ui = ui;
            B = ui.Build;
            T = ui.Theme;
            Root = B.Node(name, popup ? ui.Popups : ui.Screens, Anchor.Fill, Vector2.zero, Vector2.zero);
            group = Root.gameObject.AddComponent<CanvasGroup>();

            if (popup)
            {
                // The backdrop extends past the safe area so the whole screen dims.
                Backdrop = B.Image("Scrim", Root, null, T.Scrim, Anchor.Fill, Vector2.zero, new Vector2(-200f, -400f));
                Backdrop.raycastTarget = true;
            }

            Root.gameObject.SetActive(false);
            group.alpha = 0f;
        }

        public bool Visible { get; private set; }

        public void Show()
        {
            Visible = true;
            Root.SetAsLastSibling();
            OnShow();
            Ui.Fade(group, true, 0.2f);
        }

        public void Hide(bool instant = false)
        {
            if (!Visible && !Root.gameObject.activeSelf) return;
            Visible = false;
            Ui.Fade(group, false, instant ? 0f : 0.15f);
        }

        /// <summary>Called before the fade-in: refresh content and start entry animations here.</summary>
        protected virtual void OnShow()
        {
        }

        protected static void SetInteractable(Button button, bool interactable)
        {
            button.interactable = interactable;
            var group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = interactable ? 1f : 0.45f;
        }
    }
}
