using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    public enum VesselLook
    {
        /// <summary>The finished piece: one merged glaze with a gold border around the outside, like the in-game finish.</summary>
        Restored,

        /// <summary>A dark silhouette, for vessels not yet unlocked.</summary>
        Silhouette,

        /// <summary>The silhouette with only the gold-framed anchor shard showing: "restore this next".</summary>
        Teaser,
    }

    /// <summary>Draws a level's vessel in the UI, fitted inside a box, for the home screen and the museum.</summary>
    public static class VesselPreview
    {
        static readonly Dictionary<string, Sprite[]> SpriteCache = new Dictionary<string, Sprite[]>();

        public static RectTransform Build(UiBuilder b, Transform parent, BuiltLevel level, VesselLook look, Anchor anchor, Vector2 position, Vector2 box)
        {
            var root = b.Node("Vessel " + level.Spec.Name, parent, anchor, position, box);
            // Silhouette cells are plain squares, so a locked vessel never pays for building its glaze.
            var sprites = look == VesselLook.Silhouette ? null : SpritesFor(level);
            var theme = b.Theme;

            float cell = Mathf.Min(box.x / level.Columns, box.y / level.Rows);
            float shard = cell * 0.94f;
            var def = level.Def;

            Vector2 Cell(int socket) => new Vector2(
                (level.SocketColumn[socket] - (level.Columns - 1) / 2f) * cell,
                -(level.SocketRow[socket] - (level.Rows - 1) / 2f) * cell);

            var silhouette = UiBuilder.WithAlpha(theme.StarEmpty, 0.95f);
            var silhouetteEdge = UiBuilder.WithAlpha(Color.white, 0.85f);

            if (look == VesselLook.Teaser)
            {
                float frame = cell * 1.04f;
                b.Image("Anchor Gold", root, null, theme.Gold, Anchor.Center, Cell(def.AnchorSocket), new Vector2(frame, frame));
            }
            else if (look == VesselLook.Restored)
            {
                // A slightly larger gold square under every cell: where cells meet the shards cover it, so only the
                // outer edge of the vessel shows gold.
                float frame = cell + Mathf.Max(4f, cell * 0.08f) * 2f;
                for (int s = 0; s < def.SocketCount; s++)
                    b.Image($"Border {s}", root, null, theme.Gold, Anchor.Center, Cell(s), new Vector2(frame, frame));
            }

            for (int s = 0; s < def.SocketCount; s++)
            {
                bool lit = look == VesselLook.Restored || (look == VesselLook.Teaser && s == def.AnchorSocket);
                if (!lit)
                {
                    b.Image($"Edge {s}", root, null, silhouetteEdge, Anchor.Center, Cell(s), new Vector2(shard + 4f, shard + 4f));
                }
                // Restored shards overlap by a pixel so the merged glaze shows no hairline between cells.
                float size = look == VesselLook.Restored ? cell + 1f : shard;
                var image = b.Image($"Shard {s}", root, lit ? sprites[s] : null, lit ? Color.white : silhouette, Anchor.Center, Cell(s), new Vector2(size, size));
                image.preserveAspect = false;
            }

            return root;
        }

        static Sprite[] SpritesFor(BuiltLevel level)
        {
            if (!SpriteCache.TryGetValue(level.Spec.Name, out var sprites) || sprites[0] == null)
            {
                sprites = VesselArt.BuildSocketSprites(level);
                SpriteCache[level.Spec.Name] = sprites;
            }
            return sprites;
        }
    }
}
