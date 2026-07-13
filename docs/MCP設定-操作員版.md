# MCP for Unity 安裝與啟用說明書（操作員版）

給「人」看的一步步操作。目標：讓 Claude Code 能透過 MCP 直接控制 Unity 編輯器。
本文件依這台機器實際跑通的流程撰寫（Unity 6 + HTTP 傳輸，port 8080）。

---

## 0. 事前準備（Prerequisites）

| 需要的東西 | 說明 / 檢查方式 |
|---|---|
| **Unity 6**（6000.0.x） | 用 Unity Hub 安裝，開一個專案（2D Core 範本即可） |
| **Python 3.10+** | 終端機輸入 `python --version` |
| **uv / uvx** | 套件的伺服器用它啟動。本機在 `C:\Users\micha\.local\bin\uvx.exe`。檢查：`uvx --version` |
| **Claude Code CLI** | 檢查：`claude --version`。本機在 `C:\Users\micha\AppData\Roaming\npm\claude.cmd` |
| **VS Code + Claude 擴充** | 若你透過 VS Code 內的 Claude 面板操作 |

> 缺 Python / uv 時，下一步的「設定精靈」會引導你安裝，不用先手動裝。

---

## 1. 安裝套件到 Unity 專案

1. 開啟 Unity，打開你的專案。
2. 上方選單 **Window ▸ Package Manager**。
3. 左上角 **＋** ▸ **Add package from git URL…**。
4. 貼上這個網址後按 **Add**：

   ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
   ```

5. 等它下載匯入完成（下方進度條跑完）。

---

## 2. 跑設定精靈（第一次會自動跳出）

- 匯入後會自動開啟 **MCP for Unity** 設定精靈。
- 它會檢查 **Python** 和 **uv**；缺哪個就照它指示安裝，裝完按 **Done**。
- 之後可從 **Window ▸ MCP for Unity** 再打開這個視窗。

---

## 3. 取得伺服器啟動指令

打開 **Window ▸ MCP for Unity**，視窗裡會顯示一段啟動指令，長得像這樣（本機實際範例）：

```
C:\Users\micha\.local\bin\uvx.exe --from "mcpforunityserver==10.0.0" mcp-for-unity ^
  --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools
```

重點：這是 **HTTP 傳輸**，伺服器端點是 **`http://127.0.0.1:8080/mcp`**（base + `/mcp`）。
只要 Unity 有開、這個視窗顯示綠色 **Session Active / Connected**，port 8080 就會在聽。

> 視窗裡通常也有一排 AI 客戶端清單（Claude Code / Cursor…）可按 **Configure** 自動寫入設定。
> 但本機自動寫入沒生效，所以下一步改用指令手動註冊（最可靠）。

---

## 4. 把伺服器註冊給 Claude Code（手動、最可靠）

打開終端機（PowerShell 或 Git Bash），執行：

```bash
claude mcp add --scope user --transport http UnityMCP http://127.0.0.1:8080/mcp
```

驗證是否連上：

```bash
claude mcp list
```

看到這行就對了：

```
UnityMCP: http://127.0.0.1:8080/mcp (HTTP) - ✔ Connected
```

> 名稱一定要用 **`UnityMCP`**，因為工具會以 `mcp__UnityMCP__*` 出現，名稱要對得上。

---

## 5. 讓 Claude 這個 session 載入工具 ⚠️ 關鍵步驟

即使上面顯示 Connected，**正在進行中的 Claude Code 對話不會自動吃到新工具**。必須重載：

- VS Code：按 **Ctrl + Shift + P** ▸ 輸入並選 **Developer: Reload Window**。
- 或直接重開 Claude Code。

重載後，Claude 才會出現 `mcp__UnityMCP__manage_scene`、`read_console`、`manage_gameobject` 等工具。

---

## 6. 開始用

保持以下狀態，Claude 就能操控 Unity：

- ✅ Unity 編輯器**開著**、載入你的專案
- ✅ **Window ▸ MCP for Unity** 顯示綠色 **Session Active**（= port 8080 在跑）

之後只要跟 Claude 說要做什麼（例如「在 Unity 建一個場景」），它就會直接動手。

---

## 疑難排解（Troubleshooting）

| 症狀 | 原因 / 解法 |
|---|---|
| `claude mcp list` 顯示 **No MCP servers configured** | 註冊掉了。重跑第 4 步的 `claude mcp add …`，再 Reload Window。 |
| Claude 說找不到 `mcp__UnityMCP__` 工具 | 沒重載。做第 5 步 **Reload Window**。 |
| 工具回 **No Unity Editor instances found** | Unity 沒開，或 MCP 視窗沒 Session Active。開 Unity、確認綠燈。 |
| 連 8080 失敗 / `curl 127.0.0.1:8080` 不通 | Unity 沒開或伺服器沒啟。開 Unity ▸ MCP for Unity 視窗即會啟動。 |
| **Window 選單找不到 MCP for Unity** | 套件沒裝成功。重做第 1 步，確認匯入無錯。 |
| 改了 MCP 設定後沒反應 | 一律 **Reload Window** 才生效。 |
| 多個 Unity 專案同時開著 | Claude 需指定實例（見 Claude 版說明），或只開一個專案最單純。 |

---

## 快速檢查清單（貼在牆上版）

1. Unity 開著 + MCP for Unity 綠燈
2. `claude mcp list` → `✔ Connected`
3. VS Code 已 Reload Window
4. 完成 ✅
