using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// The night sky behind everything: a purple top-to-bottom gradient with a few soft light circles, drawn in the
    /// world behind the board so every screen (board or UI) sits on it. It follows the camera's size, which the board
    /// widens on tall phones, so the gradient always spans the visible screen.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        const int Order = -500;
        const float DesignHeight = 20.8f;

        Camera cam;
        SpriteRenderer sky;
        Transform lights;
        float fittedHeight = -1f;
        float fittedAspect = -1f;

        public static Backdrop Create(UiTheme theme, Camera camera, Transform parent)
        {
            var backdrop = new GameObject("Backdrop").AddComponent<Backdrop>();
            backdrop.transform.SetParent(parent, false);
            backdrop.cam = camera;

            backdrop.sky = new GameObject("Sky").AddComponent<SpriteRenderer>();
            backdrop.sky.transform.SetParent(backdrop.transform, false);
            backdrop.sky.sprite = UiSkin.VerticalGradient(theme.SkyTop, theme.SkyBottom);
            backdrop.sky.sortingOrder = Order;

            backdrop.lights = new GameObject("Lights").transform;
            backdrop.lights.SetParent(backdrop.transform, false);
            // Soft clouds drifting across the sky.
            Circle(backdrop.lights, new Vector2(-5.6f, 7.4f), 8.5f, new Color(1f, 1f, 1f, 0.45f));
            Circle(backdrop.lights, new Vector2(6.6f, 3.6f), 7.6f, new Color(1f, 1f, 1f, 0.35f));
            Circle(backdrop.lights, new Vector2(-6.2f, -3.4f), 9.2f, new Color(1f, 1f, 1f, 0.3f));
            Circle(backdrop.lights, new Vector2(6.0f, -8.2f), 9.8f, new Color(1f, 1f, 1f, 0.35f));

            backdrop.Fit();
            return backdrop;
        }

        void LateUpdate() => Fit();

        void Fit()
        {
            float height = cam != null ? cam.orthographicSize * 2f : DesignHeight;
            float aspect = cam != null ? cam.aspect : 9f / 16f;
            if (Mathf.Approximately(height, fittedHeight) && Mathf.Approximately(aspect, fittedAspect)) return;
            fittedHeight = height;
            fittedAspect = aspect;

            if (cam != null) transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            sky.transform.localScale = new Vector3(height * aspect * 1.1f, height * 1.04f / 256f, 1f);
            // The light circles are laid out for the design height; scale them with the screen.
            float scale = height / DesignHeight;
            lights.localScale = new Vector3(scale, scale, 1f);
        }

        static void Circle(Transform parent, Vector2 at, float size, Color color)
        {
            var sr = new GameObject("Light").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.transform.localPosition = at;
            sr.transform.localScale = new Vector3(size * 1.7f, size, 1f);
            sr.sprite = SpriteFactory.Circle;
            sr.color = color;
            sr.sortingOrder = Order + 1;
        }
    }
}
