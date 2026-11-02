using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Phones (tall screens) scale the 1080x1920 layout by width so nothing is cut at the sides; tablets and
    /// short screens scale by height so the vertical layout always fits.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class AdaptiveCanvasScaler : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        CanvasScaler scaler;
        Canvas canvas;

        void Awake() => Apply();

        void Update() => Apply();

        public void Apply()
        {
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            if (canvas == null) canvas = GetComponent<Canvas>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);

            var size = PixelSize();
            float aspect = size.y > 0f ? size.x / size.y : ReferenceWidth / ReferenceHeight;
            scaler.matchWidthOrHeight = aspect > ReferenceWidth / ReferenceHeight + 0.01f ? 1f : 0f;
        }

        Vector2 PixelSize()
        {
            var cam = canvas != null ? canvas.worldCamera : null;
            if (cam != null) return new Vector2(cam.pixelWidth, cam.pixelHeight);
            return new Vector2(Screen.width, Screen.height);
        }
    }
}
