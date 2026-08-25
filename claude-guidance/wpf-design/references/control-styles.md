# Control styles

`assets/Theme/Controls.xaml` restyles the common WPF control set. This file explains
what each template does, why it is shaped that way, and how to add a control without
drifting from the system.

## Contents

- [Implicit by default](#implicit-by-default)
- [Focus: replace, never remove](#focus-replace-never-remove)
- [Buttons](#buttons)
- [Text inputs and the missing placeholder](#text-inputs-and-the-missing-placeholder)
- [ComboBox](#combobox)
- [CheckBox vs. toggle switch](#checkbox-vs-toggle-switch)
- [Slider](#slider)
- [ProgressBar](#progressbar)
- [ScrollBar](#scrollbar)
- [Lists](#lists)
- [Tabs](#tabs)
- [Controls that cannot be saved](#controls-that-cannot-be-saved)
- [Adding a new styled control](#adding-a-new-styled-control)
- [Dynamic controls from metadata](#dynamic-controls-from-metadata)

## Implicit by default

Most styles in `Controls.xaml` have no `x:Key`, so they apply to every instance of the
type automatically. That is deliberate: relying on view authors to remember
`Style="{StaticResource ...}"` on every `TextBox` is a losing strategy, and the one
they forget is the one that ships. Keyed variants exist only where there is a real
choice to make — `Button.Primary` vs. `Button.Ghost`, `ListBoxItem.Card` vs. the
default row.

The trade-off: an implicit style breaks `BasedOn` inheritance for keyed styles of the
same type unless you say `BasedOn="{StaticResource {x:Type Button}}"`. In this file the
keyed button variants derive from `Button.Base` instead, which side-steps the issue.

## Focus: replace, never remove

Every style sets `FocusVisualStyle="{x:Null}"` and then draws its own indicator. The
default WPF focus visual is a dotted rectangle drawn in an adorner layer — it is the
single most dated element on screen, and it renders *outside* the control, so it
collides with neighbours in tight layouts.

Removing it without a replacement is an accessibility regression that makes the app
unusable by keyboard, and it will be the first thing an audit flags. The replacements
here are:

- Buttons, chips: a 2px accent ring drawn **inside** the bounds (an inside ring can
  never be clipped by a parent with `ClipToBounds`, which an outside ring routinely is)
- Inputs, combo boxes: border thickens to 2px and switches to accent
- List items: a 1px focus border, distinct from the selection fill — because focus and
  selection are different things and multi-select UIs need to show both
- Tabs: the underline appears at 50% opacity

Test it by tabbing through the entire window. If focus ever vanishes, the ring is
missing on that control or is being drawn behind something.

## Buttons

One base template (`Button.Base`), five variants layered as `Style.Triggers`. This
works because **Style triggers outrank Template triggers** in WPF's precedence order,
so a variant can repaint hover and press states without redefining the template.

| Style | Use |
|---|---|
| `Button.Primary` | The one action the screen exists for. One per region. |
| `Button.Secondary` | Everything else. This is the implicit `Button` style. |
| `Button.Ghost` | Toolbar and in-row actions — no chrome until hovered |
| `Button.Danger` | Destructive. Outline by default, fills red on hover. |
| `Button.Icon` | Square ghost button holding a glyph |
| `Chip` (ToggleButton) | Segmented pickers, filters, engine selectors |

Notes that matter:

- `Cursor="Arrow"` is set explicitly. `Cursor="Hand"` on a button is a web convention;
  on Windows the hand means hyperlink, and using it makes a desktop app feel like a
  wrapped web page.
- Disabled is `Opacity="0.4"` rather than a separate palette of disabled colors. One
  rule, applies to every variant, and it survives palette changes.
- `Button.Icon` needs a `ToolTip` **and** `AutomationProperties.Name` every time. A
  bare glyph is ambiguous to sighted users and invisible to screen readers:

```xml
<Button Style="{StaticResource Button.Icon}"
        ToolTip="Play reference"
        AutomationProperties.Name="Play reference"
        Command="{Binding PlayReferenceCommand}">
    <TextBlock Style="{StaticResource Text.Icon}" Text="&#xE768;"/>
</Button>
```

- A destructive action never uses `Button.Primary`. If "Delete voice character" is
  styled identically to "Generate", the user will eventually click the wrong one, and
  that is a design failure rather than a user error.

## Text inputs and the missing placeholder

WPF `TextBox` has no placeholder property. The template adds one by binding a
`TextBlock` to `Tag` and showing it while `Text` is empty:

```xml
<TextBox Tag="Search voices…" Text="{Binding Query, UpdateSourceTrigger=PropertyChanged}"/>
```

`Tag` is a slightly grubby carrier, but the alternative — an attached property in a
helper class — adds a code dependency to a pure resource dictionary. If the app already
has a XAML helpers assembly, promoting this to a real attached property
(`ui:Input.Placeholder`) is a clean upgrade.

A placeholder is **not** a label. It disappears the moment typing starts, so a form
whose fields are identified only by placeholder becomes unlabelled as soon as it is
filled in. Use a real label above the field and let the placeholder show *format*
("e.g. narrator_test", "44100").

`TextBox.Multiline` is the keyed variant for the big editor: `AcceptsReturn`, wrap, top
alignment, 14px padding, `LineHeight="22"` with `LineStackingStrategy="BlockLineHeight"`
(without which `LineHeight` is silently ignored), and the card radius rather than the
input radius, because at that size it reads as a surface rather than a field.

## ComboBox

A full retemplate — the default is a bevelled gradient toggle, second only to the
ScrollBar in how much it dates a window.

Structure: a `Border` for the field, a transparent `ToggleButton` two-way bound to
`IsDropDownOpen` covering the whole surface, a non-hit-testable `ContentPresenter`
bound to `SelectionBoxItem`, a chevron glyph, and `PART_Popup`.

Two details that are easy to get wrong:

- `MinWidth="{TemplateBinding ActualWidth}"` on the `Popup` makes the list at least as
  wide as the field. Without it, a long item causes a dropdown narrower than its own
  trigger, which looks broken.
- The popup's inner `Border` carries `Margin="6,0,6,8"` so the drop shadow has room to
  render. A popup with a shadow and no margin clips its own blur on three sides.

This template covers non-editable combo boxes. An editable one additionally needs a
`PART_EditableTextBox` in the template; if the app needs type-ahead, it is usually
better to compose a `TextBox` + filtered `Popup` list than to fight `IsEditable`.

## CheckBox vs. toggle switch

They are not interchangeable, and using them consistently is what makes a settings
screen feel predictable:

- **Toggle switch** — the change takes effect immediately ("Auto-play after
  generation", "Use GPU"). The switch *is* the commit.
- **CheckBox** — the change applies when the user saves or confirms, or the item is one
  of a set being selected (installer features, multi-select lists).

The switch animates its knob with a `TranslateTransform` over 150ms. That is a
composited transform, not a layout change, so it stays smooth even in a long settings
list.

## Slider

The filled portion of the track is the `DecreaseRepeatButton` restyled as a solid
accent bar. That is how WPF gives you a "value so far" fill without a second binding or
a converter — the `Track` already knows where the thumb is.

`IsMoveToPointEnabled="True"` makes clicking anywhere on the track jump the thumb there,
which is what users expect from every other slider they have used. WPF's default is to
page toward the click, which feels broken.

Pair every slider with a numeric readout. A slider alone cannot communicate "1.15", and
for engine parameters — CFG strength, temperature, speed — the exact value is what the
user is actually reasoning about:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <Slider Value="{Binding Speed}" Minimum="0.5" Maximum="2.0"
            TickFrequency="0.05" IsSnapToTickEnabled="True"/>
    <TextBlock Grid.Column="1" Width="44" Margin="8,0,0,0"
               Style="{StaticResource Text.Mono}"
               HorizontalAlignment="Right" VerticalAlignment="Center"
               Text="{Binding Speed, StringFormat={}{0:0.00}}"/>
</Grid>
```

A fixed-width, right-aligned monospace readout stops the layout jittering as digits
change — a small thing that is very visible while dragging.

## ProgressBar

Determinate uses `PART_Indicator` as WPF expects. Indeterminate sweeps a gradient
across the track rather than sliding a fixed-width block.

That choice is not cosmetic: a sliding block needs to know the control's width, so it
either hardcodes a size that looks wrong somewhere or needs a binding that fights with
virtualization. Animating three `GradientStop.Offset` values is width-independent — the
same markup works in a 90px table cell and a 900px banner. The stops are addressed with
a property path (`(Border.Background).(GradientBrush.GradientStops)[1].(GradientStop.Offset)`)
because a `GradientStop` is a `Freezable`, not a named element, so `Storyboard.TargetName`
cannot reach it directly.

Use indeterminate only when the duration is genuinely unknown. When the stage is known,
say the stage — "Loading model…", "Generating 3 of 8", "Writing WAV". A labelled
indeterminate bar is more trustworthy than an invented percentage, and users notice
invented percentages.

## ScrollBar

The highest-value retemplate in WPF, and the one most often skipped. The default is
17px wide, opaque grey, with arrow buttons that have not been used in fifteen years —
it is the loudest legacy element in any WPF window.

The replacement is a 10px transparent track with a pill thumb that thickens on hover
(margin 3 → 2). The `RepeatButton`s are kept but made invisible so page-up/page-down by
clicking the track still works — removing them entirely breaks that interaction, which
power users do notice.

The `ScrollViewer` style disables horizontal scrolling by default, because a UI that
scrolls horizontally is usually one with a layout bug. Turn it on explicitly where you
mean it (wide tables, timelines).

## Lists

`ListBoxItem` gets a soft accent-tinted selection fill with a 3px accent rail on the
left, replacing WPF's hard blue full-bleed rectangle. The rail is what makes the
selection findable when scanning a long list without the fill needing to be loud.

`ListBoxItem.Card` is the keyed variant for browser-style grids. Pair it with a
`WrapPanel` items panel:

```xml
<ListBox ItemContainerStyle="{StaticResource ListBoxItem.Card}"
         ItemsSource="{Binding Voices}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel Orientation="Horizontal"/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

Note that a `WrapPanel` items panel **disables virtualization** — `VirtualizingWrapPanel`
is not built in. For a card grid over hundreds of items, either bring in a virtualizing
wrap panel implementation or switch to a fixed-column `UniformGrid` inside a
`VirtualizingStackPanel` of rows. For a few dozen cards, plain `WrapPanel` is fine.

Two performance rules that show up as visible jank:

- **Never wrap an `ItemsControl`/`ListBox` in a `ScrollViewer`.** It measures with
  infinite height, so every item is realized and virtualization is gone.
- **No `DropShadowEffect` inside an `ItemTemplate`.** One bitmap effect pass per row.

## Tabs

Underline tabs replace the default folder-tab chrome, which cannot be made to look
current — only less bad. The `TabControl` template supplies a 1px divider under the
whole strip with the active tab's 2px underline sitting on top of it.

For the app's *primary* navigation, prefer a nav rail (see `layout-patterns.md`).
`TabControl` is for switching views *within* a page — the sections of a settings screen,
the panes of a diagnostics view.

## Controls that cannot be saved

Some WPF controls carry so much legacy chrome that retemplating them costs more than
composing the affordance yourself:

| Control | Instead |
|---|---|
| `GroupBox` | `Border` (Card style) + a `Text.Subtitle` heading above it |
| `ToolBar` | `Border` + horizontal `StackPanel` of `Button.Ghost` |
| `StatusBar` | `Border` + `Grid` (see the status bar pattern) |
| `Menu` / `MenuItem` | Retemplatable, but a command palette or ghost-button toolbar usually fits a modern tool better |
| `Expander` | Keep it, but retemplate the header: the default arrow is a legacy glyph |
| `DataGrid` | Retemplatable and worth it for editable grids; for read-only tables a `ListView` with `GridView`, or a `Grid`-based `ItemTemplate`, gives cleaner control |
| `TreeView` | Retemplate `TreeViewItem`'s expander toggle; the rest is workable |

## Adding a new styled control

1. Check whether a token already covers what you need. New values are how systems rot.
2. Base new button-like things on `Button.Base` so focus, disabled and metrics come for
   free.
3. Use `DynamicResource` for brushes (so theme swapping works) and `StaticResource` for
   sizes, radii and durations (so you do not pay lookup cost for values that never
   change).
4. Set `FocusVisualStyle="{x:Null}"` and draw a ring.
5. Set `SnapsToDevicePixels="True"` on any `Border` with a 1px stroke.
6. Handle four states: normal, hover, pressed/active, disabled. Plus focused.
7. Verify in both palettes before moving on — a hardcoded color announces itself
   immediately on theme swap, and it is much cheaper to catch now.

## Dynamic controls from metadata

Apps with pluggable back-ends — TTS engines, exporters, model providers — need per-plugin
option panels. The wrong way is `if (engine == "F5")` branching in XAML or code-behind;
it grows without bound and every new engine touches the UI.

The pattern that stays clean: the plugin describes its settings as data, the app maps
each setting to a small view-model type, and XAML picks the editor by type.

```xml
<ItemsControl ItemsSource="{Binding SettingEditors}">
    <ItemsControl.Resources>
        <DataTemplate DataType="{x:Type vm:BoolSettingViewModel}">
            <DockPanel Margin="{StaticResource Gap.FormRow}">
                <ToggleButton Style="{StaticResource ToggleSwitch}"
                              DockPanel.Dock="Right"
                              IsChecked="{Binding Value}"/>
                <StackPanel>
                    <TextBlock Text="{Binding DisplayName}"/>
                    <TextBlock Style="{StaticResource Text.Hint}" Text="{Binding Description}"/>
                </StackPanel>
            </DockPanel>
        </DataTemplate>

        <DataTemplate DataType="{x:Type vm:DoubleSettingViewModel}">
            <StackPanel Margin="{StaticResource Gap.FormRow}">
                <TextBlock Text="{Binding DisplayName}"
                           ToolTip="{Binding Description}"
                           Margin="{StaticResource Gap.LabelToControl}"/>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <Slider Value="{Binding Value}"
                            Minimum="{Binding Minimum}" Maximum="{Binding Maximum}"
                            TickFrequency="{Binding Step}" IsSnapToTickEnabled="True"/>
                    <TextBlock Grid.Column="1" Width="48" Margin="8,0,0,0"
                               Style="{StaticResource Text.Mono}"
                               HorizontalAlignment="Right" VerticalAlignment="Center"
                               Text="{Binding Value, StringFormat={}{0:0.00}}"/>
                </Grid>
            </StackPanel>
        </DataTemplate>

        <DataTemplate DataType="{x:Type vm:EnumSettingViewModel}">
            <StackPanel Margin="{StaticResource Gap.FormRow}">
                <TextBlock Text="{Binding DisplayName}" ToolTip="{Binding Description}"
                           Margin="{StaticResource Gap.LabelToControl}"/>
                <ComboBox ItemsSource="{Binding Choices}" SelectedItem="{Binding Value}"/>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.Resources>
</ItemsControl>
```

Implicit `DataTemplate`s keyed on `DataType` mean the view has no knowledge of any
specific plugin: adding a new setting kind is a new view-model type plus a new template,
and adding a new plugin is zero UI changes. Group advanced settings behind an
`Expander` so the default view stays approachable — a panel with eighteen sampling
parameters visible at once reads as a config file, not a tool.
