using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>Basic shapes generated at runtime (squares, rounded squares, circles).</summary>
    public static class SpriteFactory
    {
        const int Size = 64;

        static Sprite square;
        static Sprite rounded;
        static Sprite circle;

        /// <summary>A 1x1 world-unit white square.</summary>
        public static Sprite Square => square != null ? square : square = Make((x, y) => 1f, "Square");

        /// <summary>A 1x1 world-unit white rounded square.</summary>
        public static Sprite Rounded => rounded != null ? rounded : rounded = Make(RoundedAlpha, "Rounded");

        public static Sprite Circle => circle != null ? circle : circle = Make(CircleAlpha, "Circle");

        static float RoundedAlpha(int x, int y)
        {
            const float radius = 14f;
            float cx = Mathf.Clamp(x + 0.5f, radius, Size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, Size - radius);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - d + 0.5f);
        }

        static float CircleAlpha(int x, int y)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(Size / 2f, Size / 2f));
            return Mathf.Clamp01(Size / 2f - d);
        }

        static Sprite Make(System.Func<int, int, float> alpha, string name)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha(x, y) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
        }
    }
}
