using System.Collections.Generic;

namespace GoldenMend.Data
{
    /// <summary>
    /// The game's level list, in play order. The opening curve follows the GDD: tutorial, deeper racks,
    /// walls, a distant anchor, cracked shards, then a hard level with a locked bench slot.
    /// </summary>
    public static class LevelCatalog
    {
        static readonly LevelSpec[] Specs =
        {
            new LevelSpec
            {
                Name = "Tea Bowl", Artwork = "great-wave", Seed = 11, MinBenchPeak = 0, MaxBenchPeak = 0, Palette = 0,
                PileShape = new[]
                {
                    "ooooo",
                    ".o.o.",
                },
                Shape = new[]
                {
                    "XXXXX",
                    ".XAX.",
                },
            },
            new LevelSpec
            {
                Name = "Rice Bowl", Artwork = "morning-mist", Seed = 23, MinBenchPeak = 0, MaxBenchPeak = 1, Palette = 1,
                PileShape = new[]
                {
                    "oooooo",
                    ".oooo.",
                    "..o...",
                },
                Shape = new[]
                {
                    "XXXXXX",
                    ".XXXX.",
                    "..AX..",
                },
            },
            new LevelSpec
            {
                Name = "Sake Cup", Artwork = "bedroom", Seed = 37, MinBenchPeak = 1, MaxBenchPeak = 2, Palette = 2,
                PileShape = new[]
                {
                    "oooo",
                    "oooo",
                    "oo#o",
                },
                Shape = new[]
                {
                    "XXXX",
                    "XXXX",
                    "XAXX",
                },
            },
            new LevelSpec
            {
                Name = "Ikebana Vase", Artwork = "self-portrait", Seed = 41, MinBenchPeak = 2, MaxBenchPeak = 3, Palette = 3,
                PileShape = new[]
                {
                    "ooooo",
                    "o#o#o",
                    "ooooo",
                },
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
                Name = "Tea Caddy", Artwork = "water-lilies", Seed = 53, CrackedCount = 2, MinBenchPeak = 1, MaxBenchPeak = 3, Palette = 4,
                PileShape = new[]
                {
                    "ooooo",
                    "ooooo",
                    "#ooo#",
                },
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
                Name = "Amphora", Artwork = "irises", Seed = 67, LockedBenchSlots = 1, CrackedCount = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true, Palette = 5,
                PileShape = new[]
                {
                    "oooooo",
                    "o#oo#o",
                    "oooooo",
                    "##oo##",
                },
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
