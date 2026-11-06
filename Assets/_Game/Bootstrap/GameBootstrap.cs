using GoldenMend.Flow;
using GoldenMend.Presentation;
using UnityEngine;

namespace GoldenMend.Bootstrap
{
    /// <summary>
    /// Entry point of the game scene: composes the services, board and screens, then opens the main menu.
    /// Analytics (GameAnalytics) and ads (LevelPlay, after the privacy choice) start here.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] SoundBank sounds;

        [Tooltip("Public-domain paintings for the vessels. Leave empty for the generated glazes (Classic build).")]
        [SerializeField] ArtCatalog art;

        void Awake()
        {
            Application.targetFrameRate = 60;
            if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<AudioListener>();

            if (theme == null)
            {
                Debug.LogError("GameBootstrap has no UiTheme. Run Golden Mend > Rebuild Game Scene.", this);
                return;
            }

            var flow = GameComposition.Compose(theme, sounds, art, Camera.main, new JsonFileSaveStore(), transform,
                GameAnalyticsSink.Start(), withAds: true);
            flow.ShowHome();
            flow.StartAds();
        }
    }
}
