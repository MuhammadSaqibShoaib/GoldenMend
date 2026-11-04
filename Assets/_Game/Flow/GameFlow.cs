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
        IRewardedAds ads;

        HomeScreen home;
        MuseumScreen museum;
        VesselDetails details;
        GameplayHud hud;
        PausePopup pause;
        SettingsPopup settingsPopup;
        WinSheet win;
        FailPopup fail;
        LevelBanner banner;
        HelpOffer offer;

        int current = -1;
        bool settingsFromPause;
        List<BuiltLevel> levels;

        public FlowState State { get; private set; }
        public int CurrentLevel => current;
        public BoardPresenter Board => board;
        public ProgressService Progress => progress;
        public EconomyService Economy => economy;

        public void Init(BoardPresenter board, UiRoot ui, FeedbackService feedback, ProgressService progress, EconomyService economy, SettingsService settings,
            IRewardedAds ads = null)
        {
            this.ads = ads ?? new NoRewardedAds();
            this.board = board;
            this.ui = ui;
            this.feedback = feedback;
            this.progress = progress;
            this.economy = economy;
            this.settings = settings;

            home = new HomeScreen(ui) { Play = () => StartLevel(progress.NextLevel), OpenMuseum = ShowMuseum, OpenSettings = () => OpenSettings(false) };
            museum = new MuseumScreen(ui) { Back = ShowHome, PlayLevel = StartLevel, OpenDetails = ShowDetails };
            details = new VesselDetails(ui) { Close = () => details.Hide() };
            hud = new GameplayHud(ui) { Pause = Pause, Hint = OnHint, Pull = OfferPull };
            hud.SetPullPrice(EconomyService.PullCost);
            offer = new HelpOffer(ui) { Close = CloseOffer };
            pause = new PausePopup(ui) { Resume = Resume, Restart = () => StartLevel(current), Settings = () => OpenSettings(true), Home = ShowHome };
            settingsPopup = new SettingsPopup(ui)
            {
                Done = CloseSettings,
                SoundChanged = on => settings.Sound = on,
                HapticsChanged = on => settings.Haptics = on,
                MusicChanged = on => settings.Music = on,
            };
            win = new WinSheet(ui) { Next = NextAfterWin, Home = ShowHome };
            fail = new FailPopup(ui) { Continue = ContinueRun, Retry = () => StartLevel(current), Home = ShowHome };
            banner = new LevelBanner(ui);

            board.StatusShown += OnStatusShown;
            board.ShardTapped += _ =>
            {
                hud.HideTip();
                RefreshHelpers();
            };
            board.Idle += hud.NudgeHint;
            economy.HintsChanged += hud.SetHints;
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

        public void ShowDetails(int index)
        {
            if (!progress.IsUnlocked(index)) return;
            var r = progress.RecordFor(index);
            var record = r == null ? new VesselRecord(0, 0, -1, -1f, -1, -1f)
                : new VesselRecord(r.Clears, r.Stars, r.BestPeak, r.BestSeconds, r.CleanPeak, r.CleanSeconds);
            details.Bind(Levels[index], record);
            details.Play = () => StartLevel(index);
            details.Show();
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
            RefreshHelpers();
            hud.Show();
            State = FlowState.Playing;

            if (index == 0 && !progress.IsCompleted(0))
            {
                hud.ShowTip(FirstLevelTip);
            }
            else
            {
                banner.Show(VesselArt.DisplayName(level.Spec), BannerDetail(index, level.Spec));
            }
        }

        static string BannerDetail(int index, LevelSpec spec)
        {
            if (spec.LockedBenchSlots > 0) return "Hard  ·  one bench slot opens after 3 repairs";
            string lead = $"Level {index + 1}  ·  {VesselArt.Byline(spec)}";
            return spec.Hard ? $"{lead}  ·  hard" : lead;
        }

        void RefreshHelpers()
        {
            hud.SetHints(economy.Hints);
            hud.SetPullAvailable(board.CanPull);
        }

        // A hint comes from the stock; when it is empty the offer sells one (coins or an ad) and uses it at once.
        void OnHint()
        {
            if (State != FlowState.Playing || board.HintActive) return;
            if (economy.Hints > 0)
            {
                if (board.ShowHint()) economy.TryUseHint();
                return;
            }

            offer.Bind("Need a hint?", "Shows the piece that fits right now,\neven if it is buried.", "Get hint", EconomyService.HintCost, economy.Coins, ads.IsReady);
            offer.Buy = () =>
            {
                if (!economy.TryBuyHint()) return;
                CloseOffer();
                if (board.ShowHint()) economy.TryUseHint();
            };
            offer.WatchAd = () => ads.Show(watched =>
            {
                CloseOffer();
                if (!watched) return;
                economy.GrantHints(1);
                if (board.ShowHint()) economy.TryUseHint();
            });
            OpenOffer();
        }

        // The pull booster always asks first: it costs more than a hint and lifts a piece out for the player.
        void OfferPull()
        {
            if (State != FlowState.Playing || !board.CanPull) return;
            offer.Bind("Pull a piece", "Lifts a buried piece that fits\nstraight into the vessel.", "Pull it", EconomyService.PullCost, economy.Coins, ads.IsReady, ui.Theme.IconPull);
            offer.Buy = () =>
            {
                if (!economy.TrySpend(EconomyService.PullCost)) return;
                CloseOffer();
                if (!board.Pull()) economy.Grant(EconomyService.PullCost);
                RefreshHelpers();
            };
            offer.WatchAd = () => ads.Show(watched =>
            {
                CloseOffer();
                if (watched) board.Pull();
                RefreshHelpers();
            });
            OpenOffer();
        }

        void OpenOffer()
        {
            board.AcceptPointerInput = false;
            offer.Show();
        }

        void CloseOffer()
        {
            offer.Hide();
            board.AcceptPointerInput = true;
        }

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
            settingsPopup.Bind(settings.Sound, settings.Music, settings.Haptics);
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
                var run = new RunStats(board.BenchPeak, board.PlaySeconds, board.HintsUsed, board.PullsUsed, board.ContinuesUsed, board.Moves);
                var result = progress.Complete(current, run, best, level.Spec.Hard);
                win.Bind(VesselArt.DisplayName(level.Spec), null, result.Stars, result.Coins, result.FirstClear, board.BenchPeak, best,
                    current + 1 < progress.LevelCount, RecordLine(run, result), level);
                hud.Hide();
                win.Show();
                feedback.Coins();
                State = FlowState.LevelComplete;
            }
            else if (status == GameStatus.Failed)
            {
                fail.Bind(EconomyService.ContinueCost, economy.Coins);
                fail.Show();
                State = FlowState.WorkbenchFull;
            }
        }

        /// <summary>The win sheet's record line: this run's time against the player's best on this level.</summary>
        static string RecordLine(RunStats run, LevelResult result)
        {
            string line;
            if (result.NewRecord) line = $"New personal best!   {Clock(run.Seconds)}";
            else if (result.FirstClear) line = $"Time  {Clock(run.Seconds)}";
            else line = $"Time  {Clock(run.Seconds)}   ·   best  {Clock(result.BestSeconds)} (workbench {result.BestPeak})";
            if (result.CleanBonus > 0) line += $"   ·   no-help bonus +{result.CleanBonus}";
            return line;
        }

        static string Clock(float seconds) => VesselDetails.Clock(seconds);

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
            feedback.MusicOn = settings.Music;
            ui.SoundOn = settings.Sound;
        }

        void HideAll()
        {
            home.Hide(true);
            museum.Hide(true);
            details.Hide(true);
            hud.Hide(true);
            pause.Hide(true);
            settingsPopup.Hide(true);
            win.Hide(true);
            fail.Hide(true);
            offer.Hide(true);
            board.AcceptPointerInput = true;
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
            win.Bind(VesselArt.DisplayName(level.Spec), null, stars, coins, true, level.Solution.MinBenchPeak, level.Solution.MinBenchPeak, true,
                $"New personal best!   0:42   ·   no-help bonus +{ProgressService.CleanBonusCoins}", level);
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

        public void PreviewOffer()
        {
            banner.Hide();
            offer.Bind("Need a hint?", "Shows the piece that fits right now,\neven if it is buried.", "Get hint", EconomyService.HintCost, economy.Coins, true);
            offer.Show();
        }

        /// <summary>Shows any level on the board, locked or not, with no popups over it (screenshots).</summary>
        public void PreviewLevel(int index)
        {
            offer.Hide(true);
            current = index;
            HideAll();
            board.Load(Levels[index]);
            hud.Bind(index, Levels[index].Spec);
            RefreshHelpers();
            hud.Show();
        }

        public void PreviewBanner()
        {
            var level = Levels[current];
            banner.Show(VesselArt.DisplayName(level.Spec), BannerDetail(current, level.Spec));
        }
    }
}
