# WinUI 3 workflows and project patterns

Practical project structure, windowing, navigation, and build notes for .NET + WinUI 3 (Windows App SDK).

## Project layout (typical)

```
Solution.slnx (or .sln)
├── MyApp/                          # app (csproj, UseWinUI=true)
│   ├── MyApp.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml(.cs)        # or a shell Page
│   ├── Views/ or Pages/            # Page XAML pairs
│   ├── Assets/ or AppIcon.ico
│   └── app.manifest
└── MyApp (Package)/                # optional MSIX packaging
    ├── MyApp (Package).wapproj
    ├── Package.appxmanifest
    └── Images/                     # logos, splash
```

- App project: `OutputType=WinExe`, `UseWinUI=true`, `Nullable` as chosen by repo.
- TFM example: `net10.0-windows10.0.26100.0` with `TargetPlatformMinVersion` e.g. `10.0.17763.0`.
- `Platforms` / `RuntimeIdentifiers`: keep x86, x64, ARM64 in sync with what you pass to build.
- Packaging is optional: unpackaged apps still run for local dev if Windows App SDK supports it.

## csproj essentials

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
  <UseWinUI>true</UseWinUI>
  <Nullable>enable</Nullable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="*" />
  <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="*" />
</ItemGroup>
```

- Prefer pinned versions over `*` when the repo already pins; match existing style when editing.
- `WinUISDKReferences` / WebView2 flags: leave as-is unless the user asks to change them.

## App.xaml resources

Always keep:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

- App-level custom brushes/styles go in this dictionary or a merged `Styles.xaml`.
- Page-local resources: `<Page.Resources>` — only page-specific items.

## Window, title bar, backdrop

```csharp
public MainWindow()
{
    InitializeComponent();
    ExtendsContentIntoTitleBar = true;
    SetTitleBar(AppTitleBar);
    AppWindow.SetIcon("AppIcon.ico");
}
```

```xml
<Window.SystemBackdrop>
  <MicaBackdrop />
</Window.SystemBackdrop>
```

- Set backdrop and title bar before `Activate()`.
- Custom `TitleBar` element needs a sensible `Height`/padding; reserve space so content doesn't sit under min/max/close.
- Full custom chrome: `AppWindow.TitleBar.ExtendsContentIntoTitleBar = true` + `PreferredHeightOption` — only when required.

## Navigation pattern

1. Shell page hosts `NavigationView` + `Frame` (or just a root `Frame`).
2. Navigate by type:
   ```csharp
   ContentFrame.Navigate(typeof(SettingsPage));
   ```
3. Handle `NavigationCacheMode` / back stack when the user expects Back to work (`Frame.CanGoBack`, `GoBack`).
4. One `Page` per view; keep XAML + code-behind as a pair (`SettingsPage.xaml` / `.cs`).

## Build commands

**App project (dotnet):**
```powershell
dotnet restore "<path>/MyApp.csproj"
dotnet build "<path>/MyApp.csproj" -c Debug -p:Platform=x64
```

**Packaged (.wapproj) — MSBuild only:**
```powershell
msbuild "<path>/MyApp (Package).wapproj" /p:Configuration=Debug /p:Platform=x64
```

- Always align `-p:Platform` / `/p:Platform` with `Platforms` in the csproj.
- Solution-level: `dotnet build <slnx>` when only csproj projects are in play; mixed wapproj → MSBuild on the solution.
- Success criterion: `0 Error(s)`; binaries under `bin/<Platform>/<Configuration>/<tfm>/`.

## Debugging tips

- Run the app project (not the packaging project) for fastest inner loop when unpackaged works.
- XAML hot reload issues: restart app after `x:Class`/xmlns changes if hot reload doesn't pick them up.
- If controls look unstyled: confirm `XamlControlsResources` still merged in `App.xaml`.
- Platform errors (`R2R`/architecture): rebuild matching platform after switching x64↔ARM64.

## Adding dependencies (UI libraries)

- Check `ui-libraries.md` first: Gallery for samples, `CommunityToolkit.WinUI.*` for helpers, Reactor only on explicit request.
- After any PackageReference change: `dotnet restore` then rebuild with matching `-p:Platform`.
- Avoid mixing UWP (`CommunityToolkit.Uwp.*`) packages into a WinUI 3 app.

## Common file operations

| Task | Do this |
|------|---------|
| New page | Add `Pages/Name.xaml` + `.cs` pair; match `x:Class` to namespace; navigate via `Frame` |
| New dialog | `ContentDialog` in code or XAML; one primary Button |
| Shared styles | `Styles.xaml` merged in `App.xaml` |
| Icons | WinUI `SymbolIcon` / `FontIcon` with Segoe Fluent Icons; or packaged `Assets` |
| Strings for UI | Resources + `x:Uid` where practical |

## When to use which backdrop / surface

| Surface | Material |
|---------|----------|
| Main window | Mica |
| Popup / flyout / teaching tip | Acrylic (system) |
| In-window card | Card fill brush + border, no second Mica |
| Full-screen modal | ContentDialog over dimmed content |
