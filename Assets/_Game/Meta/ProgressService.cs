using System;

namespace GoldenMend.Meta
{
    /// <summary>What a finished level earned.</summary>
    public readonly struct LevelResult
    {
        public int Level { get; }
        public int Stars { get; }
        public int Coins { get; }
        public bool FirstClear { get; }

        public LevelResult(int level, int stars, int coins, bool firstClear)
        {
            Level = level;
            Stars = stars;
            Coins = coins;
            FirstClear = firstClear;
        }
    }

    /// <summary>Which levels are unlocked and finished, their stars, and the coins they pay.</summary>
    public sealed class ProgressService
    {
        const int CoinsPerStar = 10;
        const int ReplayCoins = 5;

        readonly SaveData data;
        readonly EconomyService economy;
        readonly ISaveStore store;

        public int LevelCount { get; }

        public ProgressService(SaveData data, int levelCount, EconomyService economy, ISaveStore store)
        {
            if (levelCount <= 0) throw new ArgumentOutOfRangeException(nameof(levelCount));
            this.data = data;
            this.economy = economy;
            this.store = store;
            LevelCount = levelCount;
            data.Unlocked = Math.Max(0, Math.Min(data.Unlocked, levelCount));
        }

        /// <summary>The level the Play button starts: the first unfinished one, or the last level once all are done.</summary>
        public int NextLevel => Math.Min(data.Unlocked, LevelCount - 1);
        public bool AllComplete => data.Unlocked >= LevelCount;
        public bool IsUnlocked(int level) => level >= 0 && level < LevelCount && level <= data.Unlocked;
        public bool IsCompleted(int level) => Find(level)?.Clears > 0;
        public int StarsFor(int level) => Find(level)?.Stars ?? 0;

        public int CompletedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < LevelCount; i++)
                {
                    if (IsCompleted(i)) count++;
                }
                return count;
            }
        }

        /// <summary>3 stars at the solver's best workbench peak, 2 within one slot of it, 1 otherwise.</summary>
        public static int StarRating(int benchPeak, int bestPeak)
        {
            if (benchPeak <= bestPeak) return 3;
            return benchPeak <= bestPeak + 1 ? 2 : 1;
        }

        public LevelResult Complete(int level, int benchPeak, int bestPeak, bool hard)
        {
            var record = Find(level);
            if (record == null)
            {
                record = new LevelRecord { Index = level };
                data.Levels.Add(record);
            }

            int stars = StarRating(benchPeak, bestPeak);
            bool firstClear = record.Clears == 0;
            record.Clears++;
            record.Stars = Math.Max(record.Stars, stars);
            record.BestPeak = record.BestPeak < 0 ? benchPeak : Math.Min(record.BestPeak, benchPeak);
            if (level >= data.Unlocked) data.Unlocked = Math.Min(level + 1, LevelCount);

            int coins = firstClear ? stars * CoinsPerStar : ReplayCoins;
            if (hard) coins *= 2;

            store.Save(data);
            economy.Grant(coins);
            return new LevelResult(level, stars, coins, firstClear);
        }

        LevelRecord Find(int level)
        {
            foreach (var record in data.Levels)
            {
                if (record.Index == level) return record;
            }
            return null;
        }
    }
}
