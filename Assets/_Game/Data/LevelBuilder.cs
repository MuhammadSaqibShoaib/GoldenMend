using System;
using System.Collections.Generic;
using GoldenMend.Core;

namespace GoldenMend.Data
{
    /// <summary>
    /// Turns a <see cref="LevelSpec"/> into a playable level: parses the vessel shape, deals shards into the
    /// pile grid, and keeps the first deal the solver accepts within the spec's difficulty band.
    /// </summary>
    public static class LevelBuilder
    {
        public const int MaxAttempts = 400;

        public static BuiltLevel Build(LevelSpec spec)
        {
            ParseShape(spec.Shape, out int columns, out int rows, out var socketColumn, out var socketRow, out int anchor);
            int socketCount = socketColumn.Length;
            var neighbours = GridNeighbours(socketColumn, socketRow);

            var shardSockets = new List<int>();
            for (int s = 0; s < socketCount; s++)
            {
                if (s != anchor) shardSockets.Add(s);
            }
            int shardCount = shardSockets.Count;

            ParsePile(spec, shardCount, out int pileColumns, out int pileRows, out var pileWalls, out var pileSlots);

            for (int attempt = spec.Deal; attempt < spec.Deal + MaxAttempts; attempt++)
            {
                var rng = new XorShift((uint)(spec.Seed * 7919 + attempt * 104729 + 1));

                var order = new int[shardCount];
                for (int i = 0; i < shardCount; i++) order[i] = i;
                rng.Shuffle(order);

                var shardCell = new int[shardCount];
                for (int i = 0; i < shardCount; i++) shardCell[order[i]] = pileSlots[i];
                var pile = new PileGrid(pileColumns, pileRows, (bool[])pileWalls.Clone(), shardCell);

                var cracked = PickCracked(spec.CrackedCount, shardSockets, neighbours, rng);

                var shards = new ShardDef[shardCount];
                for (int i = 0; i < shardCount; i++)
                {
                    shards[i] = new ShardDef(i, shardSockets[i], null, cracked.Contains(i));
                }

                var def = new LevelDef(socketCount, anchor, neighbours, shards, spec.BenchSize, spec.LockedBenchSlots, 3, pile);
                if (def.Validate().Count > 0) continue;

                var solution = Solver.Solve(def);
                if (solution.Status != SolveStatus.Solved) continue;
                if (solution.MinBenchPeak < spec.MinBenchPeak || solution.MinBenchPeak > spec.MaxBenchPeak) continue;

                return new BuiltLevel(spec, def, columns, rows, socketColumn, socketRow, solution, attempt);
            }

            throw new InvalidOperationException(
                $"Level '{spec.Name}': no deal in {MaxAttempts} attempts was solvable with a bench peak of {spec.MinBenchPeak}-{spec.MaxBenchPeak}.");
        }

        static void ParseShape(string[] shape, out int columns, out int rows, out int[] socketColumn, out int[] socketRow, out int anchor)
        {
            if (shape == null || shape.Length == 0) throw new ArgumentException("Level shape is empty.");

            rows = shape.Length;
            columns = 0;
            foreach (string line in shape) columns = Math.Max(columns, line.Length);

            var cols = new List<int>();
            var rowList = new List<int>();
            anchor = -1;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < shape[r].Length; c++)
                {
                    char ch = shape[r][c];
                    if (ch == '.') continue;
                    if (ch != 'X' && ch != 'A') throw new ArgumentException($"Unknown shape character '{ch}' at row {r}, column {c}.");
                    if (ch == 'A')
                    {
                        if (anchor >= 0) throw new ArgumentException("Level shape has more than one anchor.");
                        anchor = cols.Count;
                    }
                    cols.Add(c);
                    rowList.Add(r);
                }
            }
            if (anchor < 0) throw new ArgumentException("Level shape has no anchor ('A').");

            socketColumn = cols.ToArray();
            socketRow = rowList.ToArray();
        }

        static void ParsePile(LevelSpec spec, int shardCount, out int columns, out int rows, out bool[] walls, out int[] slots)
        {
            var shape = spec.PileShape;
            if (shape == null || shape.Length == 0) throw new ArgumentException($"Level '{spec.Name}' has no pile shape.");

            rows = shape.Length;
            columns = 0;
            foreach (string line in shape) columns = Math.Max(columns, line.Length);

            walls = new bool[columns * rows];
            var slotList = new List<int>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    char ch = c < shape[r].Length ? shape[r][c] : '.';
                    int cell = r * columns + c;
                    if (ch == '#') walls[cell] = true;
                    else if (ch == 'o') slotList.Add(cell);
                    else if (ch != '.') throw new ArgumentException($"Level '{spec.Name}': unknown pile character '{ch}'.");
                }
            }

            if (slotList.Count != shardCount)
                throw new ArgumentException($"Level '{spec.Name}': the pile has {slotList.Count} 'o' cells but the vessel has {shardCount} shards.");
            slots = slotList.ToArray();
        }

        static int[][] GridNeighbours(int[] socketColumn, int[] socketRow)
        {
            int count = socketColumn.Length;
            var result = new int[count][];
            for (int a = 0; a < count; a++)
            {
                var list = new List<int>();
                for (int b = 0; b < count; b++)
                {
                    if (a == b) continue;
                    int dc = Math.Abs(socketColumn[a] - socketColumn[b]);
                    int dr = Math.Abs(socketRow[a] - socketRow[b]);
                    if (dc + dr == 1) list.Add(b);
                }
                result[a] = list.ToArray();
            }
            return result;
        }

        static HashSet<int> PickCracked(int count, List<int> shardSockets, int[][] neighbours, XorShift rng)
        {
            var candidates = new List<int>();
            for (int shard = 0; shard < shardSockets.Count; shard++)
            {
                if (neighbours[shardSockets[shard]].Length >= 2) candidates.Add(shard);
            }
            var picked = new HashSet<int>();
            while (picked.Count < count && candidates.Count > 0)
            {
                int index = rng.Next(candidates.Count);
                picked.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return picked;
        }

        /// <summary>Small seeded generator so level deals are identical on every runtime and platform.</summary>
        sealed class XorShift
        {
            uint state;

            public XorShift(uint seed) => state = seed == 0 ? 0x9E3779B9u : seed;

            public int Next(int maxExclusive)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (int)(state % (uint)maxExclusive);
            }

            public void Shuffle(int[] items)
            {
                for (int i = items.Length - 1; i > 0; i--)
                {
                    int j = Next(i + 1);
                    (items[i], items[j]) = (items[j], items[i]);
                }
            }
        }
    }
}
