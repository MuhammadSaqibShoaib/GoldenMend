using GoldenMend.Meta;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    public class ProgressTests
    {
        static (ProgressService progress, EconomyService economy, MemorySaveStore store) Fresh(int levels = 3)
        {
            var store = new MemorySaveStore();
            var data = store.Load();
            var economy = new EconomyService(data, store);
            return (new ProgressService(data, levels, economy, store), economy, store);
        }

        [Test]
        public void NewPlayer_StartsAtLevelOneWithStartingCoins()
        {
            var (progress, economy, _) = Fresh();

            Assert.That(progress.NextLevel, Is.EqualTo(0));
            Assert.That(progress.IsUnlocked(0), Is.True);
            Assert.That(progress.IsUnlocked(1), Is.False);
            Assert.That(economy.Coins, Is.EqualTo(SaveData.StartingCoins));
        }

        [Test]
        public void CompletingALevel_UnlocksTheNextAndSaves()
        {
            var (progress, _, store) = Fresh();

            progress.Complete(0, benchPeak: 0, bestPeak: 0, hard: false);

            Assert.That(progress.NextLevel, Is.EqualTo(1));
            Assert.That(progress.IsCompleted(0), Is.True);
            Assert.That(store.SaveCount, Is.GreaterThan(0));
        }

        [TestCase(2, 2, 3)]
        [TestCase(1, 2, 3)]
        [TestCase(3, 2, 2)]
        [TestCase(5, 2, 1)]
        public void Stars_DependOnHowCloseTheBenchPeakIsToTheBest(int peak, int best, int stars)
        {
            Assert.That(ProgressService.StarRating(peak, best), Is.EqualTo(stars));
        }

        [Test]
        public void FirstClear_PaysFullReward_ReplaysPayLess()
        {
            var (progress, economy, _) = Fresh();
            int start = economy.Coins;

            var first = progress.Complete(0, 0, 0, hard: false);
            var replay = progress.Complete(0, 0, 0, hard: false);

            Assert.That(first.Coins, Is.EqualTo(30 + ProgressService.CleanBonusCoins));
            Assert.That(first.FirstClear, Is.True);
            Assert.That(replay.Coins, Is.LessThan(first.Coins));
            Assert.That(economy.Coins, Is.EqualTo(start + first.Coins + replay.Coins));
        }

        [Test]
        public void HardLevels_PayDouble()
        {
            var (progress, _, _) = Fresh();

            Assert.That(progress.Complete(0, 0, 0, hard: true).Coins, Is.EqualTo(60 + ProgressService.CleanBonusCoins));
        }

        [Test]
        public void StarsKeepTheBestResult()
        {
            var (progress, _, _) = Fresh();
            progress.Complete(0, 0, 0, false);
            progress.Complete(0, 5, 0, false);

            Assert.That(progress.StarsFor(0), Is.EqualTo(3));
        }

        [Test]
        public void CleanBonus_IsPaidOnlyForTheFirstWinWithoutHelp()
        {
            var (progress, _, _) = Fresh();

            var helped = progress.Complete(0, new RunStats(0, 30f, hints: 1), 0, false);
            var clean = progress.Complete(0, new RunStats(0, 30f), 0, false);
            var again = progress.Complete(0, new RunStats(0, 30f), 0, false);

            Assert.That(helped.CleanBonus, Is.Zero);
            Assert.That(clean.CleanBonus, Is.EqualTo(ProgressService.CleanBonusCoins));
            Assert.That(again.CleanBonus, Is.Zero);
        }

        [Test]
        public void PersonalRecord_IsTheLowestPeakThenTheFastestTime()
        {
            var (progress, _, _) = Fresh();

            var first = progress.Complete(0, new RunStats(3, 50f), 2, false);
            var slower = progress.Complete(0, new RunStats(3, 60f), 2, false);
            var faster = progress.Complete(0, new RunStats(3, 40f), 2, false);
            var lowerPeak = progress.Complete(0, new RunStats(2, 90f), 2, false);

            Assert.That(first.NewRecord, Is.False, "a first win sets the record without announcing one");
            Assert.That(slower.NewRecord, Is.False);
            Assert.That(faster.NewRecord, Is.True);
            Assert.That(lowerPeak.NewRecord, Is.True);
            Assert.That(progress.BestPeakFor(0), Is.EqualTo(2));
            Assert.That(progress.BestSecondsFor(0), Is.EqualTo(90f));
        }

        [Test]
        public void CleanRecord_IgnoresRunsThatUsedHelp()
        {
            var store = new MemorySaveStore();
            var data = store.Load();
            var progress = new ProgressService(data, 3, new EconomyService(data, store), store);

            progress.Complete(0, new RunStats(1, 20f, pulls: 1, moves: "p4"), 1, false);
            progress.Complete(0, new RunStats(2, 45f, moves: "t0 t1"), 1, false);

            var record = data.Levels[0];
            Assert.That(record.BestPeak, Is.EqualTo(1));
            Assert.That(record.CleanPeak, Is.EqualTo(2));
            Assert.That(record.CleanRun, Is.EqualTo("t0 t1"));
            Assert.That(record.PullsUsed, Is.EqualTo(1));
        }

        [Test]
        public void AfterTheLastLevel_EverythingIsCompleteAndNextStaysInRange()
        {
            var (progress, _, _) = Fresh(levels: 2);
            progress.Complete(0, 0, 0, false);
            progress.Complete(1, 0, 0, false);

            Assert.That(progress.AllComplete, Is.True);
            Assert.That(progress.CompletedCount, Is.EqualTo(2));
            Assert.That(progress.NextLevel, Is.EqualTo(1));
        }
    }

    public class EconomyTests
    {
        [Test]
        public void Spending_FailsWithoutEnoughCoins()
        {
            var store = new MemorySaveStore();
            var economy = new EconomyService(store.Load(), store);

            Assert.That(economy.TrySpend(economy.Coins + 1), Is.False);
            Assert.That(economy.TrySpend(10), Is.True);
            Assert.That(economy.Coins, Is.EqualTo(SaveData.StartingCoins - 10));
        }

        [Test]
        public void Hints_StartWithAFreeStockAndCanBeBought()
        {
            var store = new MemorySaveStore();
            var economy = new EconomyService(store.Load(), store);

            Assert.That(economy.Hints, Is.EqualTo(SaveData.StartingHints));
            for (int i = 0; i < SaveData.StartingHints; i++) Assert.That(economy.TryUseHint(), Is.True);
            Assert.That(economy.TryUseHint(), Is.False);

            Assert.That(economy.TryBuyHint(), Is.True);
            Assert.That(economy.Hints, Is.EqualTo(1));
            Assert.That(economy.Coins, Is.EqualTo(SaveData.StartingCoins - EconomyService.HintCost));
        }

        [Test]
        public void CoinChanges_RaiseAnEvent()
        {
            var store = new MemorySaveStore();
            var economy = new EconomyService(store.Load(), store);
            int seen = -1;
            economy.CoinsChanged += c => seen = c;

            economy.Grant(5);

            Assert.That(seen, Is.EqualTo(SaveData.StartingCoins + 5));
        }
    }

    public class SettingsTests
    {
        [Test]
        public void Toggles_ArePersisted()
        {
            var store = new MemorySaveStore();
            var settings = new SettingsService(store.Load(), store);

            settings.Sound = false;
            settings.Haptics = false;

            var reloaded = new SettingsService(store.Load(), store);
            Assert.That(reloaded.Sound, Is.False);
            Assert.That(reloaded.Haptics, Is.False);
        }
    }
}
