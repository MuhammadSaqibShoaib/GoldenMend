using GoldenMend.Data;
using GoldenMend.Presentation;
using UnityEngine;

namespace GoldenMend.Bootstrap
{
    /// <summary>
    /// Composition root for the game scene: creates the board and HUD and wires them together.
    /// Services (ads, analytics, save) will be created here too once they exist.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        const string LevelKey = "goldenmend.level";

        BoardPresenter board;
        GameHud hud;
        int levelIndex;

        void Awake()
        {
            Application.targetFrameRate = 60;
            if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<AudioListener>();

            var boardObject = new GameObject("Board Presenter");
            boardObject.AddComponent<AnimationQueue>();
            boardObject.AddComponent<FeedbackService>();
            board = boardObject.AddComponent<BoardPresenter>();

            hud = gameObject.AddComponent<GameHud>();
            hud.Init(board, () => levelIndex, Restart, Next, Previous);

            levelIndex = Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, LevelCatalog.Count - 1);
        }

        void Start() => LoadLevel(levelIndex);

        void LoadLevel(int index)
        {
            levelIndex = (index % LevelCatalog.Count + LevelCatalog.Count) % LevelCatalog.Count;
            PlayerPrefs.SetInt(LevelKey, levelIndex);
            hud.HidePanel();
            board.Load(LevelCatalog.Build(levelIndex));
        }

        void Restart() => LoadLevel(levelIndex);

        void Next() => LoadLevel(levelIndex + 1);

        void Previous() => LoadLevel(levelIndex - 1);
    }
}
