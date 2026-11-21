# Fluent Design guidance (WinUI 3)

Advisory baseline for Fluent Design System on WinUI 3 / Windows App SDK. Prefer system resources and built-in controls; allow deliberate user-requested deviations.

**Interactive samples:** when a control or layout pattern is non-obvious, check WinUI Gallery (`microsoft/WinUI-Gallery`) and the library guidance in `ui-libraries.md` before inventing a custom treatment.

## 1. Color and theming

- Never hardcode UI colors (`#FF0000`, fixed `SolidColorBrush` hex) for text, backgrounds, or controls that must adapt to theme.
- Use `ThemeResource` keys from WinUI, e.g.:
  - Text: `TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`, `TextFillColorDisabledBrush`
  - Surfaces: `LayerFillColorDefaultBrush`, `CardBackgroundFillColorDefaultBrush`, `SolidBackgroundFillColorBaseBrush`
  - Controls: `ControlFillColorDefaultBrush`, `ControlStrokeColorDefaultBrush`, `ControlFillColorSecondaryBrush`
  - Accent: `SystemAccentColor`, `AccentFillColorDefaultBrush`
- Define custom colors only in `ResourceDictionary` / `ThemeDictionaries` (Light / Dark / HighContrast), then reference via `ThemeResource`.
- High contrast: rely on system brushes so HC themes work; do not ship a palette that only looks right in light mode.

## 2. Typography

Use WinUI text styles instead of ad-hoc `FontFamily`/`FontSize` combos:

| Role | Style (TextBlock `Style`) | Typical size / weight |
|------|---------------------------|------------------------|
| Caption | `CaptionTextBlockStyle` | 12 |
| Body | `BodyTextBlockStyle` | 14 |
| Body strong | `BodyStrongTextBlockStyle` | 14 semibold |
| Subtitle | `SubtitleTextBlockStyle` | 20 semibold |
| Title | `TitleTextBlockStyle` | 28 semibold |
| Title large | `TitleLargeTextBlockStyle` | 40 semibold |
| Display | `DisplayTextBlockStyle` | 68 semibold |

- One clear title hierarchy per page; avoid three different "big bold" sizes in one view.
- Prefer `TextWrapping="Wrap"` for prose; truncate with `TextTrimming="CharacterEllipsis"` in list rows.
- Secondary/informational copy: `TextFillColorSecondaryBrush` + body/caption style.

## 3. Spacing and layout

- Work on a 4px grid: margins/padding in multiples of 4 (common: 4, 8, 12, 16, 24, 32, 48).
- Page content padding: typically 12–24px on the sides; denser tool surfaces use 8–12.
- Stack panels and grids: consistent gap (8 within a form group, 16–24 between sections).
- Use `Grid` with `RowDefinitions`/`ColumnDefinitions` for page chrome; avoid deep nested `StackPanel` for complex layouts.
- Max content width for reading-heavy pages (~600–800px) centered or left-aligned consistently.
- Responsive: use `VisualStateManager` + `AdaptiveTrigger` for breakpoint layout changes (e.g. narrow window collapses a side pane).

## 4. Materials and elevation

- Prefer `MicaBackdrop` on the main `Window.SystemBackdrop` for app chrome; `DesktopAcrylicBackdrop` for transient flyouts/dialogs when depth matters.
- Cards/panels: subtle `CardBackgroundFillColorDefaultBrush` + thin stroke or `ThemeShadow` — not heavy drop shadows.
- Don't stack multiple acrylic layers; one backdrop layer per surface level.

## 5. Controls and components

Prefer first-party WinUI controls over custom chrome:

| Need | Use |
|------|-----|
| Primary actions | `Button` (standard / accent) |
| Text input | `TextBox` (+ `Header`, `PlaceholderText`) |
| Choice | `ComboBox`, `RadioButtons`, `CheckBox`, `ToggleSwitch` |
| Lists | `ListView` / `GridView` (+ `ItemRepeater` only if needed) |
| App-level nav | `NavigationView` |
| In-page nav / drill-in | `Frame` + `Page` |
| Transient message | `InfoBar`, `TeachingTip` |
| Modal | `ContentDialog` |
| Progressive disclosure | `Expander` |
| Settings rows | `SettingsCard` / `SettingsExpander` (WinUI) |
| Progress | `ProgressBar` / `ProgressRing` |

- Default corner radius is applied by the control — don't override `CornerRadius` unless the user asks.
- Primary button: one per dialog/teaching moment; secondary uses standard `Button`.
- Disabled state: leave control `IsEnabled=False` rather than graying manually.

## 6. Motion

- Prefer built-in transitions (`EntranceTransition`, reposition) over hand-rolled storyboards.
- Keep durations short (100–300ms); ease-out for entrances.
- Respect reduced-motion when exposing custom animation (skip or shorten if the user system requests it).
- Use `ConnectedAnimation` only when the same element logically continues across pages.

## 7. Accessibility

- Interactive elements: set `x:Uid` or `AutomationProperties.Name` / `Name` for screen readers.
- Keyboard: full tab order; visible focus (don't remove focus visuals); `AccessKey` for common actions.
- Contrast: body text meets WCAG AA against its actual background (system brushes generally do; custom accents may not).
- Don't convey state by color alone (add icon/text).
- Targets: minimum ~40×40px hit area for primary controls.
- Localizable strings: resource files, not concatenated code-behind text.

## 8. Window and title bar (Fluent app frame)

- Main window: `MicaBackdrop` + extended title bar (`ExtendsContentIntoTitleBar = true`, `SetTitleBar(element)`).
- Use a `TitleBar` (or custom `Grid`) with explicit height; ensure caption buttons stay clickable (padding/margin for the right side).
- Title text: match `AppTitleBar` branding; icon via `AppWindow.SetIcon` or `ImageIconSource`.
- Avoid drawing your own minimize/close buttons unless the user explicitly wants a fully custom chrome.

## Design review checklist

Use when reviewing XAML (advisory — report, don't auto-rewrite unless asked).

**Color / theme**
- [ ] No hardcoded hex for themed UI surfaces/text
- [ ] Custom colors live in ThemeDictionaries and use `ThemeResource`
- [ ] Light and dark theme both plausible (spot-check key brushes)

**Typography**
- [ ] Styles from the typography table above, not ad-hoc sizes
- [ ] Clear single title hierarchy per view

**Spacing / layout**
- [ ] 4px-grid spacing; consistent section gaps
- [ ] No unnecessary nested StackPanels for grid-like layouts
- [ ] Narrow-window behavior considered if layout is complex

**Controls**
- [ ] Built-in WinUI controls first; custom UI only with reason
- [ ] Correct control for the interaction (ContentDialog vs Flyout vs InfoBar)
- [ ] Default radii/states preserved

**Window / materials**
- [ ] Mica (or intended backdrop) set on window
- [ ] Title bar extended correctly; content not under caption buttons

**Accessibility**
- [ ] Names/`x:Uid` on interactive elements
- [ ] Keyboard reachable; focus visible
- [ ] Contrast and non-color-only state cues

**Severity guide**
- High: broken theming, inaccessible control, content under caption buttons, missing XamlControlsResources
- Medium: hardcoded colors, wrong control choice, inconsistent spacing/typography
- Low: polish (extra nesting, minor hierarchy duplication)
