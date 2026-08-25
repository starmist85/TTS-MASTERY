# TTS MASTERY — project guidance

App name: **TTS MASTERY**. A fully local/offline Windows voice-generation studio
(WPF + C#) driving four Python TTS engines as external worker processes.

The authoritative spec is `MASTER DEVELOPMENT PROMPT — LOCAL OFFLINE MULTI-ENGINE TTS
STUDIO.md` in this folder. Read it before making architectural decisions. This file
records the conventions that live outside that document.

## Repository layout

| Path | Contents |
|---|---|
| `project-source/` | **All** solution and source files — the `.sln`, `src/`, `tests/`, `workers/` |
| `tts-libraries/` | Upstream cloned TTS repos (F5-TTS, Kokoro, Coqui-TTS, Fish-Speech). Do not rewrite upstream source. |
| `msi-installers/` | Every installer artifact — WiX project, bootstrapper, scripts, staging, output, manifests. No second installer folder anywhere. |
| `claude-guidance/` | Skills, design system and other Claude-facing guidance kept with the repo |
| `CLAUDE.md` | This file |

Guidance files, notes and tooling that are not application source go at the repo root
or under `claude-guidance/` — never inside `project-source/`.

## UI and design

Use the **`wpf-design` skill** (`claude-guidance/wpf-design/`) for every UI task in this
app — building a screen, styling a control, choosing colors or spacing, adding a dialog.
Treat "add the settings page" as a design task, not only a code task.

Direction: **dark pro-audio studio** — deep cool neutrals, one accent, dense but
breathable, contrast from surface elevation rather than color. Light theme is a palette
swap. The token dictionaries in `claude-guidance/wpf-design/assets/Theme/` are the
starting point; copy them into the app's `Theme/` folder and wire them into `App.xaml`
in the order Palette → Tokens → Controls.

Never type a raw color, size, radius or duration into a view. If a value is missing from
the token layer, add it there.

## Voice library requirements

The Voices screen shows two distinct groups, in this order:

1. **My Voice Characters** — user-created voices with their own reference audio and
   transcripts. These come first and get their own section header.
2. **Built-in engine voices** — voices that ship with an engine, enumerated from the
   installed library at runtime (Kokoro's bundled voice packs are the primary case;
   any engine exposing a fixed voice list belongs here too).

Built-in voices are enumerated from the actual installed engine, never hardcoded. Group
them by engine, show the engine and language on each, and make them selectable in the
generation screen exactly like a user voice character. A user voice character and a
built-in voice are both "a voice you can generate with" from the UI's point of view —
model that in the view-model layer so the selector does not branch on which kind it is.

## Non-negotiables carried from the master prompt

- MVVM with CommunityToolkit.Mvvm; no application logic in `MainWindow.xaml.cs`.
- The UI talks only to `ITtsEngine` abstractions. No engine-specific inference code in
  views or view-models.
- Each Python engine gets its own isolated runtime. Never merge environments to save
  space without proving compatibility.
- All paths resolve through an `AppPaths` service from `AppContext.BaseDirectory`.
  Never rely on the working directory, never hardcode developer paths.
- User data lives under `%LOCALAPPDATA%\LocalTtsStudio\`, never in Program Files.
- Nothing downloads from the internet at runtime. A missing model says "Model not
  installed"; it does not fetch.
- Before implementing any engine adapter, inspect the actual cloned repo under
  `tts-libraries/` and use the API that version exposes. Do not infer CLI arguments
  from documentation for a different release.

## Git

Origin: https://github.com/starmist85/TTS-MASTERY

`tts-libraries/` upstream clones and everything generated (`bin/`, `obj/`, `runtimes/`,
`staging/`, `output/`, model weights) stay out of the repo — see `.gitignore`.
