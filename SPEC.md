# SPEC — FH Reborn Windows（可执行规格）

> 版本：从 PRD v1.1（2026-06-07）浓缩 | 状态：随实现更新  
> **冲突规则**：用户指令 > PRD 已确认决策 > 本文档。本文档过期时改这里或回写 PRD。  
> 通用功能定义以 `FH_Reborn_Android_PRD.md` 为准；平台差异以 `FH_Reborn_Windows_PRD.md` 为准。

---

## 1. 一句话与成功标准

**本地音乐播放器 Windows 端**：扫描用户目录 → 流畅播放 → 歌词/歌单/统计 → Fluent 沉浸 UI。

MVP 成功（阶段一）：用户可选目录、列表可见、可播放、LRC 歌词同步、基础主题与导航可用，构建无错误。

---

## 2. 范围

### 2.1 In scope（Windows）

- 媒体库：指定目录扫描、隐藏/黑名单、目录浏览、模糊搜索  
- 播放：控制、队列、四模式、真随机、后台 + SMTC/托盘  
- UI：深/浅/跟随系统、Mica/Acrylic/Reveal、流体背景与律动（阶段二）  
- 歌词：LRC / TTML、内嵌优先、迷你 + 全屏、字体可调；**无**桌面悬浮歌词、**无**状态栏歌词  
- 歌单：CRUD、收藏系统歌单、三种移除、导出 M3U/JSON（阶段二+）  
- 数据：进度/队列/统计/灵感笔记  
- 系统：睡眠定时、崩溃页、多语言（简/繁/英/日/韩）、窗口缩放  

### 2.2 Out of scope / Won't（Windows）

| 项 | 说明 |
|----|------|
| 桌面悬浮歌词 L-010 | Won't Have（窗口化应用，PRD 明确） |
| 状态栏歌词 L-011 | Won't Have |
| 联网歌词/在线功能 | 不做；歌词纯本地 |
| 歌词编辑器 | Won't |
| DLNA/投屏 | 不做 |
| Android 专属 | MediaStore、通知栏歌词、悬浮窗、预测性返回、底部倒置单手布局等 |

---

## 3. 技术架构（目标）

```
Float Hearing (应用 WinUI 3)
├── App.xaml(.cs)          # 资源、服务入口、DB 迁移、单实例
├── MainPage / 导航        # NavigationView/Frame
├── Pages/ ViewModels/     # 手写 MVVM + x:Bind
├── Services/              # 扫描、播放、设置、托盘、SMTC、崩溃
├── Data/                  # EF Core + SQLite 实体与 EnsureColumn 迁移
├── Lyrics/ (vendored)     # 可选：本地歌词解析（拷贝源码，FloatHearing.* 命名空间）
└── Backgrounds/ (vendored)# 可选：流体/律动背景
Float Hearing (Package)    # MSIX
```

| 层 | 选型 | 备注 |
|----|------|------|
| 播放 | MediaPlayer → 失败则 NAudio | SMTC 在 PlaybackService |
| 可视化 PCM | NAudio SampleProvider 等 | **开放问题**，先原型 |
| 扫描 | System.IO + Parallel + 全局串行写库 | 无 MediaStore |
| 后台 | 托盘最小化 + SMTC | WinUI 扩展托盘 |
| 模糊 | 原生 Mica/Acrylic | 流体背景可用 Composition/Win2D（开放） |
| 最低系统 | Win10 1809+；完整材质建议 Win11 | |
| 部署 | 开发 Self-Contained/MSIX；发行再评估 | 避免 Runtime 地狱 |

**阶段划分（与 PRD 一致）**

| 阶段 | 目标 | 要点 |
|------|------|------|
| 一 MVP | 可播放媒体库 | 扫描 + 基础播放 + 基础 UI + LRC |
| 二 增强 | 体验 | FFmpeg 补扫、TTML、流体背景、律动、歌单完善 |
| 三 完善 | 完整 | 持久化完善、导出、灵感、主题/材质细化 |

---

## 4. 功能规格（按模块）

ID 与 Android PRD 对齐；**Windows 行为**为实现依据。

### 4.1 媒体库（F-001~006, M-001）

| ID | 要求 | 优先级 | Windows |
|----|------|--------|---------|
| F-001 | 快速扫描已知格式 | Must | `FolderPicker` 选目录 + `System.IO` 遍历（非全盘） |
| F-002 | 补充扫描 APE/DSD 等 | Must(阶段二) | TagLib# 主流；FFmpeg 补 |
| F-003 | 隐藏/黑名单 | Must | 用户黑名单路径；无 .nomedia 概念则映射为忽略规则 |
| F-004 | 目录浏览 | Must | 文件系统树，不依赖库索引 |
| F-005 | 模糊搜索 | Must | 标题/艺术家/专辑 |
| F-006 | 拼音首字母 | Should | 中文检索 |
| M-001 | 启动增量更新 | Must | 启动扫变更；`FileSystemWatcher` 可选辅助 |

**已确认（通用）**：只扫用户指定目录；启动自动更新 Must。  
**封面缓存**：PRD 未最终选定 → 实现前问用户（倾向磁盘缩略图缓存）。

**格式**：MP3、FLAC、AAC、OGG、WAV、M4A、OPUS、APE、DSF/DFF。

### 4.2 播放器（F-007~014）

| ID | 要求 | 优先级 | Windows |
|----|------|--------|---------|
| F-007 | 播/停/上下首/Seek | Must | 进度条点击即跳转；长按上下首 = 快进快退 ±5s/s |
| F-008 | 队列 | Must | 与播放页一体；拖拽排序、删项、插到下一首 |
| F-009 | 后台播放 | Must | 关窗可托盘后台 |
| F-010 | 媒体控制 | Must | **SMTC** + 托盘菜单 |
| F-011 | 音频焦点 | Must | 无系统焦点 → 监听 SMTC Pause/系统事件，设置可配 |
| F-012 | 真随机 | Must | 10 首内不重复；关随机还原顺序 |
| F-013 | 四模式 | Must | 顺序 / 列表循环 / 单曲 / 随机，独立切换 |
| F-014 | 队列持久化 | Should | 重启恢复 |

**已确认交互**：耳机键在 Windows 映射为 SMTC/媒体键；进度每 5s 落库（有 DB 后）。

### 4.3 UI（F-015~025）

| ID | 要求 | 优先级 | Windows |
|----|------|--------|---------|
| F-015 | 深/浅/系统 | Must | WinUI 主题 |
| F-016 | 窗口适配 | Must | 缩放；非手机 insets 布局 |
| F-017 | 流体动态背景 | Should | 封面取色 + 动态；Win2D/Composition（开放） |
| F-018 | 律动可视化 | Should | PCM/FFT；三样式可开关（阶段二） |
| F-019/020 | 自定义背景图 | Should | 播放页 / 其他页 |
| F-021 | 前景色调整 | Should | 与背景对比度 |
| F-022 | 手势 | Should | 桌面以指针/快捷键为主，非 Android 滑动手势照搬 |
| F-023 | 简洁模式 | Should | 菜单手动；仅封面+信息+进度 |
| F-024 | 过渡动画 | Could | |
| F-025 | 启动画面 | Could | |

**设计**：Fluent 原生 Mica/Acrylic/Reveal；全局圆角对齐 Fluent/G2（实现时用系统资源，不硬编码魔数）。

### 4.4 歌词（F-026~034, L-*）

| 要求 | 优先级 | Windows |
|------|--------|---------|
| LRC 三种精度 + TTML | Must / TTML Should→阶段二 | 解析纯本地 |
| 内嵌 > 同目录 > 内置目录 | Must | **禁止联网** |
| 迷你双行：有译=原文+译；无译=原文+下句；无词=**Nothing Here / 静享时刻** | Must | |
| 全屏滚动、点击跳转、长按复制 | Must/Should | |
| 字体 8–48、粗细 100–900、行距字距 | Should | 系统字体 |
| 翻译 + 日韩罗马音 | Should | 外挂数据文件，非内置强制下载 |
| 弹簧逐字高亮 | Should | |
| 桌面歌词 / 状态栏歌词 | **Won't** | 不做 |

歌词匹配时机：**播放时**加载，不预扫描。

### 4.5 歌单（F-035~040）

- CRUD、标签、默认播放模式；「我喜欢的歌曲」系统歌单不可删。  
- 三种移除：歌单 / 媒体库 / 文件（删文件二次确认）。  
- 批量与筛选添加 Should；M3U+JSON 导出 Should（阶段二+）。  
- 封面：自动拼图 / 选曲封面 / 自选图。

### 4.6 文件与元数据（F-041~046）

属性面板 Must；完整标签、复制信息、保存封面、批量选择 Should。

### 4.7 持久化（F-047~053）

进度记忆、队列恢复、收藏 Must；播放记录/统计/灵感 Should；备份 Could。  
表设计参考 Android PRD §4.7（Song、PlaybackState、PlaybackStats、InspirationNote、HiddenFolder、Playlist…）。

### 4.8 系统与本地化

- 睡眠定时、分享文件、崩溃页、空状态 Must/Should（权限引导在 Windows 改为文件夹访问说明）。  
- 多语言简/繁/英/日/韩 Should。  
- 音频输出：设备选择/模式/均衡器为 Should、阶段靠后；应用内不单独造一套系统音量。

### 4.9 视觉特效

- Reveal 按压光效 Should（WinUI 原生）。  
- 文字/图标 Plus Lighter 等发光 Could。  
- 与流体背景叠加为视觉卖点（阶段二）。

---

## 5. MoSCoW（Windows 实现序）

**Must（MVP）**：F-001/003/004/005、F-007~013、F-015/016、F-026/028/029/030/031、F-035/036、F-041/045、F-047/048/051、F-057/059（及平台等价的 F-010 SMTC、F-011 应用层焦点）。  

**Should（阶段二）**：拼音、流体、律动、TTML、歌词字体、导出、统计、灵感、睡眠、多语言、Reveal 强化、队列持久化等。  

**Could（阶段三/评估）**：智能歌单、备份、发光、过渡打磨。  

**Won't**：桌面歌词、状态栏歌词、歌词编辑器、联网歌词。

---

## 6. 验收摘录（MVP）

- 选目录后列表出现支持格式歌曲；重启可增量更新。  
- 点击播放；SMTC/托盘可暂停切歌；关窗后台可播。  
- 同目录 `.lrc` 同步误差 < 200ms；三种 LRC 时间格式可解析。  
- 深浅主题切换无硬编码色块异常。  
- `dotnet build` 0 Error。

---

## 7. 开放问题（实现前需用户确认）

- [ ] 音频栈：MediaPlayer vs NAudio 主路径是否维持「优先 MediaPlayer」  
- [ ] 可视化 PCM 获取方案原型  
- [ ] 流体背景：Win2D / Composition 具体方案  
- [ ] 托盘 + SMTC 细节（关闭行为：退出还是隐藏到托盘）  
- [ ] 全局快捷键范围  
- [ ] 发行包：MSIX / 便携 EXE / Self-Contained 组合  
- [ ] 封面缓存策略 A/B/C  
- [ ] 排序方式完整定义  
- [ ] 与旧仓数据是否迁移（包 Identity 变更会导致 LocalState 重置）  
- [ ] PRD 自 2026-06-07 后是否有未写入的需求变更  

---

## 8. 实现检查清单（每个功能 PR/提交前）

- [ ] 对应 SPEC/PRD ID 与优先级  
- [ ] 构建 `-p:Platform=x64`（或目标平台）0 Error  
- [ ] 涉及 DB：列迁移已加  
- [ ] 涉及 UI：主题资源非硬编码色；中文文案  
- [ ] 未引入未批准的 NuGet / DI / Reactor  
- [ ] 需求若有变：PRD 或 SPEC 已更新  
