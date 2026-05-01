# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About

OnlyR is a Windows desktop audio recorder built with WPF and .NET 10.0 (x86). It supports multiple audio codecs (MP3, AAC, Opus, PCM, FLAC) and features silence detection, USB recording, volume metering, and auto-purging of old recordings.

## Build & Test Commands

```bash
# Build
dotnet build OnlyR.slnx --configuration Release

# Run all tests
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release

# Run a single test class
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release --filter "ClassName=TestCommandLineParser"

# Publish (creates installer and portable zip via Inno Setup)
CreateDeliverables.cmd
```

Tests use the **TUnit** framework (not xUnit/NUnit) with **Rocks** for mocking.

## Architecture

The solution has three projects:

- **OnlyR.Core** — platform-agnostic business logic. Contains the audio recording engine (`Recorder/AudioRecorder.cs`), wrapping NAudio/WASAPI. No UI dependencies.
- **OnlyR** — WPF application (MVVM). Consumes OnlyR.Core via injected services.
- **OnlyR.Tests** — unit tests for both projects.

### Key architectural patterns

**MVVM with CommunityToolkit.Mvvm**: ViewModels live in `OnlyR/ViewModel/`. Pages are `RecordingPage` and `SettingsPage`, coordinated by `MainViewModel`. Page navigation uses `WeakReferenceMessenger` with typed message classes in `ViewModel/Messages/`.

**Dependency Injection**: `App.xaml.cs` bootstraps `Microsoft.Extensions.DependencyInjection` and exposes `Ioc.Default`. All services are registered as singletons.

**Services layer** (`OnlyR/Services/`): Eight service categories handle distinct concerns — `AudioService` wraps `AudioRecorder`, `OptionsService` persists settings to the Windows registry, `RecordingDestinationService` manages the date-organised folder hierarchy, `SilenceService` auto-stops on silence, `CopyRecordingsService`/`DriveEjectionService` handle USB workflows, and `PurgeRecordingsService` cleans old recordings.

**Package versioning**: All NuGet versions are centralised in `Directory.Packages.props`. Global build settings (nullable, analyzers, warnings-as-errors) are in `Directory.Build.props`.

### Audio pipeline

`AudioRecorder` (OnlyR.Core) uses NAudio WASAPI capture → `SampleAggregator` (volume events) → codec-specific encoder (NAudio.Lame for MP3, etc.). `VolumeFader` handles fade-in/out at recording start/stop.

## Important constraints

- **Do not suggest changes to localization resource files** (`.resx` files). Translations are managed externally via a separate localisation workflow.
- Target platform is **Windows x86** only. Do not introduce cross-platform abstractions.
- Nullable reference types are enabled globally — all new code must be null-safe.
- Code analysis uses `OnlyR.ruleset` (CA* rules) plus Roslynator. Treat warnings as errors.
