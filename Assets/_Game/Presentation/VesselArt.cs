using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Gives a vessel its picture and cuts it into one sprite per socket. The picture is what tells players where a
    /// shard belongs: its piece matches the faint ghost in its socket. With an <see cref="ArtCatalog"/> the picture is
    /// the level's public-domain painting; without one (Classic build) a glaze is generated.
    /// </summary>
    public static class VesselArt
    {
        public const int CellPixels = 64;

        static readonly HashSet<Texture> Generated = new HashSet<Texture>();

        /// <summary>The paintings in use, or null for generated glazes.</summary>
        public static ArtCatalog Catalog { get; set; }

        public static Artwork ArtworkFor(LevelSpec spec)
        {
            var work = Catalog != null ? Catalog.Find(spec.Artwork) : null;
            return work != null && work.Image != null ? work : null;
        }

        /// <summary>What the player calls this level: the painting's title, or the vessel's name in Classic.</summary>
        public static string DisplayName(LevelSpec spec) => ArtworkFor(spec)?.Title ?? spec.Name;

        /// <summary>"Artist, date" for a painting level, otherwise null.</summary>
        public static string Byline(LevelSpec spec) => ArtworkFor(spec)?.Byline;

        /// <summary>Frees sprites from <see cref="BuildSocketSprites"/>; generated textures go too, shared paintings stay.</summary>
        public static void Release(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return;
            var texture = sprites[0] != null ? sprites[0].texture : null;
            foreach (var sprite in sprites) Destroy(sprite);
            if (texture != null && Generated.Remove(texture)) Destroy(texture);
        }

        static void Destroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

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
            var artwork = ArtworkFor(level.Spec);
            return artwork != null ? CutPainting(level, artwork.Image) : PaintGlaze(level);
        }

        /// <summary>Crops the painting's centre to the vessel's grid shape and cuts one square per socket.</summary>
        static Sprite[] CutPainting(BuiltLevel level, Texture2D painting)
        {
            float aspect = level.Columns / (float)level.Rows;
            float cropWidth = painting.width, cropHeight = painting.width / aspect;
            if (cropHeight > painting.height)
            {
                cropHeight = painting.height;
                cropWidth = cropHeight * aspect;
            }
            float x0 = (painting.width - cropWidth) * 0.5f;
            float y0 = (painting.height - cropHeight) * 0.5f;
            float cell = Mathf.Floor(cropWidth / level.Columns);

            var sprites = new Sprite[level.Def.SocketCount];
            for (int s = 0; s < sprites.Length; s++)
            {
                int col = level.SocketColumn[s];
                int texRow = level.Rows - 1 - level.SocketRow[s];
                var rect = new Rect(Mathf.Floor(x0 + col * cell), Mathf.Floor(y0 + texRow * cell), cell, cell);
                sprites[s] = Sprite.Create(painting, rect, new Vector2(0.5f, 0.5f), cell, 0, SpriteMeshType.FullRect);
            }
            return sprites;
        }

        static Sprite[] PaintGlaze(BuiltLevel level)
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
            Generated.Add(tex);

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
