using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Keeps the home screen's vessel alive, all inside its cells: they cascade in row by row, a soft light wave
    /// sweeps across them every few seconds, a random cell hops now and then, another catches a gold glint, and the
    /// gold anchor shard pulses. The Play button breathes. Unscaled time; nothing runs in edit mode, so screenshots
    /// show the resting pose.
    /// </summary>
    public sealed class ShowcaseMotion : MonoBehaviour
    {
        const float CascadeStep = 0.035f;
        const float CascadeTime = 0.32f;
        const float WavePeriod = 3.2f;
        const float WaveWidth = 1.6f;
        const float HopEvery = 0.75f;
        const float HopTime = 0.36f;
        const float GlintEvery = 1.5f;
        const float GlintTime = 0.9f;

        static readonly Color WaveColor = new Color(1f, 1f, 1f, 1f);
        static readonly Color GlintColor = new Color(1f, 0.84f, 0.29f, 1f);

        /// <summary>One vessel cell: its shard image plus the frame drawn under it, which move together.</summary>
        sealed class Cell
        {
            public readonly List<RectTransform> Parts = new List<RectTransform>();
            public Image Face;
            public Color Base;
            public float Row;
            public float Column;
            public float Delay;
            public float HopAt = -10f;
            public float GlintAt = -10f;
            public bool IsAnchor;
        }

        RectTransform play;
        readonly List<Cell> cells = new List<Cell>();
        float shownAt;
        float nextHop;
        float nextGlint;
        float columns = 1f;

        public static ShowcaseMotion Attach(RectTransform area, RectTransform play)
        {
            var motion = area.gameObject.AddComponent<ShowcaseMotion>();
            motion.play = play;
            return motion;
        }

        /// <summary>Takes over a freshly built vessel preview and restarts the entrance.</summary>
        public void Track(RectTransform preview)
        {
            cells.Clear();
            var byPosition = new Dictionary<Vector2Int, Cell>();
            float top = float.MinValue, left = float.MaxValue, size = 1f;
            foreach (RectTransform child in preview)
            {
                if (!child.name.StartsWith("Shard")) continue;
                top = Mathf.Max(top, child.anchoredPosition.y);
                left = Mathf.Min(left, child.anchoredPosition.x);
                size = Mathf.Max(1f, child.sizeDelta.x);
            }

            RectTransform anchorFrame = null;
            foreach (RectTransform child in preview)
            {
                if (child.name == "Anchor Gold") anchorFrame = child;
                var key = Vector2Int.RoundToInt(child.anchoredPosition);
                if (!byPosition.TryGetValue(key, out var cell))
                {
                    cell = new Cell
                    {
                        Row = (top - child.anchoredPosition.y) / size,
                        Column = (child.anchoredPosition.x - left) / size,
                    };
                    cell.Delay = (cell.Row + cell.Column * 0.35f + 4f) * CascadeStep;
                    byPosition[key] = cell;
                    cells.Add(cell);
                }
                cell.Parts.Add(child);
                if (child.name.StartsWith("Shard"))
                {
                    cell.Face = child.GetComponent<Image>();
                    cell.Base = cell.Face.color;
                }
            }

            columns = 1f;
            foreach (var cell in cells)
            {
                columns = Mathf.Max(columns, cell.Column + 1f);
                if (anchorFrame != null && cell.Parts.Contains(anchorFrame)) cell.IsAnchor = true;
            }
            Restart();
        }

        public void Restart()
        {
            shownAt = Time.unscaledTime;
            nextHop = shownAt + 1.2f;
            nextGlint = shownAt + 0.9f;
            if (!Application.isPlaying) return;
            foreach (var cell in cells)
            {
                foreach (var part in cell.Parts) part.localScale = Vector3.zero;
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            float t = now - shownAt;

            if (cells.Count > 0 && now >= nextHop)
            {
                PickPlain().HopAt = now;
                nextHop = now + HopEvery * Random.Range(0.7f, 1.3f);
            }
            if (cells.Count > 0 && now >= nextGlint)
            {
                PickPlain().GlintAt = now;
                nextGlint = now + GlintEvery * Random.Range(0.7f, 1.3f);
            }

            // The wave runs diagonally from top-left to bottom-right, measured in cells.
            float span = columns + 8f;
            float wave = ((t % WavePeriod) / WavePeriod) * span - 2f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 3f);

            foreach (var cell in cells)
            {
                float scale = OutBack(Mathf.Clamp01((t - cell.Delay) / CascadeTime));

                float hop = Mathf.Clamp01((now - cell.HopAt) / HopTime);
                if (hop < 1f) scale *= 1f + 0.14f * Mathf.Sin(hop * Mathf.PI);
                if (cell.IsAnchor) scale *= 1f + 0.06f * pulse;

                foreach (var part in cell.Parts) part.localScale = Vector3.one * scale;

                if (cell.Face == null || cell.IsAnchor) continue;
                float lit = Mathf.Clamp01(1f - Mathf.Abs(cell.Row + cell.Column - wave) / WaveWidth);
                float glint = Mathf.Clamp01((now - cell.GlintAt) / GlintTime);
                float gold = glint < 1f ? Mathf.Sin(glint * Mathf.PI) : 0f;
                var color = Color.Lerp(cell.Base, WaveColor, Smooth(lit) * 0.35f);
                color = Color.Lerp(color, GlintColor, gold * 0.55f);
                color.a = cell.Base.a;
                cell.Face.color = color;
            }

            if (play != null) play.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(now * 3.4f));
        }

        Cell PickPlain()
        {
            for (int tries = 0; tries < 6; tries++)
            {
                var cell = cells[Random.Range(0, cells.Count)];
                if (!cell.IsAnchor) return cell;
            }
            return cells[0];
        }

        static float Smooth(float x) => x * x * (3f - 2f * x);

        static float OutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }
    }
}
