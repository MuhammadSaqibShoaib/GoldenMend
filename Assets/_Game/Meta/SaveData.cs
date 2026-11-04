using System;
using System.Collections.Generic;

namespace GoldenMend.Meta
{
    /// <summary>Everything the game remembers between sessions. Plain fields so any JSON serializer can read it.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;
        public const int StartingCoins = 200;
        public const int StartingHints = 2;

        public int Version = CurrentVersion;
        public int Coins = StartingCoins;
        public int Hints = StartingHints;

        /// <summary>The highest level index the player may play. Equals the level count once everything is done.</summary>
        public int Unlocked;
        public List<LevelRecord> Levels = new List<LevelRecord>();

        public bool Sound = true;
        public bool Music = true;
        public bool Haptics = true;

        public SaveData Clone()
        {
            var copy = (SaveData)MemberwiseClone();
            copy.Levels = new List<LevelRecord>();
            foreach (var record in Levels) copy.Levels.Add(record.Clone());
            return copy;
        }
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public int Index;
        public int Stars;
        public int Clears;

        /// <summary>Personal record over every win: the lowest workbench peak, then the fastest time at that peak.</summary>
        public int BestPeak = -1;
        public float BestSeconds = -1f;

        /// <summary>The same record over clean wins only (no hint, pull or continue). This is what leaderboards rank.</summary>
        public int CleanClears;
        public int CleanPeak = -1;
        public float CleanSeconds = -1f;

        /// <summary>The taps of the best clean run ("t3 t0 ..."), so a server can replay and check it later.</summary>
        public string CleanRun = "";

        /// <summary>Help used on this level over all runs, for difficulty tuning.</summary>
        public int HintsUsed;
        public int PullsUsed;
        public int ContinuesUsed;

        public LevelRecord Clone() => (LevelRecord)MemberwiseClone();
    }
}
