using System;
using System.Collections.Generic;

namespace GoldenMend.Meta
{
    /// <summary>Everything the game remembers between sessions. Plain fields so any JSON serializer can read it.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;
        public const int StartingCoins = 200;

        public int Version = CurrentVersion;
        public int Coins = StartingCoins;

        /// <summary>The highest level index the player may play. Equals the level count once everything is done.</summary>
        public int Unlocked;
        public List<LevelRecord> Levels = new List<LevelRecord>();

        public bool Sound = true;
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
        public int BestPeak = -1;
        public int Clears;

        public LevelRecord Clone() => (LevelRecord)MemberwiseClone();
    }
}
