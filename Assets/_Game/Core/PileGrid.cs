using System;

namespace GoldenMend.Core
{
    /// <summary>
    /// The pile as a visible grid. Every shard can be seen, but a shard can only be pulled out when it has an
    /// open path to the top edge: it is in the top row, or it touches an empty cell connected to the top.
    /// </summary>
    public sealed class PileGrid
    {
        public int Columns { get; }
        public int Rows { get; }

        /// <summary>Per cell (row-major, top row first): true for a wall that nothing passes through.</summary>
        public bool[] Walls { get; }

        /// <summary>The cell each shard starts in, indexed by shard id.</summary>
        public int[] ShardCell { get; }

        public PileGrid(int columns, int rows, bool[] walls, int[] shardCell)
        {
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (walls == null || walls.Length != columns * rows) throw new ArgumentException("One wall flag per cell.", nameof(walls));
            Columns = columns;
            Rows = rows;
            Walls = walls;
            ShardCell = shardCell ?? throw new ArgumentNullException(nameof(shardCell));
        }

        public int CellCount => Columns * Rows;
        public int Column(int cell) => cell % Columns;
        public int Row(int cell) => cell / Columns;

        /// <summary>
        /// Marks every empty cell joined to the top edge. <paramref name="occupied"/> flags cells holding a shard
        /// still in the pile.
        /// </summary>
        public void FillOpen(bool[] occupied, bool[] open, int[] queue)
        {
            Array.Clear(open, 0, open.Length);
            int head = 0, tail = 0;
            for (int c = 0; c < Columns; c++)
            {
                if (Walls[c] || occupied[c]) continue;
                open[c] = true;
                queue[tail++] = c;
            }

            while (head < tail)
            {
                int cell = queue[head++];
                int col = Column(cell), row = Row(cell);
                if (col > 0) Visit(cell - 1);
                if (col < Columns - 1) Visit(cell + 1);
                if (row > 0) Visit(cell - Columns);
                if (row < Rows - 1) Visit(cell + Columns);
            }

            void Visit(int next)
            {
                if (open[next] || Walls[next] || occupied[next]) return;
                open[next] = true;
                queue[tail++] = next;
            }
        }

        /// <summary>True if a shard in <paramref name="cell"/> can be pulled out given the open cells.</summary>
        public bool HasExit(int cell, bool[] open)
        {
            int col = Column(cell), row = Row(cell);
            if (row == 0) return true;
            if (col > 0 && open[cell - 1]) return true;
            if (col < Columns - 1 && open[cell + 1]) return true;
            if (open[cell - Columns]) return true;
            return row < Rows - 1 && open[cell + Columns];
        }
    }
}
