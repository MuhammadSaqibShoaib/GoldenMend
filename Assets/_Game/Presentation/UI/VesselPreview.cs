using System.Collections.Generic;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    public enum VesselLook
    {
        /// <summary>The finished piece: full glaze with gold seams on every join.</summary>
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
            var sprites = SpritesFor(level);
            var theme = b.Theme;

            float cell = Mathf.Min(box.x / level.Columns, box.y / level.Rows);
            float shard = cell * 0.94f;
            var def = level.Def;

            Vector2 Cell(int socket) => new Vector2(
                (level.SocketColumn[socket] - (level.Columns - 1) / 2f) * cell,
                -(level.SocketRow[socket] - (level.Rows - 1) / 2f) * cell);

            var silhouette = UiBuilder.WithAlpha(theme.Ink, 0.75f);
            var silhouetteEdge = UiBuilder.WithAlpha(theme.Cream, 0.08f);

            if (look == VesselLook.Teaser)
            {
                float frame = cell * 1.04f;
                b.Image("Anchor Gold", root, null, theme.Gold, Anchor.Center, Cell(def.AnchorSocket), new Vector2(frame, frame));
            }

            for (int s = 0; s < def.SocketCount; s++)
            {
                bool lit = look == VesselLook.Restored || (look == VesselLook.Teaser && s == def.AnchorSocket);
                if (!lit)
                {
                    b.Image($"Edge {s}", root, null, silhouetteEdge, Anchor.Center, Cell(s), new Vector2(shard + 4f, shard + 4f));
                }
                var image = b.Image($"Shard {s}", root, sprites[s], lit ? Color.white : silhouette, Anchor.Center, Cell(s), new Vector2(shard, shard));
                image.preserveAspect = false;
            }

            if (look == VesselLook.Restored)
            {
                // Gold seams along every join, like the in-game finish.
                float width = Mathf.Max(3f, cell * 0.07f), length = cell * 0.96f;
                for (int a = 0; a < def.SocketCount; a++)
                {
                    foreach (int n in def.Neighbours[a])
                    {
                        if (n < a) continue;
                        var pa = Cell(a);
                        var pb = Cell(n);
                        bool sideBySide = Mathf.Abs(pa.y - pb.y) < 0.01f;
                        var size = sideBySide ? new Vector2(width, length) : new Vector2(length, width);
                        b.Image("Seam", root, null, theme.Gold, Anchor.Center, (pa + pb) * 0.5f, size);
                    }
                }
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
