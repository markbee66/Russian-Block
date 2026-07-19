using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TetrisArcade
{
    /// <summary>
    /// Classic arcade Tetris. Fully code-driven: builds its own sprites, board,
    /// preview and HUD at runtime. Just drop this on one GameObject and press Play.
    /// </summary>
    public partial class TetrisGame : MonoBehaviour
    {
        // ---- Board dimensions ----
        const int Width = 10;
        const int Height = 20;

        // ---- Piece shape table: [piece][rotation] = 4 cells as (x,y) pairs ----
        static readonly int[][][] SHAPES = new int[][][]
        {
            // I
            new int[][]{ new int[]{0,2,1,2,2,2,3,2}, new int[]{2,0,2,1,2,2,2,3}, new int[]{0,1,1,1,2,1,3,1}, new int[]{1,0,1,1,1,2,1,3} },
            // O
            new int[][]{ new int[]{1,1,2,1,1,2,2,2}, new int[]{1,1,2,1,1,2,2,2}, new int[]{1,1,2,1,1,2,2,2}, new int[]{1,1,2,1,1,2,2,2} },
            // T
            new int[][]{ new int[]{0,1,1,1,2,1,1,2}, new int[]{1,0,1,1,1,2,2,1}, new int[]{0,1,1,1,2,1,1,0}, new int[]{1,0,1,1,1,2,0,1} },
            // S
            new int[][]{ new int[]{0,1,1,1,1,2,2,2}, new int[]{1,2,1,1,2,1,2,0}, new int[]{0,0,1,0,1,1,2,1}, new int[]{0,2,0,1,1,1,1,0} },
            // Z
            new int[][]{ new int[]{0,2,1,2,1,1,2,1}, new int[]{2,2,2,1,1,1,1,0}, new int[]{0,1,1,1,1,0,2,0}, new int[]{1,2,1,1,0,1,0,0} },
            // J
            new int[][]{ new int[]{0,2,0,1,1,1,2,1}, new int[]{1,2,2,2,1,1,1,0}, new int[]{0,1,1,1,2,1,2,0}, new int[]{1,2,1,1,1,0,0,0} },
            // L
            new int[][]{ new int[]{2,2,0,1,1,1,2,1}, new int[]{1,2,1,1,1,0,2,0}, new int[]{0,1,1,1,2,1,0,0}, new int[]{0,2,1,2,1,1,1,0} },

            // --- mutated shapes (see TetrisGame.Mutations.cs) ---
            // Piece cell counts vary from here on, so every loop over a shape
            // uses s.Length / 2 rather than a hardcoded 4.

            // 7,8,9: the three bomb kinds. Each is a single cell, identical in
            // every rotation. The kind lives in the type index rather than a
            // field, so two bombs queued at once cannot clobber each other.
            new int[][]{ new int[]{1,1}, new int[]{1,1}, new int[]{1,1}, new int[]{1,1} },
            new int[][]{ new int[]{1,1}, new int[]{1,1}, new int[]{1,1}, new int[]{1,1} },
            new int[][]{ new int[]{1,1}, new int[]{1,1}, new int[]{1,1}, new int[]{1,1} },
            // 10: 2x3 rectangle, alternating with its 3x2 form
            new int[][]{
                new int[]{1,0,2,0,1,1,2,1,1,2,2,2},
                new int[]{0,1,1,1,2,1,0,2,1,2,2,2},
                new int[]{1,0,2,0,1,1,2,1,1,2,2,2},
                new int[]{0,1,1,1,2,1,0,2,1,2,2,2} },
            // 11: 1x5 bar, upright and flat
            new int[][]{
                new int[]{2,0,2,1,2,2,2,3,2,4},
                new int[]{0,2,1,2,2,2,3,2,4,2},
                new int[]{2,0,2,1,2,2,2,3,2,4},
                new int[]{0,2,1,2,2,2,3,2,4,2} },
        };

        // Inoperable pieces reuse the standard shapes but need their own type
        // index so the board can colour them differently. The rows are shared by
        // reference, so this costs nothing but the indirection.
        static readonly int[][][] INOPERABLE_SHAPES =
        {
            SHAPES[0], SHAPES[1], SHAPES[2], SHAPES[3], SHAPES[4], SHAPES[5], SHAPES[6],
        };

        /// <summary>
        /// The occupied cells of a piece, as x,y pairs. Replaces the direct
        /// SHAPES[type][rot] lookups now that inoperable pieces have their own
        /// type indices and mutated pieces vary in cell count.
        /// </summary>
        static int[] CellsOf(int type, int rot) =>
            type >= InoperableBase
                ? INOPERABLE_SHAPES[type - InoperableBase][rot]
                : SHAPES[type][rot];

        static readonly Color[] COLORS =
        {
            new Color(0.10f,0.85f,0.95f), // I cyan
            new Color(0.98f,0.85f,0.10f), // O yellow
            new Color(0.72f,0.28f,0.92f), // T purple
            new Color(0.25f,0.85f,0.32f), // S green
            new Color(0.95f,0.22f,0.28f), // Z red
            new Color(0.22f,0.48f,0.95f), // J blue
            new Color(0.98f,0.55f,0.12f), // L orange

            // 7,8,9: bombs, each kind its own colour so the blast is predictable
            new Color(0.98f,0.30f,0.55f), // 7  3x3 box bomb, hot pink
            new Color(1.00f,0.62f,0.20f), // 8  column bomb, amber
            new Color(0.95f,0.95f,0.35f), // 9  row bomb, bright yellow

            new Color(0.55f,0.90f,0.85f), // 10 2x3 rectangle
            new Color(0.85f,0.80f,0.45f), // 11 1x5 bar

            // 12-18: inoperable variants of I O T S Z J L, one flat dead grey so
            // "you cannot steer this" is obvious at a glance.
            new Color(0.42f,0.44f,0.50f),
            new Color(0.42f,0.44f,0.50f),
            new Color(0.42f,0.44f,0.50f),
            new Color(0.42f,0.44f,0.50f),
            new Color(0.42f,0.44f,0.50f),
            new Color(0.42f,0.44f,0.50f),
            new Color(0.42f,0.44f,0.50f),
        };

        static readonly int[] LINE_POINTS = { 0, 40, 100, 300, 1200 };

        // ---- Resolution presets (flat w,h pairs; menu filters to <= desktop size) ----
        static readonly int[] RESOLUTIONS = {
            640,360, 640,480, 720,480, 800,600, 960,540, 1024,576, 1024,600, 1024,768,
            1152,648, 1152,720, 1152,864, 1176,664, 1280,720, 1280,768, 1280,800, 1280,960,
            1280,1024, 1360,768, 1366,768, 1440,810, 1440,900, 1440,1080, 1536,864, 1600,900,
            1600,1024, 1600,1200, 1680,1050, 1768,992, 1920,1080, 1920,1200, 1920,1440,
            2048,1080, 2048,1152, 2048,1280, 2048,1536, 2160,1080, 2160,1200, 2304,1296,
            2304,1440, 2560,1080, 2560,1440, 2560,1600, 2560,1920, 2880,1620, 2880,1800,
            3000,2000, 3072,1728, 3072,1920, 3200,1800, 3200,2000, 3200,2400, 3440,1440,
            3456,2160, 3840,1080, 3840,1600, 3840,2160, 3840,2400, 4096,2160, 4096,2304,
            4096,2560, 5120,1440, 5120,2160, 5120,2880, 5120,3200, 5760,1080, 5760,1200,
            5760,2160, 6016,3384, 6144,3456, 7680,2160, 7680,4320, 8192,4320
        };
        const string PrefResW = "TetrisArcade.ResW";
        const string PrefResH = "TetrisArcade.ResH";
        const string PrefMode    = "TetrisArcade.Mode";     // int: 0=Windowed, 1=Borderless (FullScreenWindow)
        const string PrefUIScale = "TetrisArcade.UIScale";  // float
        const string PrefFps     = "TetrisArcade.Fps";      // int: -1 = unlimited
        const string PrefVolume  = "TetrisArcade.Volume";   // float: 0..1 music volume
        static readonly float[] UI_SCALES = { 0.75f, 1f, 1.25f, 1.5f, 2f };
        const float VolumeStep = 0.1f;

        // ---- Palette ----
        readonly Color bgColor    = new Color(0.03f, 0.03f, 0.06f);
        readonly Color wellColor  = new Color(0.07f, 0.07f, 0.11f);
        readonly Color emptyCell  = new Color(0.12f, 0.12f, 0.17f);
        readonly Color accent     = new Color(0.20f, 0.85f, 0.95f);
        readonly Color panelEmpty = new Color(0.10f, 0.10f, 0.15f);

        // ---- Camera framing (set by ConfigureLayout based on the aspect ratio) ----
        float camX = 6f, camY = 10f, camSize = 12f;
        Vector2 previewOrigin = new Vector2(11f, 13.5f);
        bool portrait;              // true for tall aspect ratios (9:16, 3:4, ...)
        int lastScreenW, lastScreenH;

        // ---- Board state ----
        int[,] board = new int[Width, Height]; // -1 empty, else color/piece index
        int curType, curRot, curX, curY, nextType = -1;
        readonly List<int> bag = new List<int>();

        int score, lines, level;
        bool gameOver, paused;
        bool inMenu = true;         // title screen (START / SETTINGS / QUIT) shown on launch

        // ---- Timers ----
        float gravityTimer, lockTimer;
        float fallInterval = 0.8f;
        const float softInterval = 0.03f;
        const float lockDelay = 0.35f;
        float dasTimer; int dasDir; bool dasCharged;
        const float dasDelay = 0.16f, dasRepeat = 0.05f;

        // ---- Visuals ----
        Camera cam;
        SpriteRenderer[,] cells = new SpriteRenderer[Width, Height];
        SpriteRenderer[,] preview = new SpriteRenderer[4, 4];
        SpriteRenderer previewBG;
        Sprite square;

        // ---- Audio ----
        AudioSource bgm;
        float _volume = 0.5f;           // music volume, 0..1

            void Awake()
        {
            Application.runInBackground = true;
            square = MakeSquareSprite();
            LoadSettings();
            lastScreenW = Screen.width; lastScreenH = Screen.height;
            ConfigureLayout();
            SetupCamera();
            SetupBackdrop();
            BuildVisuals();
            SetupAudio();
            NewGame();
        }

        // Loads the looping background track from Resources/Music and plays it.
        void SetupAudio()
        {
            // Nothing is audible without an AudioListener; the Tetris scene has none,
            // so guarantee exactly one exists (on the camera) before playing.
            if (FindAnyObjectByType<AudioListener>() == null && cam != null)
                cam.gameObject.AddComponent<AudioListener>();

            var clip = Resources.Load<AudioClip>("Music/俄羅斯方塊背景音樂1");
            if (clip == null)
            {
                Debug.LogWarning("BGM clip not found at Resources/Music/俄羅斯方塊背景音樂1");
                return;
            }
            bgm = gameObject.AddComponent<AudioSource>();
            bgm.clip = clip;
            bgm.loop = true;
            bgm.playOnAwake = false;
            bgm.spatialBlend = 0f;      // force 2D so the 3D import flag can't attenuate it
            bgm.volume = _volume;
            bgm.Play();
        }

        // ============================ SETUP ============================

        Sprite MakeSquareSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = camSize;
            cam.transform.position = new Vector3(camX, camY, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
        }

        // ============================ RESPONSIVE LAYOUT ============================

        // Picks a landscape (side panel) or portrait (stacked) layout from the current
        // aspect ratio, then frames the camera so the whole content box always fits.
        void ConfigureLayout()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            portrait = aspect < 1f;

            if (portrait)
            {
                // Board centered; NEXT + stats above, controls below.
                // Extra headroom above the stats keeps them clear of the
                // screen-space SETTINGS button in the top-right corner.
                previewOrigin = new Vector2(0f, 20.5f);
                FitCamera(-1f, 10f, -4.5f, 26.8f, aspect);
            }
            else
            {
                // Board on the left, info panel on the right.
                previewOrigin = new Vector2(11f, 13.5f);
                FitCamera(-1f, 15.5f, -1f, 21.5f, aspect);
            }
        }

        // Frames an orthographic camera so the world rect [xMin..xMax]x[yMin..yMax]
        // is fully visible for the given aspect ratio (letterboxing the shorter axis).
        void FitCamera(float xMin, float xMax, float yMin, float yMax, float aspect)
        {
            camX = (xMin + xMax) * 0.5f;
            camY = (yMin + yMax) * 0.5f;
            float halfH = (yMax - yMin) * 0.5f;
            float halfW = (xMax - xMin) * 0.5f;
            camSize = Mathf.Max(halfH, halfW / Mathf.Max(0.0001f, aspect));
        }

        void ApplyCameraFraming()
        {
            if (cam == null) return;
            cam.orthographicSize = camSize;
            cam.transform.position = new Vector3(camX, camY, -10f);
            LayoutBackdrop();   // the backdrop is sized off the camera view
        }

        // Re-place the preview panel objects after the layout origin changes.
        void LayoutPreview()
        {
            if (previewBG != null)
                previewBG.transform.position = new Vector3(previewOrigin.x + 1.5f, previewOrigin.y + 1.5f, 0);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    if (preview[x, y] != null)
                        preview[x, y].transform.position = new Vector3(previewOrigin.x + x, previewOrigin.y + y, 0);
        }

        // Called every frame; reconfigures only when the Game view size actually changes.
        void CheckLayout()
        {
            if (Screen.width == lastScreenW && Screen.height == lastScreenH) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height;
            ConfigureLayout();
            ApplyCameraFraming();
            LayoutPreview();
        }

        SpriteRenderer MakeSR(string name, Transform parent, Vector3 pos, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        void BuildVisuals()
        {
            // Border + well behind the playfield
            MakeSR("Border", transform, new Vector3(4.5f, 9.5f, 0), new Vector3(Width + 0.7f, Height + 0.7f, 1), accent, 0);
            MakeSR("Well",   transform, new Vector3(4.5f, 9.5f, 0), new Vector3(Width + 0.3f, Height + 0.3f, 1), wellColor, 1);

            // Playfield cells
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    cells[x, y] = MakeSR($"Cell_{x}_{y}", transform, new Vector3(x, y, 0), new Vector3(0.9f, 0.9f, 1), emptyCell, 2);

            // Preview panel
            Vector3 pc = new Vector3(previewOrigin.x + 1.5f, previewOrigin.y + 1.5f, 0);
            previewBG = MakeSR("PreviewBG", transform, pc, new Vector3(4.6f, 4.6f, 1), wellColor, 1);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    preview[x, y] = MakeSR($"Prev_{x}_{y}", transform,
                        new Vector3(previewOrigin.x + x, previewOrigin.y + y, 0), new Vector3(0.85f, 0.85f, 1), panelEmpty, 2);
        }

        // ============================ GAME FLOW ============================

        // spendItems is false when the board is only being reset on the way back to
        // the title screen — that is not a run starting, so passive items must not
        // be charged for it.
        void NewGame(bool spendItems = true)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    board[x, y] = -1;

            score = 0; lines = 0; level = 0;
            fallInterval = 0.8f;
            gameOver = false; paused = false;
            gravityTimer = 0; lockTimer = 0;
            bag.Clear();
            _rewardPaid = false;
            BeginRunItems(spendItems);
            BeginRunSkills();
            nextType = NextFromBag();
            SpawnPiece();
        }

        // Refills and reshuffles the bag when it runs dry. Split out so the
        // Extra Preview panel can peek past the current piece without draining it.
        void EnsureBag()
        {
            if (bag.Count != 0) return;
            for (int i = 0; i < 7; i++) bag.Add(i);
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }

        // The bag still decides the base piece, so bag fairness is untouched;
        // the mutation is applied to whatever it hands over.
        int NextFromBag()
        {
            EnsureBag();
            int t = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return ApplyMutation(t);
        }

        void SpawnPiece()
        {
            curType = nextType;
            nextType = NextFromBag();
            curRot = 0;
            curX = 3;
            // place so the piece's highest cell sits on the top visible row
            int maxOy = 0;
            var s = CellsOf(curType, 0);
            for (int i = 0; i < s.Length / 2; i++) maxOy = Mathf.Max(maxOy, s[i * 2 + 1]);
            curY = (Height - 1) - maxOy;

            // Revive gets first refusal on a failed spawn; only if it declines is
            // the run actually over.
            if (!CanPlace(curType, curRot, curX, curY) && !TryRevive())
            {
                gameOver = true;
                AwardRunCurrency();
            }

            gravityTimer = 0; lockTimer = 0;
            _holdUsedThisPiece = false;   // the hold rule resets with each new piece
        }

        // ============================ COLLISION ============================

        bool CanPlace(int type, int rot, int px, int py)
        {
            var s = CellsOf(type, rot);
            for (int i = 0; i < s.Length / 2; i++)
            {
                int x = px + s[i * 2];
                int y = py + s[i * 2 + 1];
                if (x < 0 || x >= Width || y < 0) return false;
                if (y < Height && board[x, y] >= 0) return false;
            }
            return true;
        }

        bool TryMove(int dx, int dy)
        {
            if (CanPlace(curType, curRot, curX + dx, curY + dy))
            {
                curX += dx; curY += dy;
                return true;
            }
            return false;
        }

        void TryRotate(int dir)
        {
            int nr = (curRot + dir + 4) % 4;
            int[] kicks = { 0, -1, 1, -2, 2 };
            foreach (int k in kicks)
            {
                if (CanPlace(curType, nr, curX + k, curY))
                {
                    curRot = nr; curX += k; lockTimer = 0;
                    return;
                }
            }
        }

        void LockPiece()
        {
            SnapshotForUndo();
            var s = CellsOf(curType, curRot);
            for (int i = 0; i < s.Length / 2; i++)
            {
                int x = curX + s[i * 2];
                int y = curY + s[i * 2 + 1];
                if (y >= 0 && y < Height && x >= 0 && x < Width) board[x, y] = curType;
            }

            // A bomb goes off the instant it lands, before the line check, so the
            // collapse it causes can itself complete a line.
            if (IsBomb(curType))
            {
                int destroyed = Detonate(curType, curX + s[0], curY + s[1]);
                score += destroyed * 10;
                Toast(BombName(curType) + "   " + destroyed + " cells");
            }

            ClearLines();
            SpawnPiece();
        }

        void ClearLines()
        {
            int cleared = 0;
            for (int y = 0; y < Height; y++)
            {
                bool full = true;
                for (int x = 0; x < Width; x++) if (board[x, y] < 0) { full = false; break; }
                if (full)
                {
                    cleared++;
                    for (int yy = y; yy < Height - 1; yy++)
                        for (int x = 0; x < Width; x++) board[x, yy] = board[x, yy + 1];
                    for (int x = 0; x < Width; x++) board[x, Height - 1] = -1;
                    y--; // recheck same row after shift
                }
            }
            if (cleared > 0)
            {
                score += LINE_POINTS[cleared] * (level + 1);
                lines += cleared;
                level = lines / 10;
                fallInterval = Mathf.Max(0.05f, 0.8f - level * 0.07f);
            }
        }

        int GhostY()
        {
            int gy = curY;
            while (CanPlace(curType, curRot, curX, gy - 1)) gy--;
            return gy;
        }

        // ============================ UPDATE ============================

        void Update()
        {
            CheckLayout();
            HandleAdminHotkey();
            if (inMenu)
            {
                if (!_inSettings && !_inShop && !_inSkills && StartPressed()) { NewGame(); inMenu = false; }
                Redraw();
                return;
            }
            if (showSettings) { Redraw(); return; }

            ReadInput(out bool left, out bool right, out bool cw, out bool ccw,
                      out bool downHeld, out bool hard, out bool doPause, out bool restart,
                      out bool leftHeld, out bool rightHeld);

            if (restart) { NewGame(); Redraw(); return; }

            // Runs before the game-over gate on purpose: Undo Lock is meant to be
            // able to rewind the very lock that ended the run.
            HandleItemKeys();

            if (gameOver) { Redraw(); return; }
            if (doPause) paused = !paused;
            if (paused) { Redraw(); return; }

            HandleSkillKeys();
            // Block Remove freezes the run while the player picks a target.
            if (_targeting) { Redraw(); return; }

            float dt = Time.deltaTime;

            // An inoperable piece ignores steering entirely — hard drop is the only
            // input it answers to, so the player can always get rid of it rather
            // than sitting out the gravity timer.
            if (IsInoperable(curType))
            {
                left = right = cw = ccw = false;
                downHeld = leftHeld = rightHeld = false;
            }

            // Horizontal move with DAS auto-repeat
            if (left)  { TryMove(-1, 0); dasDir = -1; dasTimer = 0; dasCharged = false; }
            else if (right) { TryMove(1, 0); dasDir = 1; dasTimer = 0; dasCharged = false; }

            bool holdMatches = (dasDir == -1 && leftHeld) || (dasDir == 1 && rightHeld);
            if (holdMatches)
            {
                dasTimer += dt;
                float delay = dasCharged ? dasRepeat : dasDelay;
                if (dasTimer >= delay) { TryMove(dasDir, 0); dasTimer = 0; dasCharged = true; }
            }
            else { dasDir = 0; dasCharged = false; }

            // Rotation
            if (cw) TryRotate(1);
            if (ccw) TryRotate(-1);

            // Hard drop
            if (hard)
            {
                int dropped = 0;
                while (TryMove(0, -1)) dropped++;
                score += dropped * 2;
                LockPiece();
                Redraw();
                return;
            }

            // Gravity (soft drop speeds it up)
            gravityTimer += dt;
            float interval = downHeld ? softInterval : CurrentFallInterval();
            if (gravityTimer >= interval)
            {
                gravityTimer = 0;
                if (TryMove(0, -1)) { if (downHeld) score += 1; }
            }

            // Lock delay when resting on the stack
            if (!CanPlace(curType, curRot, curX, curY - 1))
            {
                lockTimer += dt;
                if (lockTimer >= lockDelay) LockPiece();
            }
            else lockTimer = 0;

            Redraw();
        }

        // ============================ RENDER ============================

        void Redraw()
        {
            // base board
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    cells[x, y].color = board[x, y] >= 0 ? COLORS[board[x, y]] : emptyCell;

            if (!gameOver && !inMenu)
            {
                // ghost
                int gy = GhostY();
                var s = CellsOf(curType, curRot);
                Color ghost = Color.Lerp(emptyCell, COLORS[curType], 0.35f);
                for (int i = 0; i < s.Length / 2; i++)
                {
                    int x = curX + s[i * 2];
                    int y = gy + s[i * 2 + 1];
                    if (x >= 0 && x < Width && y >= 0 && y < Height && board[x, y] < 0)
                        cells[x, y].color = ghost;
                }
                // active piece
                for (int i = 0; i < s.Length / 2; i++)
                {
                    int x = curX + s[i * 2];
                    int y = curY + s[i * 2 + 1];
                    if (x >= 0 && x < Width && y >= 0 && y < Height)
                        cells[x, y].color = COLORS[curType];
                }
            }

            // preview
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    preview[x, y].color = panelEmpty;
            if (nextType >= 0 && !inMenu)
            {
                var ns = CellsOf(nextType, 0);
                for (int i = 0; i < ns.Length / 2; i++)
                {
                    int x = ns[i * 2];
                    int y = ns[i * 2 + 1];
                    if (x >= 0 && x < 4 && y >= 0 && y < 4) preview[x, y].color = COLORS[nextType];
                }
            }

            ApplyTargetHighlight();
        }

        // ============================ INPUT ============================

        void ReadInput(out bool left, out bool right, out bool cw, out bool ccw,
                       out bool downHeld, out bool hard, out bool pause, out bool restart,
                       out bool leftHeld, out bool rightHeld)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) { left = right = cw = ccw = downHeld = hard = pause = restart = leftHeld = rightHeld = false; return; }
            left      = k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
            right     = k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
            cw        = k.upArrowKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
            ccw       = k.zKey.wasPressedThisFrame || k.leftCtrlKey.wasPressedThisFrame;
            downHeld  = k.downArrowKey.isPressed || k.sKey.isPressed;
            hard      = k.spaceKey.wasPressedThisFrame;
            pause     = k.pKey.wasPressedThisFrame;
            restart   = k.rKey.wasPressedThisFrame;
            leftHeld  = k.leftArrowKey.isPressed || k.aKey.isPressed;
            rightHeld = k.rightArrowKey.isPressed || k.dKey.isPressed;
#else
            left      = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            right     = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            cw        = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.W);
            ccw       = Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl);
            downHeld  = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            hard      = Input.GetKeyDown(KeyCode.Space);
            pause     = Input.GetKeyDown(KeyCode.P);
            restart   = Input.GetKeyDown(KeyCode.R);
            leftHeld  = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            rightHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
#endif
        }

        // ============================ HUD ============================

        GUIStyle _label, _value, _title, _big, _small, _stat, _smallC;
        GUIStyle _gearBtn, _menuBox, _menuTitle, _menuBtn, _menuClose, _menuField;
        bool showSettings;              // pause menu open?
        bool _inSettings;               // on the settings sub-page (vs the main pause menu)?
        bool _resOpen;                  // RES dropdown expanded?
        Vector2 _resScroll;
        bool _resSeeded;                // scrolled-to-current on open?
        int _pickW = -1, _pickH = -1;   // last selected resolution (drives the highlight immediately)
        float _appliedAt = -10f;        // realtime of the last apply, for the transient "Applied" toast
        string _appliedMsg = "";        // text shown in that toast
        Texture2D _whiteTex;
        FullScreenMode _fsMode;         // initialised from Screen.fullScreenMode in Awake
        float _uiScale = 1f;            // multiplies all HUD/menu font sizes
        int _fpsCap = -1;               // -1 = unlimited / engine default (not forced)
        string _fpsInput = "";          // FPS text-field buffer

        void OnGUI()
        {
            if (cam == null) return;
            HandleSettingsHotkey();
            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));

            if (_label == null)
            {
                _label = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                _value = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                _title = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _big   = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _small = new GUIStyle { fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft };
                _stat  = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _smallC = new GUIStyle { fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter };
                // Interactive styles must derive from the default skin so buttons keep a visible background
                _gearBtn  = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                _menuBtn  = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(2, 2, 2, 2) };
                _menuClose = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                _menuField = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter };
                _menuBox  = new GUIStyle(GUI.skin.box);
                _menuTitle = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _whiteTex.SetPixel(0, 0, Color.white); _whiteTex.Apply();
            }
            _label.fontSize = fs; _label.normal.textColor = new Color(0.6f, 0.75f, 0.85f);
            _value.fontSize = Mathf.RoundToInt(fs * 1.4f); _value.normal.textColor = Color.white;
            _title.fontSize = Mathf.RoundToInt(fs * 1.6f); _title.normal.textColor = accent;
            _big.fontSize   = Mathf.RoundToInt(fs * 2.0f); _big.normal.textColor = Color.white;
            _small.fontSize = Mathf.RoundToInt(fs * 0.85f); _small.normal.textColor = new Color(0.5f, 0.55f, 0.65f);
            _stat.fontSize  = Mathf.RoundToInt(fs * 1.15f); _stat.normal.textColor = Color.white;
            _smallC.fontSize = Mathf.RoundToInt(fs * 0.85f); _smallC.normal.textColor = new Color(0.5f, 0.55f, 0.65f);

            // Title screen replaces the whole in-game HUD until the player presses START.
            if (inMenu)
            {
                if (_inSettings) DrawSettingsPanel();
                else if (_inShop) DrawShopScreen();
                else if (_inSkills) DrawSkillTreeScreen();
                else DrawTitleMenu();
                if (_adminOpen) DrawAdminPanel();
                return;
            }

            if (portrait)
            {
                // NEXT panel (top-left) and stats (top-right)
                WLabel(previewOrigin.x, previewOrigin.y + 3.9f, "NEXT", _label, 200);
                WLabel(6.8f, 23.6f, "SCORE " + score, _stat, 340);
                WLabel(6.8f, 22.5f, "LEVEL " + level, _stat, 340);
                WLabel(6.8f, 21.4f, "LINES " + lines, _stat, 340);

                // Controls (below the board)
                WLabel(4.5f, -2.3f, "← →  Move    ↑ / X  Rotate    ↓  Soft", _smallC, 900);
                WLabel(4.5f, -3.4f, "Space  Hard drop  ·  P Pause  ·  R Restart", _smallC, 900);
            }
            else
            {
                WLabel(4.5f, 20.6f, "T E T R I S", _title, 400);
                WLabel(previewOrigin.x, previewOrigin.y + 4.2f, "NEXT", _label, 200);

                WLabel(11f, 12.3f, "SCORE", _label, 200);
                WLabel(11f, 11.5f, score.ToString(), _value, 200);
                WLabel(11f, 10.0f, "LEVEL", _label, 200);
                WLabel(11f, 9.2f, level.ToString(), _value, 200);
                WLabel(11f, 7.7f, "LINES", _label, 200);
                WLabel(11f, 6.9f, lines.ToString(), _value, 200);

                WLabel(11f, 4.6f, "← →  Move", _small, 220);
                WLabel(11f, 4.0f, "↑ / X  Rotate", _small, 220);
                WLabel(11f, 3.4f, "↓  Soft drop", _small, 220);
                WLabel(11f, 2.8f, "Space  Hard drop", _small, 220);
                WLabel(11f, 2.2f, "P Pause · R Restart", _small, 220);
            }

            if (gameOver)
            {
                DrawGameOverPanel();
            }
            else if (paused)
            {
                WLabel(4.5f, 10.5f, "PAUSED", _big, 400);
            }

            if (!showSettings)
            {
                DrawSettingsButton();
                DrawSkillHud();
                DrawItemHud();
                DrawTargetingBanner();
                DrawToast();
            }
            else if (_inSettings) DrawSettingsPanel();
            else DrawPauseMenu();

            if (_adminOpen) DrawAdminPanel();
        }

        void WLabel(float wx, float wy, string text, GUIStyle style, float width)
        {
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(wx, wy, 0));
            // Snap to whole pixels: dynamic-font glyphs drawn at fractional positions get
            // bilinear-sampled from the font atlas and look blurry. An even rect height keeps
            // the vertically-centered baseline on a pixel too.
            float h = 2f * Mathf.Round(style.fontSize * 0.9f);
            float rx = Mathf.Round(sp.x - width * 0.5f + (style.alignment == TextAnchor.MiddleLeft ? width * 0.5f : 0));
            float ry = Mathf.Round(Screen.height - sp.y - style.fontSize);
            var r = new Rect(rx, ry, width, h);
            // draw a subtle shadow for readability
            var prev = style.normal.textColor;
            style.normal.textColor = new Color(0, 0, 0, 0.6f);
            GUI.Label(new Rect(rx + 1f, ry + 1f, width, h), text, style);
            style.normal.textColor = prev;
            GUI.Label(r, text, style);
        }

        // ============================ SETTINGS MENU ============================

        // Applies every persisted display setting on boot. On a totally fresh install (no keys)
        // it touches nothing, preserving the default borderless-fullscreen launch (zero regression).
        void LoadSettings()
        {
            // Music volume
            if (PlayerPrefs.HasKey(PrefVolume))
                _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefVolume));

            // UI scale
            if (PlayerPrefs.HasKey(PrefUIScale))
                _uiScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefUIScale), 0.5f, 2f);

            // Frame-rate cap (only forced if the player set one)
            if (PlayerPrefs.HasKey(PrefFps))
            {
                _fpsCap = PlayerPrefs.GetInt(PrefFps);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = _fpsCap;
            }
            _fpsInput = _fpsCap > 0 ? _fpsCap.ToString() : "";

            // Display mode: default to whatever the build launched in
            _fsMode = PlayerPrefs.HasKey(PrefMode) && PlayerPrefs.GetInt(PrefMode) == 1
                ? FullScreenMode.FullScreenWindow
                : (PlayerPrefs.HasKey(PrefMode) ? FullScreenMode.Windowed : Screen.fullScreenMode);

            // Resolution: seed the highlight, then apply according to the resolved mode
            if (PlayerPrefs.HasKey(PrefResW) && PlayerPrefs.HasKey(PrefResH))
            {
                int w = PlayerPrefs.GetInt(PrefResW), h = PlayerPrefs.GetInt(PrefResH);
                if (w > 0 && h > 0) { _pickW = w; _pickH = h; }
            }

            if (PlayerPrefs.HasKey(PrefMode))
            {
                if (_fsMode == FullScreenMode.Windowed && _pickW > 0)
                    Screen.SetResolution(_pickW, _pickH, FullScreenMode.Windowed);
                else if (_fsMode == FullScreenMode.FullScreenWindow)
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
            else if (_pickW > 0 && !(Screen.width == _pickW && Screen.height == _pickH))
            {
                // Legacy: a resolution was saved before display-mode existed -> windowed, as before
                Screen.SetResolution(_pickW, _pickH, FullScreenMode.Windowed);
            }
        }

        void SaveResolution(int w, int h)
        {
            PlayerPrefs.SetInt(PrefResW, w);
            PlayerPrefs.SetInt(PrefResH, h);
            PlayerPrefs.Save();
        }

        void ApplyPreset(int w, int h)
        {
            // Reachable only in windowed mode (the list is disabled in borderless).
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            SaveResolution(w, h);
            // Move the highlight immediately and show a confirmation. Screen.width/height may not
            // update this frame (and never updates inside the Editor Game view), so the UI must not
            // wait on it for feedback.
            _pickW = w; _pickH = h;
            Toast("Applied  " + w + " x " + h);
        }

        void ApplyDisplayMode(FullScreenMode m)
        {
            _fsMode = m;
            if (m == FullScreenMode.Windowed)
            {
                int w = _pickW > 0 ? _pickW : Screen.width;
                int h = _pickH > 0 ? _pickH : Screen.height;
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            }
            else // borderless fullscreen always fills the monitor at desktop resolution
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
            PlayerPrefs.SetInt(PrefMode, m == FullScreenMode.FullScreenWindow ? 1 : 0);
            PlayerPrefs.Save();
            Toast("Display: " + (m == FullScreenMode.FullScreenWindow ? "Borderless" : "Windowed"));
        }

        void ApplyUIScale(float s)
        {
            _uiScale = s;
            PlayerPrefs.SetFloat(PrefUIScale, s);
            PlayerPrefs.Save();
            Toast("UI scale: " + Mathf.RoundToInt(s * 100f) + "%");
        }

        void ApplyFps(int f)
        {
            _fpsCap = f;
            QualitySettings.vSyncCount = 0;        // vSync would otherwise override the cap
            Application.targetFrameRate = f;       // -1 = unlimited
            _fpsInput = f > 0 ? f.ToString() : "";
            PlayerPrefs.SetInt(PrefFps, f);
            PlayerPrefs.Save();
            Toast("FPS: " + (f > 0 ? f.ToString() : "Unlimited"));
        }

        void ApplyVolume(float v)
        {
            _volume = Mathf.Clamp01(v);
            if (bgm != null) bgm.volume = _volume;
            PlayerPrefs.SetFloat(PrefVolume, _volume);
            PlayerPrefs.Save();
            Toast("Volume: " + Mathf.RoundToInt(_volume * 100f) + "%");
        }

        void Toast(string msg)
        {
            _appliedMsg = msg;
            _appliedAt = Time.realtimeSinceStartup;
        }

        static string AspectLabel(int w, int h)
        {
            int a = w, b = h;
            while (b != 0) { int t = a % b; a = b; b = t; }
            return (w / a) + ":" + (h / a);
        }

        void HandleSettingsHotkey()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (inMenu)
                {
                    if (_inSettings) { _inSettings = false; _resOpen = false; e.Use(); }
                    else if (_inShop) { _inShop = false; e.Use(); }
                    else if (_inSkills) { _inSkills = false; e.Use(); }
                    return;   // no pause menu on the title screen
                }

                // While picking a Block Remove target, Esc backs out of that
                // rather than opening the pause menu.
                if (_targeting) { _targeting = false; Toast("Cancelled"); e.Use(); return; }
                if (showSettings && _inSettings) _inSettings = false;   // settings -> main pause menu
                else showSettings = !showSettings;                      // open / close the menu
                _resOpen = false;
                e.Use();
            }
        }

        void DrawSettingsButton()
        {
            float bh = Mathf.Max(40f, Screen.height * 0.07f);
            float bw = bh * 3.4f;
            _gearBtn.fontSize = Mathf.RoundToInt(bh * 0.4f);
            if (GUI.Button(new Rect(Screen.width - bw - 12f, 12f, bw, bh), "MENU", _gearBtn))
            {
                showSettings = true;
                _inSettings = false;
                _resOpen = false;
            }
        }

        static readonly int[] FPS_VALUES = { -1, 30, 60, 90, 120, 144, 165, 240 };

        void DrawSettingsPanel()
        {
            // dim everything behind the panel
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));
            _menuBtn.fontSize = fs; _menuClose.fontSize = fs; _menuField.fontSize = fs; _label.fontSize = fs;

            bool windowed = _fsMode == FullScreenMode.Windowed;
            int deskW = Screen.currentResolution.width, deskH = Screen.currentResolution.height;
            var visible = new List<int>();
            for (int i = 0; i < RESOLUTIONS.Length / 2; i++)
                if (RESOLUTIONS[i * 2] <= deskW && RESOLUTIONS[i * 2 + 1] <= deskH)
                    visible.Add(i);

            float pad = fs * 0.8f, rowH = fs * 2.0f, gap = fs * 0.5f, optH = fs * 1.9f;
            float titleH = fs * 2.2f, closeH = fs * 2.2f, msgH = fs * 1.6f;
            float listH = Mathf.Min(visible.Count, 7) * optH;
            float longestRowW = _menuBtn.CalcSize(new GUIContent("8192 x 4320  (16:9)   ✓ current")).x;
            float panelW = Mathf.Round(Mathf.Clamp(longestRowW + fs * 3.2f, 360f, Screen.width * 0.94f));
            float contentH = titleH + gap + 6f * (rowH + gap) + (_resOpen ? listH : 0f) + msgH + closeH + pad * 2f;
            float panelH = Mathf.Round(Mathf.Min(contentH, Screen.height * 0.94f));
            float px = Mathf.Round((Screen.width - panelW) * 0.5f);
            float py = Mathf.Round((Screen.height - panelH) * 0.5f);
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);
            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.5f);
            _menuTitle.normal.textColor = accent;

            float y = py + fs * 0.4f;
            GUI.Label(new Rect(px, y, panelW, titleH), "SETTINGS", _menuTitle);
            y += titleH + gap;

            // MODE (2 options -> either arrow toggles)
            if (Stepper(innerX, y, innerW, rowH, gap, "MODE", windowed ? "Windowed" : "Borderless", true) != 0)
                ApplyDisplayMode(windowed ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            y += rowH + gap;

            // UI SIZE
            int uiIdx = 1;
            for (int i = 0; i < UI_SCALES.Length; i++) if (Mathf.Approximately(UI_SCALES[i], _uiScale)) uiIdx = i;
            int dUi = Stepper(innerX, y, innerW, rowH, gap, "UI", Mathf.RoundToInt(_uiScale * 100f) + "%", true);
            if (dUi != 0) ApplyUIScale(UI_SCALES[(uiIdx + dUi + UI_SCALES.Length) % UI_SCALES.Length]);
            y += rowH + gap;

            // FPS (cycles preset caps)
            int fi = 0;
            for (int i = 0; i < FPS_VALUES.Length; i++) if (FPS_VALUES[i] == _fpsCap) fi = i;
            int dFps = Stepper(innerX, y, innerW, rowH, gap, "FPS", _fpsCap < 0 ? "Unlimited" : _fpsCap + " fps", true);
            if (dFps != 0) ApplyFps(FPS_VALUES[(fi + dFps + FPS_VALUES.Length) % FPS_VALUES.Length]);
            y += rowH + gap;

            // FPS custom entry (type any value, 10-1000) — aligned under the FPS stepper
            {
                float vx = innerX + innerW * 0.24f + gap, vw = innerW - innerW * 0.24f - gap;
                float setW = Mathf.Round(vw * 0.28f);
                _fpsInput = GUI.TextField(new Rect(vx, y, vw - setW - gap, rowH), _fpsInput, 5, _menuField);
                if (GUI.Button(new Rect(vx + vw - setW, y, setW, rowH), "Set", _menuBtn))
                    if (int.TryParse(_fpsInput, out int cv)) ApplyFps(Mathf.Clamp(cv, 10, 1000));
                y += rowH + gap;
            }

            // VOLUME (music) — clamps at 0/100% instead of wrapping
            int dVol = Stepper(innerX, y, innerW, rowH, gap, "VOL", Mathf.RoundToInt(_volume * 100f) + "%", true);
            if (dVol != 0) ApplyVolume(_volume + dVol * VolumeStep);
            y += rowH + gap;

            // RES (windowed only; dropdown list picker)
            y = DrawResDropdown(innerX, y, innerW, rowH, optH, gap, windowed, visible, listH);

            // transient confirmation above CLOSE
            if (Time.realtimeSinceStartup - _appliedAt < 2.5f && _appliedMsg.Length > 0)
            {
                var prev = _stat.normal.textColor;
                _stat.normal.textColor = accent;
                GUI.Label(new Rect(px, py + panelH - closeH - msgH - pad, panelW, msgH), _appliedMsg, _stat);
                _stat.normal.textColor = prev;
            }

            if (GUI.Button(new Rect(innerX, py + panelH - closeH - pad, innerW, closeH), "BACK", _menuClose))
                _inSettings = false;
        }

        // Minecraft-style main pause menu: a vertical stack of full-width buttons.
        void DrawPauseMenu()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));
            _menuClose.fontSize = fs;

            float pad = fs * 0.9f, btnH = fs * 2.6f, gap = fs * 0.7f, titleH = fs * 2.4f;
            float panelW = Mathf.Round(Mathf.Clamp(fs * 18f, 320f, Screen.width * 0.9f));
            string[] items = { "RESUME", "SETTINGS", "RESTART", "QUIT" };
            float panelH = Mathf.Round(titleH + gap + items.Length * (btnH + gap) + pad * 2f);
            float px = Mathf.Round((Screen.width - panelW) * 0.5f);
            float py = Mathf.Round((Screen.height - panelH) * 0.5f);
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);
            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.6f);
            _menuTitle.normal.textColor = accent;

            float y = py + pad;
            GUI.Label(new Rect(px, y, panelW, titleH), "PAUSED", _menuTitle);
            y += titleH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "RESUME", _menuClose))
                showSettings = false;
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "SETTINGS", _menuClose))
            { _inSettings = true; _resOpen = false; }
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "RESTART", _menuClose))
            { NewGame(); Redraw(); showSettings = false; }
            y += btnH + gap;
            // QUIT here returns to the title screen; only the title's QUIT exits the app.
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "QUIT", _menuClose))
            { showSettings = false; _inSettings = false; _resOpen = false; inMenu = true; NewGame(false); Redraw(); }
        }

        // Title screen shown on launch: a big title over START / SETTINGS / QUIT.
        // Reuses the pause-menu button/box styles so it matches the rest of the UI.
        void DrawTitleMenu()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));
            _menuClose.fontSize = fs;

            float pad = fs * 0.9f, btnH = fs * 2.6f, gap = fs * 0.7f, titleH = fs * 3.2f;
            float panelW = Mathf.Round(Mathf.Clamp(fs * 18f, 320f, Screen.width * 0.9f));
            string[] items = { "START", "SHOP", "SKILLS", "SETTINGS", "ADMIN", "QUIT" };
            float panelH = Mathf.Round(titleH + gap + items.Length * (btnH + gap) + pad * 2f);
            float px = Mathf.Round((Screen.width - panelW) * 0.5f);
            float py = Mathf.Round((Screen.height - panelH) * 0.5f);
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);
            _menuTitle.fontSize = Mathf.RoundToInt(fs * 2.0f);
            _menuTitle.normal.textColor = accent;

            float y = py + pad;
            GUI.Label(new Rect(px, y, panelW, titleH), "T E T R I S", _menuTitle);
            y += titleH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "START", _menuClose))
            { NewGame(); inMenu = false; Redraw(); }
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "SHOP", _menuClose))
            { _inShop = true; _shopMessage = ""; }
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "SKILLS", _menuClose))
            { _inSkills = true; _skillMessage = ""; }
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "SETTINGS", _menuClose))
            { _inSettings = true; _resOpen = false; }
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "ADMIN", _menuClose))
                _adminOpen = !_adminOpen;
            y += btnH + gap;
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "QUIT", _menuClose))
                Application.Quit();
        }

        // Enter/Return also starts the game from the title screen (buttons still work).
        bool StartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        // A "◄  value  ►" selector row. Returns -1 (previous), +1 (next), or 0 (no click).
        int Stepper(float x, float y, float w, float rowH, float gap, string label, string current, bool enabled)
        {
            float labelW = w * 0.24f;
            GUI.Label(new Rect(x, y, labelW, rowH), label, _label);
            float vx = x + labelW + gap, vw = w - labelW - gap;
            float aw = Mathf.Round(Mathf.Min(rowH, vw * 0.22f));
            int d = 0;
            GUI.enabled = enabled;
            if (GUI.Button(new Rect(vx, y, aw, rowH), "◄", _menuBtn)) d = -1;
            GUI.Label(new Rect(vx + aw, y, vw - 2f * aw, rowH), current, _menuField);
            if (GUI.Button(new Rect(vx + vw - aw, y, aw, rowH), "►", _menuBtn)) d = 1;
            GUI.enabled = true;
            return d;
        }

        // RES row: a dropdown whose scrollable list lets you pick any resolution in one click.
        float DrawResDropdown(float x, float y, float w, float rowH, float optH, float gap,
                              bool windowed, List<int> visible, float listH)
        {
            float labelW = w * 0.24f, ox = x + labelW + gap, ow = w - labelW - gap;
            GUI.Label(new Rect(x, y, labelW, rowH), "RES", _label);
            string cur = !windowed ? "windowed only" : (_pickW > 0 ? _pickW + " x " + _pickH : "—");
            GUI.enabled = windowed;
            if (GUI.Button(new Rect(ox, y, ow, rowH), cur + (_resOpen ? "   ▲" : "   ▼"), _menuBtn))
            {
                _resOpen = !_resOpen;
                if (_resOpen) _resSeeded = false;
            }
            y += rowH + gap;
            if (_resOpen && windowed)
            {
                Rect view = new Rect(ox, y, ow, listH);
                Rect content = new Rect(0, 0, ow - 20f, visible.Count * optH);
                if (!_resSeeded)
                {
                    _resSeeded = true;
                    _resScroll = Vector2.zero;
                    if (_pickW <= 0) { _pickW = Screen.width; _pickH = Screen.height; }
                    for (int v = 0; v < visible.Count; v++)
                        if (RESOLUTIONS[visible[v] * 2] == _pickW && RESOLUTIONS[visible[v] * 2 + 1] == _pickH)
                        { _resScroll.y = Mathf.Max(0f, v * optH - listH * 0.5f); break; }
                }
                _resScroll = GUI.BeginScrollView(view, _resScroll, content);
                for (int v = 0; v < visible.Count; v++)
                {
                    int i = visible[v];
                    int w2 = RESOLUTIONS[i * 2], h2 = RESOLUTIONS[i * 2 + 1];
                    bool sel = (w2 == _pickW && h2 == _pickH);
                    string label = w2 + " x " + h2 + "  (" + AspectLabel(w2, h2) + ")" + (sel ? "   ✓" : "");
                    if (sel) GUI.backgroundColor = accent;
                    if (GUI.Button(new Rect(0, v * optH, content.width, optH - 2f), label, _menuBtn))
                    { ApplyPreset(w2, h2); _resOpen = false; }
                    if (sel) GUI.backgroundColor = Color.white;
                }
                GUI.EndScrollView();
                y += listH;
            }
            GUI.enabled = true;
            return y;
        }
    }
}
