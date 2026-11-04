using System.Collections.Generic;

namespace GoldenMend.Data
{
    /// <summary>
    /// The game's level list, in play order. Early vessels have few, large shards; later ones more and smaller.
    /// New ideas arrive one at a time: deeper racks, walls, a distant anchor, then hard levels with a locked bench
    /// slot about every five levels, each followed by an easier breather. Glazes alternate between traditional
    /// Japanese patterns and close-up crops of public-domain paintings. Each spec stores the deal the builder
    /// accepted, so loading a level runs the solver once.
    /// </summary>
    public static class LevelCatalog
    {
        static readonly LevelSpec[] Specs =
        {
            new LevelSpec
            {
                Name = "Tea Cup", Pattern = "seigaiha", Palette = 1, Seed = 11, Deal = 0, MinBenchPeak = 0, MaxBenchPeak = 0,
                PileShape = new[] { "ooooo" },
                Shape = new[]
                {
                    "XXX",
                    "XAX",
                },
            },
            new LevelSpec
            {
                Name = "Rice Bowl", Pattern = "sakura", Palette = 6, Seed = 23, Deal = 0, MinBenchPeak = 0, MaxBenchPeak = 1,
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
                Seed = 37, Deal = 1, MinBenchPeak = 1, MaxBenchPeak = 2,
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
                Name = "Tea Bowl", Pattern = "kikko", Palette = 4, Seed = 41, Deal = 0, MinBenchPeak = 1, MaxBenchPeak = 2,
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
                Seed = 43, Deal = 2, MinBenchPeak = 2, MaxBenchPeak = 3,
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
                Name = "Tea Caddy", Pattern = "uroko", Palette = 2, Seed = 53, Deal = 1, MinBenchPeak = 1, MaxBenchPeak = 3,
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
                Seed = 61, Deal = 8, MinBenchPeak = 2, MaxBenchPeak = 3,
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
                Name = "Amphora", Pattern = "kanoko", Palette = 5, Seed = 67, Deal = 45, LockedBenchSlots = 1,
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
            new LevelSpec
            {
                Name = "Incense Burner", Pattern = "shippo", Palette = 7, Seed = 71, Deal = 1,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "oooooo", "o#oo#o", "oooooo", "..oo.." },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    ".XA.X.",
                },
            },
            new LevelSpec
            {
                Name = "Sake Flask", Artwork = "morning-mist", ArtZoom = 2.0f, ArtFocusX = 0.5f, ArtFocusY = 0.4f,
                Seed = 73, Deal = 0,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooo", "ooooo", "o#o#o", "ooooo" },
                Shape = new[]
                {
                    "..XX..",
                    ".XXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    "..A...",
                },
            },
            new LevelSpec
            {
                Name = "Water Jar", Pattern = "seigaiha", Palette = 0, Seed = 79, Deal = 3,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "oooooo", "oo##oo", "oooooo", "..ooo." },
                Shape = new[]
                {
                    ".XXX.",
                    "XXXXX",
                    "XXXXX",
                    "XXXXX",
                    ".XA..",
                },
            },
            new LevelSpec
            {
                Name = "Plum Vase", Artwork = "irises", ArtZoom = 2.2f, ArtFocusX = 0.3f, ArtFocusY = 0.6f,
                Seed = 83, Deal = 0,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooo", "o#o#o", "ooooo", "o#o#o", "ooo.." },
                Shape = new[]
                {
                    ".XX.",
                    "XXXX",
                    "XXXX",
                    "XXXX",
                    ".XX.",
                    "XAXX",
                },
            },
            new LevelSpec
            {
                Name = "Donabe Pot", Pattern = "kikko", Palette = 2, Seed = 89, Deal = 1, LockedBenchSlots = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true,
                PileShape = new[] { "oooooo", "#oooo#", "oooooo", "#oooo#" },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXA..",
                },
            },
            new LevelSpec
            {
                Name = "Teapot", Pattern = "sakura", Palette = 6, Seed = 97, Deal = 0,
                MinBenchPeak = 1, MaxBenchPeak = 3,
                PileShape = new[] { "ooooooo", "o#ooo#o", "ooooooo", "...o..." },
                Shape = new[]
                {
                    ".XXX..",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    "..XA..",
                },
            },
            new LevelSpec
            {
                Name = "Serving Platter", Artwork = "great-wave", ArtZoom = 1.8f, ArtFocusX = 0.6f, ArtFocusY = 0.4f,
                Seed = 101, Deal = 4,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "oooooo", "oooooo", "o####o", "oooooo" },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XA.X.",
                },
            },
            new LevelSpec
            {
                Name = "Moon Jar", Pattern = "uroko", Palette = 3, Seed = 103, Deal = 3,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooo", "ooooo", "o#o#o", "ooooo", "o.o.o" },
                Shape = new[]
                {
                    "..XX..",
                    ".XXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXAX.",
                },
            },
            new LevelSpec
            {
                Name = "Brush Pot", Artwork = "self-portrait", ArtZoom = 2.6f, ArtFocusX = 0.5f, ArtFocusY = 0.5f,
                Seed = 107, Deal = 1,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "oooooo", "o#oo#o", "oooooo", "o#oo#o", ".o..o." },
                Shape = new[]
                {
                    "XXXX",
                    "XXXX",
                    "XXXX",
                    "XXXX",
                    "XXXX",
                    "XAX.",
                },
            },
            new LevelSpec
            {
                Name = "Storage Jar", Pattern = "kanoko", Palette = 4, Seed = 109, Deal = 1, LockedBenchSlots = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true,
                PileShape = new[] { "oooooo", "oo##oo", "oooooo", "oo##oo", "..oo.." },
                Shape = new[]
                {
                    ".XXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "..A...",
                },
            },
            new LevelSpec
            {
                Name = "Gourd Bottle", Pattern = "shippo", Palette = 5, Seed = 113, Deal = 5,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooooo", "o#o#o#o", "ooooooo", "oo...oo" },
                Shape = new[]
                {
                    "..XX.",
                    ".XXX.",
                    "XXXXX",
                    "XXXXX",
                    "XXXXX",
                    ".XAX.",
                },
            },
            new LevelSpec
            {
                Name = "Celadon Ewer", Artwork = "water-lilies", ArtZoom = 2.2f, ArtFocusX = 0.3f, ArtFocusY = 0.3f,
                Seed = 127, Deal = 3,
                MinBenchPeak = 3, MaxBenchPeak = 4,
                PileShape = new[] { "oooooo", "oooooo", "oooooo", "oooo.." },
                Shape = new[]
                {
                    "XXXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    "..AX..",
                },
            },
            new LevelSpec
            {
                Name = "Lotus Bowl", Pattern = "sakura", Palette = 0, Seed = 131, Deal = 0,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooooo", "o#ooo#o", "ooooooo", ".oooo.." },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    ".XA...",
                },
            },
            new LevelSpec
            {
                Name = "Dragon Jar", Artwork = "great-wave", ArtZoom = 2.8f, ArtFocusX = 0.7f, ArtFocusY = 0.7f,
                Seed = 137, Deal = 7, LockedBenchSlots = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true,
                PileShape = new[] { "oooooo", "o#oo#o", "oooooo", "o#oo#o", "oo..o." },
                Shape = new[]
                {
                    ".XXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "..XA..",
                },
            },
            new LevelSpec
            {
                Name = "Tea Kettle", Pattern = "seigaiha", Palette = 1, Seed = 139, Deal = 0,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooooo", "ooooooo", "o##o##o", "ooooooo" },
                Shape = new[]
                {
                    "..XX..",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    "..A...",
                },
            },
            new LevelSpec
            {
                Name = "Fish Plate", Pattern = "uroko", Palette = 0, Seed = 149, Deal = 4,
                MinBenchPeak = 2, MaxBenchPeak = 4,
                PileShape = new[] { "oooooo", "oooooo", "#oooo#", "oooooo", "..oo.." },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "..A...",
                },
            },
            new LevelSpec
            {
                Name = "Garden Lantern", Artwork = "bedroom", ArtZoom = 2.4f, ArtFocusX = 0.4f, ArtFocusY = 0.6f,
                Seed = 151, Deal = 1,
                MinBenchPeak = 2, MaxBenchPeak = 3,
                PileShape = new[] { "ooooooo", "o#o#o#o", "ooooooo", "o#o#o#o", "..o.o.." },
                Shape = new[]
                {
                    ".XXXX.",
                    "XXXXXX",
                    ".XXXX.",
                    ".XXXX.",
                    "XXXXXX",
                    "..A...",
                },
            },
            new LevelSpec
            {
                Name = "Tall Vase", Pattern = "kikko", Palette = 7, Seed = 157, Deal = 0,
                MinBenchPeak = 3, MaxBenchPeak = 4,
                PileShape = new[] { "ooooooo", "ooooooo", "oo###oo", "ooooooo" },
                Shape = new[]
                {
                    ".XXX.",
                    "XXXXX",
                    "XXXXX",
                    "XXXXX",
                    "XXXXX",
                    ".XAX.",
                },
            },
            new LevelSpec
            {
                Name = "Offering Bowl", Pattern = "kanoko", Palette = 5, Seed = 163, Deal = 1, LockedBenchSlots = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true,
                PileShape = new[] { "oooooo", "o#oo#o", "oooooo", "o#oo#o", "oo.ooo" },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XA...",
                },
            },
            new LevelSpec
            {
                Name = "Crane Vase", Artwork = "irises", ArtZoom = 1.6f, ArtFocusX = 0.7f, ArtFocusY = 0.3f,
                Seed = 167, Deal = 0,
                MinBenchPeak = 3, MaxBenchPeak = 4,
                PileShape = new[] { "ooooooo", "o#ooo#o", "ooooooo", "o#ooo#o", "..o.o.." },
                Shape = new[]
                {
                    ".XXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    "..A...",
                },
            },
            new LevelSpec
            {
                Name = "Imperial Jar", Pattern = "shippo", Palette = 1, Seed = 173, Deal = 2,
                MinBenchPeak = 3, MaxBenchPeak = 4,
                PileShape = new[] { "ooooooo", "ooooooo", "o#o#o#o", "ooooooo", "..o...." },
                Shape = new[]
                {
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    "..XA..",
                    "..X...",
                },
            },
            new LevelSpec
            {
                Name = "Golden Urn", Pattern = "seigaiha", Palette = 7, Seed = 179, Deal = 2, LockedBenchSlots = 1,
                MinBenchPeak = 3, MaxBenchPeak = 4, Hard = true,
                PileShape = new[] { "ooooooo", "o#ooo#o", "ooooooo", "o#ooo#o", "o..o..o" },
                Shape = new[]
                {
                    ".XXXX.",
                    "XXXXXX",
                    "XXXXXX",
                    "XXXXXX",
                    ".XXXX.",
                    "..AX..",
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
