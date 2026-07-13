# MCP for Unity 使用說明書（Claude / AI Agent 版）

給「Claude」看的操作指南：如何確認連線、用哪些工具、標準工作流程、以及會浪費時間的坑。
本文件依實際跑通的環境撰寫（CoplayDev MCP for Unity，HTTP 傳輸 `http://127.0.0.1:8080/mcp`，工具前綴 `mcp__UnityMCP__`）。

---

## 1. 前置：確認自己有沒有工具

MCP 伺服器是 **HTTP 傳輸**，Claude Code 以名稱 `UnityMCP` 註冊，工具會以 `mcp__UnityMCP__*` 出現。

- 這些工具常是 **deferred**（延遲載入）。先用 `ToolSearch` 以 `select:` 載入 schema 再呼叫，例如：
  `select:mcp__UnityMCP__manage_scene,mcp__UnityMCP__read_console,mcp__UnityMCP__manage_gameobject`
- 若 `ToolSearch` **完全找不到** `mcp__UnityMCP__*`：代表這個 session 尚未載入該伺服器。
  - 用 Bash 執行 `claude mcp list` 確認註冊與連線狀態。
  - 若顯示 `No MCP servers configured`，用指令補註冊（見下方「修復」）。
  - 註冊好之後，**本 session 仍不會自動吃到工具** → 需要操作員 **Reload Window / 重開 Claude Code**。這一步只能請人做。

---

## 2. 連線健康檢查（每次開工前）

用資源（Resources）讀狀態，不要用工具去猜：

- `mcpforunity://instances` — 列出有幾個 Unity 實例（`Name@hash`）。
- `mcpforunity://editor/state` — 看 `advice.ready_for_tools` 是否為 `true`、`compilation.is_compiling`、`play_mode.is_playing`、`active_scene`。

**多實例時**：伺服器會在沒指定 active instance 時報錯。用 `set_active_instance` 傳入 `Name@hash` 釘住整個 session，或在單一工具呼叫帶 `unity_instance` 參數。

---

## 3. Resources vs Tools 原則

- 讀狀態用 **Resources**（`editor/state`、`scene/cameras`、`project/info`、`custom-tools`…）。
- 改狀態用 **Tools**（`manage_scene`、`manage_gameobject`、`create_script`、`manage_editor` play/stop…）。
- 動手改引擎狀態前，先讀相關 resource。
- 開工前先看 `mcpforunity://custom-tools`，該專案可能有特殊自訂工具。

---

## 4. 標準工作流程（寫程式 → 驗證）

1. **建/改腳本**：`create_script` 或 `script_apply_edits` / `apply_text_edits`。
2. **編譯**：`refresh_unity(compile=request, mode=force, scope=all, wait_for_ready=true)`。
3. **檢查錯誤**：`read_console(types=[error])`。**有錯先修**，型別沒編出來就別急著用。
4. **確認編完**：讀 `editor/state`，看 `is_compiling=false` 且 `last_domain_reload_after` 有前進。
5. **建場景/物件**：`manage_scene`（新場景記得含 Camera + 主光源）、`manage_gameobject`（掛 component 用完整命名空間，如 `Namespace.TypeName`）。
6. **實測**：`manage_editor(play)` → 需要畫面時 `manage_camera(screenshot, capture_source=game_view, include_image=true)` → `manage_editor(stop)`。
7. 路徑相對 `Assets/`，一律用正斜線 `/`。

---

## 5. 驗證技巧

- **截圖需要場景裡有實體 Camera**。若相機是執行時才程式生成，截圖工具可能抓不到 → 在場景放一顆帶 `MainCamera` tag 的相機。
- **確定性邏輯驗證**：用 `execute_code` + 反射，直接呼叫私有方法 / 讀私有欄位來斷言（例如填滿一行再呼叫 `ClearLines`，檢查 `score`、`lines`）。這比等即時 tick 更快也更可靠。
- `execute_code` 走 CodeDom、在編輯器主執行緒執行；play 模式中會操作 play 模式物件。

---

## 6. 會浪費時間的坑（務必記住）

- **編輯器沒 focus 時，Update 可能不 tick**（畫面看起來凍結）。若要即時邏輯在背景也跑，在 `Awake()` 設 `Application.runInBackground = true;`。
- **anchor 插入位置**：`anchor_insert` 是插在**符合處之前**。要插進方法內部時，anchor 要對準方法內第一行，或用 `replace_method` 整段替換，避免把程式碼插到 class 層級造成 `CS1519`。
- **新 `.cs` 若在 Unity 關閉/斷線時寫入**，可能被當成非腳本資產快取，型別載不進 Assembly-CSharp（0 錯誤卻找不到型別）。解法：刪 `.cs` + 其 `.meta` 再重建，讓 Unity 以腳本重新匯入；用 `execute_code` 反射 `AppDomain...GetType("Ns.Type")` 驗證。
- **任一組件的編譯錯誤會擋住整個 domain reload**，導致型別一直載不進、錯誤清不掉 → 死結。暫時移除硬引用讓它編過、reload 後再還原。
- **play 模式在自動化下可能中途退出**。動作前先讀 `editor/state` 的 `play_mode.is_playing` 確認，別假設還在 play。

---

## 7. 修復：伺服器沒註冊 / 連不上

`UnityMCP` 註冊偶爾會從 `~/.claude.json` 的 `mcpServers` 消失。用 Bash 修復：

```bash
# 確認狀態
claude mcp list

# 若 No MCP servers configured，重新註冊（HTTP 傳輸、user scope）
claude mcp add --scope user --transport http UnityMCP http://127.0.0.1:8080/mcp

# 再確認：應顯示 UnityMCP ... ✔ Connected
claude mcp list
```

- `:8080` 有回應（HTTP 406 也算通）= Unity 伺服器已啟動。
- 註冊後仍需操作員 **Reload Window** 本 session 才會載入工具。

---

## 8. 一句話總結

先 `editor/state` 確認 ready → 改腳本 → `refresh_unity` 編譯 → `read_console` 查錯 → 建物件/場景 → `manage_editor` play + 截圖驗證。改東西前永遠先讀狀態，別假設。
