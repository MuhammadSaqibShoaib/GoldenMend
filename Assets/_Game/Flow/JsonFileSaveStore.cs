using System;
using System.IO;
using GoldenMend.Meta;
using UnityEngine;

namespace GoldenMend.Flow
{
    /// <summary>
    /// Saves to one JSON file in the persistent data folder. Writes go to a temp file first and then replace the
    /// old save, so a crash mid-write never leaves a broken save behind.
    /// </summary>
    public sealed class JsonFileSaveStore : ISaveStore
    {
        readonly string path;

        public JsonFileSaveStore(string fileName = "save.json")
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public SaveData Load()
        {
            try
            {
                if (!File.Exists(path)) return new SaveData();
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                return data ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Golden Mend: could not read the save ({e.Message}); starting fresh.");
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            try
            {
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Golden Mend: could not write the save ({e.Message}).");
            }
        }
    }
}
