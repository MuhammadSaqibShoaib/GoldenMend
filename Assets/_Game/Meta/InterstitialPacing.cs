namespace GoldenMend.Meta
{
    /// <summary>
    /// When a between-levels ad may play. Never in the first levels, never early in a session, at most one every few
    /// minutes, and not right after the player chose to watch a rewarded ad. Only ever on the way from a win to the
    /// next level, never in the middle of play. Times are seconds of real time (any clock that only moves forward).
    /// </summary>
    public sealed class InterstitialPacing
    {
        /// <summary>The first level (1-based) whose win can be followed by an interstitial.</summary>
        public const int FirstLevel = 8;
        public const float SessionGrace = 180f;
        public const float MinGap = 300f;
        public const float AfterRewarded = 180f;

        readonly float sessionStart;
        float lastInterstitial = float.NegativeInfinity;
        float lastRewarded = float.NegativeInfinity;

        public InterstitialPacing(float now)
        {
            sessionStart = now;
        }

        public bool ShouldShow(int wonLevelIndex, float now) =>
            wonLevelIndex + 1 >= FirstLevel
            && now - sessionStart >= SessionGrace
            && now - lastInterstitial >= MinGap
            && now - lastRewarded >= AfterRewarded;

        public void InterstitialShown(float now) => lastInterstitial = now;

        public void RewardedShown(float now) => lastRewarded = now;
    }
}
