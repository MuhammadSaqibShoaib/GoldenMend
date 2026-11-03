using System;

namespace GoldenMend.Meta
{
    /// <summary>How one winning run went.</summary>
    public readonly struct RunStats
    {
        public int BenchPeak { get; }

        /// <summary>Play time in seconds, pauses excluded; below 0 when unknown.</summary>
        public float Seconds { get; }
        public int Hints { get; }
        public int Pulls { get; }
        public int Continues { get; }

        /// <summary>The run's moves ("t3 p5 c ..."), for replaying it later.</summary>
        public string Moves { get; }

        public RunStats(int benchPeak, float seconds, int hints = 0, int pulls = 0, int continues = 0, string moves = "")
        {
            BenchPeak = benchPeak;
            Seconds = seconds;
            Hints = hints;
            Pulls = pulls;
            Continues = continues;
            Moves = moves ?? "";
        }

        /// <summary>Won without any hint, pull or continue.</summary>
        public bool Clean => Hints == 0 && Pulls == 0 && Continues == 0;
    }

    /// <summary>What a finished level earned, and how it compares with the player's record.</summary>
    public readonly struct LevelResult
    {
        public int Level { get; }
        public int Stars { get; }

        /// <summary>All coins granted, including <see cref="CleanBonus"/>.</summary>
        public int Coins { get; }
        public bool FirstClear { get; }

        /// <summary>Extra coins for the first win of this level without help.</summary>
        public int CleanBonus { get; }

        /// <summary>The run beat an earlier personal record (never set on a first win).</summary>
        public bool NewRecord { get; }
        public int BestPeak { get; }
        public float BestSeconds { get; }

        public LevelResult(int level, int stars, int coins, bool firstClear, int cleanBonus = 0, bool newRecord = false,
            int bestPeak = -1, float bestSeconds = -1f)
        {
            Level = level;
            Stars = stars;
            Coins = coins;
            FirstClear = firstClear;
            CleanBonus = cleanBonus;
            NewRecord = newRecord;
            BestPeak = bestPeak;
            BestSeconds = bestSeconds;
        }
    }

    /// <summary>Which levels are unlocked and finished, their stars, and the coins they pay.</summary>
    public sealed class ProgressService
    {
        const int CoinsPerStar = 10;
        const int ReplayCoins = 5;
        public const int CleanBonusCoins = 10;

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

        /// <summary>Records a win with no timing or help details (treated as a clean run of unknown time).</summary>
        public LevelResult Complete(int level, int benchPeak, int bestPeak, bool hard) =>
            Complete(level, new RunStats(benchPeak, -1f), bestPeak, hard);

        public LevelResult Complete(int level, RunStats run, int bestPeak, bool hard)
        {
            var record = Find(level);
            if (record == null)
            {
                record = new LevelRecord { Index = level };
                data.Levels.Add(record);
            }

            int stars = StarRating(run.BenchPeak, bestPeak);
            bool firstClear = record.Clears == 0;
            record.Clears++;
            record.Stars = Math.Max(record.Stars, stars);
            record.HintsUsed += run.Hints;
            record.PullsUsed += run.Pulls;
            record.ContinuesUsed += run.Continues;

            bool newRecord = !firstClear && Beats(run.BenchPeak, run.Seconds, record.BestPeak, record.BestSeconds);
            if (firstClear || newRecord)
            {
                record.BestPeak = run.BenchPeak;
                record.BestSeconds = run.Seconds;
            }

            bool firstClean = false;
            if (run.Clean)
            {
                firstClean = record.CleanClears == 0;
                record.CleanClears++;
                if (firstClean || Beats(run.BenchPeak, run.Seconds, record.CleanPeak, record.CleanSeconds))
                {
                    record.CleanPeak = run.BenchPeak;
                    record.CleanSeconds = run.Seconds;
                    record.CleanRun = run.Moves;
                }
            }
            if (level >= data.Unlocked) data.Unlocked = Math.Min(level + 1, LevelCount);

            int coins = firstClear ? stars * CoinsPerStar : ReplayCoins;
            if (hard) coins *= 2;
            int bonus = firstClean ? CleanBonusCoins : 0;

            store.Save(data);
            economy.Grant(coins + bonus);
            return new LevelResult(level, stars, coins + bonus, firstClear, bonus, newRecord, record.BestPeak, record.BestSeconds);
        }

        /// <summary>
        /// Whether a run beats a record: a lower workbench peak wins; at the same peak, the faster time.
        /// An empty record (peak below 0) is always beaten; an unknown time (below 0) never beats a known one.
        /// </summary>
        public static bool Beats(int peak, float seconds, int recordPeak, float recordSeconds)
        {
            if (recordPeak < 0) return true;
            if (peak != recordPeak) return peak < recordPeak;
            return seconds >= 0f && (recordSeconds < 0f || seconds < recordSeconds);
        }

        /// <summary>The saved record for a level, or null before its first win. Read it; don't change it.</summary>
        public LevelRecord RecordFor(int level) => Find(level);

        public int BestPeakFor(int level) => Find(level)?.BestPeak ?? -1;
        public float BestSecondsFor(int level) => Find(level)?.BestSeconds ?? -1f;

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
