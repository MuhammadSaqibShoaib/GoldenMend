using System;
using System.Collections;
using System.Collections.Generic;
using GoldenMend.Core;
using GoldenMend.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Builds the views for a level, sends taps to the <see cref="PuzzleEngine"/> and plays back the events
    /// it returns. Never changes game state itself.
    /// </summary>
    [RequireComponent(typeof(AnimationQueue))]
    public sealed class BoardPresenter : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.80f, 0.32f, 1f);
        static readonly Color PaleGold = new Color(1f, 0.95f, 0.75f, 1f);
        static readonly Color GhostTint = new Color(0.85f, 0.85f, 0.85f, 0.45f);
        static readonly Color LockedTint = new Color(0.40f, 0.38f, 0.38f, 1f);
        static readonly Color OpenFrameColor = new Color(1f, 0.93f, 0.72f, 0.95f);
        static readonly Color PileTileColor = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color LockIconColor = new Color(0.1f, 0.09f, 0.08f, 0.9f);
        static readonly Color SlotColor = new Color(1f, 1f, 1f, 0.12f);
        static readonly Color LockedSlotColor = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color TableColor = new Color(1f, 1f, 1f, 0.045f);
        static readonly Color CrackColor = new Color(0.08f, 0.06f, 0.05f, 0.85f);
        static readonly Color FailTint = new Color(1f, 0.55f, 0.5f, 1f);

        public event Action<GameStatus> StatusShown;

        /// <summary>Raised after every accepted tap.</summary>
        public event Action<int> ShardTapped;

        [Tooltip("1 = design speed. Raise it to fast-forward animations, for example in tests.")]
        public float AnimationSpeed = 1f;

        [Tooltip("Seconds without a tap before a hint pulses (GDD: 20 s).")]
        public float HintDelay = 20f;

        public bool AcceptPointerInput = true;

        public BuiltLevel Level { get; private set; }
        public PuzzleEngine Engine { get; private set; }
        public int BenchPeak { get; private set; }
        public int TapCount { get; private set; }

        /// <summary>The status the player has seen so far; it lags the engine while animations play.</summary>
        public GameStatus ShownStatus { get; private set; }
        public bool IsAnimating => queue.Busy;

        AnimationQueue queue;
        FeedbackService feedback;
        Camera cam;
        BoardLayout layout;
        Transform boardRoot;
        Transform vesselRoot;
        ShardView[] shardViews;
        SpriteRenderer[] socketGhosts;
        SpriteRenderer[] slotFrames;
        SpriteRenderer[] openFrames;
        GameObject[] lockIcons;
        Sprite[] socketSprites;
        readonly List<SpriteRenderer> seams = new List<SpriteRenderer>();

        float lastInputTime;
        bool hintShown;
        ShardView hintView;
        Coroutine hintRoutine;

        void Awake() => EnsureInitialized();

        void EnsureInitialized()
        {
            if (queue != null) return;
            queue = GetComponent<AnimationQueue>();
            feedback = GetComponent<FeedbackService>();
            if (feedback == null) feedback = gameObject.AddComponent<FeedbackService>();
        }

        public void Load(BuiltLevel level)
        {
            EnsureInitialized();
            queue.Clear();
            StopHint();
            if (boardRoot != null) Release(boardRoot.gameObject);
            VesselArt.Release(socketSprites);
            seams.Clear();
            slotFrames = null;

            Level = level;
            Engine = new PuzzleEngine(level.Def);
            layout = new BoardLayout(level);
            BenchPeak = 0;
            TapCount = 0;
            ShownStatus = GameStatus.Playing;
            lastInputTime = Time.time;
            hintShown = false;

            socketSprites = VesselArt.BuildSocketSprites(level);
            boardRoot = new GameObject("Board").transform;
            boardRoot.SetParent(transform, false);

            BuildTable();
            BuildVessel();
            BuildBench();
            BuildPile();
        }

        /// <summary>Clears the board, for menus.</summary>
        public void Unload()
        {
            EnsureInitialized();
            queue.Clear();
            StopHint();
            StopAllCoroutines();
            if (boardRoot != null) Release(boardRoot.gameObject);
            VesselArt.Release(socketSprites);
            socketSprites = null;
            slotFrames = null;
            seams.Clear();
            Level = null;
            Engine = null;
            ShownStatus = GameStatus.Playing;
        }

        /// <summary>The continue offer: two more workbench slots, and the stuck shard parks. False if not failed.</summary>
        public bool ContinueRun(int extraSlots = 2)
        {
            if (Engine == null) return false;
            var events = Engine.Continue(extraSlots);
            if (events.Count == 0) return false;

            ShownStatus = GameStatus.Playing;
            lastInputTime = Time.time;
            BenchPeak = Mathf.Max(BenchPeak, Engine.State.BenchCount);
            Play(events);
            return true;
        }

        /// <summary>Rebuilds the slot frames for the new count and slides the parked shards to their new spots.</summary>
        void ExpandBench(int count)
        {
            int before = slotFrames.Length;
            BuildBench();
            feedback.Expand();
            float size = layout.BenchShardSizeFor(count);
            var bench = Engine.State.Bench;
            for (int slot = 0; slot < before && slot < bench.Length; slot++)
            {
                int shard = bench[slot];
                if (shard < 0) continue;
                StartCoroutine(shardViews[shard].FlyTo(layout.Slot(slot, count), size, Seconds(0.25f), 20, 0f));
            }
            foreach (var frame in slotFrames) StartCoroutine(PopFrame(frame.transform));
        }

        IEnumerator PopFrame(Transform frame)
        {
            Vector3 target = frame.localScale;
            float duration = Seconds(0.3f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                frame.localScale = target * (0.6f + 0.4f * Ease.OutCubic(t));
                yield return null;
            }
            frame.localScale = target;
        }

        void BuildTable()
        {
            MakeSprite("Bench strip", boardRoot, SpriteFactory.Rounded, new Vector3(0f, BoardLayout.BenchY, 0f),
                new Vector2(10.8f, 2.4f), TableColor, -40);
            var extent = layout.PileExtent + new Vector2(0.5f, 0.5f);
            MakeSprite("Pile table", boardRoot, SpriteFactory.Rounded, layout.PileCentre, extent, TableColor, -40);
        }

        void BuildVessel()
        {
            vesselRoot = new GameObject("Vessel").transform;
            vesselRoot.SetParent(boardRoot, false);
            vesselRoot.position = new Vector3(0f, BoardLayout.VesselCenterY, 0f);

            var def = Level.Def;
            float shardSize = layout.ShardInVesselSize;
            socketGhosts = new SpriteRenderer[def.SocketCount];
            for (int s = 0; s < def.SocketCount; s++)
            {
                socketGhosts[s] = MakeSprite($"Socket {s}", vesselRoot, socketSprites[s], layout.Socket(s),
                    new Vector2(shardSize, shardSize), GhostTint, 0);
            }

            int anchor = def.AnchorSocket;
            socketGhosts[anchor].enabled = false;
            float frame = layout.CellSize * 1.04f;
            MakeSprite("Anchor gold", vesselRoot, SpriteFactory.Square, layout.Socket(anchor), new Vector2(frame, frame), Gold, 4);
            MakeSprite("Anchor", vesselRoot, socketSprites[anchor], layout.Socket(anchor), new Vector2(shardSize, shardSize), Color.white, 5);
        }

        void BuildBench()
        {
            var state = Engine.State;
            if (slotFrames != null)
            {
                foreach (var frame in slotFrames) Release(frame.gameObject);
            }
            int count = state.Bench.Length;
            float size = layout.SlotSizeFor(count);
            slotFrames = new SpriteRenderer[count];
            for (int i = 0; i < slotFrames.Length; i++)
            {
                var color = state.SlotLocked[i] ? LockedSlotColor : SlotColor;
                slotFrames[i] = MakeSprite($"Slot {i}", boardRoot, SpriteFactory.Rounded, layout.Slot(i, count),
                    new Vector2(size, size), color, 10);
            }
        }

        void BuildPile()
        {
            var def = Level.Def;
            var pile = Level.Pile;
            // One tile per piece only; walls and empty cells stay as open space.
            float tile = layout.PileCellSize * 0.95f;
            foreach (int cell in pile.ShardCell)
            {
                MakeSprite($"Pile cell {cell}", boardRoot, SpriteFactory.Rounded, layout.PileCell(cell), new Vector2(tile, tile), PileTileColor, -30);
            }

            float size = layout.PileShardSize;
            shardViews = new ShardView[def.Shards.Count];
            openFrames = new SpriteRenderer[def.Shards.Count];
            lockIcons = new GameObject[def.Shards.Count];
            for (int i = 0; i < shardViews.Length; i++)
            {
                var shard = def.Shards[i];
                var view = ShardView.Create(boardRoot, i, socketSprites[shard.Socket], layout.PileShard(i), size, 30);
                if (shard.Cracked)
                {
                    view.AddMark(SpriteFactory.Square, CrackColor, new Vector2(0.06f, 1.2f), 35f);
                    view.AddMark(SpriteFactory.Square, CrackColor, new Vector2(0.05f, 0.5f), -40f);
                }

                bool locked = Engine.IsLocked(i);
                view.Renderer.color = locked ? LockedTint : Color.white;
                openFrames[i] = MakeSprite($"Open frame {i}", boardRoot, SpriteFactory.Rounded, layout.PileShard(i),
                    new Vector2(size * 1.1f, size * 1.1f), OpenFrameColor, 29);
                openFrames[i].enabled = !locked;
                lockIcons[i] = MakeLockIcon(view);
                lockIcons[i].SetActive(locked);
                shardViews[i] = view;
            }
        }

        /// <summary>A small padlock (body + shackle) in the shard's corner, drawn from generated shapes.</summary>
        static GameObject MakeLockIcon(ShardView view)
        {
            var icon = new GameObject("Lock");
            icon.transform.SetParent(view.transform, false);
            icon.transform.localPosition = new Vector3(0.28f, -0.28f, 0f);
            int order = view.Renderer.sortingOrder + 2;

            var shackle = MakeSprite("Shackle", icon.transform, SpriteFactory.Circle, Vector3.zero, Vector2.one, LockIconColor, order);
            shackle.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            shackle.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            var hole = MakeSprite("Shackle hole", icon.transform, SpriteFactory.Circle, Vector3.zero, Vector2.one, LockedTint * 0.9f, order + 1);
            hole.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            hole.transform.localScale = new Vector3(0.11f, 0.11f, 1f);
            var body = MakeSprite("Body", icon.transform, SpriteFactory.Rounded, Vector3.zero, Vector2.one, LockIconColor, order + 2);
            body.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            body.transform.localScale = new Vector3(0.26f, 0.2f, 1f);
            return icon;
        }

        void Update()
        {
            FitCamera();
            if (Engine == null) return;

            if (AcceptPointerInput && Pointer.current != null && Pointer.current.press.wasPressedThisFrame && !PointerOverUi())
            {
                Vector3 world = cam.ScreenToWorldPoint(Pointer.current.position.ReadValue());
                int shard = ShardAt(world);
                if (shard >= 0) TryTap(shard);
            }

            if (ShownStatus == GameStatus.Playing && !hintShown && Time.time - lastInputTime > HintDelay)
                ShowHint();
        }

        // Taps on HUD buttons and popups must not reach the rack behind them.
        static bool PointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        void FitCamera()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            cam.orthographicSize = Mathf.Max(10.4f, 6.1f / cam.aspect);
        }

        /// <summary>The topmost pile shard under a world point, or -1.</summary>
        public int ShardAt(Vector2 world)
        {
            int best = -1;
            for (int i = 0; i < shardViews.Length; i++)
            {
                if (Engine.State.InPile[i] && shardViews[i].Contains(world)) best = i;
            }
            return best;
        }

        /// <summary>Sends a tap to the engine and queues its events. Returns false if the engine rejected it.</summary>
        public bool TryTap(int shard)
        {
            lastInputTime = Time.time;
            var events = Engine.Tap(shard);
            if (events.Count == 0)
            {
                if (Engine.State.InPile[shard])
                {
                    StartCoroutine(shardViews[shard].Shake(0.08f, 0.25f));
                    feedback.Reject();
                }
                return false;
            }

            StopHint();
            hintShown = false;
            TapCount++;
            BenchPeak = Mathf.Max(BenchPeak, Engine.State.BenchCount);
            Play(events);
            feedback.Pick();
            ShardTapped?.Invoke(shard);
            return true;
        }

        void Play(IReadOnlyList<GameEvent> events)
        {
            int chain = 0;
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.ShardUncovered:
                        queue.Enqueue(() => Reveal(e.Shard));
                        break;
                    case GameEventType.ShardLocked:
                    case GameEventType.ShardAutoFlown:
                        int note = chain++;
                        queue.Enqueue(() => LockRoutine(e.Shard, e.Socket, note));
                        break;
                    case GameEventType.ShardParked:
                        queue.Enqueue(() => ParkRoutine(e.Shard, e.Slot));
                        break;
                    case GameEventType.SeamFormed:
                        queue.Enqueue(() => { StartCoroutine(GrowSeam(e.Socket, e.OtherSocket)); });
                        break;
                    case GameEventType.BenchSlotUnlocked:
                        queue.Enqueue(() => { StartCoroutine(Tint(slotFrames[e.Slot], SlotColor, 0.3f)); });
                        break;
                    case GameEventType.BenchExpanded:
                        queue.Enqueue(() => ExpandBench(e.Slot));
                        break;
                    case GameEventType.LevelWon:
                        queue.Enqueue(WinRoutine);
                        break;
                    case GameEventType.LevelFailed:
                        queue.Enqueue(() => FailRoutine(e.Shard));
                        break;
                }
            }
        }

        void Reveal(int shard)
        {
            var view = shardViews[shard];
            lockIcons[shard].SetActive(false);
            feedback.Uncover();
            openFrames[shard].enabled = true;
            StartCoroutine(view.TintTo(Color.white, Seconds(0.15f)));
            StartCoroutine(view.Punch(0.08f, Seconds(0.2f)));
        }

        IEnumerator LockRoutine(int shard, int socket, int chainIndex)
        {
            var view = shardViews[shard];
            openFrames[shard].enabled = false;
            float duration = Seconds(chainIndex == 0 ? 0.35f : 0.3f);
            yield return view.FlyTo(layout.Socket(socket), layout.ShardInVesselSize, duration, 5);

            view.transform.SetParent(vesselRoot, true);
            socketGhosts[socket].enabled = false;
            if (Level.Def.Shards[shard].Cracked) view.SetMarkColor(Gold);
            feedback.Lock(chainIndex);
            StartCoroutine(view.Punch(0.1f, Seconds(0.18f)));
        }

        IEnumerator ParkRoutine(int shard, int slot)
        {
            openFrames[shard].enabled = false;
            int count = slotFrames.Length;
            shardViews[shard].Renderer.color = Color.white;
            yield return shardViews[shard].FlyTo(layout.Slot(slot, count), layout.BenchShardSizeFor(count), Seconds(0.3f), 20, 0.5f);
            feedback.Park();
        }

        IEnumerator GrowSeam(int socket, int other)
        {
            Vector3 a = layout.Socket(socket);
            Vector3 b = layout.Socket(other);
            bool sideBySide = Mathf.Abs(a.y - b.y) < 0.01f;
            float width = layout.CellSize * 0.12f;
            float length = layout.CellSize * 0.98f;
            Vector2 full = sideBySide ? new Vector2(width, length) : new Vector2(length, width);

            var seam = MakeSprite("Seam", vesselRoot, SpriteFactory.Square, (a + b) * 0.5f, Vector2.zero, PaleGold, 6);
            seams.Add(seam);

            float duration = Seconds(0.25f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float e = Ease.OutCubic(t);
                seam.transform.localScale = sideBySide
                    ? new Vector3(full.x, full.y * e, 1f)
                    : new Vector3(full.x * e, full.y, 1f);
                seam.color = Color.Lerp(PaleGold, Gold, t);
                yield return null;
            }
            seam.transform.localScale = new Vector3(full.x, full.y, 1f);
            seam.color = Gold;
        }

        IEnumerator WinRoutine()
        {
            ShownStatus = GameStatus.Won;
            feedback.Win();
            yield return new WaitForSeconds(Seconds(0.2f));

            // Beauty shot: a slow turn and swell while every seam flashes.
            float duration = Seconds(1.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float swell = Mathf.Sin(t * Mathf.PI);
                vesselRoot.localScale = Vector3.one * (1f + 0.07f * swell);
                vesselRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 4f * (1f - t));
                var glow = Color.Lerp(Gold, PaleGold, swell);
                foreach (var seam in seams) seam.color = glow;

                yield return null;
            }
            vesselRoot.localScale = Vector3.one;
            vesselRoot.localRotation = Quaternion.identity;
            foreach (var seam in seams) seam.color = Gold;

            StatusShown?.Invoke(GameStatus.Won);
        }

        IEnumerator FailRoutine(int shard)
        {
            var view = shardViews[shard];
            openFrames[shard].enabled = false;
            var stuck = new Vector3(0f, BoardLayout.BenchY + 1.9f, 0f);
            yield return view.FlyTo(stuck, layout.BenchShardSizeFor(slotFrames.Length), Seconds(0.3f), 25, 0.4f);
            feedback.Fail();
            StartCoroutine(view.TintTo(FailTint, Seconds(0.2f)));
            foreach (var frame in slotFrames) StartCoroutine(Tint(frame, new Color(1f, 0.3f, 0.25f, 0.25f), 0.2f));
            yield return view.Shake(0.15f, Seconds(0.4f));

            ShownStatus = GameStatus.Failed;
            StatusShown?.Invoke(GameStatus.Failed);
        }

        /// <summary>Pulses the shard the solver would tap next (or any tappable shard if the level is lost).</summary>
        public void ShowHint()
        {
            if (Engine == null || Engine.State.Status != GameStatus.Playing) return;
            hintShown = true;

            int move = Solver.BestMove(Engine);
            if (move < 0)
            {
                for (int i = 0; i < shardViews.Length && move < 0; i++)
                {
                    if (Engine.CanTap(i)) move = i;
                }
            }
            if (move < 0) return;

            StopHint();
            hintView = shardViews[move];
            hintRoutine = StartCoroutine(HintPulse(hintView, hintView.Size));
        }

        IEnumerator HintPulse(ShardView view, float baseSize)
        {
            const float period = 0.6f;
            for (float t = 0f; t < period * 4f; t += Time.deltaTime)
            {
                view.SetSize(baseSize * (1f + 0.12f * Mathf.Abs(Mathf.Sin(t / period * Mathf.PI))));
                yield return null;
            }
            view.SetSize(baseSize);
            hintRoutine = null;
            hintView = null;
        }

        void StopHint()
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }
            if (hintView != null)
            {
                hintView.SetSize(layout.PileShardSize);
                hintView = null;
            }
        }

        IEnumerator Tint(SpriteRenderer sr, Color target, float duration)
        {
            Color start = sr.color;
            duration = Seconds(duration);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                sr.color = Color.Lerp(start, target, t);
                yield return null;
            }
            sr.color = target;
        }

        float Seconds(float designSeconds) => designSeconds / Mathf.Max(0.01f, AnimationSpeed);

        // Load also runs in edit mode for the screenshot tool, where Destroy is not allowed.
        static void Release(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Vector3 worldPosition, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
