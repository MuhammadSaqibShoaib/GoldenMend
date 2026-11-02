using System;
using System.Collections;
using GoldenMend.Core;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// In-game HUD and popups, built in uGUI from a <see cref="UiTheme"/>: level title, stats, restart, hint and
    /// level skip buttons, plus the level-complete and workbench-full panels.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        const float ReferenceWidth = 1080f;
        const float ReferenceHeight = 1920f;
        const float Margin = 56f;

        UiTheme theme;
        BoardPresenter board;
        Func<int> levelIndex;
        Action restart;
        Action next;
        Action previous;

        Canvas canvas;
        Text title;
        Text stats;
        GameObject badge;
        RectTransform statsRect;
        GameObject winPanel;
        GameObject failPanel;
        Text winDetail;
        AudioSource clickSource;

        BuiltLevel shownLevel;
        int shownIndex = -1;
        int shownTaps = -1;
        int shownPeak = -1;

        public void Init(UiTheme theme, BoardPresenter board, Func<int> levelIndex, Action restart, Action next, Action previous)
        {
            this.theme = theme;
            this.board = board;
            this.levelIndex = levelIndex;
            this.restart = restart;
            this.next = next;
            this.previous = previous;

            Build();
            board.StatusShown += ShowPanel;
        }

        public void HidePanel()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
        }

        public void ShowPanel(GameStatus status)
        {
            HidePanel();
            GameObject panel = status == GameStatus.Won ? winPanel : status == GameStatus.Failed ? failPanel : null;
            if (panel == null) return;

            if (status == GameStatus.Won && board.Level != null)
            {
                winDetail.text = $"Restored in {board.TapCount} taps.\nBusiest workbench: {board.BenchPeak} (best {board.Level.Solution.MinBenchPeak}).";
            }
            panel.SetActive(true);
            var card = panel.transform.GetChild(0);
            if (Application.isPlaying) StartCoroutine(Pop(card));
            else card.localScale = Vector3.one;
        }

        void LateUpdate() => Refresh();

        /// <summary>Updates the title and stats when they change. Called every frame, and directly in edit mode.</summary>
        public void Refresh()
        {
            if (board == null || board.Level == null || title == null) return;

            int index = levelIndex();
            if (board.Level != shownLevel || index != shownIndex)
            {
                shownLevel = board.Level;
                shownIndex = index;
                title.text = $"Level {index + 1}  ·  {shownLevel.Spec.Name}";
                badge.SetActive(shownLevel.Spec.Hard);
                statsRect.anchoredPosition = new Vector2(shownLevel.Spec.Hard ? Margin + 168f : Margin, statsRect.anchoredPosition.y);
                shownTaps = -1;
            }

            if (board.TapCount != shownTaps || board.BenchPeak != shownPeak)
            {
                shownTaps = board.TapCount;
                shownPeak = board.BenchPeak;
                stats.text = $"Taps {shownTaps}    Bench peak {shownPeak} / best {shownLevel.Solution.MinBenchPeak}";
            }
        }

        void Build()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("HUD Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            var cam = Camera.main;
            if (cam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            clickSource = gameObject.AddComponent<AudioSource>();
            clickSource.playOnAwake = false;

            BuildTopBar(canvasObject.transform);
            winPanel = BuildPanel(canvasObject.transform, "Level Complete", "Restored!", "Next vessel", theme.Gold, () => next(), out winDetail, withStars: true);
            failPanel = BuildPanel(canvasObject.transform, "Workbench Full", "Workbench full", "Try again", theme.Gold, () => restart(), out var failText, withStars: false);
            failText.text = "There's no room left for that shard.\nTry a different order.";
            HidePanel();
        }

        void BuildTopBar(Transform parent)
        {
            var bar = Rect("Top Bar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 340f));

            title = Label("Title", bar, theme.TitleFont, 56, theme.Cream, TextAnchor.MiddleLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(Margin, -48f), new Vector2(ReferenceWidth - Margin * 2f, 76f));

            badge = new GameObject("Hard Badge", typeof(RectTransform));
            badge.transform.SetParent(bar, false);
            var badgeImage = badge.AddComponent<Image>();
            Sliced(badgeImage, theme.Badge, theme.Clay);
            Place((RectTransform)badge.transform, new Vector2(0f, 1f), new Vector2(Margin, -136f), new Vector2(150f, 54f));
            var badgeText = Label("Label", badge.transform, theme.TitleFont, 28, theme.Cream, TextAnchor.MiddleCenter);
            Stretch(badgeText.rectTransform);
            badgeText.text = "HARD";

            stats = Label("Stats", bar, theme.BodyFont, 30, WithAlpha(theme.Cream, 0.75f), TextAnchor.MiddleLeft);
            statsRect = stats.rectTransform;
            Place(statsRect, new Vector2(0f, 1f), new Vector2(Margin, -136f), new Vector2(ReferenceWidth - Margin * 2f, 54f));

            const float size = 128f, gap = 20f, y = -214f;
            IconButton("Restart", bar, theme.IconRestart, new Vector2(0f, 1f), new Vector2(Margin, y), size, () => restart());
            IconButton("Hint", bar, theme.IconHint, new Vector2(0f, 1f), new Vector2(Margin + size + gap, y), size, () => board.ShowHint());
            IconButton("Previous", bar, theme.IconPrevious, new Vector2(1f, 1f), new Vector2(-Margin - size - gap, y), size, () => previous());
            IconButton("Next", bar, theme.IconNext, new Vector2(1f, 1f), new Vector2(-Margin, y), size, () => next());
        }

        GameObject BuildPanel(Transform parent, string name, string heading, string action, Color actionColor, Action onAction, out Text body, bool withStars)
        {
            var scrim = new GameObject(name, typeof(RectTransform));
            scrim.transform.SetParent(parent, false);
            Stretch((RectTransform)scrim.transform);
            var scrimImage = scrim.AddComponent<Image>();
            scrimImage.color = theme.Scrim;

            var card = Rect("Card", scrim.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(880f, 660f));
            Sliced(card.gameObject.AddComponent<Image>(), theme.Panel, theme.Paper);

            if (withStars)
            {
                for (int i = 0; i < 3; i++)
                {
                    var star = new GameObject($"Star {i + 1}", typeof(RectTransform));
                    star.transform.SetParent(card, false);
                    var starImage = star.AddComponent<Image>();
                    starImage.sprite = theme.Star;
                    starImage.color = theme.Gold;
                    starImage.preserveAspect = true;
                    starImage.raycastTarget = false;
                    float s = i == 1 ? 128f : 96f;
                    Place((RectTransform)star.transform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 132f, i == 1 ? 72f : 44f), new Vector2(s, s), new Vector2(0.5f, 0.5f));
                }
            }

            var headingText = Label("Heading", card, theme.DisplayFont, 84, theme.Indigo, TextAnchor.MiddleCenter);
            Place(headingText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(800f, 120f), new Vector2(0.5f, 1f));
            headingText.text = heading;

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(card, false);
            var dividerImage = divider.AddComponent<Image>();
            dividerImage.sprite = theme.Divider;
            dividerImage.color = theme.Gold;
            dividerImage.raycastTarget = false;
            Place((RectTransform)divider.transform, new Vector2(0.5f, 1f), new Vector2(0f, -206f), new Vector2(360f, 8f), new Vector2(0.5f, 1f));

            body = Label("Body", card, theme.BodyFont, 38, WithAlpha(theme.Ink, 0.85f), TextAnchor.UpperCenter);
            Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(760f, 170f), new Vector2(0.5f, 1f));

            var button = Rect("Action", card, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(620f, 152f));
            var buttonImage = button.gameObject.AddComponent<Image>();
            Sliced(buttonImage, theme.Button, actionColor);
            var uiButton = button.gameObject.AddComponent<Button>();
            uiButton.targetGraphic = buttonImage;
            uiButton.onClick.AddListener(() =>
            {
                Click();
                HidePanel();
                onAction();
            });
            var label = Label("Label", button, theme.TitleFont, 50, theme.Ink, TextAnchor.MiddleCenter);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(600f, 120f), new Vector2(0.5f, 0.5f));
            label.text = action;

            return scrim;
        }

        void IconButton(string name, RectTransform parent, Sprite icon, Vector2 anchor, Vector2 position, float size, Action onClick)
        {
            var rect = Rect(name, parent, anchor, anchor, anchor, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            Sliced(image, theme.IconButton, theme.Indigo);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                Click();
                onClick();
            });

            var iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(rect, false);
            var iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = theme.Cream;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            Place((RectTransform)iconObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(size * 0.5f, size * 0.5f), new Vector2(0.5f, 0.5f));
        }

        void Click()
        {
            if (theme.Click != null) clickSource.PlayOneShot(theme.Click, 0.6f);
        }

        static IEnumerator Pop(Transform card)
        {
            const float duration = 0.28f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                // Ease-out-back: overshoot slightly, then settle.
                float u = t - 1f;
                float e = 1f + 2.2f * u * u * u + 1.2f * u * u;
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, e);
                yield return null;
            }
            card.localScale = Vector3.one;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (Application.isPlaying) DontDestroyOnLoad(go);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Text Label(string name, Transform parent, Font font, int size, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static void Sliced(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
