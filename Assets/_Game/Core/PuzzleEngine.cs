using System;
using System.Collections.Generic;

namespace GoldenMend.Core
{
    /// <summary>
    /// The rules of Golden Mend. A tap goes in, the ordered list of what happened comes out.
    /// Deterministic: the same level and taps always give the same events.
    /// </summary>
    public sealed class PuzzleEngine
    {
        static readonly IReadOnlyList<GameEvent> Rejected = Array.Empty<GameEvent>();

        public LevelDef Level { get; }
        public BoardState State { get; }

        public PuzzleEngine(LevelDef level) : this(level, BoardState.Initial(level))
        {
        }

        public PuzzleEngine(LevelDef level, BoardState state)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public PuzzleEngine Clone() => new PuzzleEngine(Level, State.Clone());

        // Grid pile: cells joined to the top edge, recomputed only after the pile changes.
        bool[] openCells;
        bool[] occupiedCells;
        int[] floodQueue;
        bool openDirty = true;

        public bool IsCovered(int shard)
        {
            foreach (int above in Level.Shards[shard].CoveredBy)
            {
                if (State.InPile[above]) return true;
            }
            return false;
        }

        /// <summary>True if the shard has no open path to the top edge of a grid pile.</summary>
        public bool IsBlockedInGrid(int shard)
        {
            var pile = Level.Pile;
            if (pile == null) return false;
            if (openDirty) RefreshOpenCells(pile);
            return !pile.HasExit(pile.ShardCell[shard], openCells);
        }

        /// <summary>Still in the pile but can't be tapped yet: covered by another shard or walled in.</summary>
        public bool IsLocked(int shard) => State.InPile[shard] && (IsCovered(shard) || IsBlockedInGrid(shard));

        public bool CanTap(int shard) =>
            State.Status == GameStatus.Playing && State.InPile[shard] && !IsCovered(shard) && !IsBlockedInGrid(shard);

        void RefreshOpenCells(PileGrid pile)
        {
            if (openCells == null)
            {
                openCells = new bool[pile.CellCount];
                occupiedCells = new bool[pile.CellCount];
                floodQueue = new int[pile.CellCount];
            }
            Array.Clear(occupiedCells, 0, occupiedCells.Length);
            for (int i = 0; i < State.InPile.Length; i++)
            {
                if (State.InPile[i]) occupiedCells[pile.ShardCell[i]] = true;
            }
            pile.FillOpen(occupiedCells, openCells, floodQueue);
            openDirty = false;
        }

        public int PlacedNeighbourCount(int socket)
        {
            int count = 0;
            foreach (int n in Level.Neighbours[socket])
            {
                if (State.Placed[n]) count++;
            }
            return count;
        }

        public bool CanLock(int shard)
        {
            var def = Level.Shards[shard];
            return PlacedNeighbourCount(def.Socket) >= def.RequiredNeighbours;
        }

        /// <summary>Resolves one tap (GDD rules 1-7). An empty result means the tap was rejected.</summary>
        public IReadOnlyList<GameEvent> Tap(int shard)
        {
            if (shard < 0 || shard >= Level.Shards.Count || !CanTap(shard)) return Rejected;

            var events = new List<GameEvent>();

            var lockedBefore = LockedShards();
            State.InPile[shard] = false;
            openDirty = true;
            for (int other = 0; other < lockedBefore.Length; other++)
            {
                if (lockedBefore[other] && !IsLocked(other)) events.Add(GameEvent.Uncovered(other));
            }

            if (CanLock(shard))
            {
                Lock(shard, -1, events);
                ResolveChain(events);
            }
            else
            {
                int slot = FirstFreeSlot();
                if (slot < 0)
                {
                    State.Status = GameStatus.Failed;
                    State.PendingShard = shard;
                    events.Add(GameEvent.Failed(shard, FailReason.BenchFull));
                    return events;
                }
                State.Bench[slot] = shard;
                events.Add(GameEvent.Parked(shard, slot));
            }

            if (AllPlaced())
            {
                State.Status = GameStatus.Won;
                events.Add(GameEvent.Won());
            }
            return events;
        }

        void Lock(int shard, int fromSlot, List<GameEvent> events)
        {
            int socket = Level.Shards[shard].Socket;
            State.Placed[socket] = true;
            State.Placements++;
            events.Add(fromSlot < 0 ? GameEvent.Locked(shard, socket) : GameEvent.AutoFlown(shard, socket, fromSlot));

            foreach (int n in Level.Neighbours[socket])
            {
                if (State.Placed[n]) events.Add(GameEvent.Seam(socket, n));
            }

            TryUnlockSlot(events);
        }

        /// <summary>Left to right, pass by pass, until a full pass locks nothing.</summary>
        void ResolveChain(List<GameEvent> events)
        {
            bool lockedAny;
            do
            {
                lockedAny = false;
                for (int slot = 0; slot < State.Bench.Length; slot++)
                {
                    int parked = State.Bench[slot];
                    if (parked < 0 || !CanLock(parked)) continue;
                    State.Bench[slot] = -1;
                    Lock(parked, slot, events);
                    lockedAny = true;
                }
            } while (lockedAny);
        }

        void TryUnlockSlot(List<GameEvent> events)
        {
            int stillLocked = State.LockedSlotCount;
            if (stillLocked == 0) return;

            int opened = Level.LockedBenchSlots - stillLocked;
            if (State.Placements < Level.PlacementsPerUnlock * (opened + 1)) return;

            int slot = Array.IndexOf(State.SlotLocked, true);
            State.SlotLocked[slot] = false;
            events.Add(GameEvent.SlotUnlocked(slot));
        }

        bool[] LockedShards()
        {
            var locked = new bool[Level.Shards.Count];
            for (int i = 0; i < locked.Length; i++) locked[i] = IsLocked(i);
            return locked;
        }

        int FirstFreeSlot()
        {
            for (int slot = 0; slot < State.Bench.Length; slot++)
            {
                if (State.Bench[slot] < 0 && !State.SlotLocked[slot]) return slot;
            }
            return -1;
        }

        bool AllPlaced()
        {
            foreach (bool placed in State.Placed)
            {
                if (!placed) return false;
            }
            return true;
        }
    }
}
