---
name: wpf-design
description: >-
  Design and build genuinely good-looking C#/WPF desktop interfaces - design tokens,
  ControlTemplates, dark/light theming, layout, density, motion, and the Win32 details
  that make a WPF app stop looking like 2009. Use whenever WPF or XAML appearance is in
  play - creating or restyling any Window, UserControl, View, page or dialog; setting up
  ResourceDictionaries or a theme; choosing colors, typography, spacing or icons;
  building a nav shell, settings screen, card browser, status bar or player transport;
  retemplating ComboBox/ScrollBar/Slider/ListBox; choosing between hand-rolled styles and
  WPF-UI or MaterialDesignInXAML; or hearing "make it look good", "modern UI", "polish
  this screen", "looks dated", "dark mode". Trigger it even when the request sounds
  purely functional - "add a settings page", "build the voice library screen" - because
  in a desktop app the visual result is half the deliverable, and retrofitting a design
  system later costs far more than starting with one.
---

# Beautiful WPF Design

WPF is one of the most capable UI frameworks ever shipped and one of the ugliest out of the box. Its
default control templates were drawn for Windows Vista: bevelled gradients, chunky scrollbars, a
1990s blue selection highlight, marching-ants focus rectangles, a white title bar bolted to your dark
app. None of that is a limitation of WPF — it is a default you are expected to replace.

So the work here is not *decorating*. It is replacing a stale default layer with a coherent one, in a
specific order, and then not breaking that coherence when you add screens. A WPF app looks expensive
when every surface, radius, gap and animation duration comes from the same small set of decisions.
It looks cheap when each screen invented its own.

## Workflow

Follow this order. Skipping ahead is the single most common cause of an app that never quite looks
right — screens built before a token layer exists end up with hardcoded `Margin="10,7,0,3"` and eight
slightly different grays that nobody wants to go back and unify.

**1. Read the app before styling it.** What kind of tool is this? A creative/production tool (audio,
video, CAD, IDE) wants density, a dark canvas, and controls that get out of the way. A business app
wants light surfaces, generous spacing and legibility at a glance. A utility wants to look native.
Who stares at it for six hours? Density and contrast follow from that answer, not from taste.

**2. Commit to one direction.** Dark pro-audio studio, Windows 11 Fluent native, or clean light SaaS.
Half-committing is what produces mush. The default this skill reaches for — because it suits creative
and AI tooling, which is most of what people build in WPF today — is **dark pro-audio studio**: deep
cool neutrals, a single accent, dense but breathable spacing, contrast carried by surface elevation
rather than by color. Light is a palette swap, not a different design.

**3. Decide hand-rolled vs. control library** — one decision, made once, at the start. See
`references/control-libraries.md`. Short version: hand-rolled when you want a distinct look and have
no dependency appetite; WPF-UI when you want Windows 11 Fluent for free; MaterialDesignInXAML when
you want Material and don't mind it looking like Material. The token layer below applies either way —
libraries theme *better* when you drive them from your own tokens.

**4. Lay the token layer first.** Copy `assets/Theme/` into the app, wire it into `App.xaml`, and from
then on never type a raw color, size or duration into a view. See `references/design-tokens.md` for
the scales and the reasoning; the files are drop-in and documented inline.

**5. Build the shell before the screens.** Window chrome, navigation, page host, status bar. The shell
fixes the app's proportions and is where the Win32 details live (dark title bar, Mica, DPI). See
`references/window-chrome.md` — the dark-title-bar P/Invoke in particular is a four-line fix for a
problem that otherwise makes a beautiful dark app look broken in every screenshot.

**6. Retemplate the controls that betray WPF's age.** ScrollBar, ComboBox, ListBoxItem, Slider,
TextBox focus, TabControl, ProgressBar, CheckBox. `assets/Theme/Controls.xaml` has all of them;
`references/control-styles.md` explains what each template does and how to extend it.

**7. Compose screens from patterns, not from scratch.** `references/layout-patterns.md` covers the
shell, three-pane workspaces, card browsers, data tables, settings pages, dialogs, toasts, empty and
error states, and the patterns specific to audio/AI tools (engine selector with health status,
voice-character cards, generation queue rows, transport bar, model manager, diagnostics).

**8. Do the fresh-eyes pass** at the end of this file. Build it, run it, look at it, and fix the three
things you'd notice if someone else had written it.

## Choosing colors, type and space

**One accent, used sparingly.** The accent marks the primary action, the current selection, the focus
ring, and the active nav item. That's it. When a screen has five accented things, none of them read as
important. Everything else is neutral; state colors (success/warning/danger) appear only where state
actually exists.

**Contrast comes from elevation and weight, not hue.** On dark, a card separates from its background
by being one step lighter plus a 1px subtle border — not by being blue. Reserve saturation for meaning.

**Four text tones, no more.** Primary (what you read), secondary (labels, metadata), tertiary (hints,
placeholder), disabled. If you need a fifth you probably need a layout change instead.

**Type: one family, six sizes, three weights.** Segoe UI Variable Text on Windows 11 with Segoe UI as
fallback. Sizes 28/20/16/14/12/11 at Regular/SemiBold/Bold. Body is 14. Note WPF has **no letter-spacing**
on `TextBlock` — do not design around tracked-out uppercase labels, they cannot be rendered cleanly.

**Space on a 4px grid.** 4/8/12/16/24/32/48. Page padding 24, card padding 16, gutter between cards 12,
gap between a label and its control 6, between form rows 16. Control heights 28 (compact) / 32 (default)
/ 40 (large). Consistency here does more for perceived quality than any individual visual flourish.

**Radii, three of them.** 6 for inputs and buttons, 10 for cards and panels, 14 for dialogs and popovers.
Pill (999) only for badges and chips. Mixing four radii on one screen reads as sloppiness.

## WPF specifics that make or break the look

These are the things that are invisible in a code review and glaring on screen.

- **Kill the focus rectangle, keep the focus.** `FocusVisualStyle="{x:Null}"` on every style, then draw a
  2px accent ring in the template triggered on `IsKeyboardFocused`. Removing focus indication entirely
  is an accessibility regression and makes the app unusable by keyboard — the point is to replace it,
  not delete it.
- **Retemplate ScrollBar or nothing else matters.** The default 17px gray scrollbar with arrow buttons
  is the loudest legacy element in WPF. A thin (6–10px) overlay thumb that fades in on hover is a
  bigger visual upgrade than any color choice you'll make.
- **The title bar is not yours by default.** A dark WPF app with a white caption bar looks broken. Fix it
  with `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` (see `references/window-chrome.md`) or take
  the chrome over entirely with `WindowChrome`.
- **`UseLayoutRounding="True"` and `SnapsToDevicePixels="True"`** on the Window and on any `Border` with a
  1px stroke. Without them hairlines render as blurry 2px smears at fractional DPI scaling, which is
  what makes an app feel subtly out of focus.
- **Avoid `DropShadowEffect` at scale.** It is a per-element render pass and it is slow in lists. On dark
  themes you rarely need it: elevation reads through surface lightness. Use a shadow only on popups,
  dialogs and drag previews — `BlurRadius="24" ShadowDepth="6" Opacity="0.45"` — never inside an
  `ItemTemplate`.
- **Legacy chrome controls are unrecoverable.** `GroupBox`, `ToolBar`, `StatusBar`, `Menu`, default
  `TabControl`, `Expander`'s default arrow. Don't try to style them into modernity; compose the same
  affordance from `Border` + `Grid` + a styled `ToggleButton`. It is less work than fighting the template.
- **Icons: a glyph font or vector paths, never emoji.** Segoe Fluent Icons (Windows 11) with Segoe MDL2
  Assets as fallback, or `Path` geometry from a set like Lucide/Fluent. Emoji render in color, at the
  wrong optical weight, and differently per machine.
- **Virtualize every list.** `VirtualizingStackPanel.IsVirtualizing="True"`,
  `VirtualizationMode="Recycling"`, and no `ScrollViewer` wrapped around an `ItemsControl` (it destroys
  virtualization by giving infinite height). A janky scroll makes a beautiful list feel cheap.
- **`StaticResource` by default, `DynamicResource` for theme-swappable brushes.** DynamicResource has a
  lookup cost per element; use it where runtime theme switching needs it (brushes) and StaticResource
  everywhere else (sizes, radii, durations).
- **Keep the arrow cursor on buttons.** `Cursor="Hand"` on buttons is a web convention; on Windows it
  signals a hyperlink. Using it makes a desktop app feel like a wrapped web page.

## States, motion and feedback

**Every screen has four states, and three of them get skipped.** Populated is the one everyone builds.
Also design: empty (with a reason and a primary action, not a blank panel), loading (skeleton or a
calm spinner with a stage label), and error (what failed, what to try, and a way to copy diagnostics).
In tools that drive long GPU or subprocess work this matters more than usual — a user staring at a
frozen-looking pane for 40 seconds while a model loads is a design failure, not a performance one.

**Motion is short and purposeful.** 90ms for hover/press feedback, 150ms for standard transitions,
240ms for something entering or leaving. `CubicEase` with `EaseOut` for entrances, `EaseInOut` for
moves. Animate opacity and transform; animating layout properties (`Width`, `Margin`) forces a layout
pass per frame and stutters. If an animation is longer than 300ms, it is now a delay, not polish.

**Progress must be honest.** Indeterminate bars only when the duration is genuinely unknown. When you
know the stage, say the stage ("Loading model…", "Generating 3/8", "Writing WAV") — a labeled
indeterminate bar beats a fake percentage every time.

**Never signal by color alone.** A status dot needs a text label beside it. Roughly 1 in 12 men cannot
distinguish your success green from your danger red, and it costs one `TextBlock` to fix.

**Destructive actions are never the accent color.** Danger styling, secondary position, and confirmation
inline in the row rather than a modal where you can manage it.

## Tool-app patterns worth knowing

For creative/AI desktop tools specifically — media studios, TTS/voice apps, generation front-ends —
`references/layout-patterns.md` has full markup for: a nav rail + content host shell; a three-pane
generation workspace (left options / center canvas / right output); engine or model selectors that
carry health status (Ready, Loading model, Missing model, CUDA unavailable) without shouting; a
character/preset card browser with search, filter and favorites; a job queue with per-row progress and
cancel; a transport bar with waveform, scrub and volume; a data-table model manager; and a diagnostics
page. It also covers the **dynamically-generated settings panel** — rendering per-engine options from
capability metadata via `DataTemplate`s selected on view-model type, which keeps the UI free of
`if (engine == "X")` branching and is the pattern that makes a plugin-style app stay clean.

## Fresh-eyes pass

Build it, run it, and look at it as if someone else wrote it. Take a screenshot —
`scripts/Capture-WindowScreenshot.ps1` grabs a window by process name — because you will notice
misalignment in a still image that you filter out in a live window.

- Does every screen use the same page padding, and do headings line up vertically across screens?
- Count the accent-colored things on the busiest screen. More than three? Demote some.
- Tab through the whole window. Does focus stay visible, in a sensible order, and never disappear?
- Resize to 900px wide and to full screen. Does anything clip, stretch to absurdity, or leave a
  stranded column of dead space?
- Switch themes. Any hardcoded color left behind will announce itself immediately.
- Set Windows display scaling to 150%. Are hairlines still hairlines? Is text still crisp?
- Trigger the empty, loading and error state of each screen. Do they exist?
- Scroll the longest list fast. Smooth?
- Is there a single element whose alignment is off by 1–2px? Fix it; it is the thing that reads as
  "homemade" even when nobody can name it.

## Reference map

Read these as needed rather than upfront:

- `references/design-tokens.md` — the palette, type, spacing, radius, elevation and motion scales, with
  the reasoning and the accessibility math. Read when setting up a theme or adding a color.
- `references/control-styles.md` — what's in `Controls.xaml`, how each template is structured, and how
  to add a new styled control without drifting. Read when styling or retemplating a control.
- `references/layout-patterns.md` — copyable markup for shells, panes, cards, tables, settings, dialogs,
  toasts, states, and the tool-app patterns above. Read when building a screen.
- `references/window-chrome.md` — dark title bar, Mica/acrylic backdrop, custom `WindowChrome`, DPI,
  icon fonts. Read when building the main window.
- `references/control-libraries.md` — WPF-UI vs. MaterialDesignInXAML vs. HandyControl vs. hand-rolled,
  and how to drive each from your own tokens. Read at step 3, once.

Assets:

- `assets/Theme/Palette.Dark.xaml`, `Palette.Light.xaml` — colors only, swappable at runtime.
- `assets/Theme/Tokens.xaml` — type, spacing, radii, durations, easing, control metrics.
- `assets/Theme/Controls.xaml` — styles and templates for the full common control set.
- `assets/Theme/App.xaml.example` — how to wire the dictionaries together and swap themes.
- `scripts/Capture-WindowScreenshot.ps1` — screenshot a running window for the fresh-eyes pass.
