using System;

namespace GoldenMend.Core
{
    public enum GameStatus
    {
        Playing,
        Won,
        Failed,
    }

    /// <summary>Everything that changes during a level. Only <see cref="PuzzleEngine"/> writes to it.</summary>
    public sealed class BoardState
    {
        public bool[] Placed { get; }
        public bool[] InPile { get; }

        /// <summary>Shard id in each workbench slot, or -1 when empty.</summary>
        public int[] Bench { get; }
        public bool[] SlotLocked { get; }

        /// <summary>Shards locked into the vessel so far (the anchor is not counted).</summary>
        public int Placements { get; internal set; }
        public GameStatus Status { get; internal set; }

        /// <summary>After a bench-full fail: the shard that had nowhere to go.</summary>
        public int PendingShard { get; internal set; } = -1;

        BoardState(bool[] placed, bool[] inPile, int[] bench, bool[] slotLocked)
        {
            Placed = placed;
            InPile = inPile;
            Bench = bench;
            SlotLocked = slotLocked;
        }

        public static BoardState Initial(LevelDef level)
        {
            var placed = new bool[level.SocketCount];
            placed[level.AnchorSocket] = true;

            var inPile = new bool[level.Shards.Count];
            for (int i = 0; i < inPile.Length; i++) inPile[i] = true;

            var bench = new int[level.BenchSize];
            for (int i = 0; i < bench.Length; i++) bench[i] = -1;

            var slotLocked = new bool[level.BenchSize];
            for (int i = level.BenchSize - level.LockedBenchSlots; i < level.BenchSize; i++) slotLocked[i] = true;

            return new BoardState(placed, inPile, bench, slotLocked);
        }

        public BoardState Clone()
        {
            return new BoardState((bool[])Placed.Clone(), (bool[])InPile.Clone(), (int[])Bench.Clone(), (bool[])SlotLocked.Clone())
            {
                Placements = Placements,
                Status = Status,
                PendingShard = PendingShard,
            };
        }

        public int BenchCount
        {
            get
            {
                int count = 0;
                foreach (int shard in Bench)
                {
                    if (shard >= 0) count++;
                }
                return count;
            }
        }

        public int LockedSlotCount
        {
            get
            {
                int count = 0;
                foreach (bool locked in SlotLocked)
                {
                    if (locked) count++;
                }
                return count;
            }
        }

        public int SlotOf(int shard) => Array.IndexOf(Bench, shard);
    }
}
