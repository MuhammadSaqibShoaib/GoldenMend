using System.Collections.Generic;
using System.Linq;
using GoldenMend.Meta;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    public class LevelAnalyticsTests
    {
        sealed class Recorder : IAnalyticsSink
        {
            public readonly List<string> Events = new List<string>();
            public void Progression(ProgressionStatus status, string level, int score = -1) => Events.Add($"{status}:{level}:{score}");
            public void Design(string eventName, float value) => Events.Add($"design:{eventName}");
            public void Resource(bool earned, string currency, int amount, string itemType, string itemId) =>
                Events.Add($"{(earned ? "source" : "sink")}:{currency}:{amount}:{itemType}:{itemId}");
            public IEnumerable<string> Progressions => Events.Where(e => !e.StartsWith("design") && !e.StartsWith("s"));
        }

        [Test]
        public void LevelIds_AreZeroPadded()
        {
            Assert.That(LevelAnalytics.LevelId(0), Is.EqualTo("level_001"));
            Assert.That(LevelAnalytics.LevelId(29), Is.EqualTo("level_030"));
        }

        [Test]
        public void Win_CompletesTheAttemptWithStarsAndCoins()
        {
            var r = new Recorder();
            var a = new LevelAnalytics(r);
            a.LevelStarted(4, "Normal");
            a.LevelWon(4, 3, 42f, 25, 0, 0, 0);
            a.LevelLeft(); // going home after a win is not a fail

            Assert.That(r.Progressions, Is.EqualTo(new[] { "Start:level_005:-1", "Complete:level_005:3" }));
            Assert.That(r.Events, Has.Member("source:Coins:25:Reward:level_005"));
        }

        [Test]
        public void FullBoard_ThenContinue_ThenWin_IsOneAttempt()
        {
            var r = new Recorder();
            var a = new LevelAnalytics(r);
            a.LevelStarted(7, "Hard");
            a.BoardFull(7);
            a.Continued(7, 60);
            a.LevelWon(7, 1, 90f, 10, 0, 0, 1);

            Assert.That(r.Progressions, Is.EqualTo(new[] { "Start:level_008:-1", "Complete:level_008:1" }));
            Assert.That(r.Events, Has.Member("sink:Coins:60:Continue:continue"));
        }

        [Test]
        public void LeavingOrRetrying_FailsTheOpenAttemptOnce()
        {
            var r = new Recorder();
            var a = new LevelAnalytics(r);
            a.LevelStarted(2, "Normal");
            a.LevelStarted(2, "Normal"); // retry
            a.LevelLeft();               // home
            a.LevelLeft();               // nothing open any more

            Assert.That(r.Progressions, Is.EqualTo(new[]
            {
                "Start:level_003:-1", "Fail:level_003:-1", "Start:level_003:-1", "Fail:level_003:-1",
            }));
            Assert.That(a.AttemptOpen, Is.False);
        }

        [Test]
        public void ResourceEvents_UseOnlyRegisteredCurrenciesAndItemTypes()
        {
            var r = new Recorder();
            var a = new LevelAnalytics(r);
            a.LevelStarted(0, "Normal");
            a.HintBought(40);
            a.HintFromAd();
            a.HintUsed(0);
            a.PullBought(0, 90);
            a.Continued(0, 60);
            a.LevelWon(0, 2, 30f, 15, 1, 1, 1);

            foreach (var e in r.Events.Where(e => e.StartsWith("source:") || e.StartsWith("sink:")))
            {
                var parts = e.Split(':');
                Assert.That(LevelAnalytics.Currencies, Has.Member(parts[1]), e);
                Assert.That(LevelAnalytics.ItemTypes, Has.Member(parts[3]), e);
            }
        }
    }
}
