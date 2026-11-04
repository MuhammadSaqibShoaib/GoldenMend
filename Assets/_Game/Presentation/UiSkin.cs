using System;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// The Berry Night skin's shapes, generated at runtime in greyscale so one sprite tints to any colour:
    /// chunky buttons with a darker lip, gradient panels, outlines, sunken insets, pills and soft shadows.
    /// All are 9-sliced. UI sprites use 100 pixels per unit (one pixel per canvas unit at the reference size);
    /// the World variants are sized for the board, where a unit is about 92 canvas units.
    /// </summary>
    public static class UiSkin
    {
        /// <summary>Height of the darker lip under a chunky button, in sprite pixels.</summary>
        public const float LipHeight = 14f;

        static Sprite chunky, panel, ring, inset, pill, round, shadow, glow, worldRound, worldInset;

        /// <summary>A chunky button: lighter at the top, a darker outline and a darker lip along the bottom.</summary>
        public static Sprite Chunky => chunky != null ? chunky : chunky = Make("Chunky", 128, 128, 34, ChunkyShade, new Vector4(40f, 40f + LipHeight, 40f, 40f));

        /// <summary>A panel with a soft top-to-bottom gradient.</summary>
        public static Sprite Panel => panel != null ? panel : panel = Make("Panel", 128, 128, 36, (x, y, d) => Mathf.Lerp(0.62f, 1f, y / 127f), new Vector4(40f, 40f, 40f, 40f));

        /// <summary>Only the outline of a panel, to draw its edge in a second colour.</summary>
        public static Sprite Ring => ring != null ? ring : ring = Make("Ring", 128, 128, 36, (x, y, d) => d > -5f ? 1f : -1f, new Vector4(40f, 40f, 40f, 40f));

        /// <summary>A sunken well: darker along its top edge, as if lit from above.</summary>
        public static Sprite Inset => inset != null ? inset : inset = Make("Inset", 128, 128, 26, InsetShade, new Vector4(30f, 30f, 30f, 30f));

        /// <summary>A fully rounded capsule.</summary>
        public static Sprite Pill => pill != null ? pill : pill = Make("Pill", 128, 64, 32, (x, y, d) => 1f, new Vector4(32f, 32f, 32f, 32f));

        /// <summary>A plain rounded square.</summary>
        public static Sprite Round => round != null ? round : round = Make("Round", 128, 128, 36, (x, y, d) => 1f, new Vector4(40f, 40f, 40f, 40f));

        /// <summary>A soft blurred rounded square, for drop shadows under panels.</summary>
        public static Sprite Shadow => shadow != null ? shadow : shadow = MakeShadow();

        /// <summary>A soft round glow that fades to nothing at its edge.</summary>
        public static Sprite Glow => glow != null ? glow : glow = MakeGlow();

        /// <summary><see cref="Round"/> for the board (sliced SpriteRenderers), with corners of about a third of a unit.</summary>
        public static Sprite WorldRound => worldRound != null ? worldRound : worldRound = Rescale(Round, 110f);

        /// <summary><see cref="Inset"/> for the board.</summary>
        public static Sprite WorldInset => worldInset != null ? worldInset : worldInset = Rescale(Inset, 110f);

        static float ChunkyShade(float x, float y, float dFull)
        {
            float dFace = RoundedRect(x, y, 0f, LipHeight, 128f, 128f, 34f);
            if (dFace > -5f) return 0.58f;
            float t = (y - LipHeight) / (128f - LipHeight);
            return Mathf.Lerp(0.8f, 1f, t);
        }

        static float InsetShade(float x, float y, float d)
        {
            float fromTop = 127f - y;
            return Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((fromTop - 2f) / 14f));
        }

        /// <summary>Signed distance to a rounded rectangle (negative inside), in pixels.</summary>
        static float RoundedRect(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
            float qx = Mathf.Abs(x - cx) - ((x1 - x0) / 2f - r);
            float qy = Mathf.Abs(y - cy) - ((y1 - y0) / 2f - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>
        /// Draws a rounded rectangle filling the texture. <paramref name="shade"/> gets the pixel and its distance to
        /// the edge and returns the grey level; a negative value leaves the pixel transparent.
        /// </summary>
        static Sprite Make(string name, int w, int h, float radius, Func<float, float, float, float> shade, Vector4 border)
        {
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float d = RoundedRect(px, py, 0f, 0f, w, h, radius);
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (alpha <= 0f) continue;
                    float s = shade(px, py, d);
                    if (s < 0f) continue;
                    byte g = (byte)(Mathf.Clamp01(s) * 255f);
                    pixels[y * w + x] = new Color32(g, g, g, (byte)(alpha * 255f));
                }
            }
            return Create(name, w, h, pixels, border, 100f);
        }

        static Sprite MakeShadow()
        {
            const int size = 128;
            const float blur = 22f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedRect(x + 0.5f, y + 0.5f, blur, blur, size - blur, size - blur, 20f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(0.5f - d / (blur * 2f)));
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            }
            return Create("Shadow", size, size, pixels, new Vector4(50f, 50f, 50f, 50f), 100f);
        }

        static Sprite MakeGlow()
        {
            const int size = 128;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            return Create("Glow", size, size, pixels, Vector4.zero, 100f);
        }

        static Sprite Rescale(Sprite source, float pixelsPerUnit) =>
            Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);

        static Sprite Create(string name, int w, int h, Color32[] pixels, Vector4 border, float pixelsPerUnit)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }

        /// <summary>A vertical two-colour gradient (top to bottom), for full-screen backgrounds.</summary>
        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            const int h = 256;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { name = "Gradient", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[h];
            for (int y = 0; y < h; y++) pixels[y] = Color.Lerp(bottom, top, y / (h - 1f));
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
