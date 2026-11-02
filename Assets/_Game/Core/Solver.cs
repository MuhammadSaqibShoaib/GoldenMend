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
    /// Depth-first search over tap orders with memoisation. Finds whether a level can be won without boosters
    /// and the lowest workbench peak a win needs.
    /// </summary>
    public static class Solver
    {
        const int Unsolvable = int.MaxValue;
        public const int DefaultMaxStates = 2_000_000;

        sealed class Context
        {
            public readonly Dictionary<(ulong pile, ulong placed), (int cost, int move)> Memo =
                new Dictionary<(ulong, ulong), (int, int)>();
            public int MaxStates;
            public bool GaveUp;
        }

        public static SolveResult Solve(LevelDef level, int maxStates = DefaultMaxStates) =>
            Solve(new PuzzleEngine(level), maxStates);

        /// <summary>Solves from the engine's current state. The engine itself is not changed.</summary>
        public static SolveResult Solve(PuzzleEngine from, int maxStates = DefaultMaxStates)
        {
            if (from.Level.Shards.Count > 64 || from.Level.SocketCount > 64)
                throw new NotSupportedException("The solver supports up to 64 shards and 64 sockets.");

            var ctx = new Context { MaxStates = maxStates };
            var start = from.Clone();
            int cost = Search(start, ctx);

            if (cost == Unsolvable)
            {
                var status = ctx.GaveUp ? SolveStatus.GaveUp : SolveStatus.Unsolvable;
                return new SolveResult(status, -1, Array.Empty<int>(), ctx.Memo.Count);
            }

            var order = new List<int>();
            var walk = from.Clone();
            while (walk.State.Status == GameStatus.Playing)
            {
                int move = ctx.Memo[Key(walk)].move;
                order.Add(move);
                walk.Tap(move);
            }

            int peak = Math.Max(cost, from.State.BenchCount);
            return new SolveResult(SolveStatus.Solved, peak, order, ctx.Memo.Count);
        }

        /// <summary>A tap that keeps the level winnable with the lowest bench peak, or -1 if none does.</summary>
        public static int BestMove(PuzzleEngine from, int maxStates = DefaultMaxStates)
        {
            if (from.State.Status != GameStatus.Playing) return -1;
            var result = Solve(from, maxStates);
            return result.Status == SolveStatus.Solved && result.TapOrder.Count > 0 ? result.TapOrder[0] : -1;
        }

        static int Search(PuzzleEngine engine, Context ctx)
        {
            if (engine.State.Status == GameStatus.Won) return 0;

            var key = Key(engine);
            if (ctx.Memo.TryGetValue(key, out var known)) return known.cost;
            if (ctx.Memo.Count >= ctx.MaxStates)
            {
                ctx.GaveUp = true;
                return Unsolvable;
            }

            int best = Unsolvable;
            int bestMove = -1;
            for (int shard = 0; shard < engine.Level.Shards.Count; shard++)
            {
                if (!engine.CanTap(shard)) continue;

                var next = engine.Clone();
                next.Tap(shard);
                if (next.State.Status == GameStatus.Failed) continue;

                int rest = Search(next, ctx);
                if (rest == Unsolvable) continue;

                int cost = Math.Max(next.State.BenchCount, rest);
                if (cost < best)
                {
                    best = cost;
                    bestMove = shard;
                }
            }

            ctx.Memo[key] = (best, bestMove);
            return best;
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
