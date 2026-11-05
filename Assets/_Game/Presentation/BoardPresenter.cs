using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        // Empty sockets show their glaze under a light 20% white wash. White mixed into soft, mid-tone glazes reads much
        // paler than its share, so this is what looks like "60% visible" on screen: the picture is clearly readable in
        // every level and still a step lighter than placed shards, so players can see what goes where.
        static readonly Color GhostTint = Color.white;
        static readonly Color GhostWash = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color OpenFrameColor = new Color(1f, 0.84f, 0.29f, 1f);
        static readonly Color PileTileColor = new Color(0.17f, 0.32f, 0.62f, 0.08f);
        static readonly Color LockBadgeColor = new Color(0.11f, 0.23f, 0.47f, 0.78f);
        static readonly Color LockGlyphColor = new Color(0.98f, 0.94f, 0.86f, 1f);
        static readonly Color PipEmpty = new Color(0.17f, 0.32f, 0.62f, 0.22f);
        static readonly Color SlotColor = new Color(0.86f, 0.91f, 0.97f, 1f);
        static readonly Color LockedSlotColor = new Color(0.68f, 0.77f, 0.9f, 1f);
        static readonly Color TrayColor = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color GlassColor = new Color(1f, 1f, 1f, 0.92f);
        static readonly Color GlassEdge = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color FailTint = new Color(1f, 0.55f, 0.5f, 1f);

        public event Action<GameStatus> StatusShown;

        /// <summary>Raised after every accepted tap.</summary>
        public event Action<int> ShardTapped;

        /// <summary>Raised once when the player has not tapped for <see cref="IdleNudgeDelay"/> seconds.</summary>
        public event Action Idle;

        [Tooltip("1 = design speed. Raise it to fast-forward animations, for example in tests.")]
        public float AnimationSpeed = 1f;

        [Tooltip("Seconds without a tap before the HUD nudges the hint button. Hints are never given for free.")]
        public float IdleNudgeDelay = 20f;

        public bool AcceptPointerInput = true;

        public BuiltLevel Level { get; private set; }
        public PuzzleEngine Engine { get; private set; }
        public int BenchPeak { get; private set; }
        public int TapCount { get; private set; }

        // Help used and moves made this run, for records and replay checks.
        public int HintsUsed { get; private set; }
        public int PullsUsed { get; private set; }
        public int ContinuesUsed { get; private set; }
        public string Moves => moves.ToString();

        /// <summary>Play time of the run, frozen at the winning move. Pauses stop the clock (time scale 0).</summary>
        public float PlaySeconds => wonAt >= 0f ? wonAt - startedAt : Time.time - startedAt;

        /// <summary>A hint glow is showing; it stays until its shard is placed.</summary>
        public bool HintActive => hintShard >= 0;

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
        SpriteRenderer[] socketWashes;
        SpriteRenderer[] slotFrames;
        SpriteRenderer[] openFrames;
        GameObject[] lockIcons;
        Sprite[] socketSprites;

        // The gold border around the restored part of the vessel, one segment per open cell edge.
        readonly Dictionary<long, SpriteRenderer> border = new Dictionary<long, SpriteRenderer>();
        readonly Dictionary<long, int> socketAtCell = new Dictionary<long, int>();
        bool[] shownPlaced;

        // Locked bench slots: a padlock plus one pip per placement still needed.
        GameObject[] slotLocks;
        SpriteRenderer[][] slotPips;
        int shownPlacements;
        int shownUnlocks;

        GameObject hintGlow;
        GameObject hintSocketGlow;
        int hintShard = -1;
        ShardView hintView;
        Coroutine hintRoutine;

        float lastInputTime;
        bool idleRaised;
        float startedAt;
        float wonAt;
        readonly StringBuilder moves = new StringBuilder();

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
            border.Clear();
            socketAtCell.Clear();
            slotFrames = null;
            slotLocks = null;
            shownPlacements = 0;
            shownUnlocks = 0;

            Level = level;
            Engine = new PuzzleEngine(level.Def);
            layout = new BoardLayout(level);
            BenchPeak = 0;
            TapCount = 0;
            HintsUsed = 0;
            PullsUsed = 0;
            ContinuesUsed = 0;
            moves.Clear();
            ShownStatus = GameStatus.Playing;
            lastInputTime = Time.time;
            idleRaised = false;
            startedAt = Time.time;
            wonAt = -1f;

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
            slotLocks = null;
            border.Clear();
            socketAtCell.Clear();
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
            ContinuesUsed++;
            AddMove("c");
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

        // Glass card behind the vessel, a dark tray under the workbench, and a glass panel under the pile.
        void BuildTable()
        {
            // Tall vessels reach down toward the workbench; the card stops short of the tray.
            float top = BoardLayout.VesselCenterY + Level.Rows * layout.CellSize / 2f + 0.55f;
            float bottom = Mathf.Max(BoardLayout.VesselCenterY - Level.Rows * layout.CellSize / 2f - 0.55f, BoardLayout.BenchY + 1.3f);
            var vesselSize = new Vector2(Level.Columns * layout.CellSize + 1.1f, top - bottom);
            MakeGlass("Vessel card", new Vector3(0f, (top + bottom) / 2f, 0f), vesselSize);
            MakePanel("Bench tray", boardRoot, UiSkin.WorldRound, new Vector3(0f, BoardLayout.BenchY, 0f), new Vector2(10.9f, 2.3f), TrayColor, -40);
            MakeGlass("Pile table", layout.PileCentre, layout.PileExtent + new Vector2(0.6f, 0.6f));
        }

        void MakeGlass(string name, Vector3 centre, Vector2 size)
        {
            MakePanel(name + " edge", boardRoot, UiSkin.WorldRound, centre, size + new Vector2(0.1f, 0.1f), GlassEdge, -42);
            MakePanel(name, boardRoot, UiSkin.WorldRound, centre, size, new Color(1f, 1f, 1f, 0.9f), -41);
            MakePanel(name + " glaze", boardRoot, UiSkin.WorldRound, centre, size, GlassColor, -40);
        }

        void BuildVessel()
        {
            vesselRoot = new GameObject("Vessel").transform;
            vesselRoot.SetParent(boardRoot, false);
            vesselRoot.position = new Vector3(0f, BoardLayout.VesselCenterY, 0f);

            var def = Level.Def;
            float shardSize = layout.ShardInVesselSize;
            socketGhosts = new SpriteRenderer[def.SocketCount];
            socketWashes = new SpriteRenderer[def.SocketCount];
            for (int s = 0; s < def.SocketCount; s++)
            {
                socketGhosts[s] = MakeSprite($"Socket {s}", vesselRoot, socketSprites[s], layout.Socket(s),
                    new Vector2(shardSize, shardSize), GhostTint, 0);
                socketWashes[s] = MakeSprite($"Socket {s} wash", vesselRoot, SpriteFactory.Square, layout.Socket(s),
                    new Vector2(shardSize, shardSize), GhostWash, 1);
            }

            int anchor = def.AnchorSocket;
            HideGhost(anchor);
            MakeSprite("Anchor", vesselRoot, socketSprites[anchor], layout.Socket(anchor), new Vector2(layout.CellSize, layout.CellSize), Color.white, 5);

            shownPlaced = new bool[def.SocketCount];
            for (int s = 0; s < def.SocketCount; s++) socketAtCell[CellKey(Level.SocketColumn[s], Level.SocketRow[s])] = s;
            MarkPlaced(anchor, animate: false);
        }

        static long CellKey(int col, int row) => ((long)col << 32) ^ (uint)row;

        static readonly int[] DirCol = { 1, -1, 0, 0 };
        static readonly int[] DirRow = { 0, 0, -1, 1 };

        /// <summary>
        /// Grows the gold border around the restored group: the new shard's open edges gain a border segment, and
        /// edges it now shares with placed neighbours lose theirs, so the group reads as one merged piece.
        /// </summary>
        void MarkPlaced(int socket, bool animate)
        {
            shownPlaced[socket] = true;
            int col = Level.SocketColumn[socket], row = Level.SocketRow[socket];
            for (int dir = 0; dir < 4; dir++)
            {
                bool hasNeighbour = socketAtCell.TryGetValue(CellKey(col + DirCol[dir], row + DirRow[dir]), out int neighbour);
                if (hasNeighbour && shownPlaced[neighbour])
                {
                    long shared = EdgeKey(neighbour, dir ^ 1);
                    if (border.TryGetValue(shared, out var segment))
                    {
                        border.Remove(shared);
                        Release(segment.gameObject);
                    }
                }
                else
                {
                    border[EdgeKey(socket, dir)] = MakeBorderSegment(socket, dir, animate);
                }
            }
        }

        static long EdgeKey(int socket, int dir) => ((long)socket << 3) | (uint)dir;

        SpriteRenderer MakeBorderSegment(int socket, int dir, bool animate)
        {
            float cell = layout.CellSize, width = cell * 0.075f;
            var centre = layout.Socket(socket) + new Vector3(DirCol[dir] * cell * 0.5f, -DirRow[dir] * cell * 0.5f, 0f);
            var size = dir < 2 ? new Vector2(width, cell + width) : new Vector2(cell + width, width);
            var segment = MakeSprite("Border", vesselRoot, SpriteFactory.Square, centre, size, Gold, 8);
            if (animate && Application.isPlaying) StartCoroutine(GrowSegment(segment, size, dir < 2));
            return segment;
        }

        IEnumerator GrowSegment(SpriteRenderer segment, Vector2 size, bool upright)
        {
            float duration = Seconds(0.22f);
            for (float t = 0f; t < 1f && segment != null; t += Time.deltaTime / duration)
            {
                float e = Ease.OutCubic(t);
                segment.transform.localScale = upright ? new Vector3(size.x, size.y * e, 1f) : new Vector3(size.x * e, size.y, 1f);
                segment.color = Color.Lerp(PaleGold, Gold, t);
                yield return null;
            }
            if (segment == null) yield break;
            segment.transform.localScale = new Vector3(size.x, size.y, 1f);
            segment.color = Gold;
        }

        void BuildBench()
        {
            var state = Engine.State;
            if (slotFrames != null)
            {
                foreach (var frame in slotFrames) Release(frame.gameObject);
            }
            if (slotLocks != null)
            {
                foreach (var icon in slotLocks)
                {
                    if (icon != null) Release(icon);
                }
            }
            int count = state.Bench.Length;
            float size = layout.SlotSizeFor(count);
            slotFrames = new SpriteRenderer[count];
            slotLocks = new GameObject[count];
            slotPips = new SpriteRenderer[count][];
            for (int i = 0; i < slotFrames.Length; i++)
            {
                bool locked = state.SlotLocked[i] && i >= FirstShownLockedSlot();
                var color = locked ? LockedSlotColor : SlotColor;
                slotFrames[i] = MakePanel($"Slot {i}", boardRoot, UiSkin.WorldInset, layout.Slot(i, count),
                    new Vector2(size, size), color, 10);
                if (locked) BuildSlotLock(i, layout.Slot(i, count), size);
            }
            UpdateSlotPips();
        }

        // Slots unlock left to right; the first still-locked slot as the player has seen it.
        int FirstShownLockedSlot() => Level.Def.BenchSize - Level.Def.LockedBenchSlots + shownUnlocks;

        void BuildSlotLock(int slot, Vector3 centre, float size)
        {
            var root = new GameObject($"Slot {slot} lock");
            root.transform.SetParent(boardRoot, false);
            root.transform.position = centre;
            MakeLockGlyph(root.transform, new Vector3(0f, size * 0.08f, 0f), size * 0.5f, 12);

            int needed = Level.Def.PlacementsPerUnlock;
            slotPips[slot] = new SpriteRenderer[needed];
            float pip = size * 0.13f, gap = size * 0.2f;
            for (int i = 0; i < needed; i++)
            {
                var at = centre + new Vector3((i - (needed - 1) / 2f) * gap, -size * 0.3f, 0f);
                slotPips[slot][i] = MakeSprite($"Pip {i}", root.transform, SpriteFactory.Circle, at, new Vector2(pip, pip), PipEmpty, 12);
            }
            slotLocks[slot] = root;
        }

        /// <summary>Fills the next locked slot's pips with the placements made toward opening it.</summary>
        void UpdateSlotPips()
        {
            if (slotPips == null) return;
            int next = FirstShownLockedSlot();
            int perUnlock = Level.Def.PlacementsPerUnlock;
            int progress = Mathf.Clamp(shownPlacements - perUnlock * shownUnlocks, 0, perUnlock);
            for (int slot = 0; slot < slotPips.Length; slot++)
            {
                var pips = slotPips[slot];
                if (pips == null) continue;
                for (int i = 0; i < pips.Length; i++)
                {
                    if (pips[i] != null) pips[i].color = slot == next && i < progress ? Gold : PipEmpty;
                }
            }
        }

        void UnlockSlot(int slot)
        {
            shownUnlocks++;
            if (slotLocks != null && slot < slotLocks.Length && slotLocks[slot] != null)
            {
                Release(slotLocks[slot]);
                slotLocks[slot] = null;
                slotPips[slot] = null;
            }
            StartCoroutine(Tint(slotFrames[slot], SlotColor, 0.3f));
            StartCoroutine(PopFrame(slotFrames[slot].transform));
            feedback.Expand();
            UpdateSlotPips();
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
                bool locked = Engine.IsLocked(i);
                view.Renderer.color = Color.white;
                openFrames[i] = MakeSprite($"Open frame {i}", boardRoot, SpriteFactory.Square, layout.PileShard(i),
                    new Vector2(size * 1.1f, size * 1.1f), OpenFrameColor, 29);
                openFrames[i].enabled = !locked;
                lockIcons[i] = MakeLockIcon(view);
                lockIcons[i].SetActive(locked);
                shardViews[i] = view;
            }
        }

        /// <summary>A small padlock badge in the shard's corner; the shard itself keeps its full colour.</summary>
        static GameObject MakeLockIcon(ShardView view)
        {
            var icon = new GameObject("Lock");
            icon.transform.SetParent(view.transform, false);
            icon.transform.localPosition = new Vector3(0.3f, -0.3f, 0f);
            icon.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            var badge = MakeSprite("Badge", icon.transform, SpriteFactory.Circle, Vector3.zero, Vector2.one, LockBadgeColor, view.Renderer.sortingOrder + 2);
            badge.transform.localPosition = Vector3.zero;
            badge.transform.localScale = Vector3.one;
            MakeLockGlyph(icon.transform, Vector3.zero, 0.62f, view.Renderer.sortingOrder + 3);
            return icon;
        }

        /// <summary>A cream padlock (shackle ring and body), centred on a parent, in local units.</summary>
        static void MakeLockGlyph(Transform parent, Vector3 centre, float size, int order)
        {
            void Part(string name, Sprite sprite, Vector2 offset, Vector2 scale, Color color, int sort)
            {
                var sr = MakeSprite(name, parent, sprite, Vector3.zero, Vector2.one, color, sort);
                sr.transform.localPosition = centre + new Vector3(offset.x * size, offset.y * size, 0f);
                sr.transform.localScale = new Vector3(scale.x * size, scale.y * size, 1f);
            }
            Part("Shackle", SpriteFactory.Circle, new Vector2(0f, 0.16f), new Vector2(0.56f, 0.56f), LockGlyphColor, order);
            Part("Shackle hole", SpriteFactory.Circle, new Vector2(0f, 0.16f), new Vector2(0.3f, 0.3f), LockBadgeColor, order + 1);
            Part("Body", SpriteFactory.Rounded, new Vector2(0f, -0.14f), new Vector2(0.74f, 0.54f), LockGlyphColor, order + 2);
            Part("Keyhole", SpriteFactory.Circle, new Vector2(0f, -0.12f), new Vector2(0.14f, 0.14f), LockBadgeColor, order + 3);
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

            if (ShownStatus == GameStatus.Playing && !idleRaised && !HintActive && Time.time - lastInputTime > IdleNudgeDelay)
            {
                idleRaised = true;
                Idle?.Invoke();
            }
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

            idleRaised = false;
            TapCount++;
            AddMove("t" + shard);
            AfterMove();
            Play(events);
            feedback.Pick();
            ShardTapped?.Invoke(shard);
            return true;
        }

        /// <summary>
        /// The pull booster: lifts the hinted shard, or else the deepest-buried shard that fits, straight into the
        /// vessel. Returns false if nothing in the pile fits (the caller should not charge for it).
        /// </summary>
        public bool Pull()
        {
            if (!CanPull) return false;
            int shard = hintShard >= 0 && Engine.CanPull(hintShard) ? hintShard : Solver.PullTarget(Engine);
            var events = Engine.Pull(shard);
            if (events.Count == 0) return false;

            lastInputTime = Time.time;
            idleRaised = false;
            PullsUsed++;
            AddMove("p" + shard);
            AfterMove();
            Play(events);
            feedback.Pick();
            ShardTapped?.Invoke(shard);
            return true;
        }

        /// <summary>True while the pull booster has a shard to lift.</summary>
        public bool CanPull => Engine != null && ShownStatus == GameStatus.Playing && Solver.PullTarget(Engine) >= 0;

        void AfterMove()
        {
            BenchPeak = Mathf.Max(BenchPeak, Engine.State.BenchCount);
            if (Engine.State.Status == GameStatus.Won && wonAt < 0f) wonAt = Time.time;
        }

        void AddMove(string move)
        {
            if (moves.Length > 0) moves.Append(' ');
            moves.Append(move);
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
                    case GameEventType.BenchSlotUnlocked:
                        queue.Enqueue(() => UnlockSlot(e.Slot));
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
            lockIcons[shard].SetActive(false);
            if (shard == hintShard) StopHint();
            float duration = Seconds(chainIndex == 0 ? 0.35f : 0.3f);
            yield return view.FlyTo(layout.Socket(socket), layout.CellSize, duration, 5);

            view.transform.SetParent(vesselRoot, true);
            HideGhost(socket);
            MarkPlaced(socket, animate: true);
            shownPlacements++;
            UpdateSlotPips();
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

        IEnumerator WinRoutine()
        {
            ShownStatus = GameStatus.Won;
            feedback.Win();
            yield return new WaitForSeconds(Seconds(0.2f));

            // Beauty shot: a slow turn and swell while the gold border flashes.
            float duration = Seconds(1.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float swell = Mathf.Sin(t * Mathf.PI);
                vesselRoot.localScale = Vector3.one * (1f + 0.07f * swell);
                vesselRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 4f * (1f - t));
                var glow = Color.Lerp(Gold, PaleGold, swell);
                foreach (var segment in border.Values) segment.color = glow;

                yield return null;
            }
            vesselRoot.localScale = Vector3.one;
            vesselRoot.localRotation = Quaternion.identity;
            foreach (var segment in border.Values) segment.color = Gold;

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

        /// <summary>The shard a hint points at, or -1. See <see cref="Solver.HintTarget"/>.</summary>
        public int HintShard => Engine != null && Engine.State.Status == GameStatus.Playing ? Solver.HintTarget(Engine) : -1;

        /// <summary>
        /// Points at the shard that fits the vessel right now, even if it is still buried, and lights the spot it
        /// fills. The glow stays until that shard is placed. Returns false if no hint was shown (nothing to charge).
        /// </summary>
        public bool ShowHint()
        {
            if (Engine == null || Engine.State.Status != GameStatus.Playing || HintActive) return false;
            int shard = HintShard;
            if (shard < 0) return false;

            hintShard = shard;
            HintsUsed++;
            hintView = shardViews[shard];
            float size = layout.PileShardSize * 1.22f;
            var glow = MakeSprite("Hint", boardRoot, SpriteFactory.Square, layout.PileShard(shard), new Vector2(size, size), Gold, 28);
            hintGlow = glow.gameObject;

            int socket = Level.Def.Shards[shard].Socket;
            var spot = MakeSprite("Hint spot", vesselRoot, SpriteFactory.Square, layout.Socket(socket), new Vector2(layout.CellSize, layout.CellSize), Gold, -1);
            hintSocketGlow = spot.gameObject;

            hintRoutine = StartCoroutine(HintPulse(hintView, glow, spot, socketWashes[socket], layout.PileShardSize));
            return true;
        }

        void HideGhost(int socket)
        {
            socketGhosts[socket].enabled = false;
            socketWashes[socket].enabled = false;
        }

        /// <summary>The hinted shard throbs in the rack while its socket's wash pulses gold, pointing where it goes.</summary>
        IEnumerator HintPulse(ShardView view, SpriteRenderer glow, SpriteRenderer spot, SpriteRenderer wash, float baseSize)
        {
            const float period = 0.9f;
            for (float t = 0f; ; t += Time.deltaTime)
            {
                float wave = Mathf.Abs(Mathf.Sin(t / period * Mathf.PI));
                view.SetSize(baseSize * (1f + 0.07f * wave));
                glow.color = new Color(Gold.r, Gold.g, Gold.b, 0.45f + 0.5f * wave);
                spot.color = new Color(Gold.r, Gold.g, Gold.b, 0.5f + 0.5f * wave);
                wash.color = Color.Lerp(GhostWash, new Color(Gold.r, Gold.g, Gold.b, 0.55f), 0.35f + 0.65f * wave);
                yield return null;
            }
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
                if (Engine != null && Engine.State.InPile[hintView.ShardId]) hintView.SetSize(layout.PileShardSize);
                hintView = null;
            }
            if (hintShard >= 0 && socketWashes != null)
            {
                var wash = socketWashes[Level.Def.Shards[hintShard].Socket];
                if (wash != null) wash.color = GhostWash;
            }
            hintShard = -1;
            if (hintGlow != null)
            {
                Release(hintGlow);
                hintGlow = null;
            }
            if (hintSocketGlow != null)
            {
                Release(hintSocketGlow);
                hintSocketGlow = null;
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

        /// <summary>A 9-sliced sprite drawn at <paramref name="size"/> world units, so its rounded corners stay crisp.</summary>
        static SpriteRenderer MakePanel(string name, Transform parent, Sprite sprite, Vector3 worldPosition, Vector2 size, Color color, int order)
        {
            var sr = MakeSprite(name, parent, sprite, worldPosition, Vector2.one, color, order);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            return sr;
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
