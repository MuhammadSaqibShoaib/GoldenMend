using GoldenMend.Core;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    /// <summary>The continue offer after a bench-full fail: extra slots, and the stuck shard parks in one.</summary>
    public class ContinueTests
    {
        static PuzzleEngine FailedLine()
        {
            var engine = new PuzzleEngine(TestLevels.Line(7));
            for (int shard = 6; shard >= 1; shard--) engine.Tap(shard); // 5 park, the 6th fails
            Assume.That(engine.State.Status, Is.EqualTo(GameStatus.Failed));
            return engine;
        }

        [Test]
        public void Continue_AddsSlotsAndParksTheStuckShard()
        {
            var engine = FailedLine();

            var events = engine.Continue(2);

            Assert.That(events, Is.EqualTo(new[]
            {
                GameEvent.BenchExpanded(7),
                GameEvent.Parked(1, 5),
            }));
            Assert.That(engine.State.Status, Is.EqualTo(GameStatus.Playing));
            Assert.That(engine.State.Bench.Length, Is.EqualTo(7));
            Assert.That(engine.State.PendingShard, Is.EqualTo(-1));
        }

        [Test]
        public void AfterContinue_TheLevelCanStillBeWon()
        {
            var engine = FailedLine();
            engine.Continue(2);

            var events = engine.Tap(0);

            Assert.That(events, Does.Contain(GameEvent.Won()));
        }

        [Test]
        public void Continue_IsRejectedWhileStillPlaying()
        {
            var engine = new PuzzleEngine(TestLevels.Line(3));

            Assert.That(engine.Continue(2), Is.Empty);
            Assert.That(engine.State.Bench.Length, Is.EqualTo(LevelDef.DefaultBenchSize));
        }

        [Test]
        public void Solver_WorksOnAnExpandedBench()
        {
            var engine = FailedLine();
            engine.Continue(2);

            Assert.That(Solver.BestMove(engine), Is.EqualTo(0));
        }
    }
}
