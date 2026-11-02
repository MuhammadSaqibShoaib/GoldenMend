using System.Collections.Generic;
using GoldenMend.Core;

namespace GoldenMend.Tests
{
    /// <summary>Tiny hand-built levels for rule tests. Sockets form a line: 0 (anchor) - 1 - 2 - ... - n.</summary>
    internal static class TestLevels
    {
        /// <summary>
        /// A line vessel with one shard per non-anchor socket. Shard i targets socket i + 1.
        /// Every shard sits alone on the pile (nothing covered) unless <paramref name="coveredBy"/> says otherwise.
        /// </summary>
        public static LevelDef Line(int shardCount, int benchSize = LevelDef.DefaultBenchSize,
            Dictionary<int, int[]> coveredBy = null, int[] cracked = null,
            int lockedBenchSlots = 0, int placementsPerUnlock = 3, int[][] extraEdges = null, PileGrid pile = null)
        {
            int socketCount = shardCount + 1;
            var neighbours = new List<int>[socketCount];
            for (int i = 0; i < socketCount; i++) neighbours[i] = new List<int>();
            for (int i = 0; i + 1 < socketCount; i++)
            {
                neighbours[i].Add(i + 1);
                neighbours[i + 1].Add(i);
            }
            if (extraEdges != null)
            {
                foreach (var e in extraEdges)
                {
                    neighbours[e[0]].Add(e[1]);
                    neighbours[e[1]].Add(e[0]);
                }
            }

            var shards = new ShardDef[shardCount];
            for (int i = 0; i < shardCount; i++)
            {
                int[] covers = coveredBy != null && coveredBy.TryGetValue(i, out var c) ? c : new int[0];
                bool isCracked = cracked != null && System.Array.IndexOf(cracked, i) >= 0;
                shards[i] = new ShardDef(i, i + 1, covers, isCracked);
            }

            var adjacency = new int[socketCount][];
            for (int i = 0; i < socketCount; i++) adjacency[i] = neighbours[i].ToArray();
            return new LevelDef(socketCount, 0, adjacency, shards, benchSize, lockedBenchSlots, placementsPerUnlock, pile);
        }

        /// <summary>
        /// A pile grid from text rows, top row first: a digit is the shard with that id, '#' a wall, '.' empty.
        /// The top edge is the way out.
        /// </summary>
        public static PileGrid Grid(params string[] rows)
        {
            int columns = rows[0].Length;
            var walls = new bool[columns * rows.Length];
            var cells = new Dictionary<int, int>();
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    char ch = rows[r][c];
                    int cell = r * columns + c;
                    if (ch == '#') walls[cell] = true;
                    else if (char.IsDigit(ch)) cells[ch - '0'] = cell;
                }
            }
            var shardCell = new int[cells.Count];
            foreach (var pair in cells) shardCell[pair.Key] = pair.Value;
            return new PileGrid(columns, rows.Length, walls, shardCell);
        }

        /// <summary>Shard ids in a single stack, bottom first: each shard is covered by the next one.</summary>
        public static Dictionary<int, int[]> Stack(params int[] bottomToTop)
        {
            var map = new Dictionary<int, int[]>();
            for (int i = 0; i + 1 < bottomToTop.Length; i++)
                map[bottomToTop[i]] = new[] { bottomToTop[i + 1] };
            return map;
        }
    }
}
