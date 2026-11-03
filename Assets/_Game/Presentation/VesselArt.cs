using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Gives a vessel its glaze and cuts it into one sprite per socket. The glaze is what tells players where a
    /// shard belongs: its piece matches the faint ghost in its socket. Glazes are traditional Japanese patterns
    /// (generated) or close-up crops of public-domain paintings from the <see cref="ArtCatalog"/>.
    /// Every pattern repeats on a period that is not a whole number of cells and sits on a gradient with a broad
    /// ribbon across it, so no two shards look the same.
    /// </summary>
    public static class VesselArt
    {
        public const int CellPixels = 128;

        static readonly HashSet<Texture> Generated = new HashSet<Texture>();

        /// <summary>Public-domain paintings that painting glazes are cropped from.</summary>
        public static ArtCatalog Catalog { get; set; }

        public static Artwork ArtworkFor(LevelSpec spec)
        {
            var work = Catalog != null ? Catalog.Find(spec.Artwork) : null;
            return work != null && work.Image != null ? work : null;
        }

        public static string DisplayName(LevelSpec spec) => spec.Name;

        /// <summary>Describes the glaze: "Seigaiha waves", or "Hokusai, The Great Wave" for a painting crop.</summary>
        public static string Byline(LevelSpec spec)
        {
            var work = ArtworkFor(spec);
            if (work != null) return $"{ShortArtist(work.Artist)}, {work.Title}";
            switch (spec.Pattern)
            {
                case "seigaiha": return "Seigaiha waves";
                case "sakura": return "Cherry blossoms";
                case "kikko": return "Kikkō hexagons";
                case "uroko": return "Uroko scales";
                case "kanoko": return "Kanoko dots";
                default: return "Shippō rings";
            }
        }

        /// <summary>"Katsushika Hokusai" to "Hokusai", "Vincent van Gogh" to "van Gogh".</summary>
        static string ShortArtist(string artist)
        {
            var parts = (artist ?? "").Split(' ');
            if (parts.Length >= 3 && (parts[parts.Length - 2] == "van" || parts[parts.Length - 2] == "de")) return parts[parts.Length - 2] + " " + parts[parts.Length - 1];
            return parts[parts.Length - 1];
        }

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
            new Palette { Top = C(0.98f, 0.87f, 0.88f), Bottom = C(0.84f, 0.58f, 0.66f), Band = C(1.00f, 0.97f, 0.97f), Motif = C(0.58f, 0.20f, 0.32f) }, // sakura
            new Palette { Top = C(0.24f, 0.24f, 0.28f), Bottom = C(0.10f, 0.10f, 0.12f), Band = C(0.92f, 0.78f, 0.42f), Motif = C(0.72f, 0.58f, 0.32f) }, // ink and gold
        };

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        public static Sprite[] BuildSocketSprites(BuiltLevel level)
        {
            var artwork = ArtworkFor(level.Spec);
            return artwork != null ? CutPainting(level, artwork.Image) : PaintPattern(level);
        }

        /// <summary>
        /// Crops the painting to the vessel's grid shape, zoomed in around the spec's focus point so brushwork
        /// becomes texture, and cuts one square per socket.
        /// </summary>
        static Sprite[] CutPainting(BuiltLevel level, Texture2D painting)
        {
            var spec = level.Spec;
            float aspect = level.Columns / (float)level.Rows;
            float cropWidth = painting.width, cropHeight = painting.width / aspect;
            if (cropHeight > painting.height)
            {
                cropHeight = painting.height;
                cropWidth = cropHeight * aspect;
            }
            float zoom = Mathf.Max(1f, spec.ArtZoom);
            cropWidth /= zoom;
            cropHeight /= zoom;

            // Focus is given top-left origin; textures count rows from the bottom.
            float cx = spec.ArtFocusX * painting.width;
            float cy = (1f - spec.ArtFocusY) * painting.height;
            float x0 = Mathf.Clamp(cx - cropWidth * 0.5f, 0f, painting.width - cropWidth);
            float y0 = Mathf.Clamp(cy - cropHeight * 0.5f, 0f, painting.height - cropHeight);
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

        static Sprite[] PaintPattern(BuiltLevel level)
        {
            int width = level.Columns * CellPixels;
            int height = level.Rows * CellPixels;
            var palette = Palettes[Mathf.Abs(level.Spec.Palette) % Palettes.Length];
            var motif = MotifFor(level.Spec.Pattern);
            int seed = level.Spec.Seed;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = level.Spec.Name + " glaze",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            // A diagonal ribbon of lighter glaze, placed per level, so the composition is never uniform.
            float ribbonAngle = 0.35f + Hash(seed, 1) * 0.6f;
            float ribbonOffset = (Hash(seed, 2) - 0.5f) * 0.5f;
            var ribbonDir = new Vector2(Mathf.Cos(ribbonAngle), Mathf.Sin(ribbonAngle));

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / CellPixels; // position in cells, x right
                    float v = (height - y - 0.5f) / CellPixels; // position in cells, y down from the top
                    float t = (y + 0.5f) / height;

                    var color = Color.Lerp(palette.Bottom, palette.Top, t);
                    float along = Vector2.Dot(new Vector2(u / level.Columns - 0.5f, t - 0.5f), new Vector2(-ribbonDir.y, ribbonDir.x));
                    float ribbon = Mathf.Clamp01(1f - Mathf.Abs(along - ribbonOffset) / 0.14f);
                    color = Color.Lerp(color, Color.Lerp(color, palette.Band, 0.45f), Smooth(ribbon));

                    color = motif(u, v, color, palette, seed);

                    color.a = 1f;
                    pixels[y * width + x] = color;
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

        delegate Color Motif(float u, float v, Color baseColor, Palette p, int seed);

        static Motif MotifFor(string pattern)
        {
            switch (pattern)
            {
                case "seigaiha": return Seigaiha;
                case "sakura": return Sakura;
                case "kikko": return Kikko;
                case "uroko": return Uroko;
                case "kanoko": return Kanoko;
                default: return Shippo;
            }
        }

        /// <summary>Seigaiha: overlapping fans of concentric arcs, each row tucked behind the one below.</summary>
        static Color Seigaiha(float u, float v, Color c, Palette p, int seed)
        {
            const float period = 1.35f, rowStep = period * 0.5f, radius = period * 0.5f;
            int baseRow = Mathf.FloorToInt(v / rowStep);
            for (int r = baseRow + 1; r >= baseRow - 1; r--) // lower rows sit in front
            {
                float cy = (r + 1) * rowStep;
                float shift = (r & 1) == 0 ? 0f : period * 0.5f;
                float cx = Mathf.Round((u - shift) / period) * period + shift;
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy));
                if (d > radius || v > cy) continue;
                float ringWidth = radius / 4f;
                int ring = Mathf.FloorToInt(d / ringWidth);
                float inRing = d / ringWidth - ring;
                var fill = (ring & 1) == 0 ? c : Color.Lerp(c, p.Band, 0.28f);
                return inRing < 0.16f ? Color.Lerp(fill, p.Motif, 0.85f) : fill;
            }
            return c;
        }

        /// <summary>Sakura: five-petal blossoms of varied size scattered on a jittered grid, with drifting petals.</summary>
        static Color Sakura(float u, float v, Color c, Palette p, int seed)
        {
            const float spacing = 1.15f;
            int gx = Mathf.FloorToInt(u / spacing), gy = Mathf.FloorToInt(v / spacing);
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cxI = gx + ox, cyI = gy + oy;
                    float cx = (cxI + 0.2f + Hash(cxI + seed, cyI) * 0.6f) * spacing;
                    float cy = (cyI + 0.2f + Hash(cxI, cyI + seed) * 0.6f) * spacing;
                    float size = 0.28f + Hash(cxI * 3 + seed, cyI * 7) * 0.24f;
                    float spin = Hash(cxI * 5, cyI * 11 + seed) * Mathf.PI * 2f;
                    float dx = u - cx, dy = v - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > size) continue;
                    float angle = Mathf.Atan2(dy, dx) + spin;
                    float petal = 0.55f + 0.45f * Mathf.Abs(Mathf.Cos(angle * 2.5f));
                    if (d > size * petal) continue;
                    if (d < size * 0.18f) return p.Motif;
                    var petalColor = Color.Lerp(p.Band, p.Motif, 0.12f + 0.25f * (d / size));
                    return d > size * petal - 0.025f ? Color.Lerp(petalColor, p.Motif, 0.5f) : petalColor;
                }
            }
            // Small drifting petals between the blossoms.
            float px = Mathf.Repeat(u * 2.3f + v * 0.7f, 1f) - 0.5f, py = Mathf.Repeat(v * 2.1f - u * 0.4f, 1f) - 0.5f;
            if (Hash(Mathf.FloorToInt(u * 2.3f + v * 0.7f), Mathf.FloorToInt(v * 2.1f - u * 0.4f) + seed) > 0.72f && px * px * 4f + py * py * 12f < 0.05f)
                return Color.Lerp(c, p.Band, 0.7f);
            return c;
        }

        /// <summary>Kikkō: a tortoise-shell hexagon grid; some hexes carry an inner hexagon, some a darker fill.</summary>
        static Color Kikko(float u, float v, Color c, Palette p, int seed)
        {
            const float radius = 0.62f;
            float colStep = radius * 1.5f, rowStep = radius * 1.7320508f;
            int baseCol = Mathf.FloorToInt(u / colStep);
            float best = float.MaxValue;
            int bestCol = 0, bestRow = 0;
            for (int col = baseCol - 1; col <= baseCol + 1; col++)
            {
                float shift = (col & 1) == 0 ? 0f : rowStep * 0.5f;
                int row = Mathf.RoundToInt((v - shift) / rowStep);
                for (int r = row - 1; r <= row + 1; r++)
                {
                    float h = HexDistance(u - col * colStep, v - (r * rowStep + shift));
                    if (h < best)
                    {
                        best = h;
                        bestCol = col;
                        bestRow = r;
                    }
                }
            }
            float edge = radius * 0.8660254f;
            float kind = Hash(bestCol + seed, bestRow);
            var fill = kind > 0.7f ? Color.Lerp(c, p.Motif, 0.35f) : c;
            if (best > edge - 0.05f) return Color.Lerp(fill, p.Motif, 0.9f);
            if (kind < 0.45f && Mathf.Abs(best - edge * 0.55f) < 0.03f) return Color.Lerp(fill, p.Band, 0.75f);
            return fill;
        }

        static float HexDistance(float x, float y)
        {
            x = Mathf.Abs(x);
            y = Mathf.Abs(y);
            return Mathf.Max(y, (x * 1.7320508f + y) * 0.5f);
        }

        /// <summary>Uroko: rows of interlocking triangles (dragon scales) in two tones.</summary>
        static Color Uroko(float u, float v, Color c, Palette p, int seed)
        {
            const float w = 0.72f, h = 0.62f;
            int row = Mathf.FloorToInt(v / h);
            float ly = v / h - row;
            float xs = u / w + ((row & 1) == 0 ? 0f : 0.5f);
            int tri = Mathf.FloorToInt(xs);
            float lx = xs - tri;
            bool up = Mathf.Abs(lx - 0.5f) * 2f < ly;
            if (!up) return c;
            float tone = Hash(tri + seed, row);
            var scale = Color.Lerp(p.Motif, p.Band, tone * 0.35f);
            float border = Mathf.Min(ly - Mathf.Abs(lx - 0.5f) * 2f, 1f - ly);
            return border < 0.06f ? Color.Lerp(scale, p.Band, 0.6f) : scale;
        }

        /// <summary>Kanoko: tie-dye dots, small diamonds with a pale centre, changing colour across broad bands.</summary>
        static Color Kanoko(float u, float v, Color c, Palette p, int seed)
        {
            const float spacing = 0.42f;
            float gx = u / spacing, gy = v / spacing;
            int ix = Mathf.FloorToInt(gx), iy = Mathf.FloorToInt(gy);
            if (((ix + iy) & 1) != 0) return c;
            float dx = gx - ix - 0.5f, dy = gy - iy - 0.5f;
            float d = Mathf.Abs(dx) + Mathf.Abs(dy);
            if (d > 0.42f) return c;
            float band = Mathf.Sin(u * 0.9f + v * 1.6f + seed) * 0.5f + 0.5f;
            var dot = band > 0.5f ? p.Band : Color.Lerp(p.Band, p.Motif, 0.6f);
            return d < 0.12f ? Color.Lerp(c, dot, 0.4f) : dot;
        }

        /// <summary>Shippō: interlocking rings on a staggered lattice, with dots at the centres.</summary>
        static Color Shippo(float u, float v, Color c, Palette p, int seed)
        {
            const float spacing = 1.6f;
            float rowIndex = Mathf.Floor(v / spacing);
            float offset = (rowIndex % 2f) * spacing * 0.5f;
            float lx = Mathf.Repeat(u + offset, spacing) - spacing * 0.5f;
            float ly = Mathf.Repeat(v, spacing) - spacing * 0.5f;
            float d = Mathf.Sqrt(lx * lx + ly * ly);
            if (Mathf.Abs(d - 0.48f) < 0.06f) return Color.Lerp(c, p.Motif, 0.9f);
            if (d < 0.14f) return Color.Lerp(c, p.Motif, 0.8f);
            return c;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

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
