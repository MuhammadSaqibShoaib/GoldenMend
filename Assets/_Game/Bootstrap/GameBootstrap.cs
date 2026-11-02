using GoldenMend.Flow;
using GoldenMend.Presentation;
using UnityEngine;

namespace GoldenMend.Bootstrap
{
    /// <summary>
    /// Entry point of the game scene: composes the services, board and screens, then opens the main menu.
    /// Ads, analytics and remote config will be created here too once they exist.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] UiTheme theme;

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

            var flow = GameComposition.Compose(theme, Camera.main, new JsonFileSaveStore(), transform);
            flow.ShowHome();
        }
    }
}
