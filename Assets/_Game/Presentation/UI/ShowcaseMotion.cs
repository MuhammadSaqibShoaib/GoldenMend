using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>
    /// Keeps the home screen alive: the vessel cascades in cell by cell, then floats and sways; the gold anchor
    /// shard pulses with a soft glow; gold sparkles twinkle around it; the Play button breathes. All on unscaled
    /// time, and nothing runs in edit mode, so screenshots show the resting pose.
    /// </summary>
    public sealed class ShowcaseMotion : MonoBehaviour
    {
        const int SparkleCount = 7;
        const float CascadeStep = 0.035f;
        const float CascadeTime = 0.32f;

        RectTransform area;
        RectTransform play;
        RectTransform vessel;
        RectTransform anchorTile;
        RectTransform anchorShard;
        Image glow;
        readonly List<(RectTransform cell, float delay)> cells = new List<(RectTransform, float)>();
        readonly List<Sparkle> sparkles = new List<Sparkle>();
        float shownAt;
        Vector2 vesselRest;

        sealed class Sparkle
        {
            public Image Image;
            public float Born;
            public float Life;
            public float Size;
        }

        public static ShowcaseMotion Attach(UiBuilder b, RectTransform area, RectTransform play)
        {
            var motion = area.gameObject.AddComponent<ShowcaseMotion>();
            motion.area = area;
            motion.play = play;
            for (int i = 0; i < SparkleCount; i++)
            {
                var image = b.Image($"Sparkle {i + 1}", area, b.Theme.Star, UiBuilder.WithAlpha(b.Theme.Gold, 0f), Anchor.Center, Vector2.zero, new Vector2(40f, 40f));
                motion.sparkles.Add(new Sparkle { Image = image, Born = -10f });
            }
            return motion;
        }

        /// <summary>Takes over a freshly built vessel preview: adds the anchor glow and restarts the entrance.</summary>
        public void Track(RectTransform preview)
        {
            vessel = preview;
            vesselRest = preview.anchoredPosition;
            cells.Clear();
            anchorTile = null;
            anchorShard = null;
            glow = null;

            float top = float.MinValue;
            foreach (RectTransform child in preview)
            {
                if (child.name.StartsWith("Shard")) top = Mathf.Max(top, child.anchoredPosition.y);
            }
            foreach (RectTransform child in preview)
            {
                if (child.name == "Anchor Gold") anchorTile = child;
                // Every cell (and its frame) drops in by row, top first.
                float row = (top - child.anchoredPosition.y) / Mathf.Max(1f, child.sizeDelta.y);
                float column = child.anchoredPosition.x / Mathf.Max(1f, child.sizeDelta.x);
                cells.Add((child, (row + column * 0.35f + 4f) * CascadeStep));
            }

            if (anchorTile != null)
            {
                foreach (RectTransform child in preview)
                {
                    if (child.name.StartsWith("Shard") && (child.anchoredPosition - anchorTile.anchoredPosition).sqrMagnitude < 1f) anchorShard = child;
                }
                glow = new GameObject("Anchor Glow", typeof(RectTransform)).AddComponent<Image>();
                glow.rectTransform.SetParent(preview, false);
                glow.rectTransform.SetAsFirstSibling();
                glow.rectTransform.anchoredPosition = anchorTile.anchoredPosition;
                glow.rectTransform.sizeDelta = anchorTile.sizeDelta * 3.2f;
                glow.sprite = UiSkin.Glow;
                glow.color = UiBuilder.WithAlpha(UiTheme.Hex(0xFFD54A), 0.45f);
                glow.raycastTarget = false;
            }

            Restart();
        }

        public void Restart()
        {
            shownAt = Time.unscaledTime;
            if (!Application.isPlaying) return;
            foreach (var (cell, _) in cells) cell.localScale = Vector3.zero;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            float t = now - shownAt;

            foreach (var (cell, delay) in cells)
            {
                if (cell == null) continue;
                float k = Mathf.Clamp01((t - delay) / CascadeTime);
                cell.localScale = Vector3.one * OutBack(k);
            }

            if (vessel != null)
            {
                vessel.anchoredPosition = vesselRest + new Vector2(0f, Mathf.Sin(now * 1.3f) * 12f);
                vessel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 0.8f) * 1.6f);
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 3f);
            if (anchorTile != null) anchorTile.localScale *= 1f + 0.06f * pulse;
            if (anchorShard != null) anchorShard.localScale *= 1f + 0.06f * pulse;
            if (glow != null)
            {
                glow.color = UiBuilder.WithAlpha(glow.color, 0.25f + 0.4f * pulse);
                glow.rectTransform.localScale = Vector3.one * (0.9f + 0.2f * pulse);
            }

            if (play != null) play.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(now * 3.4f));

            foreach (var s in sparkles) UpdateSparkle(s, now);
        }

        void UpdateSparkle(Sparkle s, float now)
        {
            float age = now - s.Born;
            if (age > s.Life)
            {
                // Respawn somewhere on the card's rim area, away from the middle of the vessel.
                var half = area.rect.size * 0.5f;
                var dir = Random.insideUnitCircle.normalized;
                var spot = new Vector2(dir.x * half.x * Random.Range(0.55f, 0.92f), dir.y * half.y * Random.Range(0.55f, 0.9f));
                s.Image.rectTransform.anchoredPosition = spot;
                s.Born = now + Random.Range(0f, 1.2f);
                s.Life = Random.Range(1.1f, 1.8f);
                s.Size = Random.Range(26f, 52f);
                s.Image.color = UiBuilder.WithAlpha(s.Image.color, 0f);
                return;
            }
            if (age < 0f) return;
            float k = Mathf.Sin(Mathf.Clamp01(age / s.Life) * Mathf.PI);
            s.Image.color = UiBuilder.WithAlpha(s.Image.color, k * 0.9f);
            s.Image.rectTransform.sizeDelta = Vector2.one * s.Size * (0.4f + 0.6f * k);
            s.Image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, age * 90f);
        }

        static float OutBack(float t)
        {
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }
    }
}
