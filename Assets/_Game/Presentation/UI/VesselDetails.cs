using System;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenMend.Presentation.UI
{
    /// <summary>The player's record on one vessel, as the details card shows it. Peaks and times below 0 mean "none yet".</summary>
    public readonly struct VesselRecord
    {
        public int Clears { get; }
        public int Stars { get; }
        public int BestPeak { get; }
        public float BestSeconds { get; }
        public int CleanPeak { get; }
        public float CleanSeconds { get; }

        public VesselRecord(int clears, int stars, int bestPeak, float bestSeconds, int cleanPeak, float cleanSeconds)
        {
            Clears = clears;
            Stars = stars;
            BestPeak = bestPeak;
            BestSeconds = bestSeconds;
            CleanPeak = cleanPeak;
            CleanSeconds = cleanSeconds;
        }
    }

    /// <summary>
    /// Opens from a museum card: the vessel large, its stars, and the player's records against the best possible
    /// workbench, with the way into the level. The best possible stays hidden until the first win, so it never
    /// gives away part of the solution.
    /// </summary>
    public sealed class VesselDetails : UiView
    {
        public Action Play;
        public Action Close;

        static readonly Vector2 CardSize = new Vector2(920f, 1500f);
        const float RowHeight = 92f;

        readonly RectTransform card;
        readonly RectTransform showcase;
        readonly RectTransform stars;
        readonly Text byline;
        readonly Text title;
        readonly Text state;
        readonly Text[] values = new Text[4];
        readonly Text playLabel;

        public VesselDetails(UiRoot ui) : base(ui, "Vessel Details", popup: true)
        {
            card = B.Popup(Root, CardSize, new Vector2(0f, -20f), () => Close?.Invoke(), out title);
            showcase = B.Inset("Niche", card, Anchor.Top, new Vector2(0f, -176f), new Vector2(CardSize.x - 136f, 500f)).rectTransform;

            byline = B.Text("Byline", card, "", T.BodyFont, 38, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -696f), new Vector2(820f, 52f));
            stars = B.Node("Stars", card, Anchor.Top, new Vector2(0f, -790f), new Vector2(400f, 110f));
            state = B.Text("State", card, "", T.TitleFont, 42, T.Muted, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -800f), new Vector2(820f, 60f));

            string[] labels = { "Your best", "Best possible", "No-help best", "Times restored" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -930f - i * RowHeight;
                B.Text($"Label {i}", card, labels[i], T.BodyFont, 40, T.Muted, TextAnchor.MiddleLeft, Anchor.Top, new Vector2(-230f, y), new Vector2(300f, RowHeight - 12f));
                values[i] = B.Text($"Value {i}", card, "", T.TitleFont, 40, T.Cream, TextAnchor.MiddleRight, Anchor.Top, new Vector2(100f, y), new Vector2(560f, RowHeight - 12f));
                if (i < labels.Length - 1) B.Image("Rule", card, null, UiBuilder.WithAlpha(Color.white, 0.08f), Anchor.Top, new Vector2(0f, y - RowHeight + 6f), new Vector2(760f, 3f));
            }

            var play = B.Button("Play", card, "Replay", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, -36f), new Vector2(680f, 184f), () => Play?.Invoke(), T.IconPlay);
            playLabel = play.GetComponentInChildren<Text>();
            playLabel.fontSize = 72;
        }

        public void Bind(BuiltLevel level, VesselRecord record)
        {
            for (int i = showcase.childCount - 1; i >= 0; i--) Release(showcase.GetChild(i).gameObject);
            for (int i = stars.childCount - 1; i >= 0; i--) Release(stars.GetChild(i).gameObject);

            bool restored = record.Clears > 0;
            VesselPreview.Build(B, showcase, level, restored ? VesselLook.Restored : VesselLook.Teaser, Anchor.Center, Vector2.zero, new Vector2(700f, 440f));
            byline.text = VesselArt.Byline(level.Spec) ?? "";
            title.text = VesselArt.DisplayName(level.Spec);

            stars.gameObject.SetActive(restored);
            if (restored) B.Stars(stars, record.Stars, Anchor.Center, Vector2.zero, 84f, raised: true);
            state.text = restored ? "" : "Waiting for repair";

            int possible = level.Solution.MinBenchPeak;
            values[0].text = restored ? Run(record.BestPeak, record.BestSeconds) : "-";
            values[1].text = !restored ? "Win once to see"
                : record.BestPeak == possible ? $"<color=#{Hex(T.Gold)}>Workbench {possible}  · matched!</color>"
                : $"Workbench {possible}";
            values[2].text = record.CleanPeak >= 0 ? Run(record.CleanPeak, record.CleanSeconds) : restored ? "Not yet" : "-";
            values[3].text = record.Clears.ToString();
            playLabel.text = restored ? "REPLAY" : "RESTORE";
        }

        /// <summary>Minutes and seconds, "--:--" when the time is unknown.</summary>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) return "--:--";
            int total = Mathf.RoundToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }

        static string Run(int peak, float seconds) => $"Workbench {peak}  ·  {Clock(seconds)}";

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        protected override void OnShow() => Ui.Pop(card);

        static void Release(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
