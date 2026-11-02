using GoldenMend.Data;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Portrait screen layout in world units (camera height about 20.8): vessel on top, workbench in the
    /// middle, pile at the bottom.
    /// </summary>
    public sealed class BoardLayout
    {
        public const float VesselCenterY = 3.9f;
        public const float VesselMaxWidth = 9.6f;
        public const float VesselMaxHeight = 7.6f;
        public const float BenchY = -1.6f;
        public const float BenchSpacing = 2.0f;
        public const float SlotSize = 1.75f;
        public const float BenchShardSize = 1.5f;
        public const float PileTop = -3.3f;
        public const float PileMaxWidth = 10.2f;
        public const float PileMaxHeight = 6.6f;
        public const float PileMaxCell = 1.9f;

        readonly BuiltLevel level;

        public float CellSize { get; }
        public float ShardInVesselSize => CellSize * 0.94f;

        /// <summary>Size of one pile grid cell; shards sit inside it with a small gap.</summary>
        public float PileCellSize { get; }
        public float PileShardSize => PileCellSize * 0.86f;

        public BoardLayout(BuiltLevel level)
        {
            this.level = level;
            CellSize = Mathf.Min(VesselMaxWidth / level.Columns, VesselMaxHeight / level.Rows, 1.9f);
            var pile = level.Pile;
            PileCellSize = Mathf.Min(PileMaxWidth / pile.Columns, PileMaxHeight / pile.Rows, PileMaxCell);
        }

        public Vector3 Socket(int socket)
        {
            float x = (level.SocketColumn[socket] - (level.Columns - 1) / 2f) * CellSize;
            float y = VesselCenterY - (level.SocketRow[socket] - (level.Rows - 1) / 2f) * CellSize;
            return new Vector3(x, y, 0f);
        }

        public Vector3 Slot(int slot)
        {
            int count = level.Def.BenchSize;
            return new Vector3((slot - (count - 1) / 2f) * BenchSpacing, BenchY, 0f);
        }

        public Vector3 PileCell(int cell)
        {
            var pile = level.Pile;
            float x = (pile.Column(cell) - (pile.Columns - 1) / 2f) * PileCellSize;
            float y = PileTop - (pile.Row(cell) + 0.5f) * PileCellSize;
            return new Vector3(x, y, 0f);
        }

        public Vector3 PileShard(int shard) => PileCell(level.Pile.ShardCell[shard]);

        /// <summary>Centre and size of the whole pile grid, for its backing panel.</summary>
        public Vector3 PileCentre => new Vector3(0f, PileTop - level.Pile.Rows * PileCellSize / 2f, 0f);
        public Vector2 PileExtent => new Vector2(level.Pile.Columns * PileCellSize, level.Pile.Rows * PileCellSize);
    }
}
