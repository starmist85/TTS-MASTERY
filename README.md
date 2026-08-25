# TTS MASTERY

A fully local, fully offline voice-generation studio for Windows.

TTS MASTERY is a WPF desktop application that drives four open-source text-to-speech
engines running on your own machine. It gives them one interface, one voice library and
one workflow — the kind of experience you get from ElevenLabs or Fish Audio, with
nothing leaving your PC and no account, API key or internet connection involved.

> **Status: in development.** The application architecture is being built first, with a
> mock engine so the whole workflow can be exercised before any Python runs. Engines are
> integrated one at a time, and each one is packaged and installed on a clean machine
> before the next is started. See [Roadmap](#roadmap).

---

## What it does

- **Four engines, one interface.** F5-TTS, Kokoro, Coqui XTTS v2 and Fish Speech, each
  in its own isolated Python runtime, all driven through the same UI.
- **Voice Character library.** Save a speaker once — reference recordings, transcript,
  default language, preferred engine — and reuse it across generations and engines.
- **Built-in engine voices.** Voices that ship with an engine (Kokoro's voice packs, for
  example) are enumerated from your installed copy and listed alongside your own voices.
- **Human-readable languages.** You pick "English (UK)"; the app translates that into
  whatever code each engine happens to want. If an engine cannot do the language you
  chose, it says so rather than quietly substituting another.
- **Engine-aware controls.** The options panel is built from each engine's declared
  capabilities, so you see the parameters that engine actually supports and nothing else.
- **Generation queue and history.** Every generation is recorded with its settings, so
  you can replay, tweak or regenerate later. Output files are never overwritten.
- **Built-in playback.** Reference recordings and generated audio play inside the app.
- **Genuinely offline.** After installation there is no network call, no Hugging Face
  fetch, no cloud fallback. A missing model says "Model not installed"; it does not
  quietly download three gigabytes.

---

## Requirements

| | |
|---|---|
| OS | Windows 10 (21H2+) or Windows 11, x64 |
| GPU | NVIDIA with recent drivers recommended. CPU works where the engine supports it, slower. |
| VRAM | 6 GB comfortable for most engines; Fish Speech wants more |
| Disk | Tens of GB — model weights dominate |
| .NET | Nothing to install. The release ships a self-contained .NET runtime. |
| Python | Nothing to install. Each engine ships its own runtime inside the package. |

---

## Repository layout

```
TTS MASTERY/
├── project-source/          Solution and all application source
│   ├── LocalTtsStudio.sln
│   ├── src/
│   │   ├── LocalTtsStudio.App/              WPF views, view-models, theme
│   │   ├── LocalTtsStudio.Core/             Entities, interfaces, DTOs, contracts
│   │   ├── LocalTtsStudio.Infrastructure/   SQLite, repositories, configuration, paths
│   │   ├── LocalTtsStudio.Tts/              Engine manager, adapters, worker protocol
│   │   └── LocalTtsStudio.Audio/            Playback, metadata, conversion
│   ├── tests/
│   ├── workers/             Python workers wrapping each engine
│   ├── scripts/             Runtime build scripts
│   └── docs/
├── tts-libraries/           Upstream engine repositories (not tracked in git)
├── msi-installers/          WiX project, bootstrapper, staging, manifests, output
├── claude-guidance/         Design system and development guidance
│   └── wpf-design/          WPF design-system skill: tokens, templates, patterns
├── CLAUDE.md                Conventions for AI-assisted development on this repo
└── README.md
```

`tts-libraries/`, Python runtimes, model weights and build output are excluded from git —
they are large, they are reproducible from scripts, and they are not ours to vendor.

---

## Architecture

The WPF application never imports a Python library. It talks to engines through an
abstraction, and each engine lives behind an adapter that owns a separate Python process:

```
WPF application (MVVM)
        │
   TTS Engine Manager
        │
   ITtsEngine  ──────  F5TtsEngine · KokoroTtsEngine · XttsEngine · FishSpeechEngine
                              │
                    local Python worker process  (JSON over stdin/stdout)
                              │
                    the actual Python TTS library
```

Three consequences worth stating, because they shape everything else:

**Engines are isolated, deliberately.** F5-TTS, Kokoro, XTTS and Fish Speech disagree
about Python versions, PyTorch builds, transformers, tokenizers and NumPy. Coqui TTS
needs Python `>=3.9,<3.12`; Kokoro needs `>=3.10,<3.14`; Fish Speech needs `>=3.10`.
There is no single environment that satisfies all four, so each gets its own runtime.
The package is larger for it. Reliability is worth more than gigabytes.

**Workers speak a normalized protocol.** The C# side does not learn four different
command-line syntaxes. It sends the same JSON request shape to every worker and receives
the same status/progress/completed/error messages back. Diagnostic chatter goes to
stderr; only machine-readable messages go to stdout. Workers stay alive between
generations so multi-gigabyte models are not reloaded every time you press Generate.

**The UI knows nothing engine-specific.** No view contains F5, Kokoro, XTTS or Fish
logic. Adding a fifth engine means writing an adapter, a worker, a capability
declaration and a language mapping — not editing screens.

---

## Building from source

**You need:** Visual Studio 2022 (17.8+) with the *.NET desktop development* workload,
or the .NET 8 SDK with `dotnet build`.

```powershell
git clone https://github.com/starmist85/TTS-MASTERY.git
cd "TTS-MASTERY\project-source"
dotnet restore
dotnet build -c Debug
dotnet run --project src\LocalTtsStudio.App
```

The application runs against a mock engine until the Python runtimes are built, so you
can develop and test the entire UI workflow without any engine installed.

To fetch the upstream engine repositories into `tts-libraries/`:

```powershell
gh repo clone SWivid/F5-TTS      "tts-libraries/F5-TTS-main"
gh repo clone hexgrad/kokoro     "tts-libraries/kokoro-main"
gh repo clone fishaudio/fish-speech "tts-libraries/fish-speech-main"
gh repo clone coqui-ai/TTS       "tts-libraries/TTS-dev"
```

Python runtimes are built by the per-engine scripts under `project-source/scripts/`.
They are reproducible builds intended for deployment — not copies of a developer's
virtual environment, which do not survive being moved to another machine.

---

## Building the installer

Everything installer-related lives under `msi-installers/`. The whole release is one
command:

```powershell
.\msi-installers\scripts\build-msi.ps1
```

That publishes the app self-contained for `win-x64`, builds and verifies the Python
runtimes, stages workers, engines, models, FFmpeg and licences into a deterministic
staging tree, generates the manifests, verifies the package, and produces:

```
msi-installers/output/
    LocalTtsStudio-x64-1.0.0.msi
    LocalTtsStudio-Setup-x64-1.0.0.exe
```

The MSI is always an independent artifact; the bootstrapper only adds prerequisite
handling on top of it.

### Where things are installed

| | |
|---|---|
| Application, runtimes, engines, workers, tools | `C:\Program Files\Local TTS Studio\` |
| Voice characters, database, generations, logs, settings | `%LOCALAPPDATA%\LocalTtsStudio\` |

Upgrades replace the first and never touch the second. Uninstall removes program files
and leaves your voices and generated audio alone.

---

## Design

The interface is built as a design system, not as a set of screens. Tokens define
colour, type, spacing, radius and motion; every control is templated from those tokens;
no view contains a raw value. The direction is a dark pro-audio studio look — deep cool
neutrals, one accent, dense but breathable, with contrast carried by surface elevation
rather than colour. A light theme is a palette swap.

The full system, including the reasoning and the drop-in `ResourceDictionary` files,
lives in [`claude-guidance/wpf-design/`](claude-guidance/wpf-design/).

---

## Roadmap

| Phase | Work |
|---|---|
| 1 | Solution architecture, MVVM shell, navigation, DI, logging, SQLite, core contracts |
| 2 | Voice Character library, audio import, transcript editing, playback, language service, history |
| 3 | `ITtsEngine`, capability system, worker protocol, mock engine — full workflow, no Python |
| 4 | First MSI: application only, verified launching from Program Files |
| 5–8 | Kokoro, then F5-TTS, then XTTS v2, then Fish Speech — each packaged and clean-machine tested before the next |
| 9 | Model manager, generation queue, diagnostics, hardware detection, memory policy, cache |
| 10 | Production MSI and bootstrapper, full offline acceptance test on a clean machine |

Each engine is packaged and installed on a clean machine as soon as it works. Waiting
until the end to discover that a worker only ran because of something on the development
machine is the failure mode this ordering exists to prevent.

---

## Licensing

TTS MASTERY is a front end. The engines it drives are separate projects with their own
licences, and those licences govern what you may do with their models and their output:

| Project | |
|---|---|
| [F5-TTS](https://github.com/SWivid/F5-TTS) | Check the repository — model weights and code may differ |
| [Kokoro](https://github.com/hexgrad/kokoro) | Apache 2.0 |
| [Coqui TTS / XTTS v2](https://github.com/coqui-ai/TTS) | MPL 2.0 code; XTTS weights under the Coqui Public Model Licence |
| [Fish Speech](https://github.com/fishaudio/fish-speech) | Check the repository — terms have changed between releases |

Distributed packages keep every upstream licence and attribution file under
`licenses/`. Read them before redistributing anything, and before using generated audio
commercially.

**On voice cloning:** these engines can reproduce a voice from a short recording. Only
clone voices you have permission to clone. Impersonation causes real harm, and in many
jurisdictions it is also illegal.
