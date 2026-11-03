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

        static readonly Vector2 CardSize = new Vector2(880f, 1360f);
        const float RowHeight = 76f;

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
            card = B.Card("Card", Root, Anchor.Center, Vector2.zero, CardSize);
            var niche = B.Image("Niche", card, T.Badge, Color.Lerp(T.Indigo, T.Ink, 0.45f), Anchor.Top, new Vector2(0f, -40f), new Vector2(CardSize.x - 80f, 470f), sliced: true);
            showcase = niche.rectTransform;
            B.IconButton("Close", card, T.IconClose, ButtonStyle.Secondary, Anchor.TopRight, new Vector2(-12f, -12f), 100f, () => Close?.Invoke());

            byline = B.Text("Byline", card, "", T.TitleFont, 28, UiBuilder.WithAlpha(T.Ink, 0.5f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -530f), new Vector2(800f, 40f));
            title = B.Text("Title", card, "", T.DisplayFont, 72, T.Indigo, TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -572f), new Vector2(820f, 96f));
            stars = B.Node("Stars", card, Anchor.Top, new Vector2(0f, -680f), new Vector2(300f, 70f));
            state = B.Text("State", card, "", T.BodyFont, 32, UiBuilder.WithAlpha(T.Ink, 0.55f), TextAnchor.MiddleCenter, Anchor.Top, new Vector2(0f, -688f), new Vector2(800f, 50f));

            string[] labels = { "Your best", "Best possible", "No-help best", "Times restored" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -782f - i * RowHeight;
                if (i > 0) B.Image("Rule", card, null, UiBuilder.WithAlpha(T.Ink, 0.1f), Anchor.Top, new Vector2(0f, y), new Vector2(720f, 2f));
                B.Text($"Label {i}", card, labels[i], T.BodyFont, 34, UiBuilder.WithAlpha(T.Ink, 0.65f), TextAnchor.MiddleLeft, Anchor.Top, new Vector2(-170f, y), new Vector2(380f, RowHeight));
                values[i] = B.Text($"Value {i}", card, "", T.TitleFont, 34, T.Ink, TextAnchor.MiddleRight, Anchor.Top, new Vector2(150f, y), new Vector2(420f, RowHeight));
            }

            var play = B.Button("Play", card, "", ButtonStyle.Primary, Anchor.Bottom, new Vector2(0f, 48f), new Vector2(680f, 150f), () => Play?.Invoke(), T.IconPlay);
            playLabel = play.GetComponentInChildren<Text>();
        }

        public void Bind(BuiltLevel level, VesselRecord record)
        {
            for (int i = showcase.childCount - 1; i >= 0; i--) Release(showcase.GetChild(i).gameObject);
            for (int i = stars.childCount - 1; i >= 0; i--) Release(stars.GetChild(i).gameObject);

            bool restored = record.Clears > 0;
            VesselPreview.Build(B, showcase, level, restored ? VesselLook.Restored : VesselLook.Teaser, Anchor.Center, Vector2.zero, new Vector2(620f, 400f));
            byline.text = VesselArt.Byline(level.Spec) ?? "";
            title.text = VesselArt.DisplayName(level.Spec);

            stars.gameObject.SetActive(restored);
            if (restored) B.Stars(stars, record.Stars, Anchor.Center, Vector2.zero, 60f, raised: false);
            state.text = restored ? "" : "Waiting for repair";

            int possible = level.Solution.MinBenchPeak;
            values[0].text = restored ? Run(record.BestPeak, record.BestSeconds) : "-";
            values[1].text = !restored ? "Win once to see"
                : record.BestPeak == possible ? $"Workbench {possible}  <color=#{Hex(T.Indigo)}>· matched!</color>"
                : $"Workbench {possible}";
            values[2].text = record.CleanPeak >= 0 ? Run(record.CleanPeak, record.CleanSeconds) : restored ? "Not yet" : "-";
            values[3].text = record.Clears.ToString();
            playLabel.text = restored ? "Replay" : "Restore";
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
