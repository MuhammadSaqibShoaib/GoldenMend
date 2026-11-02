using System;
using GoldenMend.Core;
using GoldenMend.Data;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// In-game HUD drawn with IMGUI: level info, restart, hint, level skip and the win/fail panels.
    /// To be replaced by UI Toolkit screens.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        const float ReferenceHeight = 1280f;

        BoardPresenter board;
        Func<int> levelIndex;
        Action restart;
        Action next;
        Action previous;
        GameStatus panel = GameStatus.Playing;
        GUIStyle title, body, button, bigButton, badge;

        public void Init(BoardPresenter board, Func<int> levelIndex, Action restart, Action next, Action previous)
        {
            this.board = board;
            this.levelIndex = levelIndex;
            this.restart = restart;
            this.next = next;
            this.previous = previous;
            board.StatusShown += status => panel = status;
        }

        public void HidePanel() => panel = GameStatus.Playing;

        void OnGUI()
        {
            if (board == null || board.Level == null) return;
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            var spec = board.Level.Spec;
            GUI.Label(new Rect(24, 18, width - 48, 44), $"Level {levelIndex() + 1} · {spec.Name}", title);
            if (spec.Hard) GUI.Label(new Rect(24, 64, 90, 30), "HARD", badge);

            var solution = board.Level.Solution;
            string stats = $"Taps {board.TapCount}   Bench peak {board.BenchPeak} (best {solution.MinBenchPeak})";
            GUI.Label(new Rect(spec.Hard ? 124 : 24, 64, width - 140, 30), stats, body);

            float bw = 112f, bh = 56f, gap = 10f;
            float y = 104f;
            if (GUI.Button(new Rect(24, y, bw, bh), "Restart", button)) restart();
            if (GUI.Button(new Rect(24 + (bw + gap), y, bw, bh), "Hint", button)) board.ShowHint();
            if (GUI.Button(new Rect(width - 24 - bw * 2 - gap, y, bw, bh), "< Prev", button)) previous();
            if (GUI.Button(new Rect(width - 24 - bw, y, bw, bh), "Next >", button)) next();

            if (panel == GameStatus.Won) Panel(width, "Restored!", "The gold seams hold. On to the next piece.", "Next vessel", next);
            else if (panel == GameStatus.Failed) Panel(width, "Workbench full", "No room for that shard. (Continue offer goes here.)", "Try again", restart);
        }

        void Panel(float width, string heading, string text, string action, Action onClick)
        {
            float pw = Mathf.Min(560f, width - 48f);
            var rect = new Rect((width - pw) / 2f, 470f, pw, 300f);
            GUI.Box(rect, GUIContent.none);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 24, rect.y + 28, pw - 48, 60), heading, title);
            GUI.Label(new Rect(rect.x + 24, rect.y + 96, pw - 48, 80), text, body);
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 196, pw - 120, 76), action, bigButton))
            {
                panel = GameStatus.Playing;
                onClick();
            }
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.86f, 0.5f) } };
            body = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, normal = { textColor = new Color(0.92f, 0.9f, 0.86f) } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            bigButton = new GUIStyle(GUI.skin.button) { fontSize = 32, fontStyle = FontStyle.Bold };
            badge = new GUIStyle(GUI.skin.box) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.5f, 0.4f) } };
        }
    }
}
