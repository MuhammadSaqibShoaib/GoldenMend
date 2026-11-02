using System.Collections;
using GoldenMend.Core;
using GoldenMend.Data;
using GoldenMend.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GoldenMend.Tests
{
    /// <summary>Loads real levels into the presenter and plays them through, animations included.</summary>
    public class BoardPresenterSmokeTests
    {
        GameObject cameraObject;
        GameObject boardObject;
        BoardPresenter board;
        GameStatus? shown;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Test Camera") { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>().orthographic = true;

            boardObject = new GameObject("Test Board");
            boardObject.AddComponent<AnimationQueue>();
            boardObject.AddComponent<FeedbackService>().Muted = true;
            board = boardObject.AddComponent<BoardPresenter>();
            board.AnimationSpeed = 10f;
            board.AcceptPointerInput = false;
            shown = null;
            board.StatusShown += status => shown = status;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(boardObject);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator SolverTapOrder_PlaysEveryLevelThroughToTheWinScreen()
        {
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                var level = LevelCatalog.Build(i);
                shown = null;
                board.Load(level);

                foreach (int shard in level.Solution.TapOrder)
                {
                    Assert.That(board.TryTap(shard), Is.True, $"{level.Spec.Name}: tap on shard {shard}");
                    yield return null;
                }

                yield return WaitForShownStatus(20f);
                Assert.That(shown, Is.EqualTo(GameStatus.Won), level.Spec.Name);
                Assert.That(board.BenchPeak, Is.EqualTo(level.Solution.MinBenchPeak), level.Spec.Name);
                Assert.That(board.IsAnimating, Is.False, level.Spec.Name);
            }
        }

        [UnityTest]
        public IEnumerator GreedyParking_ShowsTheFailScreen()
        {
            var level = LevelCatalog.Build(3);
            board.Load(level);
            var engine = board.Engine;

            // Always tap a shard that will park, while one exists, until the bench overflows.
            while (engine.State.Status == GameStatus.Playing)
            {
                int pick = -1;
                for (int s = 0; s < level.Def.Shards.Count; s++)
                {
                    if (!engine.CanTap(s)) continue;
                    if (pick < 0 || !engine.CanLock(s)) pick = s;
                    if (!engine.CanLock(s)) break;
                }
                board.TryTap(pick);
                yield return null;
            }

            yield return WaitForShownStatus(20f);
            Assert.That(shown, Is.EqualTo(engine.State.Status));
        }

        [UnityTest]
        public IEnumerator Hint_RunsWithoutChangingTheBoard()
        {
            board.Load(LevelCatalog.Build(2));
            int before = board.TapCount;

            board.ShowHint();
            yield return null;

            Assert.That(board.TapCount, Is.EqualTo(before));
            Assert.That(board.Engine.State.Status, Is.EqualTo(GameStatus.Playing));
        }

        IEnumerator WaitForShownStatus(float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (shown == null && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
