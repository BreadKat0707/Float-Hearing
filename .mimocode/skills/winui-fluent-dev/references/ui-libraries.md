# Windows UI libraries and sample sources

When implementing or reviewing UI, prefer looking up established patterns here before inventing custom chrome. Use as reference — do not add dependencies unless the user asks.

## Decision order

1. **WinUI built-in control** fits → use it (see fluent-design.md §5).
2. Need a **working XAML sample** for that control/layout → WinUI Gallery source.
3. Need a **helper, extension, or higher-level control** beyond WinUI → Windows Community Toolkit (`CommunityToolkit.WinUI.*`).
4. User wants **declarative C# (React/SwiftUI-style), no XAML** → Microsoft.UI.Reactor (experimental).
5. Still missing → search official docs/samples; only then hand-roll.

---

## WinUI Gallery (official)

| | |
|--|--|
| Repo | `https://github.com/microsoft/WinUI-Gallery` |
| Store | apps.microsoft.com detail `9P3JFPWWDZRC` |
| Role | Interactive samples for every WinUI 3 control + Fluent Design / accessibility guidance |

**Use when:**
- Choosing the right control or layout pattern
- Need copy-paste XAML + code-behind for NavigationView, ListView, TitleBar, InfoBar, etc.
- Checking adaptive UI / VisualStateManager patterns
- Reviewing whether a Fluent treatment matches Microsoft's own samples

**How to use:**
1. Clone shallow if needed: `git clone --depth 1 https://github.com/microsoft/WinUI-Gallery.git`
2. Browse `WinUIGallery/` pages by control name (each sample page shows markup and code-behind).
3. Prefer their structure over blog posts: it tracks current Windows App SDK / WinUI APIs.
4. Design + accessibility sections in the app map to Fluent guidance — cite them in review notes when useful.

**Note:** Gallery may pin experimental Windows App SDK features; when copying, verify the API exists in this project's Windows App SDK version.

---

## Windows Community Toolkit

| | |
|--|--|
| Current repo | `https://github.com/CommunityToolkit/Windows` (old `WindowsCommunityToolkit` repo is archived) |
| Docs | `https://aka.ms/windowstoolkitdocs` |
| Gallery app | Store: Windows Community Toolkit Gallery (`aka.ms/windowstoolkitapp`) |
| WinUI 3 packages | `CommunityToolkit.WinUI.*` (e.g. `CommunityToolkit.WinUI.UI.Controls.*`, `CommunityToolkit.WinUI.Helpers`, `CommunityToolkit.WinUI.Converters`) |
| UWP/WinUI2 packages | `CommunityToolkit.Uwp.*` — **do not** use in WinUI 3 projects |

**Use when:**
- Need controls/helpers WinUI does not ship: e.g. DataGrid (older), DropShadowPanel, Markdown parsers, wrap panels, event triggers/behaviors, connected-animation helpers, string converters, image exif, etc.
- MVVM plumbing: prefer **.NET Community Toolkit** (`CommunityToolkit.Mvvm`) for `[ObservableProperty]` / `[RelayCommand]` — separate from the Windows toolkit.

**How to add (only if the user wants the dependency):**
```powershell
dotnet add package CommunityToolkit.WinUI.<Component>
```
- Check compatibility with the project's TFM / Windows App SDK before recommending a version.
- Prefer a single focused package over the whole umbrella if one exists.

**Principle:** Toolkit marks items deprecated once the Windows SDK/WinUI ships an equivalent — prefer the built-in when both exist.

---

## Microsoft.UI.Reactor ("Reactor")

| | |
|--|--|
| Repo | `https://github.com/microsoft/microsoft-ui-reactor` |
| NuGet | `Microsoft.UI.Reactor` (public preview / experimental) |
| Role | Declarative, component-based **C#** layer on real WinUI 3 controls (hooks, DSL, flex layout, etc.) — not a new UI platform |
| Status | Experimental (pre-1.0; API may change) |

**Use when:**
- The user explicitly asks for Reactor, MVU/hooks, or "no XAML" C# UI.
- Comparing programming models or migrating a sample from React/SwiftUI mental models.

**Scaffold (only when the user opts into Reactor):**
```powershell
# Preferred: Windows App SDK CLI
winget install Microsoft.WinAppCli
winapp new -t reactor -n MyApp
# Or, after installing the template pack:
dotnet new reactor -n MyApp
```

**Rules:**
- Do **not** silently convert an existing XAML project to Reactor — XAML + MVVM remains the default for this skill.
- Always pass an explicit platform: `dotnet build -p:Platform=x64` (Reactor templates error without it).
- Host interop is possible (Reactor inside XAML or the reverse); keep the dominant model of the repo unless the user directs otherwise.
- Flag experimental status and possible breaking changes when recommending it.

---

## Other official references

| Resource | URL / id | Use for |
|----------|----------|---------|
| Fluent Design (docs) | `https://learn.microsoft.com/windows/apps/design/` | Design language, color, type, layout, motion, accessibility |
| WinUI docs | `https://learn.microsoft.com/windows/apps/winui/` | Control APIs, XAML overview |
| Windows App SDK samples | `https://github.com/microsoft/WindowsAppSDK-Samples` | Windowing, packaging, input, advanced scenarios |
| .NET Community Toolkit | `CommunityToolkit.Mvvm` | MVVM source generators (not UI controls) |
| Windows App SDK templates | `Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` | `dotnet new` templates including `reactor*` |

---

## Review checklist additions

When reviewing UI work, also check:

- [ ] Sample was compared to WinUI Gallery (or docs) for the same control when non-obvious
- [ ] No unnecessary NuGet UI package where WinUI already provides the control
- [ ] If `CommunityToolkit.WinUI.*` is used: correct WinUI 3 package family, not `.Uwp.*`
- [ ] If Reactor is in the project: user opted in; build uses `-p:Platform=...`; mixed XAML/Reactor boundary documented
- [ ] Custom controls justified (Gallery has no equivalent, or brand requirement)

## Examples

**User: "做一个带导航的设置页"** → Open/clone WinUI Gallery → copy NavigationView + SettingsCard patterns → adapt ThemeResources → build.

**User: "需要 DataGrid / 转换器 / 行为"** → Check WinUI Gallery first → if absent, suggest matching `CommunityToolkit.WinUI.*` package → add only with user OK.

**User: "用 Reactor 写个计数器"** → Scaffold `winapp new -t reactor` or document `dotnet new reactor` → Component + UseState sample → build with `-p:Platform=x64`.
