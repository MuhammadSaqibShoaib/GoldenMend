using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Plays animation steps one after another. The engine has already resolved the tap; this only paces
    /// how the player sees it.
    /// </summary>
    public sealed class AnimationQueue : MonoBehaviour
    {
        readonly Queue<Func<IEnumerator>> steps = new Queue<Func<IEnumerator>>();
        bool running;

        public bool Busy => running || steps.Count > 0;

        public void Enqueue(Func<IEnumerator> step)
        {
            steps.Enqueue(step);
            if (!running) StartCoroutine(Run());
        }

        /// <summary>Queues an instant action, keeping its place in the order.</summary>
        public void Enqueue(Action action) => Enqueue(() => Instant(action));

        public void Clear()
        {
            StopAllCoroutines();
            steps.Clear();
            running = false;
        }

        IEnumerator Run()
        {
            running = true;
            while (steps.Count > 0)
            {
                var step = steps.Dequeue()();
                if (step != null) yield return StartCoroutine(step);
            }
            running = false;
        }

        static IEnumerator Instant(Action action)
        {
            action();
            yield break;
        }
    }
}
