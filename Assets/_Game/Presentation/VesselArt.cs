using GoldenMend.Data;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Paints one glazed picture across the whole vessel and cuts it into one sprite per socket.
    /// The picture is what tells players where a shard belongs: its piece matches the faint ghost in its socket.
    /// </summary>
    public static class VesselArt
    {
        public const int CellPixels = 64;

        struct Palette
        {
            public Color Top, Bottom, Band, Motif;
        }

        static readonly Palette[] Palettes =
        {
            new Palette { Top = C(0.60f, 0.78f, 0.70f), Bottom = C(0.36f, 0.56f, 0.50f), Band = C(0.92f, 0.94f, 0.88f), Motif = C(0.22f, 0.40f, 0.38f) }, // celadon
            new Palette { Top = C(0.34f, 0.44f, 0.70f), Bottom = C(0.16f, 0.22f, 0.44f), Band = C(0.93f, 0.93f, 0.96f), Motif = C(0.80f, 0.85f, 0.95f) }, // indigo
            new Palette { Top = C(0.84f, 0.52f, 0.34f), Bottom = C(0.58f, 0.30f, 0.20f), Band = C(0.96f, 0.86f, 0.70f), Motif = C(0.33f, 0.16f, 0.10f) }, // terracotta
            new Palette { Top = C(0.92f, 0.89f, 0.82f), Bottom = C(0.72f, 0.68f, 0.60f), Band = C(0.28f, 0.34f, 0.46f), Motif = C(0.42f, 0.50f, 0.62f) }, // ash white
            new Palette { Top = C(0.46f, 0.29f, 0.20f), Bottom = C(0.20f, 0.12f, 0.08f), Band = C(0.78f, 0.56f, 0.30f), Motif = C(0.88f, 0.68f, 0.36f) }, // tenmoku
            new Palette { Top = C(0.74f, 0.24f, 0.21f), Bottom = C(0.44f, 0.11f, 0.11f), Band = C(0.96f, 0.82f, 0.46f), Motif = C(0.14f, 0.14f, 0.17f) }, // cinnabar
        };

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        public static Sprite[] BuildSocketSprites(BuiltLevel level)
        {
            int width = level.Columns * CellPixels;
            int height = level.Rows * CellPixels;
            var palette = Palettes[Mathf.Abs(level.Spec.Palette) % Palettes.Length];

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = level.Spec.Name + " glaze",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = Paint(x, y, width, height, level.Columns, level.Rows, palette);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var sprites = new Sprite[level.Def.SocketCount];
            for (int s = 0; s < sprites.Length; s++)
            {
                int col = level.SocketColumn[s];
                int texRow = level.Rows - 1 - level.SocketRow[s];
                var rect = new Rect(col * CellPixels, texRow * CellPixels, CellPixels, CellPixels);
                sprites[s] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), CellPixels, 0, SpriteMeshType.FullRect);
            }
            return sprites;
        }

        static Color32 Paint(int x, int y, int width, int height, int columns, int rows, Palette p)
        {
            float u = (x + 0.5f) / width;
            float v = (y + 0.5f) / height;
            var color = Color.Lerp(p.Bottom, p.Top, v);

            // Two horizontal bands across the whole vessel.
            float wave = 0.025f * Mathf.Sin(u * Mathf.PI * 4f);
            if (Mathf.Abs(v - 0.74f - wave) < 0.035f) color = Color.Lerp(color, p.Band, 0.85f);
            if (Mathf.Abs(v - 0.30f + wave) < 0.022f) color = Color.Lerp(color, p.Band, 0.7f);

            // Ring motifs on a staggered lattice, about 1.6 cells apart, so each cell gets a different slice.
            float cellsX = u * columns;
            float cellsY = v * rows;
            const float spacing = 1.6f;
            float rowIndex = Mathf.Floor(cellsY / spacing);
            float offset = (rowIndex % 2f) * spacing * 0.5f;
            float lx = Mathf.Repeat(cellsX + offset, spacing) - spacing * 0.5f;
            float ly = Mathf.Repeat(cellsY, spacing) - spacing * 0.5f;
            float d = Mathf.Sqrt(lx * lx + ly * ly);
            if (Mathf.Abs(d - 0.48f) < 0.06f) color = Color.Lerp(color, p.Motif, 0.9f);
            else if (d < 0.14f) color = Color.Lerp(color, p.Motif, 0.8f);

            // A slight brightness shift per cell and darker shard edges.
            int cellX = Mathf.Min(x / CellPixels, columns - 1);
            int cellY = Mathf.Min(y / CellPixels, rows - 1);
            float jitter = (Hash(cellX, cellY) - 0.5f) * 0.12f;
            color = new Color(color.r + jitter, color.g + jitter, color.b + jitter, 1f);

            int ex = x % CellPixels;
            int ey = y % CellPixels;
            int edge = Mathf.Min(Mathf.Min(ex, CellPixels - 1 - ex), Mathf.Min(ey, CellPixels - 1 - ey));
            if (edge < 3) color *= 0.78f + edge * 0.06f;

            color.a = 1f;
            return color;
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
