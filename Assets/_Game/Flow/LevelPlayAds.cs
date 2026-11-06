using System;
using GoldenMend.Meta;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace GoldenMend.Flow
{
    /// <summary>The LevelPlay dashboard IDs. Not secrets: they ship inside every build.</summary>
    public static class AdIds
    {
        public const string AndroidAppKey = "295532fe5";
        public const string AndroidRewarded = "pnxu6dpmkcyr7va2";
        public const string AndroidInterstitial = "3jfzx2hxh9dg16g8";

        /// <summary>
        /// Opens LevelPlay's Test Suite after the SDK starts, to check each network on a device.
        /// TEMPORARY: must be false for any build that goes to players.
        /// </summary>
        public const bool TestSuite = false;

        public static bool Configured =>
            !string.IsNullOrEmpty(AndroidAppKey) && !string.IsNullOrEmpty(AndroidRewarded) && !string.IsNullOrEmpty(AndroidInterstitial);
    }

    /// <summary>
    /// Rewarded and interstitial ads through LevelPlay. Privacy choices are applied before the SDK starts; both ads
    /// are loaded as soon as it is ready and reloaded after each show or failure (with a growing delay), so an ad is
    /// normally ready when the player reaches an offer. If the IDs are missing or the SDK fails, the ads are simply
    /// never ready and the game falls back to coins only.
    /// </summary>
    public sealed class LevelPlayAds : MonoBehaviour, IRewardedAds, IInterstitialAds
    {
        /// <summary>How long to wait after a rewarded ad closes for its reward callback, which can arrive late.</summary>
        const float RewardGrace = 1.5f;

        LevelPlayRewardedAd rewarded;
        LevelPlayInterstitialAd interstitial;
        Action<bool> rewardedDone;
        Action interstitialDone;
        bool rewardEarned;
        float rewardedRetry = 5f;
        float interstitialRetry = 5f;

        public event Action RewardedShown;
        public event Action InterstitialShown;

        bool IRewardedAds.IsReady => rewarded != null && rewarded.IsAdReady();
        bool IInterstitialAds.IsReady => interstitial != null && interstitial.IsAdReady();

        public static LevelPlayAds Create(Transform parent)
        {
            var go = new GameObject("LevelPlay Ads");
            go.transform.SetParent(parent, false);
            return go.AddComponent<LevelPlayAds>();
        }

        /// <summary>Applies the privacy choices, then starts the SDK. Call once, after consent is settled.</summary>
        public void Initialize(bool personalized)
        {
            if (!AdIds.Configured || !Application.isMobilePlatform && !Application.isEditor)
            {
                Debug.Log("Golden Mend: LevelPlay ad IDs are not set yet; ads are off.");
                return;
            }

            // Privacy settings must be in place before Init. A no to personalized ads also counts as the US
            // "do not sell or share" opt-out. Golden Mend is a general-audience game, so it is not child-directed.
            LevelPlayPrivacySettings.SetGDPRConsent(personalized);
            LevelPlayPrivacySettings.SetCCPA(!personalized);
            LevelPlayPrivacySettings.SetCOPPA(false);

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
#pragma warning disable CS0162
            if (AdIds.TestSuite) LevelPlay.SetMetaData("is_test_suite", "enable");
#pragma warning restore CS0162
            LevelPlay.Init(AdIds.AndroidAppKey);
        }

        /// <summary>Applies a changed choice from Settings to future ad requests.</summary>
        public void UpdateConsent(bool personalized)
        {
            LevelPlayPrivacySettings.SetGDPRConsent(personalized);
            LevelPlayPrivacySettings.SetCCPA(!personalized);
        }

        void OnInitSuccess(LevelPlayConfiguration config)
        {
            Debug.Log("LevelPlay SDK initialized successfully");

            rewarded = new LevelPlayRewardedAd(AdIds.AndroidRewarded);
            rewarded.OnAdLoaded += OnRewardedLoaded;
            rewarded.OnAdLoadFailed += OnRewardedLoadFailed;
            rewarded.OnAdDisplayFailed += OnRewardedDisplayFailed;
            rewarded.OnAdRewarded += OnRewardedRewarded;
            rewarded.OnAdClosed += OnRewardedClosed;

            interstitial = new LevelPlayInterstitialAd(AdIds.AndroidInterstitial);
            interstitial.OnAdLoaded += OnInterstitialLoaded;
            interstitial.OnAdLoadFailed += OnInterstitialLoadFailed;
            interstitial.OnAdDisplayFailed += OnInterstitialDisplayFailed;
            interstitial.OnAdClosed += OnInterstitialClosed;

            // Eager preload: the offers only show the ad button when an ad is ready, so keep one loaded.
            rewarded.LoadAd();
            interstitial.LoadAd();

#pragma warning disable CS0162
            if (AdIds.TestSuite) LevelPlay.LaunchTestSuite();
#pragma warning restore CS0162
        }

        void OnInitFailed(LevelPlayInitError error) => Debug.LogWarning($"LevelPlay initialization failed: {error.ErrorMessage}");

        // ---- Rewarded --------------------------------------------------------------------------------------------

        public void Show(Action<bool> done)
        {
            if (rewarded == null || !rewarded.IsAdReady() || rewardedDone != null)
            {
                done?.Invoke(false);
                return;
            }
            rewardedDone = done;
            rewardEarned = false;
            RewardedShown?.Invoke();
            rewarded.ShowAd();
        }

        void OnRewardedLoaded(LevelPlayAdInfo info) => rewardedRetry = 5f;

        void OnRewardedLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"Rewarded ad failed to load: {error.ErrorMessage}");
            Invoke(nameof(LoadRewarded), rewardedRetry);
            rewardedRetry = Mathf.Min(rewardedRetry * 2f, 120f);
        }

        void OnRewardedDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"Rewarded ad failed to display: {error.ErrorMessage}");
            FinishRewarded(false);
            LoadRewarded();
        }

        // The reward is granted here, never on close. It may arrive after the close callback.
        void OnRewardedRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            rewardEarned = true;
            if (!IsInvoking(nameof(ResolveRewardedAfterClose))) return;
            CancelInvoke(nameof(ResolveRewardedAfterClose));
            FinishRewarded(true);
        }

        void OnRewardedClosed(LevelPlayAdInfo info)
        {
            LoadRewarded();
            if (rewardEarned) FinishRewarded(true);
            else Invoke(nameof(ResolveRewardedAfterClose), RewardGrace);
        }

        void ResolveRewardedAfterClose() => FinishRewarded(rewardEarned);

        void FinishRewarded(bool earned)
        {
            var done = rewardedDone;
            rewardedDone = null;
            done?.Invoke(earned);
        }

        void LoadRewarded() => rewarded?.LoadAd();

        // ---- Interstitial ----------------------------------------------------------------------------------------

        public void Show(Action done)
        {
            if (interstitial == null || !interstitial.IsAdReady() || interstitialDone != null)
            {
                done?.Invoke();
                return;
            }
            interstitialDone = done;
            InterstitialShown?.Invoke();
            interstitial.ShowAd();
        }

        void OnInterstitialLoaded(LevelPlayAdInfo info) => interstitialRetry = 5f;

        void OnInterstitialLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"Interstitial ad failed to load: {error.ErrorMessage}");
            Invoke(nameof(LoadInterstitial), interstitialRetry);
            interstitialRetry = Mathf.Min(interstitialRetry * 2f, 120f);
        }

        void OnInterstitialDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"Interstitial ad failed to display: {error.ErrorMessage}");
            FinishInterstitial();
            LoadInterstitial();
        }

        void OnInterstitialClosed(LevelPlayAdInfo info)
        {
            FinishInterstitial();
            LoadInterstitial();
        }

        void FinishInterstitial()
        {
            var done = interstitialDone;
            interstitialDone = null;
            done?.Invoke();
        }

        void LoadInterstitial() => interstitial?.LoadAd();

        void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            if (rewarded != null)
            {
                rewarded.OnAdLoaded -= OnRewardedLoaded;
                rewarded.OnAdLoadFailed -= OnRewardedLoadFailed;
                rewarded.OnAdDisplayFailed -= OnRewardedDisplayFailed;
                rewarded.OnAdRewarded -= OnRewardedRewarded;
                rewarded.OnAdClosed -= OnRewardedClosed;
                rewarded.DestroyAd();
            }
            if (interstitial != null)
            {
                interstitial.OnAdLoaded -= OnInterstitialLoaded;
                interstitial.OnAdLoadFailed -= OnInterstitialLoadFailed;
                interstitial.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
                interstitial.OnAdClosed -= OnInterstitialClosed;
                interstitial.DestroyAd();
            }
        }
    }
}
