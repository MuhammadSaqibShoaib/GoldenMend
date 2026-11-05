namespace GoldenMend.Flow
{
    /// <summary>What the Android back button does.</summary>
    public enum BackAction
    {
        CloseDetails,
        CloseSettings,
        CloseOffer,
        Pause,
        Resume,
        GoHome,
        LeaveApp,
    }

    /// <summary>
    /// The back button's rule, kept apart from the screens so it can be tested: the topmost popup closes first, then
    /// back steps out of the current state. On Home it leaves the app (sends it to the background, as Android does).
    /// </summary>
    public static class BackNavigation
    {
        public static BackAction For(FlowState state, bool detailsOpen, bool settingsOpen, bool offerOpen)
        {
            if (detailsOpen) return BackAction.CloseDetails;
            if (settingsOpen) return BackAction.CloseSettings;
            if (offerOpen) return BackAction.CloseOffer;
            switch (state)
            {
                case FlowState.Playing: return BackAction.Pause;
                case FlowState.Paused: return BackAction.Resume;
                case FlowState.Home: return BackAction.LeaveApp;
                default: return BackAction.GoHome;
            }
        }
    }
}
