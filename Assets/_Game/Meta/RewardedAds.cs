using System;

namespace GoldenMend.Meta
{
    /// <summary>
    /// A rewarded video provider. The game only asks whether an ad is ready and whether the player earned the
    /// reward, so an ad network can be plugged in later without touching the flow.
    /// </summary>
    public interface IRewardedAds
    {
        bool IsReady { get; }

        /// <summary>Plays an ad; <paramref name="done"/> gets true only if the player watched it to the end.</summary>
        void Show(Action<bool> done);
    }

    /// <summary>A full-screen ad between levels. <paramref name="done"/> always runs, shown or not.</summary>
    public interface IInterstitialAds
    {
        bool IsReady { get; }
        void Show(Action done);
    }

    public sealed class NoInterstitialAds : IInterstitialAds
    {
        public bool IsReady => false;
        public void Show(Action done) => done?.Invoke();
    }

    /// <summary>Used until an ad network is integrated: never ready, so the ad buttons stay hidden.</summary>
    public sealed class NoRewardedAds : IRewardedAds
    {
        public bool IsReady => false;

        public void Show(Action<bool> done) => done?.Invoke(false);
    }
}
