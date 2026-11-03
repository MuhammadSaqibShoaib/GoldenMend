using System;

namespace GoldenMend.Meta
{
    /// <summary>The player's coins. Every grant and spend goes through here, so it is saved and announced once.</summary>
    public sealed class EconomyService
    {
        /// <summary>Price of continuing a failed level with two extra workbench slots.</summary>
        public const int ContinueCost = 60;

        /// <summary>Price of one hint once the free ones are gone.</summary>
        public const int HintCost = 40;

        /// <summary>Price of the pull booster, which lifts a buried shard that fits straight into the vessel.</summary>
        public const int PullCost = 90;

        readonly SaveData data;
        readonly ISaveStore store;

        public event Action<int> CoinsChanged;
        public event Action<int> HintsChanged;

        public EconomyService(SaveData data, ISaveStore store)
        {
            this.data = data;
            this.store = store;
        }

        public int Coins => data.Coins;
        public int Hints => data.Hints;

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

        public void GrantHints(int count)
        {
            if (count <= 0) return;
            data.Hints += count;
            HintsChangedNow();
        }

        /// <summary>Uses one hint from the player's stock. False when there are none left.</summary>
        public bool TryUseHint()
        {
            if (data.Hints <= 0) return false;
            data.Hints--;
            HintsChangedNow();
            return true;
        }

        /// <summary>Buys one hint with coins. False when the player can't afford it.</summary>
        public bool TryBuyHint()
        {
            if (!TrySpend(HintCost)) return false;
            GrantHints(1);
            return true;
        }

        void HintsChangedNow()
        {
            store.Save(data);
            HintsChanged?.Invoke(data.Hints);
        }

        void Changed()
        {
            store.Save(data);
            CoinsChanged?.Invoke(data.Coins);
        }
    }
}
