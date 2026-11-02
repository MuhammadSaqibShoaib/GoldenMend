using GoldenMend.Core;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    /// <summary>
    /// The grid pile: every shard is visible, but only shards with an open path to the top edge can be tapped.
    /// </summary>
    public class PileGridTests
    {
        [Test]
        public void TopRowShards_AreOpen_ShardsBehindThemAreLocked()
        {
            var engine = new PuzzleEngine(TestLevels.Line(3, pile: TestLevels.Grid(
                "01",
                "2.")));

            Assert.That(engine.CanTap(0), Is.True);
            Assert.That(engine.CanTap(1), Is.True);
            Assert.That(engine.IsLocked(2), Is.True);
            Assert.That(engine.Tap(2), Is.Empty);
        }

        [Test]
        public void TakingTheShardInFront_OpensTheOneBehindAndReportsIt()
        {
            var engine = new PuzzleEngine(TestLevels.Line(3, pile: TestLevels.Grid(
                "01",
                "2.")));

            var events = engine.Tap(0);

            Assert.That(events[0], Is.EqualTo(GameEvent.Uncovered(2)));
            Assert.That(engine.CanTap(2), Is.True);
        }

        [Test]
        public void ShardNextToAnOpenGap_IsOpen()
        {
            // Shard 1 sits under an empty cell that reaches the top, so it can be pulled out.
            var engine = new PuzzleEngine(TestLevels.Line(2, pile: TestLevels.Grid(
                "0.",
                "#1")));

            Assert.That(engine.CanTap(1), Is.True);
        }

        [Test]
        public void EmptyPocketCutOffFromTheTop_DoesNotOpenAShard()
        {
            // The gap beside shard 2 is boxed in by walls and shard 1, so shard 2 stays locked.
            var engine = new PuzzleEngine(TestLevels.Line(3, pile: TestLevels.Grid(
                "#0#",
                "#1#",
                "2.#")));

            Assert.That(engine.IsLocked(2), Is.True);

            engine.Tap(0);
            var events = engine.Tap(1);

            Assert.That(events, Does.Contain(GameEvent.Uncovered(2)));
        }

        [Test]
        public void OpeningAGap_CanReleaseSeveralShardsAtOnce()
        {
            // Taking shard 0 opens the gap under it, and both shards beside the gap get a way out.
            var engine = new PuzzleEngine(TestLevels.Line(4, pile: TestLevels.Grid(
                "#0#2",
                "1.3#")));

            var events = engine.Tap(0);

            Assert.That(events, Does.Contain(GameEvent.Uncovered(1)));
            Assert.That(events, Does.Contain(GameEvent.Uncovered(3)));
        }

        [Test]
        public void Solver_HandlesGridPiles()
        {
            // Shard 0 (needed first) is buried; the solver must dig through 1 and 2, parking them.
            var level = TestLevels.Line(3, benchSize: 2, pile: TestLevels.Grid(
                "#2#",
                "#1#",
                "#0#"));

            var result = Solver.Solve(level);

            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.TapOrder, Is.EqualTo(new[] { 2, 1, 0 }));
            Assert.That(result.MinBenchPeak, Is.EqualTo(2));
        }

        [Test]
        public void Validate_RejectsShardOnAWallOrSharedCell()
        {
            var onWall = TestLevels.Line(1, pile: new PileGrid(2, 1, new[] { true, false }, new[] { 0 }));
            var shared = TestLevels.Line(2, pile: new PileGrid(2, 1, new[] { false, false }, new[] { 1, 1 }));

            Assert.That(onWall.Validate(), Has.Some.Contains("wall"));
            Assert.That(shared.Validate(), Has.Some.Contains("same pile cell"));
        }
    }
}
