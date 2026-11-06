using GoldenMend.Meta;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    public class AdPolicyTests
    {
        sealed class MemoryStore : ISaveStore
        {
            public int Saves;
            public SaveData Load() => new SaveData();
            public void Save(SaveData data) => Saves++;
        }

        [TestCase("DE", true)]
        [TestCase("fr", true)]
        [TestCase("GB", true)]
        [TestCase("CH", true)]
        [TestCase("NO", true)]
        [TestCase("US", false)]
        [TestCase("PK", false)]
        [TestCase("", true)]
        [TestCase(null, true)]
        public void AskFirst_InGdprCountriesAndWhenUnknown(string country, bool expected)
        {
            Assert.That(AdConsent.MustAskFirst(country), Is.EqualTo(expected));
        }

        [Test]
        public void Consent_PromptsOnceInEurope_DefaultsOnElsewhere()
        {
            var store = new MemoryStore();
            var consent = new AdConsent(new SaveData(), store);

            Assert.That(consent.NeedsPrompt("DE"), Is.True);
            Assert.That(consent.Personalized("DE"), Is.False);
            Assert.That(consent.NeedsPrompt("US"), Is.False);
            Assert.That(consent.Personalized("US"), Is.True);

            consent.Set(false);
            Assert.That(consent.NeedsPrompt("DE"), Is.False);
            Assert.That(consent.Personalized("US"), Is.False, "a no from Settings also opts a US player out");
            Assert.That(store.Saves, Is.EqualTo(1));
        }

        [Test]
        public void Interstitial_NotBeforeLevel8_NorEarlyInTheSession()
        {
            var pacing = new InterstitialPacing(0f);
            Assert.That(pacing.ShouldShow(5, 1000f), Is.False, "level 6 is too early");
            Assert.That(pacing.ShouldShow(7, 60f), Is.False, "too early in the session");
            Assert.That(pacing.ShouldShow(7, InterstitialPacing.SessionGrace), Is.True);
        }

        [Test]
        public void Interstitial_KeepsItsGap_AndBacksOffAfterARewardedAd()
        {
            var pacing = new InterstitialPacing(0f);
            pacing.InterstitialShown(200f);
            Assert.That(pacing.ShouldShow(10, 200f + InterstitialPacing.MinGap - 1f), Is.False);
            Assert.That(pacing.ShouldShow(10, 200f + InterstitialPacing.MinGap), Is.True);

            pacing.RewardedShown(600f);
            Assert.That(pacing.ShouldShow(10, 600f + InterstitialPacing.AfterRewarded - 1f), Is.False);
            Assert.That(pacing.ShouldShow(10, 600f + InterstitialPacing.AfterRewarded), Is.True);
        }
    }
}
