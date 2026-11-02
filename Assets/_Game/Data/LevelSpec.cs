namespace GoldenMend.Data
{
    /// <summary>
    /// A designer-facing level recipe. The vessel is drawn as text rows:
    /// 'X' is a socket, 'A' the pre-placed anchor socket, '.' empty space.
    /// </summary>
    public sealed class LevelSpec
    {
        public string Name;
        public string[] Shape;

        /// <summary>
        /// The pile grid, top row first: 'o' holds a shard, '#' is a wall, '.' is empty. The top edge is the way
        /// out, so shards deeper down stay locked until a path to the top opens. Needs one 'o' per shard.
        /// </summary>
        public string[] PileShape;
        public int Seed = 1;
        public int CrackedCount;
        public int LockedBenchSlots;
        public int BenchSize = Core.LevelDef.DefaultBenchSize;

        /// <summary>Accepted range for the solver's minimum bench peak: the level's difficulty band.</summary>
        public int MinBenchPeak;
        public int MaxBenchPeak = 5;
        public bool Hard;

        /// <summary>Glaze palette index for the generated vessel art.</summary>
        public int Palette;
    }
}
