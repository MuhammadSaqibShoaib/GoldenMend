using System.Linq;
using GoldenMend.Core;
using GoldenMend.Data;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    public class SolverTests
    {
        // 12
        [Test]
        public void Solver_FindsWinAndMinimumBenchPeak()
        {
            // One stack, bottom to top: shard 0, 1, 2. The top two must park before shard 0 opens the chain.
            var level = TestLevels.Line(3, benchSize: 2, coveredBy: TestLevels.Stack(0, 1, 2));

            var result = Solver.Solve(level);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.MinBenchPeak, Is.EqualTo(2));
            Assert.That(result.TapOrder, Is.EqualTo(new[] { 2, 1, 0 }));
        }

        [Test]
        public void Solver_ReportsImpossibleLevel()
        {
            var level = TestLevels.Line(3, benchSize: 1, coveredBy: TestLevels.Stack(0, 1, 2));

            var result = Solver.Solve(level);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Unsolvable));
        }

        [Test]
        public void Solver_TapOrderReplaysToAWin()
        {
            var level = TestLevels.Line(5, coveredBy: TestLevels.Stack(0, 3, 1));
            var result = Solver.Solve(level);
            var engine = new PuzzleEngine(level);

            foreach (int shard in result.TapOrder) engine.Tap(shard);

            Assert.That(engine.State.Status, Is.EqualTo(GameStatus.Won));
        }

        [Test]
        public void Solver_PrefersLowerBenchPeak()
        {
            // Shards 0 and 1 are free; tapping 0 first locks both with no parking.
            var level = TestLevels.Line(2);

            var result = Solver.Solve(level);

            Assert.That(result.MinBenchPeak, Is.EqualTo(0));
            Assert.That(result.TapOrder.First(), Is.EqualTo(0));
        }

        [Test]
        public void HintMove_PrefersAShardThatLocksRightNow()
        {
            // Shards 0 (beside the anchor) and 2 (two sockets away) are both open; only 0 locks straight in.
            var engine = new PuzzleEngine(TestLevels.Line(3));

            Assert.That(Solver.HintMove(engine), Is.EqualTo(0));
        }

        [Test]
        public void HintMove_FallsBackToTheSolverWhenNothingCanLockYet()
        {
            var engine = new PuzzleEngine(TestLevels.Line(3, benchSize: 2, coveredBy: TestLevels.Stack(0, 1, 2)));

            Assert.That(Solver.HintMove(engine), Is.EqualTo(2));
        }

        [Test]
        public void HintMove_IsAlwaysATappableShard()
        {
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                var engine = new PuzzleEngine(LevelCatalog.Build(i).Def);
                int hint = Solver.HintMove(engine);
                Assert.That(engine.CanTap(hint), Is.True, LevelCatalog.Spec(i).Name);
            }
        }

        [Test]
        public void Hint_PointsAtAShardThatKeepsTheLevelWinnable()
        {
            var level = TestLevels.Line(3, benchSize: 2, coveredBy: TestLevels.Stack(0, 1, 2));
            var engine = new PuzzleEngine(level);

            Assert.That(Solver.BestMove(engine), Is.EqualTo(2));
        }
    }

    public class LevelDefTests
    {
        [Test]
        public void Validate_AcceptsWellFormedLevel()
        {
            Assert.That(TestLevels.Line(4).Validate(), Is.Empty);
        }

        [Test]
        public void Validate_RejectsCrackedShardWithOneNeighbour()
        {
            // The last socket of a line has a single neighbour, so a cracked shard there can never lock.
            var level = TestLevels.Line(3, cracked: new[] { 2 });

            Assert.That(level.Validate(), Has.Some.Contains("cracked"));
        }

        [Test]
        public void Validate_RejectsCoverCycle()
        {
            var level = TestLevels.Line(2, coveredBy: new System.Collections.Generic.Dictionary<int, int[]>
            {
                { 0, new[] { 1 } },
                { 1, new[] { 0 } },
            });

            Assert.That(level.Validate(), Has.Some.Contains("cycle"));
        }
    }

    public class LevelCatalogTests
    {
        [Test]
        public void EveryCatalogLevel_BuildsValidAndSolvableWithinItsDifficultyBand()
        {
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                var built = LevelCatalog.Build(i);
                var spec = built.Spec;

                Assert.That(built.Def.Validate(), Is.Empty, spec.Name);
                Assert.That(built.Solution.Status, Is.EqualTo(SolveStatus.Solved), spec.Name);
                Assert.That(built.Solution.MinBenchPeak, Is.InRange(spec.MinBenchPeak, spec.MaxBenchPeak), spec.Name);
            }
        }

        [Test]
        public void EveryLevel_HasAGlaze()
        {
            string[] patterns = { "seigaiha", "sakura", "kikko", "uroko", "kanoko", "shippo" };
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                var spec = LevelCatalog.Spec(i);
                bool hasGlaze = !string.IsNullOrEmpty(spec.Artwork) || System.Array.IndexOf(patterns, spec.Pattern) >= 0;
                Assert.That(hasGlaze, Is.True, spec.Name);
            }
        }

        [Test]
        public void ShardCount_NeverShrinksAsLevelsProgress()
        {
            int previous = 0;
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                int shards = LevelCatalog.Build(i).Def.Shards.Count;
                Assert.That(shards, Is.GreaterThanOrEqualTo(previous), LevelCatalog.Spec(i).Name);
                previous = shards;
            }
        }

        [Test]
        public void CatalogLevels_AreDeterministic()
        {
            var a = LevelCatalog.Build(3);
            var b = LevelCatalog.Build(3);

            Assert.That(a.Pile.ShardCell, Is.EqualTo(b.Pile.ShardCell));
        }
    }
}
