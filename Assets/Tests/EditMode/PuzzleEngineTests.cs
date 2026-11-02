using System.Linq;
using GoldenMend.Core;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    /// <summary>One test per GDD rule, in the order the Technical Design lists them.</summary>
    public class PuzzleEngineTests
    {
        // 1
        [Test]
        public void TapShardTouchingAnchor_LocksAndFormsOneSeam()
        {
            var engine = new PuzzleEngine(TestLevels.Line(2));

            var events = engine.Tap(0);

            Assert.That(events, Is.EqualTo(new[]
            {
                GameEvent.Locked(0, 1),
                GameEvent.Seam(1, 0),
            }));
            Assert.That(engine.State.Placed[1], Is.True);
            Assert.That(engine.State.InPile[0], Is.False);
        }

        // 2
        [Test]
        public void TapShardWithNoPlacedNeighbour_ParksInLeftmostFreeSlot()
        {
            var engine = new PuzzleEngine(TestLevels.Line(3));

            var first = engine.Tap(2);
            var second = engine.Tap(1);

            Assert.That(first, Is.EqualTo(new[] { GameEvent.Parked(2, 0) }));
            Assert.That(second, Is.EqualTo(new[] { GameEvent.Parked(1, 1) }));
            Assert.That(engine.State.Bench.Take(2), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(engine.State.Placed[3], Is.False);
        }

        // 3
        [Test]
        public void TapCoveredShard_IsRejectedAndChangesNothing()
        {
            var engine = new PuzzleEngine(TestLevels.Line(2, coveredBy: TestLevels.Stack(0, 1)));

            var events = engine.Tap(0);

            Assert.That(events, Is.Empty);
            Assert.That(engine.CanTap(0), Is.False);
            Assert.That(engine.State.InPile[0], Is.True);
            Assert.That(engine.State.BenchCount, Is.EqualTo(0));
        }

        // 4
        [Test]
        public void RemovingShard_UncoversShardBeneath()
        {
            var engine = new PuzzleEngine(TestLevels.Line(2, coveredBy: TestLevels.Stack(0, 1)));

            var events = engine.Tap(1);

            Assert.That(events.First(), Is.EqualTo(GameEvent.Uncovered(0)));
            Assert.That(engine.CanTap(0), Is.True);
        }

        // 5
        [Test]
        public void Lock_AutoFliesParkedShardThatNowHasNeighbour()
        {
            var engine = new PuzzleEngine(TestLevels.Line(2));
            engine.Tap(1); // socket 2 has no placed neighbour yet: parks in slot 0

            var events = engine.Tap(0);

            Assert.That(events, Is.EqualTo(new[]
            {
                GameEvent.Locked(0, 1),
                GameEvent.Seam(1, 0),
                GameEvent.AutoFlown(1, 2, 0),
                GameEvent.Seam(2, 1),
                GameEvent.Won(),
            }));
            Assert.That(engine.State.Bench[0], Is.EqualTo(-1));
        }

        // 6
        [Test]
        public void AutoFlyChain_ResolvesFullyLeftToRightPassByPass()
        {
            var engine = new PuzzleEngine(TestLevels.Line(3));
            engine.Tap(2); // slot 0, needs socket 2
            engine.Tap(1); // slot 1, needs socket 1

            var events = engine.Tap(0);

            // Pass 1 checks slot 0 (still blocked) then slot 1 (locks). Pass 2 frees slot 0.
            Assert.That(events, Is.EqualTo(new[]
            {
                GameEvent.Locked(0, 1),
                GameEvent.Seam(1, 0),
                GameEvent.AutoFlown(1, 2, 1),
                GameEvent.Seam(2, 1),
                GameEvent.AutoFlown(2, 3, 0),
                GameEvent.Seam(3, 2),
                GameEvent.Won(),
            }));
        }

        // 7
        [Test]
        public void ParkingWithFullBench_FailsLevel()
        {
            var engine = new PuzzleEngine(TestLevels.Line(7));
            for (int shard = 6; shard >= 2; shard--) engine.Tap(shard); // fills all 5 slots

            var events = engine.Tap(1);

            Assert.That(events, Is.EqualTo(new[] { GameEvent.Failed(1, FailReason.BenchFull) }));
            Assert.That(engine.State.Status, Is.EqualTo(GameStatus.Failed));
            Assert.That(engine.State.PendingShard, Is.EqualTo(1));
            Assert.That(engine.Tap(0), Is.Empty, "no taps after a fail");
        }

        // 8
        [Test]
        public void FillingLastSocket_WinsLevel()
        {
            var engine = new PuzzleEngine(TestLevels.Line(2));
            engine.Tap(0);

            var events = engine.Tap(1);

            Assert.That(events.Last(), Is.EqualTo(GameEvent.Won()));
            Assert.That(engine.State.Status, Is.EqualTo(GameStatus.Won));
        }

        [Test]
        public void SeamFormsWithEveryPlacedNeighbour()
        {
            // Socket 3 also touches the anchor, so placing it next to socket 2 makes two seams.
            var engine = new PuzzleEngine(TestLevels.Line(3, extraEdges: new[] { new[] { 0, 3 } }));
            engine.Tap(0);
            engine.Tap(1);

            var events = engine.Tap(2);

            Assert.That(events.Where(e => e.Type == GameEventType.SeamFormed).Select(e => e.OtherSocket),
                Is.EquivalentTo(new[] { 2, 0 }));
        }

        // 9
        [Test]
        public void CrackedShard_NeedsTwoPlacedNeighbours()
        {
            // Square: 0-1-2-3 plus 3-0. Shard 2 (socket 3) is cracked and touches sockets 2 and 0.
            var level = TestLevels.Line(3, cracked: new[] { 2 }, extraEdges: new[] { new[] { 3, 0 } });
            var engine = new PuzzleEngine(level);

            var parked = engine.Tap(2); // only the anchor is placed beside it: 1 neighbour is not enough
            engine.Tap(0);
            var chain = engine.Tap(1); // socket 2 placed: socket 3 now has 2 placed neighbours

            Assert.That(parked, Is.EqualTo(new[] { GameEvent.Parked(2, 0) }));
            Assert.That(chain, Does.Contain(GameEvent.AutoFlown(2, 3, 0)));
        }

        // 10
        [Test]
        public void LockedBenchSlot_OpensAfterThreePlacements()
        {
            var engine = new PuzzleEngine(TestLevels.Line(5, lockedBenchSlots: 1, placementsPerUnlock: 3));
            Assert.That(engine.State.SlotLocked, Is.EqualTo(new[] { false, false, false, false, true }));

            engine.Tap(0);
            engine.Tap(1);
            var third = engine.Tap(2);

            Assert.That(third, Does.Contain(GameEvent.SlotUnlocked(4)));
            Assert.That(engine.State.SlotLocked[4], Is.False);
        }

        [Test]
        public void LockedBenchSlot_CannotBeParkedIn()
        {
            var engine = new PuzzleEngine(TestLevels.Line(6, lockedBenchSlots: 1));
            for (int shard = 5; shard >= 2; shard--) engine.Tap(shard); // slots 0-3

            var events = engine.Tap(1);

            Assert.That(events, Is.EqualTo(new[] { GameEvent.Failed(1, FailReason.BenchFull) }));
        }

        [Test]
        public void SameTaps_GiveSameEvents()
        {
            var a = new PuzzleEngine(TestLevels.Line(4));
            var b = new PuzzleEngine(TestLevels.Line(4));
            int[] taps = { 3, 1, 0, 2 };

            var eventsA = taps.SelectMany(t => a.Tap(t)).ToList();
            var eventsB = taps.SelectMany(t => b.Tap(t)).ToList();

            Assert.That(eventsA, Is.EqualTo(eventsB));
        }
    }
}
