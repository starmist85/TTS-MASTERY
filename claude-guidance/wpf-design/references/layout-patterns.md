# Layout patterns

Copyable structures for the screens most desktop tools need. Everything here assumes
`assets/Theme/` is wired up, so no colors or sizes are hardcoded.

## Contents

- [Shell: nav rail + content host](#shell-nav-rail--content-host)
- [Page header](#page-header)
- [Three-pane workspace](#three-pane-workspace)
- [Card browser](#card-browser)
- [Data table](#data-table)
- [Settings page](#settings-page)
- [The four states](#the-four-states)
- [Status pill and health rows](#status-pill-and-health-rows)
- [Job queue rows](#job-queue-rows)
- [Transport bar](#transport-bar)
- [Status bar](#status-bar)
- [Dialogs and toasts](#dialogs-and-toasts)
- [Drag and drop](#drag-and-drop)
- [Density and responsive behaviour](#density-and-responsive-behaviour)

## Shell: nav rail + content host

A left rail beats a top menu bar for a tool with 5–9 destinations: it stays visible, it
has room for status, and it does not require a click to reveal.

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{StaticResource Width.NavRail}"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- Rail -->
    <Border Grid.Row="0" Grid.Column="0" Style="{StaticResource Panel}" BorderThickness="0,0,1,0">
        <DockPanel>
            <StackPanel DockPanel.Dock="Top" Margin="16,20,16,20">
                <TextBlock Style="{StaticResource Text.Subtitle}" Text="TTS Mastery"/>
                <TextBlock Style="{StaticResource Text.Caption}" Text="Local offline studio"/>
            </StackPanel>

            <!-- Bottom-docked secondary destinations: settings and diagnostics are
                 things you visit rarely, so they should not compete with the daily
                 workflow at the top. -->
            <ListBox DockPanel.Dock="Bottom" Margin="8,0,8,12"
                     ItemsSource="{Binding SecondaryDestinations}"
                     SelectedItem="{Binding Current}"
                     ItemTemplate="{StaticResource NavItemTemplate}"/>

            <ListBox Margin="8,0,8,0"
                     ItemsSource="{Binding Destinations}"
                     SelectedItem="{Binding Current}"
                     ItemTemplate="{StaticResource NavItemTemplate}"/>
        </DockPanel>
    </Border>

    <!-- Content host: ContentControl + implicit DataTemplates per page view-model
         keeps navigation in the view-model and out of code-behind. -->
    <ContentControl Grid.Row="0" Grid.Column="1" Content="{Binding Current.Page}"/>

    <!-- Status bar spans both columns -->
    <ContentControl Grid.Row="1" Grid.ColumnSpan="2" Content="{Binding StatusBar}"/>
</Grid>
```

```xml
<DataTemplate x:Key="NavItemTemplate">
    <StackPanel Orientation="Horizontal" Height="34">
        <TextBlock Style="{StaticResource Text.Icon}" Text="{Binding Glyph}" Width="20"/>
        <TextBlock Text="{Binding Title}" Margin="12,0,0,0" VerticalAlignment="Center"/>
    </StackPanel>
</DataTemplate>
```

The default `ListBoxItem` style already gives the accent rail + tinted fill for the
active destination, so nothing extra is needed to show "you are here".

**Collapsing the rail:** bind the column width to a view-model bool via a converter
between `Width.NavRail` and `Width.NavRail.Collapsed`, and collapse the label
`TextBlock`s. Keep the glyphs and add `ToolTip="{Binding Title}"` — a collapsed rail
with no tooltips is a memory test.

## Page header

Every page opens the same way, which is most of what makes an app feel coherent.

```xml
<Grid Margin="{StaticResource Pad.Page}">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <Grid Grid.Row="0" Margin="0,0,0,20">
        <StackPanel>
            <TextBlock Style="{StaticResource Text.Title}" Text="Voice Library"/>
            <TextBlock Style="{StaticResource Text.Hint}" Margin="0,4,0,0"
                       Text="Reusable speaker identities and their reference recordings."/>
        </StackPanel>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center">
            <Button Content="Import" Margin="0,0,8,0"/>
            <Button Style="{StaticResource Button.Primary}" Content="New Voice Character"/>
        </StackPanel>
    </Grid>

    <!-- page body -->
</Grid>
```

Title, one line of subtext explaining what the page is for, actions right-aligned on the
same baseline, 20px to the body. The subtext is worth writing: it is the cheapest
onboarding an app has, and it costs one line.

## Three-pane workspace

The generation/editor screen shape: options left, canvas centre, output right.

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="320" MinWidth="260"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*" MinWidth="420"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="340" MinWidth="280"/>
    </Grid.ColumnDefinitions>

    <ScrollViewer Grid.Column="0" Padding="20">
        <StackPanel><!-- engine, voice, language, engine options --></StackPanel>
    </ScrollViewer>

    <GridSplitter Grid.Column="1" Width="1" HorizontalAlignment="Stretch"
                  Background="{DynamicResource Brush.Divider}"
                  ResizeBehavior="PreviousAndNext"/>

    <Grid Grid.Column="2" Margin="20"><!-- text editor + generate --></Grid>

    <GridSplitter Grid.Column="3" Width="1" HorizontalAlignment="Stretch"
                  Background="{DynamicResource Brush.Divider}"
                  ResizeBehavior="PreviousAndNext"/>

    <Grid Grid.Column="4" Margin="20"><!-- output preview + history --></Grid>
</Grid>
```

A 1px `GridSplitter` with a divider background reads as a hairline but still has a
5px hit area if you give it `Margin="-2,0,-2,0"` on a transparent parent. Side panes get
`MinWidth` so a drag cannot destroy the layout, and the centre pane takes `*` because it
is the one that benefits from extra space.

Persist splitter positions to settings. Users move them once and expect them to stay.

## Card browser

Search, filter, sort above; cards below.

```xml
<DockPanel>
    <Grid DockPanel.Dock="Top" Margin="0,0,0,16">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" MaxWidth="380"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <TextBox Tag="Search voices…" Text="{Binding Query, UpdateSourceTrigger=PropertyChanged, Delay=200}"/>

        <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="12,0,0,0">
            <ToggleButton Style="{StaticResource Chip}" Content="Favorites" Margin="0,0,6,0"
                          IsChecked="{Binding FavoritesOnly}"/>
            <ToggleButton Style="{StaticResource Chip}" Content="Cloning" Margin="0,0,6,0"/>
        </StackPanel>

        <ComboBox Grid.Column="3" Width="160" ItemsSource="{Binding SortOptions}"
                  SelectedItem="{Binding SortBy}"/>
    </Grid>

    <ListBox ItemsSource="{Binding Voices}"
             ItemContainerStyle="{StaticResource ListBoxItem.Card}"
             ItemTemplate="{StaticResource VoiceCardTemplate}">
        <ListBox.ItemsPanel>
            <ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate>
        </ListBox.ItemsPanel>
    </ListBox>
</DockPanel>
```

`Delay=200` on the search binding debounces filtering — without it every keystroke
re-filters and a large collection stutters visibly.

Card template: a fixed width (240–280) so the grid stays even, avatar or initial, name
in `Text.Subtitle`, two lines of metadata in `Text.Caption`, and row actions that appear
on hover:

```xml
<DataTemplate x:Key="VoiceCardTemplate">
    <StackPanel Width="248">
        <Grid>
            <Border Width="40" Height="40" HorizontalAlignment="Left"
                    CornerRadius="{StaticResource Radius.Sm}"
                    Background="{DynamicResource Brush.Accent.Muted}">
                <TextBlock Text="{Binding Initials}" HorizontalAlignment="Center"
                           VerticalAlignment="Center" FontWeight="SemiBold"
                           Foreground="{DynamicResource Brush.Accent.Text}"/>
            </Border>
            <TextBlock Style="{StaticResource Text.Icon}" HorizontalAlignment="Right"
                       Text="&#xE735;" Foreground="{DynamicResource Brush.Warning.Text}"
                       Visibility="{Binding IsFavorite, Converter={StaticResource BoolToVisibility}}"/>
        </Grid>
        <TextBlock Style="{StaticResource Text.Subtitle}" Margin="0,12,0,0"
                   Text="{Binding Name}" TextTrimming="CharacterEllipsis"/>
        <TextBlock Style="{StaticResource Text.Caption}" Margin="0,2,0,0"
                   Text="{Binding LanguageDisplay}"/>
        <TextBlock Style="{StaticResource Text.Caption}" Margin="0,2,0,0"
                   Text="{Binding ReferenceCount, StringFormat={}{0} reference recordings}"/>
    </StackPanel>
</DataTemplate>
```

`TextTrimming="CharacterEllipsis"` on any user-supplied string in a fixed-width
container. Without it a long name blows out the card and the whole grid goes ragged.

## Data table

For rows the user compares — models, history, files. A `Grid`-based `ItemTemplate` with
`SharedSizeGroup` gives aligned columns without `DataGrid`'s chrome:

```xml
<Grid Grid.IsSharedSizeScope="True">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- Header -->
    <Grid Grid.Row="0" Margin="12,0,12,8">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition SharedSizeGroup="Size" Width="Auto"/>
            <ColumnDefinition SharedSizeGroup="Status" Width="Auto"/>
        </Grid.ColumnDefinitions>
        <TextBlock Style="{StaticResource Text.Caption}" Text="Model"/>
        <TextBlock Grid.Column="1" Style="{StaticResource Text.Caption}" Text="Size" MinWidth="90"/>
        <TextBlock Grid.Column="2" Style="{StaticResource Text.Caption}" Text="Status" MinWidth="120"/>
    </Grid>
    <Border Grid.Row="0" Style="{StaticResource Divider}" VerticalAlignment="Bottom"/>

    <ListBox Grid.Row="1" ItemsSource="{Binding Models}">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition SharedSizeGroup="Size" Width="Auto"/>
                        <ColumnDefinition SharedSizeGroup="Status" Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Text="{Binding Name}" TextTrimming="CharacterEllipsis"/>
                    <TextBlock Grid.Column="1" Style="{StaticResource Text.Mono}"
                               Text="{Binding SizeDisplay}" MinWidth="90"/>
                    <ContentControl Grid.Column="2" Content="{Binding Status}" MinWidth="120"/>
                </Grid>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</Grid>
```

Right-align and monospace anything numeric the user will compare column-wise. Left-aligned
proportional numbers are measurably harder to scan.

## Settings page

Two-column form, sections separated by headings, max width so lines stay readable on a
wide monitor.

```xml
<ScrollViewer>
    <StackPanel MaxWidth="760" HorizontalAlignment="Left" Margin="{StaticResource Pad.Page}">

        <TextBlock Style="{StaticResource Text.Subtitle}" Text="General"/>
        <Border Style="{StaticResource Divider}" Margin="0,8,0,16"/>

        <Grid Margin="{StaticResource Gap.FormRow}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="240"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <StackPanel>
                <TextBlock Text="Default engine"/>
                <TextBlock Style="{StaticResource Text.Hint}" Text="Pre-selected when the app starts."/>
            </StackPanel>
            <ComboBox Grid.Column="1" VerticalAlignment="Top"
                      ItemsSource="{Binding Engines}" SelectedItem="{Binding DefaultEngine}"/>
        </Grid>

        <Grid Margin="{StaticResource Gap.FormRow}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="240"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <StackPanel>
                <TextBlock Text="Auto-play after generation"/>
                <TextBlock Style="{StaticResource Text.Hint}" Text="Plays the result as soon as it is written."/>
            </StackPanel>
            <ToggleButton Grid.Column="1" Style="{StaticResource ToggleSwitch}"
                          HorizontalAlignment="Left" IsChecked="{Binding AutoPlay}"/>
        </Grid>

    </StackPanel>
</ScrollViewer>
```

Label left with a one-line explanation, control right. The explanation under the label
is what stops users guessing — and a settings screen full of unexplained switches is one
users avoid. Toggles apply immediately; if a setting needs Save, use a `CheckBox` and
put a sticky action bar at the bottom.

## The four states

Every data-bound region has four, and three usually get skipped. Swap them with a
`ContentControl` + `DataTrigger` on a view-model state enum rather than stacking
`Visibility` bindings — one source of truth, no chance of two states showing at once.

**Empty** — say why it is empty and offer the action that fixes it:

```xml
<StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" MaxWidth="360">
    <TextBlock Style="{StaticResource Text.Icon}" FontSize="32" Text="&#xE8D6;"
               Foreground="{DynamicResource Brush.Text.Tertiary}"/>
    <TextBlock Style="{StaticResource Text.Subtitle}" Margin="0,16,0,0"
               HorizontalAlignment="Center" Text="No voice characters yet"/>
    <TextBlock Style="{StaticResource Text.Prose}" Margin="0,6,0,0"
               TextAlignment="Center"
               Text="A voice character stores reference audio and its transcript so you can reuse a speaker across generations."/>
    <Button Style="{StaticResource Button.Primary}" Margin="0,20,0,0"
            HorizontalAlignment="Center" Content="New Voice Character"/>
</StackPanel>
```

**Loading** — skeleton rows for lists (shapes at the right size, `Brush.Surface.Hover`
fill, opacity pulsing 0.4→0.8 over 1.2s); a small indeterminate bar plus a stage label
for operations. Never a blank pane.

**Error** — what failed, in the user's terms; what to try; and a **Copy diagnostics**
button. For a tool driving subprocesses and GPUs, that button is the difference between
a usable bug report and "it didn't work". Put the stack trace behind an expander, never
in the primary message.

**Populated** — the one everybody builds.

## Status pill and health rows

Engine, service and model status needs to be readable at a glance without shouting.
A dot plus a word, tinted for state — never color alone:

```xml
<DataTemplate x:Key="StatusPill">
    <Border CornerRadius="{StaticResource Radius.Pill}" Padding="8,3,10,3"
            Background="{DynamicResource Brush.Surface.Overlay}"
            BorderBrush="{DynamicResource Brush.Border.Subtle}" BorderThickness="1">
        <StackPanel Orientation="Horizontal">
            <Ellipse Width="7" Height="7" VerticalAlignment="Center"
                     Fill="{Binding StatusBrush}"/>
            <TextBlock Style="{StaticResource Text.Caption}" Margin="7,0,0,0"
                       Text="{Binding StatusText}"/>
        </StackPanel>
    </Border>
</DataTemplate>
```

Map states to brushes in the view-model, not in triggers scattered through views:
Ready → `Brush.Success.Text`; Initializing / Loading model → `Brush.Accent.Text`;
Not configured / Missing model → `Brush.Warning.Text`; Error → `Brush.Danger.Text`;
Disabled → `Brush.Neutral.Base`.

A health list (diagnostics, first-run setup) is these rows stacked, each with a detail
line and a repair action:

```xml
<Grid Height="{StaticResource Height.ListRow}">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="160"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <TextBlock Text="{Binding EngineName}" VerticalAlignment="Center"/>
    <ContentControl Grid.Column="1" ContentTemplate="{StaticResource StatusPill}" Content="{Binding}"/>
    <TextBlock Grid.Column="2" Style="{StaticResource Text.Caption}" Margin="12,0,0,0"
               VerticalAlignment="Center" Text="{Binding Detail}" TextTrimming="CharacterEllipsis"/>
    <Button Grid.Column="3" Style="{StaticResource Button.Ghost}" Content="Test"/>
</Grid>
```

**One failure must never take the screen down.** If one engine is broken, its row says so
and the rest stay usable. An app that shows a full-page error because one optional
back-end is missing feels fragile even when everything else works.

## Job queue rows

Per-row progress, stage text, elapsed time, cancel.

```xml
<DataTemplate x:Key="QueueRowTemplate">
    <Grid Margin="0,6,0,6">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Text="{Binding TextPreview}" TextTrimming="CharacterEllipsis"/>
        <StackPanel Grid.Column="1" Orientation="Horizontal">
            <TextBlock Style="{StaticResource Text.Mono}" Text="{Binding Elapsed}"
                       VerticalAlignment="Center"/>
            <Button Style="{StaticResource Button.Icon}" Margin="8,0,0,0"
                    ToolTip="Cancel" AutomationProperties.Name="Cancel"
                    Command="{Binding CancelCommand}">
                <TextBlock Style="{StaticResource Text.Icon}" Text="&#xE711;"/>
            </Button>
        </StackPanel>

        <Grid Grid.Row="1" Grid.ColumnSpan="2" Margin="0,8,0,0">
            <ProgressBar Value="{Binding Progress}" Maximum="1"
                         IsIndeterminate="{Binding IsProgressUnknown}"/>
        </Grid>
        <TextBlock Grid.Row="1" Grid.ColumnSpan="2" Margin="0,20,0,0"
                   Style="{StaticResource Text.Caption}" Text="{Binding StageText}"/>
    </Grid>
</DataTemplate>
```

Stage text is what makes a long job tolerable: "Loading model (first run is slower)",
"Generating chunk 3 of 8", "Writing WAV". A bar with no words leaves the user unsure
whether anything is happening at all.

## Transport bar

Playback for a generated or reference file. Waveform, scrub, time, volume.

```xml
<Border Style="{StaticResource Card}" Padding="12">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <Button Style="{StaticResource Button.Icon}" ToolTip="Play"
                AutomationProperties.Name="Play" Command="{Binding PlayPauseCommand}">
            <TextBlock Style="{StaticResource Text.Icon}" FontSize="18" Text="{Binding PlayGlyph}"/>
        </Button>

        <Grid Grid.Column="1" Margin="12,0,12,0">
            <!-- Waveform behind, scrub slider on top: the slider owns interaction,
                 the waveform is decoration that must not steal hit-testing. -->
            <ItemsControl ItemsSource="{Binding Peaks}" Height="36" IsHitTestVisible="False">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate><UniformGrid Rows="1"/></ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Rectangle Width="2" Margin="1,0,1,0" RadiusX="1" RadiusY="1"
                                   Height="{Binding Height}" VerticalAlignment="Center"
                                   Fill="{DynamicResource Brush.Waveform}"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            <Slider Value="{Binding PositionSeconds}" Maximum="{Binding DurationSeconds}"
                    VerticalAlignment="Center" Background="Transparent"/>
        </Grid>

        <TextBlock Grid.Column="2" Style="{StaticResource Text.Mono}" VerticalAlignment="Center"
                   Text="{Binding TimeDisplay}"/>

        <StackPanel Grid.Column="3" Orientation="Horizontal" Margin="16,0,0,0">
            <TextBlock Style="{StaticResource Text.Icon}" Text="&#xE767;"/>
            <Slider Width="80" Margin="6,0,0,0" Value="{Binding Volume}" Maximum="1"/>
        </StackPanel>
    </Grid>
</Border>
```

For more than a few hundred peaks, draw the waveform with a `DrawingVisual` or a
`StreamGeometry` in a custom `FrameworkElement` rather than one `Rectangle` per sample —
an `ItemsControl` of thousands of rectangles will not scroll or resize smoothly.

## Status bar

28px, caption type, secondary tone. Persistent global state only — never notifications.

```xml
<Border Height="{StaticResource Height.StatusBar}" Style="{StaticResource Panel}"
        BorderThickness="0,1,0,0">
    <Grid Margin="16,0,16,0">
        <StackPanel Orientation="Horizontal">
            <TextBlock Style="{StaticResource Text.Caption}" Text="{Binding EngineName}"/>
            <Border Width="1" Height="12" Margin="12,0,12,0"
                    Background="{DynamicResource Brush.Divider}"/>
            <ContentControl ContentTemplate="{StaticResource StatusPill}" Content="{Binding EngineStatus}"/>
            <Border Width="1" Height="12" Margin="12,0,12,0"
                    Background="{DynamicResource Brush.Divider}"/>
            <TextBlock Style="{StaticResource Text.Caption}" Text="{Binding DeviceDisplay}"/>
        </StackPanel>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <TextBlock Style="{StaticResource Text.Caption}" Text="{Binding QueueSummary}"/>
        </StackPanel>
    </Grid>
</Border>
```

## Dialogs and toasts

**Prefer inline over modal.** A modal interrupts; an inline confirmation in the row
being deleted does not. Reserve dialogs for genuinely blocking decisions and for forms
that need focus (new voice character, first-run setup).

When a dialog is right, host it as an overlay in the main window rather than a separate
`Window` — a real `Window` gets OS chrome you would have to re-theme, and it can be
dragged away from its parent:

```xml
<Grid> <!-- root of the shell -->
    <ContentControl Content="{Binding CurrentPage}"/>

    <Grid Visibility="{Binding IsDialogOpen, Converter={StaticResource BoolToVisibility}}">
        <Rectangle Fill="{DynamicResource Brush.Surface.Base}" Opacity="0.6"/>
        <Border MaxWidth="520" VerticalAlignment="Center" HorizontalAlignment="Center"
                Background="{DynamicResource Brush.Surface.Overlay}"
                BorderBrush="{DynamicResource Brush.Border.Subtle}" BorderThickness="1"
                CornerRadius="{StaticResource Radius.Lg}" Padding="24"
                Effect="{StaticResource Shadow.Dialog}">
            <ContentControl Content="{Binding Dialog}"/>
        </Border>
    </Grid>
</Grid>
```

Dialog buttons bottom-right, primary rightmost, Cancel to its left. Handle Esc to cancel
and Enter to confirm — a dialog that ignores Esc feels stuck.

**Toasts** for transient success ("Saved to Documents\…") — bottom-right, 4 seconds,
fade in over 240ms, dismissible. Never for errors that need action: those belong inline
where the user can act on them, because a toast is gone before anyone has read it.

## Drag and drop

Highlight the drop target on `DragOver` — a dashed accent border and a tinted fill —
and always provide a Browse button alongside. Drag-only affordances are undiscoverable.

```xml
<Border x:Name="DropZone" AllowDrop="True" Style="{StaticResource Well}" MinHeight="96">
    <Border.Style>
        <Style TargetType="Border" BasedOn="{StaticResource Well}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsDragOver}" Value="True">
                    <Setter Property="BorderBrush" Value="{DynamicResource Brush.Accent.Base}"/>
                    <Setter Property="Background" Value="{DynamicResource Brush.Accent.Muted}"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock Style="{StaticResource Text.Hint}" TextAlignment="Center"
                   Text="Drop WAV, MP3 or FLAC here"/>
        <Button Style="{StaticResource Button.Ghost}" Margin="0,8,0,0" Content="Browse…"/>
    </StackPanel>
</Border>
```

Validate on drop and say what was rejected and why — silently ignoring an unsupported
file reads as a broken drop target.

## Density and responsive behaviour

Desktop tools are dense, but density is not the same as cramped. 32px controls, 36px
list rows, 8–12px internal padding, and generous space around the *primary* surface —
the editor, the canvas, the thing the user actually works in.

Minimum window 1024×600. Below ~1200 wide, collapse the nav rail to icons; below ~1000,
collapse the right pane into a tab alongside the centre pane. Use `Grid` with `*` and
`MinWidth` rather than fixed pixel columns, and never let a text column exceed ~620px —
`Text.Prose` caps this already.

Test at 150% display scaling before calling a layout done. Fixed-height rows containing
text are the usual casualty: at 150% the text grows and the row does not, and the
descenders get clipped.
