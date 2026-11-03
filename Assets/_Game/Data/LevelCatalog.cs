using System.Collections.Generic;

namespace GoldenMend.Data
{
    /// <summary>
    /// The game's level list, in play order. Early vessels have few, large shards; later ones more and smaller.
    /// New ideas arrive one at a time: deeper racks, walls, a distant anchor, then a hard level
    /// with a locked bench slot. Glazes alternate between traditional Japanese patterns and close-up crops of
    /// public-domain paintings.
    /// </summary>
    public static class LevelCatalog
    {
        static readonly LevelSpec[] Specs =
        {
            new LevelSpec
            {
                Name = "Tea Cup", Pattern = "seigaiha", Palette = 1, Seed = 11, MinBenchPeak = 0, MaxBenchPeak = 0,
                PileShape = new[] { "ooooo" },
                Shape = new[]
                {
                    "XXX",
                    "XAX",
                },
            },
            new LevelSpec
            {
                Name = "Rice Bowl", Pattern = "sakura", Palette = 6, Seed = 23, MinBenchPeak = 0, MaxBenchPeak = 1,
                PileShape = new[] { "oooo", "ooo." },
                Shape = new[]
                {
                    "XXXX",
                    "XAXX",
                },
            },
            new LevelSpec
            {
                Name = "Sake Cup", Artwork = "great-wave", ArtZoom = 2.4f, ArtFocusX = 0.3f, ArtFocusY = 0.62f,
                Seed = 37, MinBenchPeak = 1, MaxBenchPeak = 2,
                PileShape = new[] { "oooo", "oooo" },
                Shape = new[]
                {
                    "XXX",
                    "XXX",
                    "XAX",
                },
            },
            new LevelSpec
            {
                Name = "Tea Bowl", Pattern = "kikko", Palette = 4, Seed = 41, MinBenchPeak = 1, MaxBenchPeak = 2,
                PileShape = new[] { "ooooo", "o#o#o", "o.o.." },
                Shape = new[]
                {
                    "XXXXX",
                    ".XXX.",
                    ".XAX.",
                },
            },
            new LevelSpec
            {
                Name = "Ikebana Vase", Artwork = "irises", ArtZoom = 2f, ArtFocusX = 0.45f, ArtFocusY = 0.45f,
                Seed = 43, MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooo", "o#o#o", "ooooo" },
                Shape = new[]
                {
                    ".XX.",
                    "XXXX",
                    "XXXX",
                    ".XX.",
                    ".AX.",
                },
            },
            new LevelSpec
            {
                Name = "Tea Caddy", Pattern = "uroko", Palette = 2, Seed = 53, MinBenchPeak = 1, MaxBenchPeak = 3,
                PileShape = new[] { "ooooo", "ooooo", "#ooo#" },
                Shape = new[]
                {
                    "XXXXX",
                    "XXXXX",
                    ".XXX.",
                    "..A..",
                },
            },
            new LevelSpec
            {
                Name = "Lantern", Artwork = "water-lilies", ArtZoom = 1.7f, ArtFocusX = 0.55f, ArtFocusY = 0.5f,
                Seed = 61, MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooo", "ooooo", "#ooo#", ".oo.." },
                Shape = new[]
                {
                    "XXXX",
                    "XXXX",
                    "XXXX",
                    "XAXX",
                },
            },
            new LevelSpec
            {
                Name = "Amphora", Pattern = "kanoko", Palette = 5, Seed = 67, LockedBenchSlots = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true,
                PileShape = new[] { "oooooo", "o#oo#o", "oooooo", "##oo##" },
                Shape = new[]
                {
                    ".XXX.",
                    "XXXXX",
                    ".XXX.",
                    "XXXXX",
                    ".XAX.",
                },
            },
        };

        static readonly Dictionary<int, BuiltLevel> Cache = new Dictionary<int, BuiltLevel>();

        public static int Count => Specs.Length;

        public static LevelSpec Spec(int index) => Specs[index];

        public static BuiltLevel Build(int index)
        {
            lock (Cache)
            {
                if (!Cache.TryGetValue(index, out var built))
                {
                    built = LevelBuilder.Build(Specs[index]);
                    Cache[index] = built;
                }
                return built;
            }
        }
    }
}
