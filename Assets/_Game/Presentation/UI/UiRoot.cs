using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// The one UI canvas: camera-space so it composites over the board, scaled for every screen shape, with a
    /// safe-area root and three layers (screens, popups, overlay). Also runs the shared UI animations.
    /// </summary>
    public sealed class UiRoot : MonoBehaviour
    {
        AudioSource clickSource;

        public UiBuilder Build { get; private set; }
        public UiTheme Theme { get; private set; }
        public Canvas Canvas { get; private set; }
        public RectTransform Screens { get; private set; }
        public RectTransform Popups { get; private set; }
        public RectTransform Overlay { get; private set; }

        /// <summary>UI clicks are silent when false.</summary>
        public bool SoundOn { get; set; } = true;

        public static UiRoot Create(UiTheme theme, Camera camera, Transform parent)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            var root = go.AddComponent<UiRoot>();
            root.Setup(theme, camera);
            return root;
        }

        void Setup(UiTheme theme, Camera camera)
        {
            Theme = theme;
            EnsureEventSystem();

            Canvas = gameObject.AddComponent<Canvas>();
            if (camera != null)
            {
                Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                Canvas.worldCamera = camera;
                Canvas.planeDistance = 5f;
            }
            else
            {
                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            Canvas.sortingOrder = 100;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<AdaptiveCanvasScaler>().Apply();
            gameObject.AddComponent<GraphicRaycaster>();

            clickSource = gameObject.AddComponent<AudioSource>();
            clickSource.playOnAwake = false;
            Build = new UiBuilder(theme, Click);

            var safe = Build.Node("Safe Area", transform, Anchor.Fill, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            Screens = Build.Node("Screens", safe, Anchor.Fill, Vector2.zero, Vector2.zero);
            Popups = Build.Node("Popups", safe, Anchor.Fill, Vector2.zero, Vector2.zero);
            Overlay = Build.Node("Overlay", safe, Anchor.Fill, Vector2.zero, Vector2.zero);
        }

        public void Click()
        {
            if (SoundOn && Theme.Click != null) clickSource.PlayOneShot(Theme.Click, 0.6f);
        }

        /// <summary>Fades a group in or out. Instant in edit mode (screenshots) or when <paramref name="duration"/> is 0.</summary>
        public void Fade(CanvasGroup group, bool visible, float duration, Action done = null)
        {
            group.gameObject.SetActive(true);
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (!Application.isPlaying || duration <= 0f)
            {
                group.alpha = visible ? 1f : 0f;
                if (!visible) group.gameObject.SetActive(false);
                done?.Invoke();
                return;
            }
            StartCoroutine(FadeRoutine(group, visible, duration, done));
        }

        IEnumerator FadeRoutine(CanvasGroup group, bool visible, float duration, Action done)
        {
            float from = group.alpha, to = visible ? 1f : 0f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            group.alpha = to;
            if (!visible) group.gameObject.SetActive(false);
            done?.Invoke();
        }

        /// <summary>Scales in with a slight overshoot.</summary>
        public void Pop(Transform target, float delay = 0f)
        {
            if (!Application.isPlaying)
            {
                target.localScale = Vector3.one;
                return;
            }
            StartCoroutine(PopRoutine(target, delay));
        }

        IEnumerator PopRoutine(Transform target, float delay)
        {
            target.localScale = Vector3.one * 0.85f;
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            const float duration = 0.3f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                target.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, OutBack(t));
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        /// <summary>Slides a rect in from <paramref name="offset"/> to its resting position.</summary>
        public void SlideIn(RectTransform target, Vector2 rest, Vector2 offset)
        {
            if (!Application.isPlaying)
            {
                target.anchoredPosition = rest;
                return;
            }
            StartCoroutine(SlideRoutine(target, rest + offset, rest));
        }

        IEnumerator SlideRoutine(RectTransform target, Vector2 from, Vector2 to)
        {
            const float duration = 0.38f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, OutBack(t, 0.9f));
                yield return null;
            }
            target.anchoredPosition = to;
        }

        public Coroutine Run(IEnumerator routine) => Application.isPlaying ? StartCoroutine(routine) : null;

        static float OutBack(float t, float strength = 1.7f)
        {
            t = Mathf.Clamp01(t);
            float u = t - 1f;
            return 1f + (strength + 1f) * u * u * u + strength * u * u;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (Application.isPlaying) DontDestroyOnLoad(go);
        }
    }
}
