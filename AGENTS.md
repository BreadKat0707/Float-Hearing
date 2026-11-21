# AGENTS.md — FH Reborn (Windows / Float Hearing)

> 本文件是**跨 AI 工具**的仓库级规则。项目级权威规则见 MiMoCode `MEMORY.md`（`## Rules`）。  
> 两者互补：本文件写「所有 agent 都要遵守的约定」；MEMORY 补「本机/会话沉淀的踩坑细节」。不要同一条规则两边重复维护。

## 0. 项目状态（必读）

| 项 | 状态 |
|----|------|
| 产品名 | **FH Reborn**（Float Hearing Reborn）— 本地音乐播放器 Windows 端 |
| 本仓库 | **新开仓库**，未来**取代**旧项目 `D:\Project\FloatHearingWindows` |
| 当前代码 | **脚手架阶段**：仅 WinUI 3 空白应用 + MSIX 打包工程 + PRD |
| 需求来源 | 本仓 `FH_Reborn_Windows_PRD.md`（平台差异）+ `FH_Reborn_Android_PRD.md`（通用功能定义，权威功能清单） |
| 规格摘要 | 本仓 `SPEC.md`（从 PRD 抽出的可执行规格；与 PRD 冲突时以 PRD 修订 + 用户确认为准） |

**不要**把旧仓库路径、旧文件名或「功能已实现」当成当前仓库事实。旧项目仅作经验参考；未在本仓出现的代码一律视为**未实现**。

## 1. 产品与技术栈

- **定位**：面向本地音乐收藏的高品质播放器；三平台独立原生实现（Android 主力 / Linux / **Windows = 本仓**）。
- **设计**：原生 **Fluent Design**（Mica / Acrylic / Reveal）；视觉优先 + 沉浸式播放 + 专业本地功能。
- **技术栈（Windows）**：
  - 语言 / UI：C# · **WinUI 3** · Windows App SDK · .NET（当前 TFM：`net10.0-windows10.0.26100.0`）
  - 音频：`Windows.Media.Playback.MediaPlayer` 优先，不支持时回退 **NAudio**（必要时 FFmpeg 解码）
  - 数据：**SQLite + EF Core**（schema 演进见 §3）
  - 元数据：**TagLib#**；扫描：`System.IO` + 可选 FFmpeg 补充（APE/DSD 等）
  - 打包：开发/CI **MSIX**；用户分发可评估 **Inno Setup / 便携 EXE**
  - 项目 skill：`.mimocode/skills/winui-fluent-dev/`（WinUI + Fluent 开发流程）

## 2. 文档优先级

1. **用户当场指令**（最高）
2. **PRD 已确认决策**（Android PRD 中「已确认」块 + Windows PRD 差异表）
3. **`SPEC.md`**（本仓浓缩规格；过期时改 SPEC 或回写 PRD）
4. 旧仓库经验 / 记忆（**只作参考**，须在本仓验证后才能当事实）

PRD 状态为「需求收集中」；`SPEC.md` §开放问题 中的未决项 **禁止当成已定需求擅自实现**，先向用户确认。

## 3. 编码与工程约定

### 3.1 通用

- UI 文案与代码注释：**中文**。
- 全项目：`Nullable` + `ImplicitUsings` 开启；文件级命名空间。
- XAML：优先 **`x:Bind`**；不变化属性用 `Mode=OneTime`。
- MVVM：**手写** `INotifyPropertyChanged`；**不引入** CommunityToolkit.Mvvm、**不引入** DI 容器（服务用 `App.*` 静态入口，待有服务层后沿用）。
- 选择器 HWND：统一 `App.MainWindow`（有窗口后）。
- 不改系统音量；播放音量若需要只用软件增益。
- 歌词与播放**纯本地**：不联网拉歌词/封面（内嵌 > 同目录文件 > 内置目录）。
- 不要提交：`bin/`、`obj/`、`.vs/`、崩溃转储、`WindowsAppRuntimeInstall.exe`、安装包产物。

### 3.2 数据库（引入 EF Core 后强制）

- **不用** EF Migrations（WinUI 下 `dotnet ef` 不可靠）：`EnsureCreatedAsync()` + 手写 `PRAGMA table_info` / `EnsureColumnAsync` 做 `ALTER TABLE`。
- **任何实体字段变更**必须同步列迁移，否则旧库缺列。
- 扫描写库必须**全局串行**（唯一索引：歌曲路径等；多入口并发插入会 `DbUpdateException`）。

### 3.3 构建与运行

```powershell
# 还原 + 构建（平台与 csproj Platforms 对齐）
dotnet build "Float Hearing.slnx" -c Debug -p:Platform=x64

# 运行应用工程（打包身份按当前 MSIX 配置）
dotnet run --project "Float Hearing/Float Hearing/Float Hearing.csproj" -p:Platform=x64

# 仅打包工程用 MSBuild
msbuild "Float Hearing/Float Hearing (Package)/Float Hearing (Package).wapproj" /p:Configuration=Debug /p:Platform=x64
```

- 构建成功标准：`0 Error(s)`；**绿构建 ≠ UI 正确**，视觉问题需真机运行确认。
- 改 `Package.appxmanifest` / `csproj` 前先重读文件（用户可能中途改过）。

### 3.4 UI / Fluent

- 布局与控件先对照 **WinUI Gallery** 与 skill 的 `references/fluent-design.md`（参考性规范，非硬门禁；用户可要求偏离）。
- 材质优先原生：`MicaBackdrop` / `DesktopAcrylicBackdrop` / Reveal；主题色用 `ThemeResource`，禁止硬编码主题色。
- 第三方 UI 包默认**不加**；需要时用 `CommunityToolkit.WinUI.*` 并先征得用户同意。
- **Reactor**（`Microsoft.UI.Reactor`）仅用户明确要求时使用，不把 XAML 工程静默改成 Reactor。

### 3.5 第三方源码（引入 Lyricify 等时）

- 从上游**拷贝源码**进本仓（如 `Lyrics/`、`Backgrounds/`），命名空间改为 `FloatHearing.*`；**禁止** `ProjectReference` 指到本机绝对路径（CI/可移植性）。
- 拷贝后删掉 netstandard 专用的 `IsExternalInit` polyfill，避免 net10 `CS0729` + XAML 编译级联错误。
- vendored 目录改动保持最小，勿大面积重构命名空间。

## 4. 需求变更流程

1. 用户提出变更 → 更新 **PRD**（或标注「待写入 PRD」）→ 同步改 **`SPEC.md`** 对应章节。
2. 已勾选的「已确认决策」若被推翻，必须在 PRD 更新记录里写明，禁止只改代码不改文档。
3. 实现前扫一眼 `SPEC.md` 的 MoSCoW 与阶段表，避免跳阶段做 Could/后续项。

## 5. 分支与提交（若启用 git 工作流）

- 本机部分环境 `git` 可能不可用；有 git 时遵循仓库既有提交信息风格（当前历史为中文短句）。
- 不 force push `master`；不提交密钥、`.pfx` 私钥（若存在临时证书仅本地使用）。

## 6. 快速索引

| 路径 | 说明 |
|------|------|
| `FH_Reborn_Windows_PRD.md` | Windows 平台 PRD（差异与技术方案） |
| `FH_Reborn_Android_PRD.md` | 通用功能与已确认决策的主 PRD |
| `SPEC.md` | 可执行规格摘要（实现时优先扫这个） |
| `Float Hearing/` | 应用 + MSIX 打包工程 |
| `.mimocode/skills/winui-fluent-dev/` | WinUI Fluent 开发 skill |

## 7. 与旧项目的关系

- 旧仓：`D:\Project\FloatHearingWindows`（功能更完整的上一代实现）。
- 本仓目标：**取代**旧仓；迁移经验（播放回退链、托盘、SMTC、扫描串行、列迁移等）写入 SPEC/MEMORY 作为**目标架构**，但代码以本仓为准按阶段重建。
- 不要把旧仓未拷贝的文件路径写进本仓构建脚本。
