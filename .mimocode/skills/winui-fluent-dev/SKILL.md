---
name: winui-fluent-dev
description: Develops and reviews Windows desktop apps with .NET and WinUI 3 (Windows App SDK) following Microsoft Fluent Design guidance. Use when the user asks to create, modify, or review a WinUI/XAML interface; mentions Fluent Design, Mica, TitleBar, NavigationView, Windows App SDK, WinUI Gallery, Windows Community Toolkit, Microsoft.UI.Reactor; says WinUI开发, 做Windows应用, Fluent风格界面; or works in a csproj with UseWinUI. Covers scaffolding, XAML/C# implementation, consulting Gallery/Toolkit/Reactor samples, design review, and dotnet build. Guidance is advisory, not a hard compliance gate.
compatibility: Requires Windows 10 1809+, .NET SDK, and Windows App SDK build tools; packaged .wapproj builds need MSBuild/Visual Studio.
---

# WinUI Fluent Development

End-to-end workflow for Windows apps on .NET + WinUI 3, with Fluent Design as the UI baseline.

## Instructions

Follow steps in order. Skip Step 2 when a WinUI project already exists.

### Step 1: Detect project context

1. Look for a `*.csproj` containing `<UseWinUI>true</UseWinUI>` (ignore `bin/`, `obj/`).
2. Note TFM (e.g. `net10.0-windows10.0.26100.0`), `RootNamespace`, platforms, and whether a `*.wapproj` packaging project exists.
3. Read `App.xaml` (must merge `XamlControlsResources`) and the main window XAML before editing.
4. Report briefly: project path, TFM, packaged vs unpackaged, then continue.

### Step 2: Scaffold a new project (only when the user asks to create one)

1. Prefer templates if available:
   ```powershell
   dotnet new list | Select-String -Pattern "winui|windowsapp|uno"
   ```
   If a WinUI/Windows App SDK template exists, use it: `dotnet new <template> -n <ProjectName>`.
2. If no CLI template, hand-author a minimal project mirroring this repo's layout:
   - `<Name>/<Name>.csproj` — `OutputType=WinExe`, `UseWinUI=true`, `Nullable=enable`, PackageReferences for `Microsoft.WindowsAppSDK` and `Microsoft.Windows.SDK.BuildTools`
   - `App.xaml` / `App.xaml.cs` — merge `XamlControlsResources`; `OnLaunched` creates and activates the main window
   - `MainWindow.xaml` / `MainWindow.xaml.cs` — `MicaBackdrop` + `TitleBar` with `ExtendsContentIntoTitleBar`
   - Optional sibling `<Name> (Package)/` with `.wapproj` + `Package.appxmanifest` only if the user wants MSIX packaging
3. Namespace: PascalCase without spaces (folder `Float Hearing` → `Float_Hearing` or `FloatHearing` — match existing `RootNamespace` when extending).
4. After scaffolding, run a build (Step 5) before adding features.

### Step 3: Implement UI and logic

1. Before writing any layout XAML, read `references/fluent-design.md` for color, type, spacing, controls, and layout rules.
2. For navigation, windowing, backdrops, resources, or project-structure questions, read `references/winui-workflows.md`.
3. When choosing a control, layout pattern, or third-party helper, read `references/ui-libraries.md` — consult WinUI Gallery samples first, then Windows Community Toolkit (`CommunityToolkit.WinUI.*`), and use Microsoft.UI.Reactor only if the user opts into declarative C# UI. Do not add NuGet packages unless the user agrees.
4. Prefer built-in WinUI 3 controls over custom-drawn UI; prefer `ThemeResource`/`ThemeDictionaries` over hardcoded colors.
5. Keep code-behind thin; put reusable logic in plain C# classes when state grows beyond a page.
6. Use `x:Uid` for localized/automation-visible strings on interactive elements when adding resources.

### Step 4: Fluent design review (when asked to review, or after UI work if the user wants a check)

1. Read the checklist in `references/fluent-design.md` ("Design review checklist") and the review additions at the end of `references/ui-libraries.md`.
2. Where useful, compare non-obvious controls/layouts against WinUI Gallery (or official docs) before calling something non-Fluent.
3. Review changed or specified XAML files against the checklists.
4. Report findings as: file:line, what deviates from Fluent, suggested fix — severity High/Medium/Low.
5. This is advisory: flag issues and offer fixes; only rewrite when the user agrees (or when they asked for a fix, not just a review).

### Step 5: Build and verify

1. Build the app project (adjust name/platform to the detected project):
   ```powershell
   dotnet build "<path>/<Project>.csproj" -c Debug -p:Platform=x64
   ```
2. Expected success ends with `0 Error(s)` and a produced executable under `bin/<Platform>/Debug/<tfm>/`.
3. If a `.wapproj` must be built, use MSBuild (not `dotnet build`):
   ```powershell
   msbuild "<path>/<Package>.wapproj" /p:Configuration=Debug /p:Platform=x64
   ```
4. On failure: fix compile errors first (missing xmlns, wrong x:Class, platform mismatch). See Troubleshooting.
5. Do not claim the UI looks correct from a green build alone — say explicitly that visual verification needs running the app.

## Examples

**User: "帮我给这个 WinUI 项目加一个设置页"**
→ Detect project → check ui-libraries.md (WinUI Gallery SettingsCard/NavigationView pattern) → add `SettingsPage.xaml` + navigate → build with `dotnet build` → summarize changes and how to open the page.

**User: "审查一下 MainWindow 的 Fluent 合规性"**
→ Read MainWindow.xaml + design review checklist (+ ui-libraries review additions) → list issues with file:line and suggested XAML fixes → ask before applying.

**User: "新建一个 WinUI 3 项目，要用 Fluent 风格"**
→ Step 2 scaffold (template or hand-authored minimal set) → ensure Mica + TitleBar + XamlControlsResources → build → show folder layout.

**User: "用 Reactor 写个界面" / "加 CommunityToolkit 的 DataGrid"**
→ Read ui-libraries.md → Reactor: opt-in scaffold with `-p:Platform=...`; Toolkit: recommend matching `CommunityToolkit.WinUI.*` package and add only after user OK.

## Troubleshooting

| Error / symptom | Cause | Fix |
|---|---|---|
| `MSB3644` / platform target mismatch | csproj `Platforms` vs `-p:Platform` disagree | Pass matching `-p:Platform=x64` (or x86/ARM64); restore first if needed |
| `dotnet build` fails on `.wapproj` | Packaging project needs MSBuild | Build the `.csproj` with dotnet; use `msbuild` only for `.wapproj` |
| XAML: `The type 'X' was not found` | Missing xmlns or WinUI package version | Add proper `xmlns`; verify `Microsoft.WindowsAppSDK` PackageReference |
| `x:Class` mismatch between XAML and .cs | Renamed namespace/class | Align `x:Class`, `namespace`, and partial class names |
| Colors wrong in dark/light theme | Hardcoded `#RRGGBB` | Switch to `ThemeResource` brushes (see fluent-design.md) |
| Mica/backdrop not visible | Window not activated, or unsupported path | Ensure `SystemBackdrop` set before `Activate()`; verify OS supports Mica |
| Title bar buttons overlap content | Missing `ExtendsContentIntoTitleBar` / `SetTitleBar` | Set both; give TitleBar a defined height; see winui-workflows.md |

## Important

- Always match the existing project's TFM, nullable setting, and namespace conventions when extending.
- Never replace `XamlControlsResources` — WinUI controls lose theming without it.
- Treat Fluent rules as strong defaults: when the user explicitly wants a deliberate deviation, follow the user and note the tradeoff once.
- Prefer WinUI Gallery / official samples over random blog posts when copying UI patterns.
- Never convert a XAML project to Reactor (or pull in Toolkit packages) without an explicit user request.
