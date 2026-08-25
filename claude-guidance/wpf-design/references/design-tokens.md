# Design tokens

The token layer is the only place a raw value is allowed to exist. Views reference
tokens; tokens reference nothing. When that rule holds, a theme swap is two lines and
a redesign is one file — and, more importantly, screens built three weeks apart still
look like the same app.

Files: `assets/Theme/Palette.Dark.xaml`, `Palette.Light.xaml` (colors),
`assets/Theme/Tokens.xaml` (everything else).

## Contents

- [Why semantic names](#why-semantic-names)
- [The surface ladder](#the-surface-ladder)
- [Accent discipline](#accent-discipline)
- [Text tones and contrast](#text-tones-and-contrast)
- [State colors](#state-colors)
- [Type scale](#type-scale)
- [Space, radius, control metrics](#space-radius-control-metrics)
- [Motion](#motion)
- [Elevation](#elevation)
- [Adding a token](#adding-a-token)
- [Building a different palette](#building-a-different-palette)

## Why semantic names

`Brush.Surface.Raised` rather than `Brush.Gray20`. A literal name tells you what a
color *is*; a semantic name tells you where it *goes*, which is the thing you actually
need at the moment you're writing a view. Literal names also make theming impossible:
"gray20" cannot be light-theme white without the name becoming a lie, and a codebase
full of lying names is one nobody trusts enough to refactor.

The test for a proposed token: can you state its rule in one sentence ("the background
of a container that sits on top of the app background")? If not, it is probably a
one-off value that belongs inline, or a sign the design needs a decision rather than a
token.

## The surface ladder

Four levels, and on dark they go *up* in lightness as they come toward the viewer:

| Token | Dark | Where |
|---|---|---|
| `Brush.Surface.Sunken` | `#0A0C10` | Wells: text editors, log panes, list backgrounds — things content sits *inside* |
| `Brush.Surface.Base` | `#0F1115` | The window background |
| `Brush.Surface.Raised` | `#161920` | Panels, cards, nav rail |
| `Brush.Surface.Overlay` | `#1D212A` | Popups, menus, dialogs, inputs on raised surfaces |

Plus three interaction states — `Hover`, `Pressed`, `Selected` — which are the ladder
continued by a step or two, not new colors.

**The light theme inverts the logic, not the values.** On light, `Base` is near-white
and depth reads as *recessed* (sunken is darker) plus stronger borders. Mechanically
flipping the dark ordering is what produces the washed-out gray-on-gray light themes
that look like a mistake.

Two adjacent surface levels differ by roughly 6–8 units of lightness. That is enough
to perceive a boundary in peripheral vision and little enough that the screen still
reads as one calm object. Add a 1px `Brush.Border.Subtle` when you need the boundary to
be unambiguous — border plus one step of lightness is the reliable combination.

## Accent discipline

One accent. Four sanctioned uses:

1. The primary action in the current region (one button, not three)
2. The current selection
3. The keyboard focus ring
4. The active navigation item

That's the whole list. Everything else — icons, headings, borders, hover states,
decoration — is neutral. This is the single rule with the largest effect on whether an
app reads as designed or as assembled, because accent color is how the eye is told
where to go, and an interface with eleven accented elements is telling the eye nothing.

Two accent tokens exist for a reason:

- `Brush.Accent.Base` is a **fill**. White text on it clears WCAG AA (4.9:1 dark,
  6.3:1 light).
- `Brush.Accent.Text` is the **text** tone. On dark it is much lighter than the fill,
  because a fill color that works under white text is far too dark to read *as* text
  against a near-black background. Using the fill color for a hyperlink is the most
  common accessibility bug in dark themes.

`Brush.Accent.Muted` is a low-alpha accent for selected chips and tinted rows.

## Text tones and contrast

| Token | Dark value | Contrast on `Surface.Base` | Use |
|---|---|---|---|
| `Text.Primary` | `#E9ECF2` | ~13.9:1 | What the user reads |
| `Text.Secondary` | `#9BA3B4` | ~6.6:1 | Labels, metadata, column headers |
| `Text.Tertiary` | `#7A8395` | ~4.6:1 | Placeholders, hints, timestamps |
| `Text.Disabled` | `#4E5565` | ~2.4:1 | Unavailable — *intentionally* below AA |

Disabled text failing contrast is correct: it must read as unavailable at a glance, and
WCAG explicitly exempts disabled controls. Everything else must clear 4.5:1 for body
text and 3:1 for text at 18.66px+ bold or 24px+.

Four tones is a constraint worth keeping. If a design seems to need a fifth, what it
usually needs is a layout change — grouping, spacing, or a divider — because hierarchy
built from six shades of gray is hierarchy nobody can perceive.

## State colors

`Success` / `Warning` / `Danger` each come in `.Base` (a fill for badges, bars, chips)
and `.Text` (the readable tone on the current background). They appear only where state
genuinely exists — a status dot, a validation message, a failed queue row. A green
"Save" button is not a success state; it is an accent violation.

**Always pair a state color with a word.** Roughly 1 in 12 men has some form of
red-green color vision deficiency, and a green dot next to a red dot is invisible to
them. `● Ready` costs one `TextBlock`.

## Type scale

Six sizes, three weights, one family.

| Token | px | Weight | Use |
|---|---|---|---|
| `Size.Display` 28 | 28 | SemiBold | Page title, once per screen |
| `Size.Title` 20 | 20 | SemiBold | Section heading |
| `Size.Subtitle` 16 | 16 | SemiBold | Card title, group label |
| `Size.Body` 14 | 14 | Regular / SemiBold | Everything |
| `Size.Caption` 12 | 12 | Regular | Metadata, helper text |
| `Size.Micro` 11 | 11 | Regular | Badges, status bar |

Body is 14 because 12 is fatiguing for sustained desktop reading and 15+ starts to feel
like a web page rather than a tool. WPF sizes are device-independent pixels, so these
map 1:1 to CSS px at 100% scaling.

`Tokens.xaml` exposes these as named `TextBlock` styles (`Text.Title`, `Text.Body`,
`Text.Label`, `Text.Caption`, `Text.Hint`, `Text.Prose`, `Text.Mono`, `Text.Icon`), and
`Controls.xaml` makes `Text.Body` the implicit style — so most `TextBlock`s in a view
need no style attribute at all.

### WPF type gotchas

- **There is no letter-spacing.** `TextBlock` has no tracking property. Do not design
  around spaced-out uppercase labels — they cannot be rendered without per-character
  hacks that break selection, wrapping and localization.
- **`FontWeight="Medium"` mostly does nothing.** Segoe UI ships Light/Regular/SemiBold/
  Bold; Medium falls back. Use SemiBold.
- **`TextOptions.TextFormattingMode="Display"`** sharpens small UI text at 100% scaling
  by snapping glyphs to pixels. For large display type or long-form prose, `Ideal` gives
  better spacing. Set it once at the `Window` level and override on the rare exception.
- **`LineHeight` needs `LineStackingStrategy="BlockLineHeight"`** to actually apply.
  Without it WPF silently uses the font's own line spacing.
- **Numbers in tables jitter** unless the font supports tabular figures. Use
  `Font.Mono` for columns of durations, sizes and counts that the user will compare.

## Space, radius, control metrics

A 4px grid: 2 / 4 / 8 / 12 / 16 / 24 / 32 / 48. Named `Space.Xxs` … `Space.Huge` plus
ready-made `Thickness` tokens (`Pad.Page`, `Pad.Card`, `Gap.FormRow`, …).

Defaults worth internalizing, because reaching for the same numbers everywhere is what
makes screens line up:

- Page padding **24**; card padding **16**; gutter between cards **12**
- Label to its control **6**; between form rows **16**; between sections **24**
- Icon to adjacent text **8**; between buttons in a row **8**

Radii: **6** inputs and buttons, **10** cards and panels, **14** dialogs and popovers,
pill for badges and chips. Three plus a pill. A fourth radius on the same screen reads
as an accident, and it usually is one.

Control heights are fixed (**28** compact / **32** default / **40** large) so a row of
a button, a combo box and a text box lines up optically. WPF will not do that for you —
each control's natural height differs by a pixel or two depending on its content.

## Motion

| Token | Duration | Use |
|---|---|---|
| `Duration.Micro` | 90ms | Hover, press, color changes |
| `Duration.Standard` | 150ms | State transitions, toggle knobs |
| `Duration.Enter` | 240ms | Something appearing or leaving |

`Ease.Out` (CubicEase, EaseOut) for entrances — fast start, gentle settle, which reads
as responsive. `Ease.InOut` for things moving between two known positions.

**Animate `Opacity` and `RenderTransform`.** Those are composited. Animating `Width`,
`Height` or `Margin` forces a layout pass every frame and will stutter the moment the
subtree is non-trivial. If a panel must expand, animate a `ScaleTransform` or animate
the `Width` of a *small, layout-isolated* element, not a whole pane.

Anything past ~300ms stops being polish and becomes latency the user has to wait out.

## Elevation

On dark, elevation is lightness, not shadow. `Surface.Raised` on `Surface.Base` reads
as raised with no shadow at all, and costs nothing to render.

Shadows (`Shadow.Popup`, `Shadow.Dialog`) exist for things that genuinely float free of
the layout: popups, menus, dialogs, drag previews. `DropShadowEffect` is a per-element
bitmap effect pass — putting one in an `ItemTemplate` will visibly stutter a list at a
couple of hundred rows, and it is one of the most common causes of "why is my WPF app
slow". `RenderingBias="Performance"` is set on both tokens for this reason.

A popup with a shadow needs `Margin` on its inner border so the blur has room to
render; otherwise the popup clips its own shadow. The `ComboBox` template in
`Controls.xaml` shows the pattern.

## Adding a token

Ask three questions before adding one:

1. **Will this be used in at least three places?** Fewer than three and it is an inline
   value, not a token. A token used once is indirection with no payoff.
2. **Can you name it by role?** If the only honest name is what it looks like
   (`Brush.SlightlyBluerGray`), the design has not made a decision yet.
3. **Does it exist in both palettes?** Every key in `Palette.Dark.xaml` must exist in
   `Palette.Light.xaml`. A missing key throws at runtime on theme swap, and it throws
   in whichever view the user happened to have open.

## Building a different palette

To move off the default dark studio look, change `Palette.*.xaml` only — every style
and view follows automatically.

- **Pick the accent first**, then check it: the fill must clear 4.5:1 against your
  on-accent text color, and the text tone must clear 4.5:1 against `Surface.Base`. If
  the brand color fails, keep the brand color as the *fill* and derive a lighter tint
  for `Accent.Text`. Do not compromise the contrast; nobody notices a 10% hue shift,
  everybody notices unreadable links.
- **Give neutrals a slight hue cast** — the defaults are cool-blue (`#0F1115` is not
  pure gray). Perfectly neutral grays look dead on screen; a 2–4% cast toward the
  accent's hue makes a dark UI feel intentional. Cast *toward* the accent for cohesion,
  *away* from it for contrast.
- **Keep the ladder spacing.** Whatever the hue, keep roughly 6–8 lightness units
  between adjacent surface levels. That spacing, more than the hue, is what makes the
  depth read.
- **Windows 11 Fluent variant:** set `Accent.Base` from `SystemParameters` /
  `UISettings.GetColorValue(UIColorType.Accent)` so the app follows the user's system
  accent, and pair it with a Mica backdrop (see `window-chrome.md`).
