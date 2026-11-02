using System;

namespace GoldenMend.Meta
{
    public sealed class SettingsService
    {
        readonly SaveData data;
        readonly ISaveStore store;

        public event Action Changed;

        public SettingsService(SaveData data, ISaveStore store)
        {
            this.data = data;
            this.store = store;
        }

        public bool Sound
        {
            get => data.Sound;
            set => Set(ref data.Sound, value);
        }

        public bool Haptics
        {
            get => data.Haptics;
            set => Set(ref data.Haptics, value);
        }

        void Set(ref bool field, bool value)
        {
            if (field == value) return;
            field = value;
            store.Save(data);
            Changed?.Invoke();
        }
    }
}
