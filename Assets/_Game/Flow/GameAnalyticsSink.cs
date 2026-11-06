using GameAnalyticsSDK;
using GoldenMend.Meta;
using UnityEngine;

namespace GoldenMend.Flow
{
    /// <summary>
    /// Sends the game's analytics to GameAnalytics. Keys, currencies and item types live in the GameAnalytics settings
    /// asset (Assets/Resources/GameAnalytics/Settings.asset); without keys for the platform the SDK only logs a
    /// warning and sends nothing, so the game runs the same either way. Events are not sent from the editor.
    /// </summary>
    public sealed class GameAnalyticsSink : IAnalyticsSink
    {
        public static GameAnalyticsSink Start()
        {
            if (Object.FindAnyObjectByType<GameAnalytics>() == null)
                new GameObject("GameAnalytics").AddComponent<GameAnalytics>();
            GameAnalytics.Initialize();
            return new GameAnalyticsSink();
        }

        public void Progression(ProgressionStatus status, string level, int score = -1)
        {
            var s = status == ProgressionStatus.Start ? GAProgressionStatus.Start
                : status == ProgressionStatus.Complete ? GAProgressionStatus.Complete
                : GAProgressionStatus.Fail;
            if (score >= 0) GameAnalytics.NewProgressionEvent(s, level, score);
            else GameAnalytics.NewProgressionEvent(s, level);
        }

        public void Design(string eventName, float value) => GameAnalytics.NewDesignEvent(eventName, value);

        public void Resource(bool earned, string currency, int amount, string itemType, string itemId) =>
            GameAnalytics.NewResourceEvent(earned ? GAResourceFlowType.Source : GAResourceFlowType.Sink, currency, amount, itemType, itemId);
    }
}
