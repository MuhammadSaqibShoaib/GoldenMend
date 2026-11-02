using System.Collections.Generic;
using GoldenMend.Core;
using GoldenMend.Data;
using GoldenMend.Meta;
using GoldenMend.Presentation;
using GoldenMend.Presentation.UI;
using UnityEngine;

namespace GoldenMend.Flow
{
    public enum FlowState
    {
        Home,
        Museum,
        Playing,
        Paused,
        LevelComplete,
        WorkbenchFull,
    }

    /// <summary>
    /// The app's state machine: Home → Play → Level complete / Workbench full → next level or back home, with the
    /// museum, pause and settings around it. Owns no visuals; it tells screens what to show and services what changed.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        const string FirstLevelTip = "Tap a bright shard. It locks in when it touches the gold-framed piece.";

        BoardPresenter board;
        UiRoot ui;
        FeedbackService feedback;
        ProgressService progress;
        EconomyService economy;
        SettingsService settings;

        HomeScreen home;
        MuseumScreen museum;
        GameplayHud hud;
        PausePopup pause;
        SettingsPopup settingsPopup;
        WinSheet win;
        FailPopup fail;
        LevelBanner banner;

        int current = -1;
        bool settingsFromPause;
        List<BuiltLevel> levels;

        public FlowState State { get; private set; }
        public int CurrentLevel => current;
        public BoardPresenter Board => board;
        public ProgressService Progress => progress;
        public EconomyService Economy => economy;

        public void Init(BoardPresenter board, UiRoot ui, FeedbackService feedback, ProgressService progress, EconomyService economy, SettingsService settings)
        {
            this.board = board;
            this.ui = ui;
            this.feedback = feedback;
            this.progress = progress;
            this.economy = economy;
            this.settings = settings;

            home = new HomeScreen(ui) { Play = () => StartLevel(progress.NextLevel), OpenMuseum = ShowMuseum, OpenSettings = () => OpenSettings(false) };
            museum = new MuseumScreen(ui) { Back = ShowHome, PlayLevel = StartLevel };
            hud = new GameplayHud(ui) { Pause = Pause, Hint = () => board.ShowHint() };
            pause = new PausePopup(ui) { Resume = Resume, Restart = () => StartLevel(current), Settings = () => OpenSettings(true), Home = ShowHome };
            settingsPopup = new SettingsPopup(ui)
            {
                Done = CloseSettings,
                SoundChanged = on => settings.Sound = on,
                HapticsChanged = on => settings.Haptics = on,
            };
            win = new WinSheet(ui) { Next = NextAfterWin, Home = ShowHome };
            fail = new FailPopup(ui) { Continue = ContinueRun, Retry = () => StartLevel(current), Home = ShowHome };
            banner = new LevelBanner(ui);

            board.StatusShown += OnStatusShown;
            board.ShardTapped += _ => hud.HideTip();
            settings.Changed += ApplySettings;
            ApplySettings();
        }

        List<BuiltLevel> Levels
        {
            get
            {
                if (levels != null) return levels;
                levels = new List<BuiltLevel>();
                for (int i = 0; i < LevelCatalog.Count; i++) levels.Add(LevelCatalog.Build(i));
                return levels;
            }
        }

        public void ShowHome()
        {
            Time.timeScale = 1f;
            board.Unload();
            HideAll();
            home.Bind(economy.Coins, Levels[progress.NextLevel], progress.NextLevel, progress.CompletedCount, progress.LevelCount, progress.AllComplete);
            home.Show();
            State = FlowState.Home;
        }

        public void ShowMuseum()
        {
            HideAll();
            museum.Bind(Levels, progress.IsUnlocked, progress.IsCompleted, progress.StarsFor);
            museum.Show();
            State = FlowState.Museum;
        }

        public void StartLevel(int index)
        {
            if (!progress.IsUnlocked(index)) return;
            Time.timeScale = 1f;
            current = index;
            HideAll();

            var level = Levels[index];
            board.Load(level);
            hud.Bind(index, level.Spec);
            hud.Show();
            State = FlowState.Playing;

            if (index == 0 && !progress.IsCompleted(0))
            {
                hud.ShowTip(FirstLevelTip);
            }
            else
            {
                banner.Show(level.Spec.Name, BannerDetail(index, level.Spec));
            }
        }

        static string BannerDetail(int index, LevelSpec spec) =>
            spec.Hard ? $"Level {index + 1}  ·  hard  ·  plan your workbench" : $"Level {index + 1}";

        void Pause()
        {
            if (State != FlowState.Playing) return;
            Time.timeScale = 0f;
            banner.Hide();
            pause.Show();
            State = FlowState.Paused;
        }

        void Resume()
        {
            pause.Hide();
            Time.timeScale = 1f;
            State = FlowState.Playing;
        }

        void OpenSettings(bool fromPause)
        {
            settingsFromPause = fromPause;
            if (fromPause) pause.Hide();
            settingsPopup.Bind(settings.Sound, settings.Haptics);
            settingsPopup.Show();
        }

        void CloseSettings()
        {
            settingsPopup.Hide();
            if (settingsFromPause) pause.Show();
            else if (State == FlowState.Home) home.Bind(economy.Coins, Levels[progress.NextLevel], progress.NextLevel, progress.CompletedCount, progress.LevelCount, progress.AllComplete);
        }

        void OnStatusShown(GameStatus status)
        {
            banner.Hide();
            if (status == GameStatus.Won)
            {
                var level = Levels[current];
                int best = level.Solution.MinBenchPeak;
                var result = progress.Complete(current, board.BenchPeak, best, level.Spec.Hard);
                win.Bind(level.Spec.Name, result.Stars, result.Coins, result.FirstClear, board.BenchPeak, best, current + 1 < progress.LevelCount);
                hud.Hide();
                win.Show();
                State = FlowState.LevelComplete;
            }
            else if (status == GameStatus.Failed)
            {
                fail.Bind(EconomyService.ContinueCost, economy.Coins);
                fail.Show();
                State = FlowState.WorkbenchFull;
            }
        }

        void ContinueRun()
        {
            if (!economy.TrySpend(EconomyService.ContinueCost)) return;
            fail.Hide();
            board.ContinueRun();
            State = FlowState.Playing;
        }

        void NextAfterWin()
        {
            if (current + 1 < progress.LevelCount) StartLevel(current + 1);
            else ShowMuseum();
        }

        void ApplySettings()
        {
            feedback.Muted = !settings.Sound;
            feedback.HapticsOn = settings.Haptics;
            ui.SoundOn = settings.Sound;
        }

        void HideAll()
        {
            home.Hide(true);
            museum.Hide(true);
            hud.Hide(true);
            pause.Hide(true);
            settingsPopup.Hide(true);
            win.Hide(true);
            fail.Hide(true);
            hud.HideTip();
            banner.Hide();
        }

        // Screen previews for the screenshot tool, which runs in edit mode without playing a level.

        public void PreviewPause()
        {
            banner.Hide();
            pause.Show();
            State = FlowState.Paused;
        }

        public void PreviewSettings()
        {
            banner.Hide();
            OpenSettings(false);
        }

        public void PreviewWin(int stars, int coins)
        {
            var level = Levels[current];
            win.Bind(level.Spec.Name, stars, coins, true, level.Solution.MinBenchPeak, level.Solution.MinBenchPeak, true);
            banner.Hide();
            hud.Hide(true);
            win.Show();
        }

        public void PreviewFail()
        {
            banner.Hide();
            fail.Bind(EconomyService.ContinueCost, economy.Coins);
            fail.Show();
        }

        public void PreviewBanner()
        {
            var level = Levels[current];
            banner.Show(level.Spec.Name, BannerDetail(current, level.Spec));
        }
    }
}
