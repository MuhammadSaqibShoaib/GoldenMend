namespace GoldenMend.Meta
{
    /// <summary>Where the save lives. The game uses a JSON file; tests use <see cref="MemorySaveStore"/>.</summary>
    public interface ISaveStore
    {
        SaveData Load();
        void Save(SaveData data);
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        SaveData saved;

        public int SaveCount { get; private set; }

        public SaveData Load() => saved != null ? saved.Clone() : new SaveData();

        public void Save(SaveData data)
        {
            saved = data.Clone();
            SaveCount++;
        }
    }
}
