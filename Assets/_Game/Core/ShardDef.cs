using System;

namespace GoldenMend.Core
{
    public sealed class ShardDef
    {
        public int Id { get; }

        /// <summary>The vessel socket this shard belongs in.</summary>
        public int Socket { get; }

        /// <summary>Shards lying on top of this one in the pile. It can be tapped once all of them are gone.</summary>
        public int[] CoveredBy { get; }

        /// <summary>A cracked shard locks only when it touches 2 placed neighbours.</summary>
        public bool Cracked { get; }

        public ShardDef(int id, int socket, int[] coveredBy = null, bool cracked = false)
        {
            Id = id;
            Socket = socket;
            CoveredBy = coveredBy ?? Array.Empty<int>();
            Cracked = cracked;
        }

        public int RequiredNeighbours => Cracked ? 2 : 1;
    }
}
