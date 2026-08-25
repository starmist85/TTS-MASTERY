# MASTER DEVELOPMENT PROMPT — LOCAL OFFLINE MULTI-ENGINE TTS STUDIO

I want you to design and implement a Windows desktop application that acts as a fully local/offline voice-generation studio similar in workflow to ElevenLabs, FakeYou, Fish Audio, etc., but running locally on the user's PC.

The application must use a WPF-style C# desktop interface and integrate four local text-to-speech systems:

- F5-TTS
- Kokoro
- Coqui XTTS v2
- Fish Speech

The repositories for these projects already exist underneath:

`tts-libraries/`

Do not replace these repositories unless absolutely necessary.

The application should regard these Python projects as external TTS engines controlled by the main C# application.

The finished program must be capable of being packaged as an offline Windows installer.

The application should be modular, maintainable, strongly typed, asynchronous, and designed so additional TTS engines can later be added without rewriting the UI.

---

# 1. REQUIRED TECHNOLOGY

Create the primary desktop application using:

- C#
- WPF
- modern .NET compatible with Visual Studio 2022
- x64 target only
- MVVM architecture

Prefer a current long-supported/stable .NET target supported by Visual Studio 2022 rather than .NET Framework.

Do NOT build the application using WinForms.

Do NOT implement the entire application inside MainWindow.xaml.cs.

Use dependency injection and clear separation between:

- UI
- ViewModels
- application services
- persistence
- engine adapters
- Python processes
- audio handling

Recommended NuGet packages where appropriate:

CommunityToolkit.Mvvm

Microsoft.Extensions.Hosting

Microsoft.Extensions.DependencyInjection

Microsoft.Extensions.Configuration

Microsoft.EntityFrameworkCore

Microsoft.EntityFrameworkCore.Sqlite

NAudio

Serilog

Serilog.Sinks.File

System.Text.Json

Use additional packages only where they provide a clear benefit.

---

# 2. FUNDAMENTAL ARCHITECTURE

The C# WPF application must NOT directly import the Python TTS libraries.

Instead build an engine adapter architecture.

Use:

WPF Application
↓
TTS Engine Manager
↓
ITtsEngine interface
↓
individual engine implementations
↓
local Python worker processes
↓
actual Python TTS libraries

Create adapters such as:

F5TtsEngine

KokoroTtsEngine

XttsEngine

FishSpeechEngine

All four must implement the same core interface.

Example conceptual interface:

`ITtsEngine`

Properties:

EngineId

DisplayName

IsInstalled

IsAvailable

SupportsVoiceCloning

SupportsReferenceTranscript

SupportsLanguageSelection

SupportsMultipleReferenceFiles

SupportsStreaming

SupportsSeed

SupportsSpeedControl

SupportsTemperature

Capabilities

Methods:

InitializeAsync()

CheckEnvironmentAsync()

GenerateAsync()

CancelAsync()

GetAvailableLanguagesAsync()

GetAvailableVoicesAsync()

GetEngineSettingsAsync()

ValidateRequestAsync()

ShutdownAsync()

The UI must communicate only with ITtsEngine abstractions.

The UI must never contain F5-specific, Kokoro-specific, XTTS-specific, or Fish-specific inference code.

---

# 3. PYTHON ENGINE ISOLATION

DO NOT install all four engines into one Python environment.

Each engine must have its own environment.

Conceptually:

runtime/
    f5/
    kokoro/
    xtts/
    fish/

Each environment must be independently executable.

This prevents dependency conflicts involving:

- Python versions
- PyTorch
- CUDA
- Transformers
- tokenizers
- phonemizers
- NumPy
- inference libraries

The application should locate each engine environment through configuration.

Example:

engines.json

with entries containing:

EngineId

RepositoryPath

PythonExecutable

WorkerScript

ModelPath

Enabled

EnvironmentVersion

---

# 4. PYTHON WORKER PROTOCOL

Create our own small Python wrapper/worker around each library.

Do NOT force the C# side to understand the native command-line syntax of every project.

Create something like:

workers/
    common/
    f5_worker.py
    kokoro_worker.py
    xtts_worker.py
    fish_worker.py

Use a normalized request/response protocol.

Prefer either:

A. JSON messages through stdin/stdout

or

B. a localhost-only HTTP API

For the first implementation, prefer JSON stdin/stdout unless an engine works significantly better as a persistent HTTP worker.

The protocol should allow a worker to remain alive so models do not reload for every generation.

Example normalized request:

{
  "requestId": "...",
  "command": "generate",
  "engine": "f5",
  "text": "Hello world",
  "language": "en-US",
  "referenceAudio": "...",
  "referenceTranscript": "...",
  "outputFile": "...",
  "settings": {}
}

Example worker responses:

{
  "requestId": "...",
  "type": "status",
  "message": "Loading model"
}

{
  "requestId": "...",
  "type": "progress",
  "value": 0.40
}

{
  "requestId": "...",
  "type": "completed",
  "outputFile": "..."
}

And errors:

{
  "requestId": "...",
  "type": "error",
  "code": "CUDA_OUT_OF_MEMORY",
  "message": "..."
}

Never parse random console text as the primary protocol.

Normal diagnostic output should go to stderr.

Machine-readable responses should go to stdout.

---

# 5. ENGINE CAPABILITY SYSTEM

The UI must dynamically change according to the selected engine.

Do not hard-code a giant collection of:

if engine == F5

inside the UI.

Create an EngineCapabilities model.

Potential fields:

SupportsReferenceAudio

SupportsReferenceTranscript

SupportsLanguage

SupportsBuiltInVoices

SupportsVoiceCloning

SupportsMultipleReferences

SupportsSpeed

SupportsPitch

SupportsTemperature

SupportsTopP

SupportsTopK

SupportsSeed

SupportsCfg

SupportsStreaming

SupportsTraining

SupportsFineTuning

SupportsEmotion

SupportsStyle

SupportsSpeakerSelection

SupportsModelSelection

When an engine is selected, show only controls relevant to that engine.

Example:

If F5-TTS requires/reference-supports:

Reference Audio
Reference Transcript
Speed
Model
Seed or inference options

show those.

If Kokoro primarily uses:

Language
Voice
Speed

show those instead.

If XTTS uses:

Reference Audio
Language
Speaker
voice cloning settings

show those.

Fish Speech should expose the options supported by our installed version of Fish Speech.

Build settings panels dynamically from engine capabilities.

---

# 6. LANGUAGE NORMALIZATION LAYER

Implement a central language system.

The UI must NEVER display confusing engine-specific language IDs as the primary language selector.

Use canonical internal language identifiers.

Example canonical values:

en-US
en-GB
no-NO
sv-SE
da-DK
de-DE
fr-FR
es-ES
it-IT
pt-BR
pl-PL
nl-NL
cs-CZ
ru-RU
tr-TR
ar
zh-CN
ja-JP
ko-KR
hi-IN

Create:

ILanguageMapper

and engine-specific mappings.

Example:

English (US)

canonical:
en-US

XTTS:
en

Kokoro:
a

F5:
whatever is appropriate or null if the engine does not need a code

Fish:
mapped appropriately or null

English (UK):

canonical:
en-GB

XTTS:
en

Kokoro:
b

This means the GUI always operates on canonical language IDs while each TTS adapter converts them into the format expected by its engine.

If an engine does not support a selected language, the GUI must clearly show that it is unavailable.

Do NOT silently substitute another language.

Create a model such as:

LanguageDefinition

DisplayName

CanonicalCode

EngineMappings

IsSupported(engine)

MapForEngine(engine)

The mappings should be data-driven where practical so they can be updated later.

---

# 7. VOICE CHARACTER LIBRARY

A major component of the application is a persistent Voice Character Library.

A Voice Character represents a reusable speaker/voice identity.

Voice Character fields should include at minimum:

Id / GUID

Name

Description

CreatedDate

ModifiedDate

DefaultLanguage

PreferredEngine

ReferenceTranscript

ReferenceAudioFiles

Thumbnail or avatar path, optional

Tags

Notes

Engine-specific metadata

Favorite flag

LastUsed

GenerationCount

A character MAY have multiple reference recordings.

Store database metadata in SQLite.

Do not store large WAV files inside SQLite blobs.

Store audio files on disk and references to them in SQLite.

Suggested user data directory:

%LOCALAPPDATA%\LocalTtsStudio\

or similarly appropriate application data folder.

Example:

Data/
    database/
        voices.db

    voices/
        {voice-guid}/
            metadata.json
            references/
                reference_001.wav
                reference_002.wav
            avatar.png

    generations/

    cache/

    logs/

    models/

A voice character should survive application updates.

Never store user-generated voice characters inside Program Files.

---

# 8. VOICE CHARACTER MANAGEMENT UI

Create a Voice Library screen.

It should resemble a modern content/model browser.

Provide:

Search

Sort

Filter

Favorites

Cards or list mode

Each voice character card should display:

Name

Optional avatar

Language

Preferred engine

Number of reference recordings

Last modified

Play reference button

Edit button

Delete button

Use Voice button

Opening a character displays an editor.

Voice Character Editor:

Name

Description

Default language

Preferred TTS engine

Transcript

Reference audio recordings

Add Reference

Replace Reference

Remove Reference

Play Reference

Rename

Tags

Notes

Save

Delete

For each reference audio item display:

filename

duration

sample rate

channels

created/added date

transcript if reference-specific transcripts are supported

Allow drag-and-drop WAV/MP3/FLAC audio imports where practical.

Imported audio should be normalized/copied into the application's voice storage rather than permanently depending on the original source path.

---

# 9. NEW VOICE CHARACTER WORKFLOW

Provide a prominent:

+ New Voice Character

button.

Workflow:

1. Enter character name.

2. Choose default language.

3. Choose preferred engine if desired.

4. Import reference audio.

5. Enter transcript of the reference recording.

6. Preview the recording.

7. Save character.

The transcript must remain editable later.

The audio must remain replaceable later.

Support multiple reference files structurally even if a particular engine initially only uses one.

Engine adapters should determine how many reference files are actually passed during inference.

---

# 10. AUDIO IMPORT / PREPROCESSING

Implement an audio preprocessing service.

Use FFmpeg if appropriate.

Bundle or locate FFmpeg through a dedicated service.

The service should be able to produce engine-compatible temporary reference audio.

For example:

mono

PCM WAV

specific sample rate

normalized amplitude where appropriate

trim invalid leading/trailing sections if explicitly selected

Do NOT destructively overwrite the original imported reference.

Keep:

Original user reference

and optionally

engine-specific processed/cache version

separate.

Create something like:

IAudioPreprocessingService

PrepareReferenceAsync(
    originalFile,
    engineRequirements
)

Cache processed audio by hash so it does not need to be converted repeatedly.

---

# 11. MAIN GENERATION SCREEN

The primary generation screen should contain roughly this layout:

LEFT PANEL:

Engine selector

Voice Character selector/library shortcut

Language selector

Engine-specific options

CENTER PANEL:

Large text input editor

RIGHT/BOTTOM:

Generation configuration

Output folder

Filename

Generate button

Cancel button

Progress

Generated output preview

History

Exact layout may be improved according to good WPF UX conventions.

---

# 12. ENGINE SELECTOR

Create a clear selector for:

F5-TTS

Kokoro

XTTS v2

Fish Speech

Show status for each:

Ready

Not configured

Missing model

Missing Python environment

CUDA unavailable

Error

Initializing

Loading model

Do not crash if one engine is unavailable.

The other engines must remain usable.

Provide a diagnostic/details button.

---

# 13. CHARACTER SELECTION DURING GENERATION

The user should be able to choose:

No saved character

or

a saved Voice Character.

If selecting an existing voice character:

load its reference audio

load its transcript

load its default language

load its preferred engine if desired

However, allow overriding these values for the current generation without modifying the stored character unless the user explicitly selects Save Changes.

Provide:

Edit Voice Character

button directly from generation UI.

---

# 14. UNSAVED / ONE-OFF REFERENCE AUDIO

For engines supporting reference audio, allow a generation without first creating a Voice Character.

Provide:

Reference Audio:
[ Browse ]

Reference Transcript:
[ text field ]

Optional:

[ Save as Voice Character ]

Therefore users can quickly test an audio reference and only save it if useful.

---

# 15. MAIN TEXT INPUT

Provide a large multi-line text editor.

Features:

word count

character count

clear button

paste support

Undo/Redo

Ctrl+A

Ctrl+Enter or configurable shortcut to Generate

Keep entered text when switching engines unless explicitly cleared.

Allow large text.

Later architecture should permit text chunking.

---

# 16. OUTPUT FILE SYSTEM

Provide output folder selector.

Remember the last output directory.

Default to something sensible such as:

Documents\Local TTS Studio\Generated

Provide filename input.

Example:

Filename:

narrator_test

If output already exists:

narrator_test.wav

automatically create:

narrator_test_002.wav

then:

narrator_test_003.wav

etc.

Never overwrite automatically.

Implement:

IOutputFileNamingService

Method such as:

GetNextAvailableFilename(
 directory,
 baseName,
 extension
)

Sanitize invalid Windows filename characters.

---

# 17. GENERATION HISTORY

Store generation history.

Fields:

Id

Timestamp

Engine

Voice Character

Language

Input Text

Output File

Duration

Generation Time

Settings JSON

Success/Failure

Error details where appropriate

Allow:

Play

Open folder

Regenerate

Duplicate settings

Delete history record

Delete output file

Load text back into editor

History can be stored in SQLite.

---

# 18. AUDIO PLAYER

Implement built-in playback using NAudio or another good .NET audio library.

Player should support:

Play

Pause

Stop

Seek

Current time

Duration

Volume

Replay

Play reference recording

Play generated output

Do not launch an external player for normal playback.

Provide:

Open File Location

as a separate function.

---

# 19. GENERATION QUEUE

Do not freeze the UI during generation.

Use async/await throughout.

Implement a Generation Queue.

Each queue item should contain:

engine

voice

text

settings

language

output destination

status

progress

Start time

finish time

Support:

Pending

Preparing

Loading Engine

Generating

Saving

Completed

Failed

Cancelled

Initially run one GPU generation job at a time.

Architect it so concurrency could be added later.

---

# 20. MODEL LIFETIME MANAGEMENT

Loading large TTS models repeatedly is inefficient.

Workers should support persistent execution.

When F5 is selected and used:

start F5 worker

load model

keep worker alive

When switching engines:

either retain the existing worker if memory allows

or unload according to application settings.

Create a configurable Model Memory Policy:

Conservative

Balanced

Performance

Conservative:

Unload unused model when switching engines.

Balanced:

Keep most recently used model loaded if sufficient VRAM.

Performance:

Keep workers/models loaded where practical.

GPU memory errors should trigger a graceful error and optionally suggest unloading other engines.

---

# 21. HARDWARE DETECTION

Create hardware diagnostics.

Detect:

Windows version

CPU

system RAM

GPU name

GPU VRAM

NVIDIA driver availability

CUDA/PyTorch availability per engine

Python environment health

disk space

Display this under:

Settings → System / Diagnostics

Provide:

Test Engines

button.

Test each worker independently.

Results:

F5-TTS: Ready

Kokoro: Ready

XTTS: Missing checkpoint

Fish Speech: Insufficient VRAM / initialization failed

etc.

Do not assume CUDA exists.

Allow CPU mode where the underlying engine supports it.

---

# 22. ENGINE SETTINGS SYSTEM

Each adapter should expose configuration metadata to the UI.

Create generalized setting definitions.

For example:

EngineSettingDefinition

Id

DisplayName

Description

Type

DefaultValue

Minimum

Maximum

Step

Choices

Advanced

Supported

Types could include:

Boolean

Integer

Double

String

Enum

File

Folder

Slider

This allows engine-specific controls to be rendered dynamically.

Example F5:

Speed

CFG strength

NFE steps

Sway sampling coefficient

Seed where supported

Model

Vocoder

Remove silence

Example Kokoro:

Voice

Speed

Language-related options

Example XTTS:

Language

Temperature

Length penalty

Repetition penalty

Top-K

Top-P

Speed

speaker/reference settings

Example Fish:

Expose parameters that the installed Fish Speech version actually supports.

IMPORTANT:

Before implementing an engine's settings, inspect the actual cloned repository under `tts-libraries` and determine the options supported by THAT VERSION.

Do not hallucinate CLI arguments.

Do not assume documentation for another release matches the local repository.

---

# 23. ADVANCED SETTINGS

Separate simple and advanced controls.

Normal users should initially see:

Engine

Voice

Language

Text

Output location

Filename

Generate

Common speed/quality controls

Advanced panel may show:

sampling parameters

seed

temperature

model path

device

precision

chunk settings

CUDA settings

engine-specific experimental controls

---

# 24. APPLICATION SETTINGS SCREEN

Create a Settings screen with sections:

General

Audio

TTS Engines

Models

GPU / Performance

Storage

Appearance

Diagnostics

Advanced

Settings should include:

Default engine

Default language

Default output directory

Remember last voice

Auto-play after generation

Model memory policy

CPU/GPU preference

Delete temporary files automatically

Log level

Data directory

Python environment paths

Repository paths

Model directories

FFmpeg location

---

# 25. ENGINE CONFIGURATION SCREEN

For each engine create a configuration page.

Show:

Repository path

Python executable

Worker status

Installed version / commit if detectable

Model directory

Device

Environment health

Test button

Open logs

Restart worker

Unload model

Do not require users to edit JSON manually for ordinary setup.

---

# 26. PROJECT DIRECTORY STRUCTURE

Create a clean Visual Studio solution.

Suggested structure:

LocalTtsStudio.sln

src/
    LocalTtsStudio.App/
    LocalTtsStudio.Core/
    LocalTtsStudio.Infrastructure/
    LocalTtsStudio.Tts/
    LocalTtsStudio.Audio/

tests/
    LocalTtsStudio.Tests/

workers/
    common/
    f5/
    kokoro/
    xtts/
    fish/

tts-libraries/
    existing cloned repositories

installer/

scripts/

docs/

Recommended responsibilities:

LocalTtsStudio.App

WPF

Views

ViewModels

Resources

Themes

Converters

LocalTtsStudio.Core

Entities

interfaces

DTOs

language model

generation contracts

voice model

LocalTtsStudio.Infrastructure

SQLite

repositories

configuration

logging

filesystem

LocalTtsStudio.Tts

engine manager

worker communication

F5 adapter

Kokoro adapter

XTTS adapter

Fish adapter

LocalTtsStudio.Audio

playback

waveform metadata

audio conversion

FFmpeg abstraction

---

# 27. MVVM

Use CommunityToolkit.Mvvm.

Use:

ObservableObject

ObservableProperty

RelayCommand

AsyncRelayCommand

Avoid excessive code-behind.

Code-behind is acceptable only for UI-specific behavior that cannot reasonably live in ViewModels.

Use DataTemplates where appropriate for dynamic engine settings.

---

# 28. DATABASE

Use SQLite through Entity Framework Core.

Create database entities such as:

VoiceCharacterEntity

VoiceReferenceEntity

GenerationHistoryEntity

ApplicationSettingEntity if necessary

EngineProfileEntity if useful

Use migrations.

Database creation/update must be automatic.

Never delete an existing user database during app update.

---

# 29. FILE STORAGE

Separate application binaries from user data.

Program installation:

Program Files\Local TTS Studio\

User data:

LocalAppData or configurable storage directory.

Large models may be stored in:

LocalAppData

ProgramData

or a user-selected models directory.

Do not silently duplicate multi-gigabyte checkpoints.

---

# 30. PACKAGING THE PYTHON RUNTIMES

The final release should not require the user to manually install Python.

Design release packaging to include the runtime necessary to run the Python workers.

Possible strategy:

runtime/
    python/
or separate runtime per engine if required.

However, because these libraries may require incompatible Python/PyTorch packages, keep environments logically isolated.

Do not simply copy developer virtual environments without evaluating portability.

Create reproducible environment build scripts.

For each engine provide something like:

scripts/build-f5-runtime.ps1

scripts/build-kokoro-runtime.ps1

scripts/build-xtts-runtime.ps1

scripts/build-fish-runtime.ps1

The scripts should:

create runtime

install pinned requirements

install the local repository

verify imports

run a health check

prepare release files

Generate a manifest containing runtime versions.

---

# 31. MODEL PACKAGING

Treat application runtime and model weights separately in the architecture.

Models can be extremely large.

Support two release concepts:

FULL OFFLINE BUILD

Includes selected model weights.

LIGHT BUILD

Includes application and runtimes but models are installed separately.

Even if only FULL OFFLINE BUILD is initially used, architect the model manager to support both.

Do not download a model unexpectedly when the application claims to be offline.

Add Model Manager page.

Display:

Engine

Model name

Version

Location

Size

Installed

Status

Optional checksum

---

# 32. PREPACKAGED `tts-libraries`

The existing:

tts-libraries/

repositories need to remain available for development.

For release, do NOT blindly expose your entire Git checkout including:

.git/

test datasets

documentation assets

developer caches

unused checkpoints

etc.

Create staging scripts that copy only required runtime source/package files into the release.

If an engine is installed into its Python runtime as a package, the release may not need the entire Git repository.

Determine this separately for each project.

Keep license/copyright files required by upstream projects in distribution packages.

---

# 33. OFFLINE REQUIREMENT

After installation, inference must not require:

Internet

cloud APIs

web services

external authentication

ElevenLabs

Fish Audio cloud

Hugging Face network access

etc.

All model checkpoints required for the selected offline build must be locally available.

Workers must be explicitly configured for offline behavior where necessary.

If a model file is missing, show:

"Model not installed"

rather than automatically downloading it without clear user action.

---

# 34. MSI INSTALLER / FULL OFFLINE DEPLOYMENT REQUIREMENT

A critical end goal of this project is that the application must ultimately be distributable as a Windows MSI installer that can install and configure the complete application on another Windows PC.

An existing folder is already present at:

`\msi-installers\`

All MSI installer source files, installer projects, scripts, bootstrapper files, manifests, build output, documentation, and packaging configuration must be created inside this existing folder.

Do not create a second installer folder elsewhere.

The resulting repository structure should conceptually include:

```text
LocalTtsStudio.sln

src/
    LocalTtsStudio.App/
    LocalTtsStudio.Core/
    LocalTtsStudio.Infrastructure/
    LocalTtsStudio.Tts/
    LocalTtsStudio.Audio/

workers/
    common/
    f5/
    kokoro/
    xtts/
    fish/

tts-libraries/
    F5-TTS/
    Kokoro/
    Coqui-TTS/
    Fish-Speech/

scripts/

docs/

msi-installers/
    README.md

    LocalTtsStudio.Setup/
        installer source/project files

    bootstrapper/
        prerequisite/bootstrapper configuration

    scripts/
        build-msi.ps1
        stage-release.ps1
        verify-package.ps1

    staging/
        generated during packaging

    output/
        generated MSI / setup packages

    manifests/
        runtime-manifest.json
        models-manifest.json
        package-manifest.json
```

The exact names may be adapted if required by the installer technology, but all installer-related resources must remain underneath:

`\msi-installers\`

---

# INSTALLER TECHNOLOGY

Prefer a robust MSI-capable installer technology suitable for a Visual Studio/.NET desktop application.

Evaluate, in this order:

1. WiX Toolset
2. Visual Studio Installer Projects, if it satisfies all deployment requirements
3. Another MSI-producing system only if necessary

Prefer WiX if the installer needs sophisticated prerequisite detection, custom actions, upgrade handling, feature selection, registry configuration, environment setup, or bootstrapper support.

The final installer must ultimately produce a real:

`.msi`

package.

A bootstrapper `.exe` may additionally be provided when needed for prerequisites, but the MSI must remain available as an independent build artifact.

Example output:

```text
msi-installers/output/
    LocalTtsStudio-x64-1.0.0.msi
    LocalTtsStudio-Setup-x64-1.0.0.exe
```

---

# TARGET PLATFORM

Target:

Windows 10/11 x64

Do not target x86.

The WPF application should be published using:

```text
Configuration: Release
RuntimeIdentifier: win-x64
SelfContained: true
```

The installed application must not require the user to install the matching .NET runtime manually.

The installer should install the application's self-contained .NET publish output.

---

# MAIN DEPLOYMENT GOAL

After building the final installer, the following workflow must be possible:

1. Copy the installer to a different compatible Windows PC.

2. Run:

   `LocalTtsStudio-Setup-x64.exe`

   or:

   `LocalTtsStudio-x64.msi`

3. Complete the installer.

4. Start Local TTS Studio.

5. The application detects the installed TTS engines and runtime environments.

6. The user can generate audio without manually cloning repositories or installing Python packages.

The destination user should not be required to:

- install Visual Studio
- install VS Code
- install Git
- clone repositories
- manually install Python
- manually create virtual environments
- manually run pip
- manually install NuGet packages
- manually configure PYTHONPATH
- edit system environment variables
- manually install the TTS repositories

The installer/package preparation process must handle these requirements.

---

# IMPORTANT DISTINCTION: DEVELOPMENT SOURCE VS INSTALLED RUNTIME

The development repository contains:

```text
tts-libraries/
```

with cloned upstream GitHub repositories.

Do not simply copy the complete development Git repositories into Program Files unless that is truly necessary.

Instead, create reproducible runtime packaging.

Development:

```text
tts-libraries/
    F5-TTS/
    Kokoro/
    Coqui-TTS/
    Fish-Speech/
```

Release staging should contain only the files actually required to run inference.

For example:

```text
msi-installers/staging/
    app/

    engines/
        f5/
        kokoro/
        xtts/
        fish/

    runtimes/
        ...

    workers/
        ...

    models/
        ...

    tools/
        ffmpeg/

    licenses/
```

The staging structure is generated by:

```text
msi-installers/scripts/stage-release.ps1
```

The MSI is built from the staging output.

The staging directory itself should be considered temporary/generated content.

---

# PYTHON RUNTIME DEPLOYMENT

The finished application must NOT depend on an already-installed system Python installation.

Package compatible Python runtime environments with the application.

Because the four TTS systems may require incompatible dependency versions, preserve environment isolation.

Do NOT combine all TTS libraries into one Python virtual environment unless repository inspection proves that doing so is safe.

Preferred conceptual structure:

```text
Program Files/
    Local TTS Studio/
        LocalTtsStudio.exe

        workers/
            ...

        engines/
            f5/
            kokoro/
            xtts/
            fish/

        runtimes/
            f5/
            kokoro/
            xtts/
            fish/

        tools/
            ffmpeg/
```

Each runtime should contain the Python interpreter and dependencies necessary for that engine.

Example:

```text
runtimes/f5/python.exe
runtimes/kokoro/python.exe
runtimes/xtts/python.exe
runtimes/fish/python.exe
```

However, first inspect actual compatibility.

If two engines can safely share an identical runtime and doing so significantly reduces installer size, sharing may be implemented.

Do not sacrifice reliability merely to reduce size.

---

# DO NOT RELY ON NORMAL VENV PORTABILITY

Do not assume that copying a developer `.venv` to another computer will always work.

Create reproducible release runtimes.

Potential approaches include:

- Python embeddable distribution
- portable Python installation
- prepared application-local Python runtime
- another proven redistributable Python runtime strategy

The build system must produce runtimes specifically intended for deployment.

Create build scripts that reconstruct each runtime.

Examples:

```text
scripts/build-f5-runtime.ps1
scripts/build-kokoro-runtime.ps1
scripts/build-xtts-runtime.ps1
scripts/build-fish-runtime.ps1
```

These scripts should:

1. Create a clean runtime.

2. Install the correct Python version.

3. Install pinned dependencies.

4. Install the local cloned TTS repository or copy required package files.

5. Install the appropriate PyTorch build.

6. Verify imports.

7. Verify worker startup.

8. Run a lightweight health test.

9. Remove unnecessary caches.

10. Produce a runtime manifest.

The packaged runtime must not contain:

- pip download caches
- `.git`
- unnecessary unit tests
- developer build files
- temporary model outputs
- local user data
- unnecessary documentation
- `__pycache__` where safe to remove

---

# PYTHON VERSION REQUIREMENTS

Do not assume all four libraries use the same Python version.

Inspect the cloned repositories for:

- `pyproject.toml`
- `requirements.txt`
- `setup.py`
- `.python-version`
- Conda environment files
- README requirements
- CI configuration

Determine the supported Python version for each cloned version.

Record the selected Python versions in:

```text
msi-installers/manifests/runtime-manifest.json
```

Example conceptual content:

```json
{
  "f5": {
    "python": "3.x.x",
    "runtime": "runtimes/f5"
  },
  "kokoro": {
    "python": "3.x.x",
    "runtime": "runtimes/kokoro"
  },
  "xtts": {
    "python": "3.x.x",
    "runtime": "runtimes/xtts"
  },
  "fish": {
    "python": "3.x.x",
    "runtime": "runtimes/fish"
  }
}
```

Use the actual versions required by the checked-out repositories.

---

# PYTORCH / CUDA DEPLOYMENT

The installer architecture must account for PyTorch and GPU acceleration.

Do not assume that installing CUDA Toolkit globally on Windows is required.

Where supported, use application-local PyTorch CUDA packages that include their required CUDA runtime components.

The application must detect:

- NVIDIA GPU
- driver version
- CUDA availability as seen by PyTorch
- available VRAM
- CPU fallback capability

The installer must NOT silently modify or replace the user's GPU drivers.

Do not bundle NVIDIA display drivers.

Instead, perform runtime detection.

If the installed NVIDIA driver is insufficient, report clearly:

```text
NVIDIA GPU detected, but the installed driver is not compatible with the packaged PyTorch/CUDA runtime.
```

Provide diagnostic information rather than failing silently.

---

# CPU AND GPU BUILDS

Architect packaging so that different release editions could eventually exist.

For example:

```text
LocalTtsStudio-GPU-x64.msi
LocalTtsStudio-CPU-x64.msi
```

or installer features:

```text
Core Application
F5-TTS
Kokoro
XTTS
Fish Speech
GPU Runtime
Models
```

The first implementation does not necessarily need multiple editions, but the installer architecture should not prevent them.

---

# INSTALLER FEATURE SELECTION

If practical using WiX, create MSI features corresponding to major optional components.

Example:

```text
Core Application              REQUIRED
Audio Tools                   REQUIRED

F5-TTS Engine                 OPTIONAL
Kokoro Engine                 OPTIONAL
XTTS v2 Engine                OPTIONAL
Fish Speech Engine            OPTIONAL

F5 Models                     OPTIONAL
Kokoro Models                 OPTIONAL
XTTS Models                   OPTIONAL
Fish Speech Models            OPTIONAL
```

Default installation may select all engines for the full offline package.

This architecture is useful because the combined package could eventually become very large.

---

# MODELS AND MODEL WEIGHTS

Model weights may represent the majority of installer size.

Treat model packages separately from Python source.

Create:

```text
msi-installers/manifests/models-manifest.json
```

Record:

- engine
- model name
- model version
- source version
- expected relative path
- file size
- checksum where practical
- whether included in current installer
- optional/required status

For a FULL OFFLINE installer, required models should be packaged locally so that inference does not require a network connection after installation.

Do not automatically download missing models on first startup if the release is advertised as fully offline.

Instead show:

```text
Model missing
```

and allow explicit installation/configuration later.

---

# INSTALLER SIZE

Expect the complete installer to potentially become several gigabytes because of:

- PyTorch
- CUDA runtime libraries
- multiple Python installations
- transformers
- model checkpoints
- Fish Speech models
- XTTS models
- F5 models

Do not optimize prematurely by merging incompatible environments.

Reliability comes first.

However, identify obvious deduplication opportunities during release packaging.

Document installer size contributors.

---

# BOOTSTRAPPER

Create a bootstrapper configuration under:

```text
msi-installers/bootstrapper/
```

Use a bootstrapper when MSI alone is insufficient for prerequisite handling.

The bootstrapper may:

- detect supported Windows version
- check x64 architecture
- check disk space
- check Visual C++ runtime if required
- invoke the MSI
- optionally install required Microsoft redistributables

Do not use the bootstrapper to download TTS models from the Internet for a FULL OFFLINE release.

If prerequisites are redistributed, comply with their redistribution requirements.

---

# VISUAL C++ RUNTIME

Some Python/Torch/native libraries may require the Microsoft Visual C++ Redistributable.

Determine whether this is necessary.

If required, either:

- bundle its official redistributable in the bootstrapper

or:

- detect an existing compatible installation

Do not copy individual Visual C++ DLLs manually unless explicitly supported by the dependency.

---

# FFMPEG

If FFmpeg is used by the application, make it part of the deployment.

Suggested location:

```text
Program Files/
    Local TTS Studio/
        tools/
            ffmpeg/
                ffmpeg.exe
                ffprobe.exe
```

The application must resolve it through AppPaths or an FFmpeg service.

Do not depend on FFmpeg being globally available in PATH.

---

# PROGRAM FILES VS USER DATA

Installed binaries:

```text
C:\Program Files\Local TTS Studio\
```

User-managed data:

```text
%LOCALAPPDATA%\LocalTtsStudio\
```

For example:

```text
%LOCALAPPDATA%\LocalTtsStudio\
    database/
        voices.db

    voices/

    generations/

    logs/

    cache/

    settings/
```

Do not store mutable user data inside Program Files.

The MSI should never replace or reset the user's voice database during an upgrade.

---

# LARGE MODEL DIRECTORY OPTION

Allow large model storage to eventually be redirected to another location.

For example:

```text
D:\AI\Models\LocalTtsStudio\
```

Do not hardcode user model storage exclusively to Program Files.

The packaged default models may initially reside with the application, but the application architecture should allow an alternate model root.

---

# INSTALLER INITIAL CONFIGURATION

The installer should install a default configuration template containing relative installation paths rather than machine-specific development paths.

Never package configuration containing developer-specific paths such as:

```text
C:\Users\<developer>\source\repos\...
```

Instead the installed app should dynamically resolve:

```text
InstallationRoot
RuntimeRoot
WorkerRoot
EngineRoot
ModelRoot
ToolRoot
UserDataRoot
```

through the central AppPaths service.

---

# RELEASE PATH RESOLUTION

Development mode might resolve:

```text
tts-libraries/F5-TTS
```

Installed release mode might resolve:

```text
{InstallRoot}/engines/f5
```

Workers:

```text
{InstallRoot}/workers/f5/
```

Python:

```text
{InstallRoot}/runtimes/f5/python.exe
```

Never rely on the current working directory.

Always derive paths from:

```text
AppContext.BaseDirectory
```

or the central application path service.

---

# INSTALLER UPGRADE SUPPORT

The MSI must support future application versions cleanly.

Implement proper:

- ProductVersion
- UpgradeCode
- ProductCode handling
- MajorUpgrade behavior

An upgrade should:

1. Detect the previous version.

2. Replace application binaries.

3. Replace bundled engine runtime files as necessary.

4. Preserve user data.

5. Preserve voice characters.

6. Preserve generation history.

7. Preserve user settings where compatible.

Do not create multiple broken side-by-side installations accidentally.

---

# USER DATA DURING UNINSTALL

Normal uninstall should remove:

- application files
- packaged engine files
- Python runtimes
- installer-managed model files where appropriate

It should NOT automatically remove:

- user-created Voice Characters
- generated audio
- SQLite database
- user settings
- custom imported models

unless explicitly requested by the user.

Document where remaining user data is located.

---

# CLEAN INSTALL VERIFICATION

Create:

```text
msi-installers/scripts/verify-package.ps1
```

This script should verify the staging/package before building the MSI.

Check at minimum:

- main EXE exists
- all required assemblies exist
- worker scripts exist
- expected Python executables exist
- engine packages exist
- required models exist for FULL OFFLINE build
- FFmpeg exists if required
- manifests are valid
- version files exist
- license files exist
- no `.git` folders exist
- no development virtual environments accidentally leaked in
- no developer absolute paths exist
- no temporary generation data exists

Fail the packaging build if critical files are missing.

---

# MSI BUILD SCRIPT

Create:

```text
msi-installers/scripts/build-msi.ps1
```

The script should automate:

1. Clean previous staging output.

2. Build WPF solution in Release.

3. Publish WPF application:

   `win-x64`
   `self-contained`

4. Build/verify Python runtimes.

5. Stage workers.

6. Stage required TTS engine components.

7. Stage models included in this edition.

8. Stage FFmpeg/tools.

9. Copy required license files.

10. Generate package manifests.

11. Run package verification.

12. Build MSI.

13. Build bootstrapper if configured.

14. Place final artifacts under:

```text
msi-installers/output/
```

15. Print final file locations and sizes.

The entire release process should ideally be executable with one command:

```powershell
.\msi-installers\scripts\build-msi.ps1
```

---

# RELEASE STAGING SCRIPT

Create:

```text
msi-installers/scripts/stage-release.ps1
```

This script should create a deterministic staging directory.

Example:

```text
msi-installers/staging/LocalTtsStudio/
```

The MSI project should consume files from this staging directory rather than random files throughout the source repository.

This separates:

development tree

from:

release payload

and makes installer debugging substantially easier.

---

# RUNTIME MANIFEST

Generate:

```text
msi-installers/manifests/runtime-manifest.json
```

Include:

- application version
- .NET publish target
- engine names
- Python versions
- PyTorch versions
- CUDA runtime versions if applicable
- repository commit/version for each engine
- packaged model versions
- FFmpeg version
- packaging timestamp

The application Diagnostics screen should be able to read this manifest.

---

# INSTALLED ENGINE HEALTH CHECK

After installation, the application must be able to independently test each packaged engine.

Example:

```text
F5-TTS
Runtime: OK
Python: OK
Imports: OK
Model: OK
CUDA: OK
Status: Ready

Kokoro
Runtime: OK
Python: OK
Model: OK
Status: Ready

XTTS v2
Runtime: OK
Python: OK
Model: OK
CUDA: OK
Status: Ready

Fish Speech
Runtime: OK
Python: OK
Model: OK
CUDA: OK
Status: Ready
```

If one engine fails, the application must continue operating with the others.

---

# POST-INSTALL FIRST RUN

Do not perform heavy model installation or pip operations during ordinary application startup if those components were supposed to be part of the MSI.

The release should already contain prepared runtimes.

First-run setup should primarily:

- initialize SQLite
- create user directories
- inspect hardware
- validate engines
- validate models
- save initial settings

Avoid first-start experiences where the application spends a long period silently running pip.

---

# OPTION FOR INSTALL-TIME ENGINE PREPARATION

If an upstream engine proves technically impossible or impractical to package as a prepared portable runtime, installation-time setup may be used as a fallback.

In that situation:

1. Include all dependency wheels/packages required for offline installation inside the installer payload.

2. Run setup from local files.

3. Do not require Internet access.

4. Log setup output.

5. Detect failures.

6. Allow repair.

7. Never depend on PyPI during installation.

For example, prepare an offline wheelhouse:

```text
installer-payload/
    wheels/
        f5/
        kokoro/
        xtts/
        fish/
```

Then installation can use something conceptually similar to:

```text
pip install --no-index --find-links=<local wheel folder> ...
```

Use the exact syntax appropriate to the runtime design.

Prepared portable runtimes are preferable where reliable.

---

# MSI REPAIR

The installer should support Windows Installer Repair where practical.

Repair should restore missing:

- application binaries
- workers
- packaged runtime files

It should not overwrite or delete user Voice Characters.

---

# INSTALL LOGGING

Installer failures need diagnostics.

Document how users can generate verbose MSI logs, for example through:

```text
msiexec
```

The bootstrapper should also write installation logs.

Application engine-install/setup scripts should have separate logs.

---

# VERSIONING

Use one application version across:

- WPF assembly
- MSI
- bootstrapper
- manifests

Example:

```text
1.0.0
```

Keep engine/runtime versions independently recorded.

Do not equate the application's version with the version of F5-TTS, XTTS, Kokoro, or Fish Speech.

---

# LICENSE FILES

Create a release license directory under staging.

Example:

```text
licenses/
    LocalTtsStudio/
    F5-TTS/
    Kokoro/
    Coqui-TTS/
    Fish-Speech/
    FFmpeg/
    third-party/
```

Preserve required upstream attribution/license files.

Do not strip licensing metadata merely because this is initially a hobby project.

---

# BUILD CONFIGURATIONS

Eventually support build configurations conceptually like:

```text
Debug

Release

Release-Offline-Full

Release-Offline-Core
```

For the first production installer, prioritize:

```text
Release-Offline-Full
```

which should contain everything needed for offline operation on a compatible target PC.

---

# FULL OFFLINE ACCEPTANCE TEST

Before considering the installer finished, perform the following acceptance test.

Use a clean Windows 10/11 x64 machine or VM that does NOT have:

- Visual Studio
- Python
- Git
- the cloned TTS repositories
- project environment variables
- developer PATH modifications

Disconnect network access.

Install the generated package.

Launch the application.

Verify:

1. Application starts.

2. Database initializes.

3. Voice Character can be created.

4. Reference audio can be imported.

5. Reference transcript can be saved.

6. Kokoro generates speech.

7. F5-TTS generates speech.

8. XTTS v2 generates speech.

9. Fish Speech generates speech if target hardware satisfies its requirements.

10. Output audio can be played.

11. Output file is saved.

12. Generation history is recorded.

13. Application can restart and retain voice characters.

14. Application remains functional without network access.

15. Uninstall works.

16. Reinstall works.

17. Upgrade installation preserves user data.

The installer is not considered complete until this clean-machine test succeeds.

---

# CLEAN MACHINE TESTING SUPPORT

Provide documentation:

```text
msi-installers/README.md
```

Include:

- installer technology
- required build tools
- how to generate runtime environments
- how to generate staging
- how to build MSI
- how to build bootstrapper
- output locations
- installer size
- expected hardware requirements
- supported Windows versions
- clean-machine test procedure
- known engine-specific requirements

---

# DEVELOPMENT REQUIREMENT FOR THE CODING AGENT

Do not postpone installer architecture until the application is finished.

While implementing the application:

- keep all runtime paths relocatable
- avoid developer-specific paths
- avoid dependencies on globally installed Python
- avoid dependencies on globally installed FFmpeg
- avoid dependencies on working-directory assumptions
- separate Program Files from user data
- ensure each engine can be addressed through application-local paths

Every implementation decision should be evaluated against this requirement:

"Will this still work after being installed under Program Files on a different Windows computer?"

If not, redesign it.

---

# UPDATED IMPLEMENTATION PHASES

PHASE 1

Build the WPF solution architecture.

PHASE 2

Build database, Voice Character Library, audio import, playback, language normalization and generation UI.

PHASE 3

Implement ITtsEngine, capability system, worker protocol and MockTtsEngine.

PHASE 4

Create the initial installer structure under:

`\msi-installers\`

including:

```text
msi-installers/
    README.md
    scripts/
    staging/
    output/
    manifests/
```

Create an initial MSI that installs only the WPF application.

Confirm that it launches successfully from Program Files.

PHASE 5

Integrate Kokoro.

Create its deployable runtime.

Add it to release staging.

Build MSI.

Install on clean test machine.

Verify Kokoro generation.

PHASE 6

Integrate F5-TTS.

Create deployable F5 runtime.

Add it to MSI staging.

Perform clean-machine test.

PHASE 7

Integrate XTTS v2.

Create deployable XTTS runtime.

Perform clean-machine test.

PHASE 8

Integrate Fish Speech.

Create deployable Fish runtime.

Perform clean-machine test.

PHASE 9

Add:

- Model Manager
- generation queue
- diagnostics
- hardware detection
- model memory policy
- cache
- engine repair/restart
- complete offline model packaging

PHASE 10

Complete production MSI/bootstrapper.

Run full clean-machine offline acceptance test.

---

# IMPORTANT IMPLEMENTATION STRATEGY

Do NOT wait until all four engines work before testing MSI deployment.

After each engine becomes operational:

1. Stage it.

2. Package it.

3. Install it on a clean environment.

4. Test it.

This avoids discovering at the very end that a Python runtime, native DLL, model path, Torch dependency, FFmpeg binary, or worker script only worked because of the development computer's environment.

---

# FINAL DELIVERABLES

The completed repository should ultimately produce:

```text
msi-installers/output/
    LocalTtsStudio-x64-X.Y.Z.msi
    LocalTtsStudio-Setup-x64-X.Y.Z.exe
```

The MSI must install a functional desktop application.

The bootstrapper EXE may handle additional prerequisites.

The installed application should be capable of using the packaged local versions of:

- F5-TTS
- Kokoro
- XTTS v2
- Fish Speech

without requiring the destination user to manually install those libraries.

The release must remain fully local/offline after installation when all required models have been included.

The project is only considered complete when the application can be built on the development machine, packaged from the `\msi-installers\` directory, installed on another clean compatible Windows PC, and successfully generate speech using the installed local TTS engines.

---

# 35. DEVELOPMENT VS RELEASE MODE

Support development paths like:

..\..\..\tts-libraries\F5-TTS

but do not depend on relative source-tree paths in the installed product.

Use configuration/environment abstraction.

Development mode:

use local repository working tree.

Release mode:

use packaged runtime paths relative to installation root.

Implement an AppPaths service that centrally resolves all paths.

Do not scatter path strings throughout the application.

---

# 36. ERROR HANDLING

No unhandled Python error should crash the WPF process.

Capture:

worker exit codes

stderr

structured error response

timeouts

missing files

invalid reference audio

CUDA OOM

model loading failure

Python import error

checkpoint missing

invalid language

unsupported setting

Show concise errors to the user.

Log technical details.

Provide:

Copy Diagnostic Information

button.

---

# 37. LOGGING

Use Serilog.

Log files:

%LOCALAPPDATA%\LocalTtsStudio\logs\

Implement rolling logs.

Log:

application startup

engine initialization

worker startup/shutdown

generation requests without unnecessarily storing sensitive text if logging can avoid it

model load times

output path

errors

hardware diagnostics

Do not flood logs with binary/audio data.

---

# 38. CANCELLATION

Generation needs cancellation support.

Use CancellationToken in C#.

If an engine cannot cancel gracefully, the adapter may terminate and restart its Python worker.

The UI must recover after cancellation.

---

# 39. TEMPORARY FILES

Use an application-specific temp/cache folder.

Do not litter engine repository folders with:

fake.npy

codes_0.npy

temporary WAV files

generated chunks

etc.

Every request should have its own workspace:

cache/jobs/{request-guid}/

Pass that working directory to the worker wherever possible.

Clean completed job directories according to retention settings.

---

# 40. F5-TTS ADAPTER

Inspect the locally cloned F5-TTS repository before implementation.

Provide support for reference audio and reference transcript.

The adapter should translate the normalized TTS request into the actual F5 inference API/CLI supported by our cloned version.

The wrapper should support at minimum:

input generation text

reference audio

reference transcript

output filename

model selection if appropriate

speed if supported

other valid inference parameters discovered in the local code

Do not launch the F5 Gradio interface.

Use the underlying Python inference layer or CLI.

Prefer direct Python APIs in our worker when this provides cleaner persistent model loading.

---

# 41. KOKORO ADAPTER

Inspect the locally cloned Kokoro repository.

Use its KPipeline or equivalent supported API.

Support built-in voices.

Map canonical application languages to Kokoro language identifiers.

Important example mappings may include:

en-US -> a

en-GB -> b

but verify the installed repository.

Populate the Kokoro voice selector automatically using available voice information.

Do not show reference-audio controls if this Kokoro implementation does not support user reference cloning.

---

# 42. XTTS V2 ADAPTER

Inspect the locally cloned Coqui TTS / XTTS implementation.

Use the XTTS v2 model.

Support:

reference audio

reference voice cloning

language selection

output generation

supported inference parameters

If multiple reference WAV files are supported by the installed implementation, allow the Voice Character system to supply multiple references.

Use the normalized language mapper.

Examples conceptually include:

en -> English

de -> German

fr -> French

etc.

But obtain the authoritative language list from the installed model/library rather than maintaining unnecessary duplicated assumptions.

---

# 43. FISH SPEECH ADAPTER

Inspect the locally cloned Fish Speech repository.

Fish Speech APIs have changed across releases.

Do NOT build the adapter based only on old online tutorials.

Determine what the cloned repository currently supports.

Prefer its internal inference API or supported HTTP/CLI interface.

Wrap reference-audio processing, prompt/reference transcript and speech generation behind FishSpeechEngine.

If Fish generates intermediate semantic/VQ token files, store those in the per-job cache directory.

If useful, cache reusable voice conditioning data associated with Voice Characters.

---

# 44. ENGINE-SPECIFIC VOICE CACHE

Create an extensible cache system.

A saved Voice Character may optionally have generated engine-specific conditioning data.

For example:

voices/{voice-id}/cache/fish/

voices/{voice-id}/cache/xtts/

etc.

Cache metadata should record:

engine

engine/model version

source reference hash

creation timestamp

If source audio or transcript changes, invalidate engine-specific caches.

---

# 45. USER EXPERIENCE

The application should feel like a modern audio production tool rather than a developer utility.

Use a clean dark-capable interface.

Navigation idea:

Generate

Voices

History

Models

Queue

Settings

Diagnostics

Use sensible spacing.

Avoid excessive modal dialogs.

Provide tooltips for advanced AI parameters.

Keep basic workflows simple.

---

# 46. STATUS BAR

Provide a bottom status area showing:

Selected Engine

Engine Status

GPU/device

Queue state

Current generation progress

Optionally VRAM usage where obtainable.

---

# 47. DRAG AND DROP

Where reasonable support:

dragging reference audio onto the Voice Editor

dragging audio onto one-off reference input

dragging text files into the generation text editor

---

# 48. FIRST-RUN EXPERIENCE

On first launch:

initialize database

detect hardware

find packaged workers

test Python runtimes

detect installed models

do not require internet

Show a concise Setup/Health page if something is missing.

Example:

F5-TTS — Ready

Kokoro — Ready

XTTS v2 — Model missing

Fish Speech — Ready

FFmpeg — Ready

CUDA — Available

Do not prevent application startup merely because one engine fails.

---

# 49. TESTING

Create unit tests for at minimum:

language mappings

output filename incrementing

voice character repository

path resolution

engine capability handling

JSON protocol serialization

generation request validation

settings serialization

audio cache invalidation

Do not require actual multi-gigabyte models for normal unit tests.

Create mock ITtsEngine implementation for UI/development/testing.

---

# 50. SECURITY / PROCESS SAFETY

Never interpolate user text directly into shell command strings.

Use ProcessStartInfo.ArgumentList or structured worker JSON.

Avoid command injection.

Canonicalize paths.

Validate imported files.

Workers listen only locally if HTTP is used.

No external network binding.

---

# 51. PHASED IMPLEMENTATION

Implement this application incrementally.

PHASE 1

Create solution architecture.

Create WPF shell.

Set up MVVM.

Navigation.

Dependency injection.

Logging.

SQLite.

Settings.

Basic entities.

PHASE 2

Implement:

Voice Character Library

reference audio import

transcript editing

audio player

language service

output path handling

generation history schema

PHASE 3

Create:

ITtsEngine

EngineCapabilities

EngineManager

worker protocol

MockTtsEngine

Use mock generation first to validate complete UI workflow.

PHASE 4

Integrate Kokoro.

Verify full pipeline:

C# request

Python worker

generation

output WAV

playback

history

PHASE 5

Integrate F5-TTS.

PHASE 6

Integrate XTTS v2.

PHASE 7

Integrate Fish Speech.

PHASE 8

Implement:

hardware diagnostics

model manager

runtime health checks

generation queue

cancellation

cache

performance controls

PHASE 9

Create reproducible runtime packaging scripts.

PHASE 10

Create self-contained Windows release and installer.

DO NOT attempt all engines before the basic architecture works.

---

# 52. FIRST IMPLEMENTATION TASK

Start now by examining the existing repository structure.

First determine:

existing Visual Studio solution/projects

contents of tts-libraries

actual folder names for the four engines

their Python versions if specified

their requirements/pyproject files

their current inference entry points

their model expectations

Do not modify upstream TTS source code unnecessarily.

Then produce a concise implementation plan based on what actually exists.

After that begin Phase 1.

Create the Visual Studio solution structure.

Set up WPF + MVVM + DI + logging + SQLite.

Create the core entities/interfaces.

Create application navigation.

Create a mock TTS engine.

Create the engine-capability system.

Create the initial LanguageMapper architecture.

Create the VoiceCharacter data model.

Create GenerationRequest and GenerationResult models.

Create the worker protocol DTOs.

Build the solution after each meaningful stage and fix compiler errors immediately.

Do not leave placeholder code that prevents compilation.

---

# 53. CODING STANDARDS

Enable nullable reference types.

Use async APIs for I/O.

Use CancellationToken for long operations.

Prefer records for immutable DTOs where appropriate.

Prefer interfaces at subsystem boundaries.

Use explicit dependency injection.

Avoid static global application state.

Avoid service locator patterns.

Avoid giant classes.

Avoid duplicated engine logic.

Document non-obvious architecture.

Use meaningful names.

Do not suppress compiler warnings merely to make the build appear clean.

---

# 54. IMPORTANT RULE ABOUT UPSTREAM LIBRARIES

The four existing TTS projects are external/upstream dependencies.

Do not substantially rewrite their source code.

Integration code belongs in:

workers/

and:

LocalTtsStudio.Tts/

If an upstream library requires a small patch, document exactly:

why

file

change

upstream version

and preferably keep the patch in:

patches/

so it can be reapplied later.

---

# 55. END GOAL

The finished application should allow this workflow:

Launch Local TTS Studio.

Select F5-TTS, Kokoro, XTTS v2 or Fish Speech.

Choose a saved Voice Character or create one.

If necessary import voice reference audio.

Enter or edit its transcript.

Choose language using human-readable language names.

Enter text to synthesize.

Adjust engine-specific generation parameters.

Choose an output folder.

Enter a base filename.

Press Generate.

See generation progress.

Listen to the result directly inside the application.

Regenerate if desired.

Automatically save generation history.

Automatically increment filenames rather than overwrite existing generations.

Return later and reuse, modify or delete saved Voice Characters.

Everything should run locally on the Windows PC without requiring a cloud TTS API.

The architecture must make adding another future engine something like:

Create NewEngine : ITtsEngine

Create Python worker

Define capabilities

Define language mappings

Register adapter

rather than redesigning the application.

Build toward that architecture from the beginning.