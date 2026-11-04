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

        /// <summary>
        /// The deal attempt the builder starts from. The catalog stores the attempt it accepted, so the game finds the
        /// level on its first solve instead of re-solving every rejected deal before it.
        /// </summary>
        public int Deal;
        public int CrackedCount;
        public int LockedBenchSlots;
        public int BenchSize = Core.LevelDef.DefaultBenchSize;

        /// <summary>Accepted range for the solver's minimum bench peak: the level's difficulty band.</summary>
        public int MinBenchPeak;
        public int MaxBenchPeak = 5;
        public bool Hard;

        /// <summary>
        /// The glaze pattern painted across the vessel: seigaiha, sakura, kikko, uroko, kanoko or shippo.
        /// Ignored when <see cref="Artwork"/> is set.
        /// </summary>
        public string Pattern = "shippo";

        /// <summary>Glaze colour scheme for the generated pattern.</summary>
        public int Palette;

        /// <summary>
        /// Optional public-domain painting (Content/Art/artworks.json) used as the glaze instead of a pattern.
        /// It is zoomed in on <see cref="ArtFocusX"/>/<see cref="ArtFocusY"/> so it reads as texture, not as the picture.
        /// </summary>
        public string Artwork;
        public float ArtZoom = 1f;
        public float ArtFocusX = 0.5f;
        public float ArtFocusY = 0.5f;
    }
}
