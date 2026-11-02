using UnityEngine;

namespace GoldenMend.Presentation.UI
{
    /// <summary>Keeps a RectTransform inside the device safe area (notches, rounded corners, home bar).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied || screen.x != Screen.width || screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var rect = (RectTransform)transform;
            applied = Screen.safeArea;
            screen = new Vector2Int(Screen.width, Screen.height);

            if (Application.isBatchMode || Screen.width <= 0 || Screen.height <= 0)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
            }
            else
            {
                rect.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
                rect.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            }
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
