using System;

namespace GoldenMend.Meta
{
    /// <summary>The player's coins. Every grant and spend goes through here, so it is saved and announced once.</summary>
    public sealed class EconomyService
    {
        /// <summary>Price of continuing a failed level with two extra workbench slots.</summary>
        public const int ContinueCost = 60;

        readonly SaveData data;
        readonly ISaveStore store;

        public event Action<int> CoinsChanged;

        public EconomyService(SaveData data, ISaveStore store)
        {
            this.data = data;
            this.store = store;
        }

        public int Coins => data.Coins;

        public bool CanAfford(int amount) => data.Coins >= amount;

        public void Grant(int amount)
        {
            if (amount <= 0) return;
            data.Coins += amount;
            Changed();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || data.Coins < amount) return false;
            data.Coins -= amount;
            Changed();
            return true;
        }

        void Changed()
        {
            store.Save(data);
            CoinsChanged?.Invoke(data.Coins);
        }
    }
}
