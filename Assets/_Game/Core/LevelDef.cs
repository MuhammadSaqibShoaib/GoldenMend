using System;
using System.Collections.Generic;

namespace GoldenMend.Core
{
    /// <summary>
    /// Read-only puzzle definition: the vessel's socket graph, one shard per non-anchor socket,
    /// and which pile shards cover which.
    /// </summary>
    public sealed class LevelDef
    {
        public const int DefaultBenchSize = 5;

        public int SocketCount { get; }
        public int AnchorSocket { get; }
        public IReadOnlyList<int[]> Neighbours { get; }
        public IReadOnlyList<ShardDef> Shards { get; }
        public int BenchSize { get; }

        /// <summary>Slots that start locked (the rightmost ones). Each opens after <see cref="PlacementsPerUnlock"/> more placements.</summary>
        public int LockedBenchSlots { get; }
        public int PlacementsPerUnlock { get; }

        /// <summary>Optional grid pile. When set, a shard also needs an open path to the top edge to be tapped.</summary>
        public PileGrid Pile { get; }

        public LevelDef(int socketCount, int anchorSocket, int[][] neighbours, ShardDef[] shards,
            int benchSize = DefaultBenchSize, int lockedBenchSlots = 0, int placementsPerUnlock = 3, PileGrid pile = null)
        {
            if (socketCount <= 0) throw new ArgumentOutOfRangeException(nameof(socketCount));
            if (anchorSocket < 0 || anchorSocket >= socketCount) throw new ArgumentOutOfRangeException(nameof(anchorSocket));
            if (neighbours == null || neighbours.Length != socketCount) throw new ArgumentException("One neighbour list per socket.", nameof(neighbours));
            if (shards == null) throw new ArgumentNullException(nameof(shards));
            if (benchSize <= 0) throw new ArgumentOutOfRangeException(nameof(benchSize));
            if (lockedBenchSlots < 0 || lockedBenchSlots >= benchSize) throw new ArgumentOutOfRangeException(nameof(lockedBenchSlots));
            if (placementsPerUnlock <= 0) throw new ArgumentOutOfRangeException(nameof(placementsPerUnlock));

            SocketCount = socketCount;
            AnchorSocket = anchorSocket;
            Neighbours = neighbours;
            Shards = shards;
            BenchSize = benchSize;
            LockedBenchSlots = lockedBenchSlots;
            PlacementsPerUnlock = placementsPerUnlock;
            Pile = pile;
        }

        /// <summary>Returns every problem found; an empty list means the level is well formed.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            for (int s = 0; s < SocketCount; s++)
            {
                foreach (int n in Neighbours[s])
                {
                    if (n < 0 || n >= SocketCount) errors.Add($"Socket {s} lists unknown neighbour {n}.");
                    else if (n == s) errors.Add($"Socket {s} lists itself as a neighbour.");
                    else if (Array.IndexOf(Neighbours[n], s) < 0) errors.Add($"Socket {s} -> {n} has no matching {n} -> {s}.");
                }
            }

            var shardForSocket = new int[SocketCount];
            for (int s = 0; s < SocketCount; s++) shardForSocket[s] = -1;
            for (int i = 0; i < Shards.Count; i++)
            {
                var shard = Shards[i];
                if (shard.Id != i) errors.Add($"Shard at index {i} has id {shard.Id}.");
                if (shard.Socket < 0 || shard.Socket >= SocketCount)
                {
                    errors.Add($"Shard {i} targets unknown socket {shard.Socket}.");
                    continue;
                }
                if (shard.Socket == AnchorSocket) errors.Add($"Shard {i} targets the anchor socket.");
                else if (shardForSocket[shard.Socket] >= 0) errors.Add($"Socket {shard.Socket} has two shards ({shardForSocket[shard.Socket]} and {i}).");
                else shardForSocket[shard.Socket] = i;

                foreach (int above in shard.CoveredBy)
                {
                    if (above < 0 || above >= Shards.Count) errors.Add($"Shard {i} is covered by unknown shard {above}.");
                    else if (above == i) errors.Add($"Shard {i} covers itself.");
                }

                if (shard.Cracked && Neighbours[shard.Socket].Length < 2)
                    errors.Add($"Shard {i} is cracked but its socket has fewer than 2 neighbours, so it can never lock.");
            }

            for (int s = 0; s < SocketCount; s++)
            {
                if (s != AnchorSocket && shardForSocket[s] < 0) errors.Add($"Socket {s} has no shard.");
            }

            if (Pile != null) ValidatePile(errors);

            if (HasCoverCycle()) errors.Add("The pile has a cover cycle, so some shards can never be uncovered.");

            return errors;
        }

        void ValidatePile(List<string> errors)
        {
            if (Pile.ShardCell.Length != Shards.Count)
            {
                errors.Add($"The pile grid places {Pile.ShardCell.Length} shards but the level has {Shards.Count}.");
                return;
            }
            var owner = new int[Pile.CellCount];
            for (int c = 0; c < owner.Length; c++) owner[c] = -1;
            for (int i = 0; i < Pile.ShardCell.Length; i++)
            {
                int cell = Pile.ShardCell[i];
                if (cell < 0 || cell >= Pile.CellCount) errors.Add($"Shard {i} is outside the pile grid.");
                else if (Pile.Walls[cell]) errors.Add($"Shard {i} sits on a wall cell.");
                else if (owner[cell] >= 0) errors.Add($"Shards {owner[cell]} and {i} share the same pile cell.");
                else owner[cell] = i;
            }
        }

        bool HasCoverCycle()
        {
            // 0 = unvisited, 1 = on the current path, 2 = done.
            var mark = new int[Shards.Count];
            for (int i = 0; i < Shards.Count; i++)
            {
                if (mark[i] == 0 && Visit(i, mark)) return true;
            }
            return false;
        }

        bool Visit(int shard, int[] mark)
        {
            mark[shard] = 1;
            foreach (int above in Shards[shard].CoveredBy)
            {
                if (above < 0 || above >= Shards.Count || above == shard) continue;
                if (mark[above] == 1) return true;
                if (mark[above] == 0 && Visit(above, mark)) return true;
            }
            mark[shard] = 2;
            return false;
        }
    }
}
