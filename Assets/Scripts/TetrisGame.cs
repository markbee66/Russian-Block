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
    public class TetrisGame : MonoBehaviour
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
        };

        static readonly Color[] COLORS =
        {
            new Color(0.10f,0.85f,0.95f), // I cyan
            new Color(0.98f,0.85f,0.10f), // O yellow
            new Color(0.72f,0.28f,0.92f), // T purple
            new Color(0.25f,0.85f,0.32f), // S green
            new Color(0.95f,0.22f,0.28f), // Z red
            new Color(0.22f,0.48f,0.95f), // J blue
            new Color(0.98f,0.55f,0.12f), // L orange
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

        
            void Awake()
        {
            Application.runInBackground = true;
            square = MakeSquareSprite();
            LoadResolution();
            lastScreenW = Screen.width; lastScreenH = Screen.height;
            ConfigureLayout();
            SetupCamera();
            BuildVisuals();
            NewGame();
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

        void NewGame()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    board[x, y] = -1;

            score = 0; lines = 0; level = 0;
            fallInterval = 0.8f;
            gameOver = false; paused = false;
            gravityTimer = 0; lockTimer = 0;
            bag.Clear();
            nextType = NextFromBag();
            SpawnPiece();
        }

        int NextFromBag()
        {
            if (bag.Count == 0)
            {
                for (int i = 0; i < 7; i++) bag.Add(i);
                for (int i = bag.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (bag[i], bag[j]) = (bag[j], bag[i]);
                }
            }
            int t = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return t;
        }

        void SpawnPiece()
        {
            curType = nextType;
            nextType = NextFromBag();
            curRot = 0;
            curX = 3;
            // place so the piece's highest cell sits on the top visible row
            int maxOy = 0;
            var s = SHAPES[curType][0];
            for (int i = 0; i < 4; i++) maxOy = Mathf.Max(maxOy, s[i * 2 + 1]);
            curY = (Height - 1) - maxOy;

            if (!CanPlace(curType, curRot, curX, curY))
                gameOver = true;

            gravityTimer = 0; lockTimer = 0;
        }

        // ============================ COLLISION ============================

        bool CanPlace(int type, int rot, int px, int py)
        {
            var s = SHAPES[type][rot];
            for (int i = 0; i < 4; i++)
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
            var s = SHAPES[curType][curRot];
            for (int i = 0; i < 4; i++)
            {
                int x = curX + s[i * 2];
                int y = curY + s[i * 2 + 1];
                if (y >= 0 && y < Height && x >= 0 && x < Width) board[x, y] = curType;
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
            if (showSettings) { Redraw(); return; }

            ReadInput(out bool left, out bool right, out bool cw, out bool ccw,
                      out bool downHeld, out bool hard, out bool doPause, out bool restart,
                      out bool leftHeld, out bool rightHeld);

            if (restart) { NewGame(); Redraw(); return; }
            if (gameOver) { Redraw(); return; }
            if (doPause) paused = !paused;
            if (paused) { Redraw(); return; }

            float dt = Time.deltaTime;

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
            float interval = downHeld ? softInterval : fallInterval;
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

            if (!gameOver)
            {
                // ghost
                int gy = GhostY();
                var s = SHAPES[curType][curRot];
                Color ghost = Color.Lerp(emptyCell, COLORS[curType], 0.35f);
                for (int i = 0; i < 4; i++)
                {
                    int x = curX + s[i * 2];
                    int y = gy + s[i * 2 + 1];
                    if (x >= 0 && x < Width && y >= 0 && y < Height && board[x, y] < 0)
                        cells[x, y].color = ghost;
                }
                // active piece
                for (int i = 0; i < 4; i++)
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
            if (nextType >= 0)
            {
                var ns = SHAPES[nextType][0];
                for (int i = 0; i < 4; i++)
                {
                    int x = ns[i * 2];
                    int y = ns[i * 2 + 1];
                    if (x >= 0 && x < 4 && y >= 0 && y < 4) preview[x, y].color = COLORS[nextType];
                }
            }
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
        GUIStyle _gearBtn, _menuBox, _menuTitle, _menuBtn, _menuClose;
        bool showSettings;
        Vector2 _menuScroll;
        bool _menuScrollInit;
        Texture2D _whiteTex;

        void OnGUI()
        {
            if (cam == null) return;
            HandleSettingsHotkey();
            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f));

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
                _menuBtn  = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
                _menuClose = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
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
                WLabel(4.5f, 10.5f, "GAME OVER", _big, 400);
                WLabel(4.5f, 8.8f, "Press R to restart", _label, 400);
            }
            else if (paused)
            {
                WLabel(4.5f, 10.5f, "PAUSED", _big, 400);
            }

            if (!showSettings) DrawSettingsButton();
            else               DrawSettingsPanel();
        }

        void WLabel(float wx, float wy, string text, GUIStyle style, float width)
        {
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(wx, wy, 0));
            var r = new Rect(sp.x - width * 0.5f + (style.alignment == TextAnchor.MiddleLeft ? width * 0.5f : 0),
                             Screen.height - sp.y - style.fontSize, width, style.fontSize * 1.8f);
            // draw a subtle shadow for readability
            var prev = style.normal.textColor;
            style.normal.textColor = new Color(0, 0, 0, 0.6f);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
            style.normal.textColor = prev;
            GUI.Label(r, text, style);
        }

        // ============================ SETTINGS MENU ============================

        void LoadResolution()
        {
            if (!PlayerPrefs.HasKey(PrefResW) || !PlayerPrefs.HasKey(PrefResH)) return;
            int w = PlayerPrefs.GetInt(PrefResW), h = PlayerPrefs.GetInt(PrefResH);
            if (w <= 0 || h <= 0) return;
            if (Screen.width == w && Screen.height == h) return;
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
        }

        void SaveResolution(int w, int h)
        {
            PlayerPrefs.SetInt(PrefResW, w);
            PlayerPrefs.SetInt(PrefResH, h);
            PlayerPrefs.Save();
        }

        void ApplyPreset(int w, int h)
        {
            // Windowed is the only mode where the OS window actually takes the requested shape;
            // borderless fullscreen would keep covering the whole monitor regardless.
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            SaveResolution(w, h);
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
                showSettings = !showSettings;
                _menuScrollInit = false;
                e.Use();
            }
        }

        void DrawSettingsButton()
        {
            float bh = Mathf.Max(26f, Screen.height * 0.045f);
            float bw = bh * 3.4f;
            _gearBtn.fontSize = Mathf.RoundToInt(bh * 0.42f);
            if (GUI.Button(new Rect(Screen.width - bw - 12f, 12f, bw, bh), "SETTINGS", _gearBtn))
            {
                showSettings = true;
                _menuScrollInit = false;
            }
        }

        void DrawSettingsPanel()
        {
            // dim everything behind the panel
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f));
            float panelW = Mathf.Min(Screen.width * 0.85f, 560f);
            float panelH = Screen.height * 0.8f;
            float px = (Screen.width - panelW) * 0.5f;
            float py = (Screen.height - panelH) * 0.5f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);

            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.5f);
            _menuTitle.normal.textColor = accent;
            float titleH = fs * 2.4f;
            GUI.Label(new Rect(px, py + fs * 0.4f, panelW, titleH), "RESOLUTION", _menuTitle);

            float closeH = fs * 2.2f;
            float pad = fs * 0.8f;
            float rowH = fs * 2.0f;
            float rowGap = fs * 0.3f;
            _menuBtn.fontSize = fs;
            _menuClose.fontSize = fs;

            // resolutions that fit on the current desktop
            int deskW = Screen.currentResolution.width, deskH = Screen.currentResolution.height;
            int count = RESOLUTIONS.Length / 2;
            var visible = new List<int>(count);
            for (int i = 0; i < count; i++)
                if (RESOLUTIONS[i * 2] <= deskW && RESOLUTIONS[i * 2 + 1] <= deskH)
                    visible.Add(i);

            Rect viewRect = new Rect(px + pad, py + titleH, panelW - pad * 2f, panelH - titleH - closeH - pad * 2f);
            float contentH = visible.Count * (rowH + rowGap);
            Rect contentRect = new Rect(0, 0, viewRect.width - 20f, contentH);

            // first open: scroll to the currently active entry
            if (!_menuScrollInit)
            {
                _menuScrollInit = true;
                _menuScroll = Vector2.zero;
                for (int v = 0; v < visible.Count; v++)
                {
                    int i = visible[v];
                    if (RESOLUTIONS[i * 2] == Screen.width && RESOLUTIONS[i * 2 + 1] == Screen.height)
                    {
                        _menuScroll.y = Mathf.Max(0f, v * (rowH + rowGap) - viewRect.height * 0.5f);
                        break;
                    }
                }
            }

            _menuScroll = GUI.BeginScrollView(viewRect, _menuScroll, contentRect);
            for (int v = 0; v < visible.Count; v++)
            {
                int i = visible[v];
                int w = RESOLUTIONS[i * 2], h = RESOLUTIONS[i * 2 + 1];
                bool current = (w == Screen.width && h == Screen.height);
                string label = w + " x " + h + "  (" + AspectLabel(w, h) + ")" + (current ? "  (current)" : "");
                if (current) GUI.backgroundColor = accent;
                if (GUI.Button(new Rect(0, v * (rowH + rowGap), contentRect.width, rowH), label, _menuBtn))
                    ApplyPreset(w, h);
                if (current) GUI.backgroundColor = Color.white;
            }
            GUI.EndScrollView();

            if (GUI.Button(new Rect(px + pad, py + panelH - closeH - pad, panelW - pad * 2f, closeH), "CLOSE", _menuClose))
                showSettings = false;
        }
    }
}
