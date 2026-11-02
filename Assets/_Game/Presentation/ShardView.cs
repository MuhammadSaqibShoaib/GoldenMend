using System.Collections;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>One shard on screen. Holds no game state; the presenter tells it where to go.</summary>
    public sealed class ShardView : MonoBehaviour
    {
        public int ShardId { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public float Size => transform.localScale.x;

        public static ShardView Create(Transform parent, int shardId, Sprite sprite, Vector3 position, float size, int sortingOrder)
        {
            var go = new GameObject($"Shard {shardId}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ShardView>();
            view.ShardId = shardId;
            view.Renderer = go.AddComponent<SpriteRenderer>();
            view.Renderer.sprite = sprite;
            view.Renderer.sortingOrder = sortingOrder;
            view.transform.position = position;
            view.SetSize(size);
            return view;
        }

        readonly System.Collections.Generic.List<SpriteRenderer> marks = new System.Collections.Generic.List<SpriteRenderer>();

        public void SetSize(float size) => transform.localScale = new Vector3(size, size, 1f);

        /// <summary>A decal drawn on top of the shard (for example the crack line on a cracked shard).</summary>
        public SpriteRenderer AddMark(Sprite sprite, Color color, Vector2 localSize, float rotation)
        {
            var go = new GameObject("Mark");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            go.transform.localScale = new Vector3(localSize.x, localSize.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = Renderer.sortingOrder + 1;
            marks.Add(sr);
            return sr;
        }

        public void SetMarksVisible(bool visible)
        {
            foreach (var mark in marks) mark.enabled = visible;
        }

        public void SetMarkColor(Color color)
        {
            foreach (var mark in marks) mark.color = color;
        }

        public void SetSortingOrder(int order)
        {
            Renderer.sortingOrder = order;
            foreach (var mark in marks) mark.sortingOrder = order + 1;
        }

        public bool Contains(Vector2 worldPoint)
        {
            Vector2 delta = worldPoint - (Vector2)transform.position;
            float half = Size * 0.5f;
            return Mathf.Abs(delta.x) <= half && Mathf.Abs(delta.y) <= half;
        }

        public IEnumerator FlyTo(Vector3 target, float size, float duration, int landedSortingOrder, float arc = 0.8f)
        {
            Vector3 start = transform.position;
            float startSize = Size;
            SetSortingOrder(200);

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float e = Ease.OutCubic(t);
                Vector3 p = Vector3.LerpUnclamped(start, target, e);
                p.y += Mathf.Sin(t * Mathf.PI) * arc;
                transform.position = p;
                SetSize(Mathf.Lerp(startSize, size, e));
                yield return null;
            }

            transform.position = target;
            SetSize(size);
            SetSortingOrder(landedSortingOrder);
        }

        public IEnumerator TintTo(Color target, float duration)
        {
            Color start = Renderer.color;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                Renderer.color = Color.Lerp(start, target, t);
                yield return null;
            }
            Renderer.color = target;
        }

        public IEnumerator Punch(float amount, float duration)
        {
            float baseSize = Size;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                SetSize(baseSize * (1f + Mathf.Sin(t * Mathf.PI) * amount));
                yield return null;
            }
            SetSize(baseSize);
        }

        public IEnumerator Shake(float strength, float duration)
        {
            Vector3 origin = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = origin + new Vector3(Mathf.Sin(t * 60f) * strength * (1f - t), 0f, 0f);
                yield return null;
            }
            transform.position = origin;
        }
    }

    public static class Ease
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
