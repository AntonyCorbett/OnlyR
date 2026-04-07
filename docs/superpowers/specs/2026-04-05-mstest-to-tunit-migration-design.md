# Phase 1: MSTest to TUnit + Moq to TUnit.Mocks/Rocks Migration

## Context

OnlyR has 3 test files using MSTest 4.1.0 and Moq 4.20.72. The goal is to migrate to TUnit (a modern, source-generator-based .NET test framework) and replace Moq with TUnit.Mocks (primary, currently beta) and Rocks (stable fallback). This is Phase 1 of a two-phase effort — Phase 2 will introduce new tests.

The test surface is small, so we also restructure during migration (Approach B): remove the `MockGenerator` factory and inline mocks into test classes for idiomatic TUnit.

## Package Changes

### `Directory.Packages.props`

**Remove:**
- `Microsoft.NET.Test.Sdk` 18.3.0
- `MSTest.TestFramework` 4.1.0
- `MSTest.TestAdapter` 4.1.0
- `Moq` 4.20.72

**Add:**
- `TUnit` (latest stable)
- `TUnit.Mocks` (latest prerelease — beta, requires C# 14 / .NET 10)
- `Rocks` (10.1.0 — stable fallback)

**Keep:**
- `coverlet.collector` 8.0.1

### `OnlyR.Tests/OnlyR.Tests.csproj`

Update `<PackageReference>` entries to match the new packages. The project already targets `net10.0-windows` (x86), which satisfies TUnit.Mocks' C# 14 requirement.

### `OnlyR.Tests/app.config` — DELETE

Binding redirects for `Castle.Core`, `System.ValueTuple`, and `System.Runtime.CompilerServices.Unsafe` are Moq/DynamicProxy dependencies. No longer needed.

## Test Attribute Migration

| MSTest | TUnit | Notes |
|--------|-------|-------|
| `[TestClass]` | _(removed)_ | TUnit doesn't require class-level attributes |
| `[TestMethod]` | `[Test]` | |
| `[ClassInitialize]` | `[Before(Class)]` | Static method, takes `ClassHookContext` parameter |

## Assertion Migration

TUnit assertions are async and fluent. All test methods become `async Task`.

| MSTest | TUnit |
|--------|-------|
| `Assert.AreEqual(expected, actual)` | `await Assert.That(actual).IsEqualTo(expected)` |
| `Assert.AreNotEqual(expected, actual)` | `await Assert.That(actual).IsNotEqualTo(expected)` |
| `Assert.IsTrue(value)` | `await Assert.That(value).IsTrue()` |
| `Assert.IsNotNull(value)` | `await Assert.That(value).IsNotNull()` |

## Per-File Migration

### `TestEnumExtensions.cs`

- Remove `[TestClass]`, swap `[TestMethod]` to `[Test]`
- Remove `#pragma warning disable S2699` (tests should include assertions)
- Add proper assertion: `await Assert.That(description).IsNotNull()` for each enum value
- Method becomes `async Task`
- No mocks involved

### `TestOptions.cs`

- Remove `[TestClass]`, swap `[TestMethod]` to `[Test]`
- Convert 6x `Assert.AreNotEqual` to `await Assert.That(...).IsNotEqualTo(...)`
- Method becomes `async Task`
- No mocks involved

### `TestMainViewModel.cs`

- Remove `[TestClass]`, swap `[TestMethod]` to `[Test]`
- `[ClassInitialize] static void ClassInit(TestContext _)` becomes `[Before(Class)] public static async Task ClassInit(ClassHookContext context)` — the `Application.LoadComponent` call stays the same
- Convert assertions to async fluent form (6x `IsTrue`, 2x `IsNotNull`, 1x `IsEqualTo`)
- Manual STA thread creation stays as-is (TUnit has no built-in STA support; the current `new Thread(...)` + `SetApartmentState(ApartmentState.STA)` approach is framework-independent)
- Wrap STA thread in `TaskCompletionSource` pattern instead of blocking `Thread.Join()`: create a `TaskCompletionSource`, run test logic on the STA thread calling `SetResult()`/`SetException()`, and `await tcs.Task` from the async test method
- Inline mocks from `MockGenerator` (see next section)

## Mock Migration

### Strategy

- **TUnit.Mocks** (beta) is primary — source-generated extension methods on `Mock<T>`, implicit conversion to `T`
- **Rocks** (10.1.0, stable) is fallback — used if TUnit.Mocks can't handle a specific mock pattern (e.g., complex matchers)
- **Hand-written `MockAudioService`** stays as-is — it's a behavioral simulation (DispatcherTimer, events, random volume) that neither mocking library should replace

### `MockGenerator.cs` — DELETE

All 8 factory methods are inlined into `TestMainViewModel.cs` where they're consumed:

| Mock | Current (Moq) | Target |
|------|---------------|--------|
| `IAudioService` | Hand-written `MockAudioService` | Stays as-is (no change) |
| `IOptionsService` | `Mock<T>` + `.Setup(o => o.Options).Returns(new Options())` | TUnit.Mocks with source-generated `.Options.Returns(...)` |
| `IRecordingDestinationService` | `Mock<T>` + `.Setup()` with `It.IsAny<>` matchers | TUnit.Mocks if it supports argument matchers (beta API); otherwise Rocks for this specific mock |
| `ICommandLineService` | Bare `Mock<T>` | TUnit.Mocks bare mock |
| `ICopyRecordingsService` | Bare `Mock<T>` | TUnit.Mocks bare mock |
| `ISnackbarService` | Bare `Mock<T>` | TUnit.Mocks bare mock |
| `IPurgeRecordingsService` | Bare `Mock<T>` | TUnit.Mocks bare mock |
| `ISilenceService` | Bare `Mock<T>` | TUnit.Mocks bare mock |

### `MockAudioService.cs` — KEEP

No changes. Hand-written test double implementing `IAudioService`. Framework-independent.

## CI Changes

### `.github/workflows/app-ci.yml`

Add after the existing Build step:

```yaml
- name: Test
  run: dotnet test OnlyR.Tests/OnlyR.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=test-results.trx" --results-directory ./test-results

- name: Upload test results
  if: failure()
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: ./test-results
```

TUnit uses the standard `dotnet test` runner via its test adapter, so `--logger trx` works without framework-specific flags.

## Files Changed

| File | Action |
|------|--------|
| `Directory.Packages.props` | Edit (swap packages) |
| `OnlyR.Tests/OnlyR.Tests.csproj` | Edit (update references) |
| `OnlyR.Tests/app.config` | Delete |
| `OnlyR.Tests/Mocks/MockGenerator.cs` | Delete |
| `OnlyR.Tests/Mocks/MockAudioService.cs` | No change |
| `OnlyR.Tests/TestEnumExtensions.cs` | Edit (attributes, add assertion, remove pragma) |
| `OnlyR.Tests/TestOptions.cs` | Edit (attributes, assertions) |
| `OnlyR.Tests/TestMainViewModel.cs` | Edit (attributes, assertions, inline mocks, async pattern) |
| `.github/workflows/app-ci.yml` | Edit (add test + upload steps) |

## Verification

1. `dotnet build` — solution compiles without errors
2. `dotnet test OnlyR.Tests/OnlyR.Tests.csproj` — all 3 tests pass
3. Check LSP diagnostics for type errors or missing imports
4. Verify CI workflow syntax is valid (push to branch, observe GitHub Actions run)
