using GoldenMend.Data;
using GoldenMend.Meta;
using GoldenMend.Presentation;
using GoldenMend.Presentation.UI;
using UnityEngine;

namespace GoldenMend.Flow
{
    /// <summary>Wires the whole game together. Used by the bootstrap scene and by the screenshot tool.</summary>
    public static class GameComposition
    {
        public static GameFlow Compose(UiTheme theme, SoundBank sounds, ArtCatalog art, Camera camera, ISaveStore store, Transform parent)
        {
            VesselArt.Catalog = art;
            var data = store.Load();
            var economy = new EconomyService(data, store);
            var progress = new ProgressService(data, LevelCatalog.Count, economy, store);
            var settings = new SettingsService(data, store);

            var boardObject = new GameObject("Board Presenter");
            boardObject.transform.SetParent(parent, false);
            boardObject.AddComponent<AnimationQueue>();
            var feedback = boardObject.AddComponent<FeedbackService>();
            feedback.Init(sounds);
            var board = boardObject.AddComponent<BoardPresenter>();

            var ui = UiRoot.Create(theme, camera, parent);

            var flow = new GameObject("Game Flow").AddComponent<GameFlow>();
            flow.transform.SetParent(parent, false);
            flow.Init(board, ui, feedback, progress, economy, settings, new NoRewardedAds());
            return flow;
        }
    }
}
