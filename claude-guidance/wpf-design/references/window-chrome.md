# Window chrome, DPI and icons

The window frame is the first thing a user sees and the last thing anyone remembers to
style. A carefully themed dark app with a bright white caption bar looks broken in every
screenshot, and it is a four-line fix.

## Contents

- [Dark title bar (the four-line fix)](#dark-title-bar-the-four-line-fix)
- [Mica and acrylic backdrops](#mica-and-acrylic-backdrops)
- [Custom chrome with WindowChrome](#custom-chrome-with-windowchrome)
- [DPI and crispness](#dpi-and-crispness)
- [Icon fonts and glyphs](#icon-fonts-and-glyphs)
- [Window sizing and state](#window-sizing-and-state)

## Dark title bar (the four-line fix)

Windows 10 build 18985+ and Windows 11 honour `DWMWA_USE_IMMERSIVE_DARK_MODE`. Call it
after the window has an HWND — that is `SourceInitialized`, not the constructor.

```csharp
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

public static class WindowChromeHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE     = 38;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    /// <summary>
    /// Call from Window.SourceInitialized, and again whenever the app theme changes.
    /// Silently no-ops on Windows versions that do not know the attribute.
    /// </summary>
    public static void ApplyDarkTitleBar(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;          // no HWND yet — too early

        int value = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }
}
```

```csharp
protected override void OnSourceInitialized(EventArgs e)
{
    base.OnSourceInitialized(e);
    WindowChromeHelper.ApplyDarkTitleBar(this, themeService.Current == AppTheme.Dark);
}
```

Two things to know:

- On Windows 10 versions **before** 18985 the attribute id was `19`, not `20`. If you
  support older builds, try 20 and fall back to 19 when it returns a non-zero HRESULT.
- The title bar does not repaint retroactively in every build. If it does not change
  immediately after a theme swap, nudge it — toggling `WindowState` or resizing by a
  pixel forces a repaint. Doing this on theme change only is fine; do not do it on every
  window activation.

## Mica and acrylic backdrops

Windows 11 build 22621+ supports system backdrops via `DWMWA_SYSTEMBACKDROP_TYPE`:
1 = Auto, 2 = Mica, 3 = Acrylic, 4 = Mica Alt (tabbed).

```csharp
public static void ApplyMica(Window window, bool micaAlt = false)
{
    var hwnd = new WindowInteropHelper(window).Handle;
    if (hwnd == IntPtr.Zero) return;

    int backdrop = micaAlt ? 4 : 2;
    DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
}
```

For the backdrop to be *visible*, the window must actually be transparent behind your
content — `Background="Transparent"` on the `Window`, with your surfaces painted on
panels above it. That means the app's real background becomes the desktop wallpaper,
blurred and tinted.

**Decide whether you want that.** Mica makes a utility feel native and integrated; it
also makes a dark studio tool's canvas subtly vary with whatever wallpaper the user has,
which is exactly wrong for anything involving color judgement — image editors, video
tools, waveform displays. A good compromise is Mica on the shell chrome (nav rail, title
area) with an opaque canvas.

Rounded window corners are automatic on Windows 11. `DWMWA_WINDOW_CORNER_PREFERENCE`
(2 = round, 3 = round-small, 1 = don't round) exists if you need to override.

## Custom chrome with WindowChrome

When the design calls for tabs, a search box or a command bar *in* the title area, take
the chrome over. `WindowChrome` keeps the OS resize borders, snap layouts and Aero Snap
behaviour while letting you draw the bar yourself.

```xml
<Window ...
        Background="{DynamicResource Brush.Surface.Base}"
        UseLayoutRounding="True" SnapsToDevicePixels="True">
    <WindowChrome.WindowChrome>
        <WindowChrome CaptionHeight="40"
                      ResizeBorderThickness="6"
                      CornerRadius="0"
                      GlassFrameThickness="0"
                      UseAeroCaptionButtons="False"/>
    </WindowChrome.WindowChrome>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="{StaticResource Height.TitleBar}"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <Grid Grid.Row="0" Background="{DynamicResource Brush.Surface.Raised}">
            <StackPanel Orientation="Horizontal" Margin="16,0,0,0" VerticalAlignment="Center">
                <TextBlock Style="{StaticResource Text.BodyStrong}" Text="TTS Mastery"/>
            </StackPanel>

            <!-- Interactive content inside the caption area MUST opt out of the drag
                 region, or clicks become window drags. -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
                        WindowChrome.IsHitTestVisibleInChrome="True">
                <Button Style="{StaticResource CaptionButton}" Content="&#xE921;"
                        Command="{Binding MinimizeCommand}" AutomationProperties.Name="Minimize"/>
                <Button Style="{StaticResource CaptionButton}" Content="&#xE922;"
                        Command="{Binding MaximizeCommand}" AutomationProperties.Name="Maximize"/>
                <Button Style="{StaticResource CaptionButton.Close}" Content="&#xE8BB;"
                        Command="{Binding CloseCommand}" AutomationProperties.Name="Close"/>
            </StackPanel>
        </Grid>

        <ContentControl Grid.Row="1" Content="{Binding Shell}"/>
    </Grid>
</Window>
```

Caption buttons follow Windows conventions, and users notice when they don't: 46×32
each, glyphs from Segoe Fluent Icons (`E921` minimize, `E922` maximize, `E923` restore,
`E8BB` close), hover is a neutral tint except Close which goes `#E81123`. Order is always
minimize, maximize, close, top-right.

Three things custom chrome breaks if you are not careful:

- **Maximize clips content.** A maximized `WindowChrome` window extends past the work
  area by the resize border. Handle `WM_GETMINMAXINFO`, or set
  `Padding="8"` on the root grid when `WindowState="Maximized"` via a trigger.
- **Snap Layouts** (hovering the maximize button on Windows 11) needs the maximize
  button to respond to `WM_NCHITTEST` with `HTMAXBUTTON`. Without hooking that, the
  feature silently disappears — which is a real regression for keyboard/window-manager
  users.
- **Double-click to maximize and the system menu** on right-click come free with
  `CaptionHeight`, but only in areas not marked `IsHitTestVisibleInChrome`.

If none of that is worth it — and often it isn't — keep the OS title bar and just make
it dark. That is the pragmatic default.

## DPI and crispness

Set these on the `Window` and they cascade:

```xml
UseLayoutRounding="True"
SnapsToDevicePixels="True"
TextOptions.TextFormattingMode="Display"
TextOptions.TextRenderingMode="ClearType"
```

- `UseLayoutRounding` rounds layout positions to whole device pixels. Without it, a 1px
  border at 150% scaling lands on a half-pixel and renders as a blurry 2px smear. This
  is the main reason WPF apps sometimes feel slightly out of focus.
- `SnapsToDevicePixels` does the same for rendering primitives; set it on individual
  `Border`s with hairline strokes too.
- `TextFormattingMode="Display"` snaps glyphs to the pixel grid — better for small UI
  text at 100%. Switch to `Ideal` for large display type or long-form prose, where glyph
  spacing matters more than stem sharpness.

Ensure the app is per-monitor DPI aware, or it will be bitmap-stretched (and blurry) when
dragged to a second monitor with different scaling:

```xml
<PropertyGroup>
  <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
</PropertyGroup>
```

Or, for older SDKs, an `app.manifest` with
`<dpiAwareness>PerMonitorV2</dpiAwareness>`. Then actually drag the window between two
monitors at different scale factors and look at it.

Use vector `Path` or icon fonts rather than bitmaps. If a raster image is unavoidable,
ship 1x/1.5x/2x and set `RenderOptions.BitmapScalingMode="HighQuality"`.

## Icon fonts and glyphs

**Segoe Fluent Icons** (Windows 11) with **Segoe MDL2 Assets** (Windows 10) as fallback —
`Font.Icon` in `Tokens.xaml` lists both, and WPF resolves left to right.

```xml
<TextBlock Style="{StaticResource Text.Icon}" Text="&#xE768;"/>
```

Codepoints worth memorizing for a media/AI tool:

| Glyph | Code | | Glyph | Code |
|---|---|---|---|---|
| Play | `E768` | | Settings | `E713` |
| Pause | `E769` | | Refresh | `E72C` |
| Stop | `E71A` | | Delete | `E74D` |
| Save | `E74E` | | Add | `E710` |
| Folder | `E8B7` | | Cancel/Close | `E711` |
| Open file | `E8E5` | | Search | `E721` |
| Microphone | `E720` | | Favorite (star) | `E734`/`E735` |
| Volume | `E767` | | More (…) | `E712` |
| Chevron down | `E70D` | | Info | `E946` |
| Checkmark | `E73E` | | Warning | `E7BA` |

Rules that keep icons from looking amateur:

- **Never emoji.** They render in color, at the wrong optical weight, and differ per
  machine and per font fallback.
- **Never an icon alone as the only label for an unfamiliar action.** Icon + text for
  primary actions; icon + `ToolTip` + `AutomationProperties.Name` for dense toolbars.
- **One size per context.** 16 in toolbars and rows, 20 for primary actions, 32+ only in
  empty states. Mixed icon sizes in one row is the most visible kind of misalignment.
- **Optical, not mathematical, centring.** Play triangles look off-centre when centred
  by bounding box; nudge 1px right.

If a glyph font is too limited, use vector `Path` data from an open icon set (Lucide,
Fluent System Icons) as `Geometry` resources. Do not mix two icon *families* in one app —
different stroke weights and corner treatments read as inconsistency even when nobody
can articulate why.

## Window sizing and state

```xml
Height="820" Width="1360" MinHeight="600" MinWidth="1024"
WindowStartupLocation="CenterScreen"
```

Persist size, position and maximized state to user settings, and restore on launch —
but validate the restored rectangle against the current monitor layout before applying
it. A window restored onto a monitor that is no longer connected is invisible, and the
user's only recourse is deleting a settings file they don't know about.

```csharp
var virtualScreen = new Rect(
    SystemParameters.VirtualScreenLeft,  SystemParameters.VirtualScreenTop,
    SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

if (!virtualScreen.IntersectsWith(savedBounds))
    savedBounds = null;   // fall back to CenterScreen
```
