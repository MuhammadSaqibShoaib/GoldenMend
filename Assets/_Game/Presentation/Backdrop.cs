using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// The sky behind everything, drawn in the world behind the board so every screen (board or UI) sits on it: a
    /// smooth top-to-bottom gradient, big soft cloud blobs that drift and breathe, and a few large glassy bubbles that
    /// rise slowly, sway and fade. Only three, so the sky stays calm rather than busy. It follows the camera, which the board widens on tall phones, so it always fills the screen.
    /// Everything is laid out in screen fractions and moves on unscaled time; in edit mode (screenshots) it holds a
    /// still frame.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        const int Order = -500;
        const float DesignHeight = 20.8f;
        const int BubbleCount = 3;

        static Sprite blobSprite;
        static Sprite bubbleSprite;

        Camera cam;
        SpriteRenderer sky;
        readonly Blob[] blobs = new Blob[5];
        readonly Bubble[] bubbles = new Bubble[BubbleCount];

        /// <summary>A big soft cloud: a home position (screen fractions), a slow drift and a breathing scale.</summary>
        struct Blob
        {
            public SpriteRenderer Renderer;
            public Vector2 Home;
            public float Size;
            public float Alpha;
            public float Speed;
            public float Phase;
        }

        /// <summary>A rising bubble: x and height as screen fractions, rise speed, sway and size.</summary>
        struct Bubble
        {
            public SpriteRenderer Renderer;
            public float X;
            public float Y;
            public float Rise;
            public float Sway;
            public float SwaySpeed;
            public float Size;
            public float Phase;
        }

        public static Backdrop Create(UiTheme theme, Camera camera, Transform parent)
        {
            var backdrop = new GameObject("Backdrop").AddComponent<Backdrop>();
            backdrop.transform.SetParent(parent, false);
            backdrop.cam = camera;

            backdrop.sky = new GameObject("Sky").AddComponent<SpriteRenderer>();
            backdrop.sky.transform.SetParent(backdrop.transform, false);
            backdrop.sky.sprite = SmoothGradient(theme.SkyTop, theme.SkyBottom);
            backdrop.sky.sortingOrder = Order;

            var clouds = new[]
            {
                new Vector4(-0.32f, 0.36f, 0.62f, 0.5f), new Vector4(0.36f, 0.18f, 0.55f, 0.38f), new Vector4(-0.3f, -0.12f, 0.6f, 0.32f),
                new Vector4(0.34f, -0.36f, 0.66f, 0.38f), new Vector4(-0.05f, 0.52f, 0.5f, 0.3f),
            };
            for (int i = 0; i < backdrop.blobs.Length; i++)
            {
                backdrop.blobs[i] = new Blob
                {
                    Renderer = MakeRenderer(backdrop.transform, "Cloud", BlobSprite, Order + 1),
                    Home = new Vector2(clouds[i].x, clouds[i].y),
                    Size = clouds[i].z,
                    Alpha = clouds[i].w,
                    Speed = 0.05f + 0.025f * i,
                    Phase = i * 1.7f,
                };
            }

            var rng = new System.Random(7);
            for (int i = 0; i < BubbleCount; i++)
            {
                // Spread evenly up the screen at the start so they never arrive in a clump.
                var bubble = NewBubble(rng, MakeRenderer(backdrop.transform, "Bubble", BubbleSprite, Order + 2), -0.45f + i * 0.42f);
                bubble.X = (i % 2 == 0 ? -1f : 1f) * (0.18f + 0.1f * i);
                backdrop.bubbles[i] = bubble;
            }

            backdrop.Layout(0f);
            return backdrop;
        }

        static Bubble NewBubble(System.Random rng, SpriteRenderer renderer, float y)
        {
            float r() => (float)rng.NextDouble();
            return new Bubble
            {
                Renderer = renderer,
                X = r() * 0.8f - 0.4f,
                Y = y,
                Rise = 0.012f + r() * 0.012f,
                Sway = 0.015f + r() * 0.02f,
                SwaySpeed = 0.25f + r() * 0.35f,
                Size = 0.08f + r() * 0.1f,
                Phase = r() * 6.28f,
            };
        }

        readonly System.Random respawn = new System.Random(11);

        void LateUpdate()
        {
            float dt = Application.isPlaying ? Mathf.Min(Time.unscaledDeltaTime, 0.1f) : 0f;
            for (int i = 0; i < bubbles.Length; i++)
            {
                bubbles[i].Y += bubbles[i].Rise * dt;
                if (bubbles[i].Y > 0.7f) bubbles[i] = NewBubble(respawn, bubbles[i].Renderer, -0.7f);
            }
            Layout(Application.isPlaying ? Time.unscaledTime : 0f);
        }

        void Layout(float time)
        {
            float height = cam != null ? cam.orthographicSize * 2f : DesignHeight;
            float aspect = cam != null ? cam.aspect : 9f / 16f;
            float width = height * aspect;
            if (cam != null) transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);

            sky.transform.localScale = new Vector3(width * 1.1f / sky.sprite.bounds.size.x, height * 1.04f / sky.sprite.bounds.size.y, 1f);

            for (int i = 0; i < blobs.Length; i++)
            {
                var b = blobs[i];
                float t = time * b.Speed + b.Phase;
                var drift = new Vector2(Mathf.Sin(t) * 0.07f, Mathf.Cos(t * 0.8f) * 0.04f);
                float breathe = 1f + 0.07f * Mathf.Sin(t * 1.3f);
                b.Renderer.transform.localPosition = new Vector3((b.Home.x + drift.x) * width, (b.Home.y + drift.y) * height, 0f);
                float size = b.Size * height * breathe;
                b.Renderer.transform.localScale = new Vector3(size * 1.6f, size, 1f) / BlobSprite.bounds.size.x;
                b.Renderer.color = new Color(1f, 1f, 1f, b.Alpha);
            }

            for (int i = 0; i < bubbles.Length; i++)
            {
                var b = bubbles[i];
                float x = b.X + Mathf.Sin(time * b.SwaySpeed + b.Phase) * b.Sway;
                b.Renderer.transform.localPosition = new Vector3(x * width, b.Y * height, 0f);
                float size = b.Size * height * (1f + 0.05f * Mathf.Sin(time * 2f + b.Phase));
                b.Renderer.transform.localScale = Vector3.one * size / BubbleSprite.bounds.size.x;
                // Fade in from the bottom edge and out towards the top.
                float fade = Mathf.Clamp01((b.Y + 0.7f) / 0.25f) * Mathf.Clamp01((0.7f - b.Y) / 0.35f);
                b.Renderer.color = new Color(1f, 1f, 1f, 0.75f * fade);
            }
        }

        static SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>A soft white cloud blob: solid in the middle, fading smoothly to nothing, no hard edge.</summary>
        static Sprite BlobSprite => blobSprite != null ? blobSprite : blobSprite = Radial("Cloud", 256, d => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, d)));

        /// <summary>A glassy bubble: a faint fill, a brighter rim and a small highlight at the top left.</summary>
        static Sprite BubbleSprite => bubbleSprite != null ? bubbleSprite : bubbleSprite = MakeBubble(256);

        static Sprite Radial(string name, int size, System.Func<float, float> alpha)
        {
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(d)) * 255f));
                }
            }
            return Create(name, size, size, pixels);
        }

        static Sprite MakeBubble(int size)
        {
            var pixels = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half) / half;
                    float d = p.magnitude;
                    float edge = Mathf.Clamp01((1f - d) * half * 0.9f);
                    if (edge <= 0f) continue;
                    float rim = Mathf.Exp(-Mathf.Pow((1f - d) / 0.09f, 2f)) * 0.75f;
                    float fill = 0.12f + 0.1f * d * d;
                    var spot = (p - new Vector2(-0.38f, 0.42f)) / new Vector2(0.26f, 0.16f);
                    float shine = Mathf.Clamp01(1f - spot.magnitude) * 0.9f;
                    float a = Mathf.Clamp01(Mathf.Max(fill, rim) + shine) * edge;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            return Create("Bubble", size, size, pixels);
        }

        /// <summary>
        /// A tall gradient with 1024 steps and a whisper of noise, so it stays smooth with no visible bands when
        /// stretched over a full phone screen.
        /// </summary>
        static Sprite SmoothGradient(Color top, Color bottom)
        {
            const int w = 4, h = 1024;
            var rng = new System.Random(3);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (h - 1f));
                for (int x = 0; x < w; x++)
                {
                    float n = ((float)rng.NextDouble() - 0.5f) / 255f;
                    pixels[y * w + x] = new Color(c.r + n, c.g + n, c.b + n, 1f);
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Sky", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite Create(string name, int w, int h, Color32[] pixels)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
