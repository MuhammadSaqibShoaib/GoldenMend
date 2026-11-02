using System;

namespace GoldenMend.Core
{
    public enum GameEventType
    {
        ShardUncovered,
        ShardLocked,
        ShardParked,
        ShardAutoFlown,
        SeamFormed,
        BenchSlotUnlocked,
        LevelWon,
        LevelFailed,
    }

    public enum FailReason
    {
        None,
        BenchFull,
    }

    /// <summary>One thing that happened during a tap. Views play these back in order.</summary>
    public readonly struct GameEvent : IEquatable<GameEvent>
    {
        public GameEventType Type { get; }
        public int Shard { get; }
        public int Socket { get; }
        public int Slot { get; }

        /// <summary>For <see cref="GameEventType.SeamFormed"/>: the placed neighbour the seam joins.</summary>
        public int OtherSocket { get; }
        public FailReason Reason { get; }

        GameEvent(GameEventType type, int shard = -1, int socket = -1, int slot = -1, int otherSocket = -1,
            FailReason reason = FailReason.None)
        {
            Type = type;
            Shard = shard;
            Socket = socket;
            Slot = slot;
            OtherSocket = otherSocket;
            Reason = reason;
        }

        public static GameEvent Uncovered(int shard) => new GameEvent(GameEventType.ShardUncovered, shard);
        public static GameEvent Locked(int shard, int socket) => new GameEvent(GameEventType.ShardLocked, shard, socket);
        public static GameEvent Parked(int shard, int slot) => new GameEvent(GameEventType.ShardParked, shard, slot: slot);
        public static GameEvent AutoFlown(int shard, int socket, int fromSlot) => new GameEvent(GameEventType.ShardAutoFlown, shard, socket, fromSlot);
        public static GameEvent Seam(int socket, int otherSocket) => new GameEvent(GameEventType.SeamFormed, socket: socket, otherSocket: otherSocket);
        public static GameEvent SlotUnlocked(int slot) => new GameEvent(GameEventType.BenchSlotUnlocked, slot: slot);
        public static GameEvent Won() => new GameEvent(GameEventType.LevelWon);
        public static GameEvent Failed(int shard, FailReason reason) => new GameEvent(GameEventType.LevelFailed, shard, reason: reason);

        public bool Equals(GameEvent other) =>
            Type == other.Type && Shard == other.Shard && Socket == other.Socket && Slot == other.Slot &&
            OtherSocket == other.OtherSocket && Reason == other.Reason;

        public override bool Equals(object obj) => obj is GameEvent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Type;
                hash = hash * 31 + Shard;
                hash = hash * 31 + Socket;
                hash = hash * 31 + Slot;
                hash = hash * 31 + OtherSocket;
                return hash * 31 + (int)Reason;
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case GameEventType.ShardUncovered: return $"Uncovered(shard {Shard})";
                case GameEventType.ShardLocked: return $"Locked(shard {Shard} -> socket {Socket})";
                case GameEventType.ShardParked: return $"Parked(shard {Shard} -> slot {Slot})";
                case GameEventType.ShardAutoFlown: return $"AutoFlown(shard {Shard}, slot {Slot} -> socket {Socket})";
                case GameEventType.SeamFormed: return $"Seam(socket {Socket} | socket {OtherSocket})";
                case GameEventType.BenchSlotUnlocked: return $"SlotUnlocked({Slot})";
                case GameEventType.LevelWon: return "Won";
                case GameEventType.LevelFailed: return $"Failed(shard {Shard}, {Reason})";
                default: return Type.ToString();
            }
        }
    }
}
