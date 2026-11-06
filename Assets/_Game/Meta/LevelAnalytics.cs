namespace GoldenMend.Meta
{
    public enum ProgressionStatus
    {
        Start,
        Complete,
        Fail,
    }

    /// <summary>Where analytics events go. The game uses GameAnalytics; tests and the editor use a stand-in.</summary>
    public interface IAnalyticsSink
    {
        void Progression(ProgressionStatus status, string level, int score = -1);
        void Design(string eventName, float value);
        void Resource(bool earned, string currency, int amount, string itemType, string itemId);
    }

    public sealed class NoAnalytics : IAnalyticsSink
    {
        public void Progression(ProgressionStatus status, string level, int score = -1) { }
        public void Design(string eventName, float value) { }
        public void Resource(bool earned, string currency, int amount, string itemType, string itemId) { }
    }

    /// <summary>
    /// The game's analytics vocabulary, kept apart from the SDK so it can be tested.
    ///
    /// One level attempt is one progression: Start when the level opens, then Complete on a win, or Fail when the
    /// player leaves it unfinished (retry, home, or restarting from pause). A full workbench alone is not a fail,
    /// because the player may still continue; it is counted as its own design event.
    ///
    /// Currencies and item types must also be registered in the GameAnalytics settings (see AnalyticsSetup).
    /// </summary>
    public sealed class LevelAnalytics
    {
        public const string Coins = "Coins";
        public const string Hints = "Hints";

        public const string ItemReward = "Reward";
        public const string ItemBooster = "Booster";
        public const string ItemContinue = "Continue";
        public const string ItemShop = "Shop";
        public const string ItemAd = "Ad";
        public const string ItemGameplay = "Gameplay";

        public static readonly string[] Currencies = { Coins, Hints };
        public static readonly string[] ItemTypes = { ItemReward, ItemBooster, ItemContinue, ItemShop, ItemAd, ItemGameplay };

        readonly IAnalyticsSink sink;
        int openLevel = -1;

        public LevelAnalytics(IAnalyticsSink sink)
        {
            this.sink = sink ?? new NoAnalytics();
        }

        /// <summary>Zero-padded so levels sort in order in the dashboard: level_001 ... level_030.</summary>
        public static string LevelId(int index) => $"level_{index + 1:000}";

        public bool AttemptOpen => openLevel >= 0;

        public void LevelStarted(int index, string tier)
        {
            LevelLeft();
            openLevel = index;
            sink.Progression(ProgressionStatus.Start, LevelId(index));
            if (index == 0) sink.Design("tutorial:start", 0f);
            sink.Design($"level:start_tier:{tier}", index + 1);
        }

        public void LevelWon(int index, int stars, float seconds, int coinsEarned, int hintsUsed, int pullsUsed, int continuesUsed)
        {
            openLevel = -1;
            string id = LevelId(index);
            sink.Progression(ProgressionStatus.Complete, id, stars);
            if (index == 0) sink.Design("tutorial:complete", 0f);
            sink.Design($"level:time:{id}", seconds);
            sink.Design($"level:help:{id}", hintsUsed + pullsUsed + continuesUsed);
            if (coinsEarned > 0) sink.Resource(true, Coins, coinsEarned, ItemReward, id);
        }

        /// <summary>The workbench filled up and the fail popup is showing. The attempt is still open.</summary>
        public void BoardFull(int index) => sink.Design($"level:board_full:{LevelId(index)}", 0f);

        /// <summary>Ends an unfinished attempt as a fail. Safe to call when no attempt is open.</summary>
        public void LevelLeft()
        {
            if (openLevel < 0) return;
            sink.Progression(ProgressionStatus.Fail, LevelId(openLevel));
            openLevel = -1;
        }

        public void Continued(int index, int coinCost)
        {
            sink.Design($"booster:continue:{LevelId(index)}", coinCost);
            sink.Resource(false, Coins, coinCost, ItemContinue, "continue");
        }

        public void ContinuedWithAd(int index)
        {
            sink.Design("ad:rewarded:continue", 0f);
            sink.Design($"booster:continue:{LevelId(index)}", 0f);
        }

        public void InterstitialShown(int index) => sink.Design($"ad:interstitial:{LevelId(index)}", 0f);

        public void ConsentChosen(bool personalized) => sink.Design(personalized ? "privacy:ads:personalized" : "privacy:ads:general", 0f);

        public void HintBought(int coinCost)
        {
            sink.Resource(false, Coins, coinCost, ItemBooster, "hint");
            sink.Resource(true, Hints, 1, ItemShop, "hint");
        }

        public void HintFromAd()
        {
            sink.Design("ad:rewarded:hint", 0f);
            sink.Resource(true, Hints, 1, ItemAd, "hint");
        }

        public void HintUsed(int index)
        {
            sink.Design($"booster:hint:{LevelId(index)}", 0f);
            sink.Resource(false, Hints, 1, ItemGameplay, "hint");
        }

        public void PullBought(int index, int coinCost)
        {
            sink.Design($"booster:pull:{LevelId(index)}", coinCost);
            sink.Resource(false, Coins, coinCost, ItemBooster, "pull");
        }

        public void PullFromAd(int index)
        {
            sink.Design("ad:rewarded:pull", 0f);
            sink.Design($"booster:pull:{LevelId(index)}", 0f);
        }
    }
}
