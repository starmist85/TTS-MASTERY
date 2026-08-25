# Hand-rolled vs. a control library

Decide this once, at the start. Switching later means rewriting every view, because the
two approaches disagree about who owns the visual language.

## Contents

- [The decision](#the-decision)
- [Hand-rolled](#hand-rolled)
- [WPF-UI (Fluent / Windows 11)](#wpf-ui-fluent--windows-11)
- [MaterialDesignInXAML](#materialdesigninxaml)
- [HandyControl](#handycontrol)
- [Syncfusion / DevExpress / Telerik](#syncfusion--devexpress--telerik)
- [Driving any library from your own tokens](#driving-any-library-from-your-own-tokens)
- [Mixing](#mixing)

## The decision

| If… | Choose |
|---|---|
| The app should have its own identity, or must match an existing brand | **Hand-rolled** (`assets/Theme/`) |
| The app should look like it shipped with Windows 11 | **WPF-UI** |
| The team already thinks in Material, or it ships alongside an Android/Flutter app | **MaterialDesignInXAML** |
| You need heavy data controls fast and appearance is secondary | **HandyControl**, or a commercial suite |
| It's an internal tool nobody will look at twice | **WPF-UI** — fastest path to "fine" |

Two honest observations.

**The token layer applies either way.** Every library exposes colors, and the ones worth
using expose them as swappable resources. Defining your own palette, type and spacing
tokens and then pointing the library's keys at them gives a themed app rather than a
default-skinned one, and it makes the library replaceable later.

**A library is a dependency on someone else's taste and release schedule.** That is
often a good trade — a library gives you a coherent, tested control set immediately, and
the controls WPF lacks (`NumberBox`, `InfoBar`, `NavigationView`, date pickers that
don't look like 2007) for free. It is a bad trade when the design brief is specific,
because the last 20% of customization inside a library is usually harder than building
the control yourself.

## Hand-rolled

What `assets/Theme/` provides. Roughly 1,200 lines of XAML covering buttons, inputs,
combo boxes, checkboxes, switches, sliders, progress bars, scrollbars, lists, tabs and
tooltips.

**Good when** the look matters, there's a brand to match, or you want zero third-party
surface area (relevant for offline/air-gapped deployment and for corporate approval
processes where each NuGet package is a review item).

**Costs:** controls WPF doesn't have, you build. Notably absent from WPF and worth
knowing before you commit: a numeric up/down, a modern date/time picker, a rating
control, an auto-complete box, a data grid that isn't a fight, a navigation view, an
in-app notification bar. `NumberBox` is a `TextBox` with validation plus two
`RepeatButton`s — an afternoon. A good `DataGrid` is not an afternoon.

Realistic middle path: hand-rolled theme, plus one library referenced *only* for the two
or three complex controls you actually need.

## WPF-UI (Fluent / Windows 11)

`WPF-UI` (lepo.co) — MIT, actively maintained, targets .NET 6+.

**Gives you:** Fluent-styled versions of the whole standard control set, `NavigationView`
with the Windows 11 rail/pane behaviour, `TitleBar` with proper caption buttons, Mica
backdrop handling, `InfoBar`, `NumberBox`, `Snackbar`, `ContentDialog`, and the Fluent
System Icons font bundled.

**Good when** you want the app to feel native on Windows 11 with very little work, and
you're happy for it to look like a Microsoft app. The Mica + `NavigationView` + `TitleBar`
combination is the single biggest time saver here — those are the three things that are
genuinely fiddly to hand-roll well.

**Watch for:** version churn between major releases has been significant, so pin the
version; the Fluent look is opinionated and pushing it far from Windows 11 fights the
library; and Mica means the app background is the user's wallpaper, which is wrong for
color-critical work (see `window-chrome.md`).

Theming it from your tokens:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ui:ThemesDictionary Theme="Dark"/>
            <ui:ControlsDictionary/>
            <!-- After the library, so these win. -->
            <ResourceDictionary Source="pack://application:,,,/Theme/Palette.Dark.xaml"/>
            <ResourceDictionary Source="pack://application:,,,/Theme/Tokens.xaml"/>
            <ResourceDictionary>
                <!-- Point the library's accent keys at yours. -->
                <SolidColorBrush x:Key="SystemAccentColorPrimaryBrush"
                                 Color="{StaticResource Color.Accent.Base}"/>
                <SolidColorBrush x:Key="AccentTextFillColorPrimaryBrush"
                                 Color="{StaticResource Color.Accent.Text}"/>
            </ResourceDictionary>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

To follow the *system* accent instead, read
`Windows.UI.ViewManagement.UISettings.GetColorValue(UIColorType.Accent)` and set the
brush at startup — that is the most "native" choice a Fluent app can make.

## MaterialDesignInXAML

`MaterialDesignThemes` — MIT, mature, large control set.

**Gives you:** a full Material 2 implementation — elevation, ripples, floating labels,
`DialogHost`, `Snackbar`, `Card`, color-tool palette generation from a primary/secondary
pair.

**Good when** the organization already runs Material, or the desktop app sits beside a
mobile app that does. The palette generator is genuinely useful: give it two colors and
it derives a consistent set of tints and shades.

**Watch for:** Material is *strongly* opinionated — ripples, uppercase buttons, floating
labels and heavy elevation are the look, and removing them leaves you fighting the
library rather than using it. On Windows it reads as "an Android app on the desktop",
which is a defensible choice but should be a choice. Ripple animations also cost more per
interaction than you might expect in dense lists.

Swap the palette:

```xml
<materialDesign:BundledTheme BaseTheme="Dark"
                             PrimaryColor="DeepPurple"
                             SecondaryColor="Lime"/>
```

For exact brand colors use `ColorZone`/`PaletteHelper` in code and set the primary and
secondary from your own `Color.Accent.*` tokens rather than the named presets.

## HandyControl

`HandyControl` — MIT, ~80 controls, strong on data and input widgets (`NumericUpDown`,
`TimePicker`, `Carousel`, `TagContainer`, `CircleProgressBar`, growl notifications).

**Good when** you need breadth of controls quickly and the visual result only has to be
tidy. Documentation is partly Chinese-language, which is a real friction point for some
teams.

**Watch for:** the default look is generic-modern rather than native or distinctive; it
is themeable but the theming story is less structured than WPF-UI's.

## Syncfusion / DevExpress / Telerik

Commercial. Worth it for one reason: **serious data grids, charts, docking layouts and
scheduling controls.** If the app is fundamentally a grid over a large dataset, or needs
Visual-Studio-style dockable panes, building that is months and buying it is days.

Syncfusion has a free community license under a revenue/headcount threshold, which makes
it viable for small teams and hobby projects.

**Watch for:** licensing complexity in redistributables and installers; large assembly
footprint (relevant when the installer is already gigabytes); and default themes that
look enterprise-2015 unless deliberately restyled.

## Driving any library from your own tokens

The pattern is the same regardless of library:

1. Merge the library's dictionaries **first**.
2. Merge your `Palette` + `Tokens` **after**, so your keys win where they collide.
3. Add a small override dictionary that assigns the library's named brush keys from your
   colors — accent, background, foreground, border are usually 80% of the visual result.
4. Keep your own `Text.*` styles and spacing tokens and use them in your views even for
   library controls. Type and rhythm are what make screens feel unified; the library's
   control internals matter less than you'd think.
5. Never edit library files in place. When you need a control to differ, define a keyed
   style `BasedOn` the library's and apply it locally — an edited library file is a
   change you lose on the next `dotnet restore` and cannot explain to the next developer.

## Mixing

Mixing two *styling* libraries is a bad idea — you get two shadow languages, two radius
scales and two focus treatments, and the seams are visible immediately.

Mixing hand-rolled theming with a library used purely for *complex controls* works well:
your theme owns buttons, inputs, lists and layout; the library supplies the grid or the
chart. Restyle the library control's chrome — header, borders, scrollbars — from your
tokens so it sits inside your surfaces without a visual seam. That is usually a handful
of brush overrides rather than a retemplate.
