using System;
using System.Collections.Generic;

namespace GoldenMend.Core
{
    public enum SolveStatus
    {
        Solved,
        Unsolvable,

        /// <summary>The search hit its state limit before finishing.</summary>
        GaveUp,
    }

    public sealed class SolveResult
    {
        public SolveStatus Status { get; }

        /// <summary>The fewest workbench slots any no-booster win needs at its busiest moment. The level's main difficulty number.</summary>
        public int MinBenchPeak { get; }
        public IReadOnlyList<int> TapOrder { get; }
        public int StatesExplored { get; }

        public SolveResult(SolveStatus status, int minBenchPeak, IReadOnlyList<int> tapOrder, int statesExplored)
        {
            Status = status;
            MinBenchPeak = minBenchPeak;
            TapOrder = tapOrder;
            StatesExplored = statesExplored;
        }
    }

    /// <summary>
    /// Depth-first search over tap orders, remembering dead ends. Finds whether a level can be won without boosters
    /// and the lowest workbench peak a win needs.
    /// </summary>
    public static class Solver
    {
        public const int DefaultMaxStates = 2_000_000;

        sealed class Context
        {
            /// <summary>States already shown to have no win within <see cref="Bound"/>.</summary>
            public readonly HashSet<(ulong pile, ulong placed)> Dead = new HashSet<(ulong, ulong)>();
            public readonly List<int> Path = new List<int>();
            public int Bound;
            public int MaxStates;
            public bool GaveUp;
        }

        public static SolveResult Solve(LevelDef level, int maxStates = DefaultMaxStates) =>
            Solve(new PuzzleEngine(level), maxStates);

        /// <summary>
        /// Solves from the engine's current state. The engine itself is not changed. Asks "can it be won without the
        /// workbench ever holding more than k?" for k = 0, 1, 2 and so on; the first k that works is the minimum peak.
        /// Each question prunes every branch that goes over k, which is far cheaper than scoring every state.
        /// </summary>
        public static SolveResult Solve(PuzzleEngine from, int maxStates = DefaultMaxStates)
        {
            if (from.Level.Shards.Count > 64 || from.Level.SocketCount > 64)
                throw new NotSupportedException("The solver supports up to 64 shards and 64 sockets.");

            var start = from.Clone();
            int explored = 0;
            for (int bound = from.State.BenchCount; bound <= from.State.Bench.Length; bound++)
            {
                var ctx = new Context { Bound = bound, MaxStates = maxStates - explored };
                bool won = Search(start, ctx);
                explored += ctx.Dead.Count + ctx.Path.Count;
                if (won)
                {
                    ctx.Path.Reverse();
                    return new SolveResult(SolveStatus.Solved, bound, ctx.Path, explored);
                }
                if (ctx.GaveUp) return new SolveResult(SolveStatus.GaveUp, -1, Array.Empty<int>(), explored);
            }
            return new SolveResult(SolveStatus.Unsolvable, -1, Array.Empty<int>(), explored);
        }

        /// <summary>A tap that keeps the level winnable with the lowest bench peak, or -1 if none does.</summary>
        public static int BestMove(PuzzleEngine from, int maxStates = DefaultMaxStates)
        {
            if (from.State.Status != GameStatus.Playing) return -1;
            var result = Solve(from, maxStates);
            return result.Status == SolveStatus.Solved && result.TapOrder.Count > 0 ? result.TapOrder[0] : -1;
        }

        /// <summary>
        /// The one shard a hint points at: a shard that fits the vessel right now, even if it is still buried,
        /// choosing the one with the fewest shards in the way. Placing a fitting shard never makes a level harder,
        /// so no search is needed. Falls back to the solver's next move only if nothing in the pile fits.
        /// </summary>
        public static int HintTarget(PuzzleEngine from, int maxStates = DefaultMaxStates)
        {
            if (from.State.Status != GameStatus.Playing) return -1;

            int best = -1, bestCost = int.MaxValue;
            for (int shard = 0; shard < from.Level.Shards.Count; shard++)
            {
                if (!from.CanPull(shard)) continue;
                int cost = from.DigCost(shard);
                if (cost < bestCost)
                {
                    best = shard;
                    bestCost = cost;
                }
            }
            if (best >= 0) return best;

            int move = BestMove(from, maxStates);
            if (move >= 0) return move;
            for (int shard = 0; shard < from.Level.Shards.Count; shard++)
            {
                if (from.CanTap(shard)) return shard;
            }
            return -1;
        }

        /// <summary>The shard the pull booster lifts out: the fitting shard buried deepest, so the pull saves the most digging.</summary>
        public static int PullTarget(PuzzleEngine from)
        {
            int best = -1, bestCost = -1;
            for (int shard = 0; shard < from.Level.Shards.Count; shard++)
            {
                if (!from.CanPull(shard)) continue;
                int cost = from.DigCost(shard);
                if (cost > bestCost)
                {
                    best = shard;
                    bestCost = cost;
                }
            }
            return best;
        }

        /// <summary>Depth-first: true if a win within the bound exists from here, with its taps pushed onto the path in reverse.</summary>
        static bool Search(PuzzleEngine engine, Context ctx)
        {
            if (engine.State.Status == GameStatus.Won) return true;

            var key = Key(engine);
            if (ctx.Dead.Contains(key)) return false;
            if (ctx.Dead.Count >= ctx.MaxStates)
            {
                ctx.GaveUp = true;
                return false;
            }

            // Shards that fit the vessel go first: placing one never makes the rest harder, so wins turn up early.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int shard = 0; shard < engine.Level.Shards.Count; shard++)
                {
                    if (!engine.CanTap(shard) || engine.CanPull(shard) != (pass == 0)) continue;

                    var next = engine.Clone();
                    next.Tap(shard);
                    if (next.State.Status == GameStatus.Failed || next.State.BenchCount > ctx.Bound) continue;

                    if (Search(next, ctx))
                    {
                        ctx.Path.Add(shard);
                        return true;
                    }
                    if (ctx.GaveUp) return false;
                }
            }

            ctx.Dead.Add(key);
            return false;
        }

        // The bench holds exactly the shards that left the pile but are not placed, and placements
        // follow from the placed sockets, so pile + placed fully identify a state for solving.
        static (ulong pile, ulong placed) Key(PuzzleEngine engine)
        {
            ulong pile = 0;
            var inPile = engine.State.InPile;
            for (int i = 0; i < inPile.Length; i++)
            {
                if (inPile[i]) pile |= 1UL << i;
            }

            ulong placed = 0;
            var placedSockets = engine.State.Placed;
            for (int i = 0; i < placedSockets.Length; i++)
            {
                if (placedSockets[i]) placed |= 1UL << i;
            }
            return (pile, placed);
        }
    }
}
