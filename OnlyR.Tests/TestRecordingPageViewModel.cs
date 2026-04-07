#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Model;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;

namespace OnlyR.Tests;

public sealed class TestRecordingPageViewModel
{
    [Test]
    public async Task InitialStatusIsNotRecording()
    {
        RecordingStatus? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.RecordingStatus;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsEqualTo(RecordingStatus.NotRecording);
    }

    [Test]
    public async Task IsNotRecordingTrueInitially()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.IsNotRecording;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRecordingFalseInitially()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.IsRecording;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task RecordingStatusSetterFiresPropertyChanged()
    {
        List<string>? changedProperties = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                changedProperties = [];
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != null)
                    {
                        changedProperties.Add(e.PropertyName);
                    }
                };

                vm.RecordingStatus = RecordingStatus.Recording;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(changedProperties).IsNotNull();
        await Assert.That(changedProperties!).Contains("RecordingStatus");
        await Assert.That(changedProperties).Contains("IsNotRecording");
        await Assert.That(changedProperties).Contains("IsRecording");
        await Assert.That(changedProperties).Contains("IsRecordingOrStopping");
        await Assert.That(changedProperties).Contains("IsReadyToRecord");
        await Assert.That(changedProperties).Contains("ShowStopOnly");
        await Assert.That(changedProperties).Contains("ShowStopAndPause");
        await Assert.That(changedProperties).Contains("IsSaveEnabled");
    }

    [Test]
    public async Task ShowStopOnlyWhenPauseDisabled()
    {
        ShowStopResult? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var options = new Options { ShowPauseRecordingButton = false };
                var vm = CreateViewModel(options);
                vm.RecordingStatus = RecordingStatus.Recording;
                result = new ShowStopResult(vm.ShowStopOnly, vm.ShowStopAndPause);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShowStopOnly).IsTrue();
        await Assert.That(result.ShowStopAndPause).IsFalse();
    }

    [Test]
    public async Task ShowStopAndPauseWhenPauseEnabled()
    {
        ShowStopResult? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var options = new Options { ShowPauseRecordingButton = true };
                var vm = CreateViewModel(options);
                vm.RecordingStatus = RecordingStatus.Recording;
                result = new ShowStopResult(vm.ShowStopOnly, vm.ShowStopAndPause);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShowStopAndPause).IsTrue();
        await Assert.That(result.ShowStopOnly).IsFalse();
    }

    [Test]
    public async Task ClosingPreventsCloseWhenRecording()
    {
        bool? cancelValue = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.RecordingStatus = RecordingStatus.Recording;
                var args = new CancelEventArgs();
                vm.Closing(this, args);
                cancelValue = args.Cancel;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(cancelValue).IsTrue();
    }

    [Test]
    public async Task ClosingAllowsCloseWhenNotRecording()
    {
        bool? cancelValue = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                var args = new CancelEventArgs();
                vm.Closing(this, args);
                cancelValue = args.Cancel;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(cancelValue).IsFalse();
    }

    [Test]
    public async Task MaxRecordingTimeStringNullWhenZero()
    {
        string? result = "placeholder";

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var options = new Options { MaxRecordingTimeSeconds = 0 };
                var vm = CreateViewModel(options);
                result = vm.MaxRecordingTimeString;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task MaxRecordingTimeStringFormattedWhenSet()
    {
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var options = new Options { MaxRecordingTimeSeconds = 3661 };
                var vm = CreateViewModel(options);
                result = vm.MaxRecordingTimeString;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsEqualTo("01:01:01");
    }

    [Test]
    public async Task ElapsedTimeStrZeroWhenNotRecording()
    {
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.ElapsedTimeStr;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsEqualTo("00:00:00");
    }

    [Test]
    public async Task IsPausedWhenStatusPaused()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.RecordingStatus = RecordingStatus.Paused;
                result = vm.IsPaused;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRecordingOrPausedWhenRecording()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.RecordingStatus = RecordingStatus.Recording;
                result = vm.IsRecordingOrPaused;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRecordingOrPausedWhenPaused()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.RecordingStatus = RecordingStatus.Paused;
                result = vm.IsRecordingOrPaused;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsMaxRecordingTimeSpecifiedWhenSet()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel(new Options { MaxRecordingTimeSeconds = 300 });
                result = vm.IsMaxRecordingTimeSpecified;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsMaxRecordingTimeSpecifiedWhenZero()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel(new Options { MaxRecordingTimeSeconds = 0 });
                result = vm.IsMaxRecordingTimeSpecified;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task NoSettingsReturnsCommandLineValue()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var commandLineMock = Mock.Of<ICommandLineService>();
                commandLineMock.NoSettings.Returns(true);
                var vm = CreateViewModel(cmdLineMock: commandLineMock);
                result = vm.NoSettings;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NoFolderReturnsCommandLineValue()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var commandLineMock = Mock.Of<ICommandLineService>();
                commandLineMock.NoFolder.Returns(true);
                var vm = CreateViewModel(cmdLineMock: commandLineMock);
                result = vm.NoFolder;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NoSaveReturnsCommandLineValue()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var commandLineMock = Mock.Of<ICommandLineService>();
                commandLineMock.NoSave.Returns(true);
                var vm = CreateViewModel(cmdLineMock: commandLineMock);
                result = vm.NoSave;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSaveEnabledWhenNotCopyingNotRecording()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.IsSaveEnabled;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSaveEnabledFalseWhenCopying()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.IsCopying = true;
                result = vm.IsSaveEnabled;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ErrorMsgSetterNotifiesPropertyChanged()
    {
        List<string>? changedProperties = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                changedProperties = [];
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != null)
                    {
                        changedProperties.Add(e.PropertyName);
                    }
                };
                vm.ErrorMsg = "Test error";
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(changedProperties).IsNotNull();
        await Assert.That(changedProperties!).Contains("ErrorMsg");
    }

    private static RecordingPageViewModel CreateViewModel(Options? options = null, Mock<ICommandLineService>? cmdLineMock = null)
    {
        var audioService = new MockAudioService();

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(options ?? new Options());

        var destMock = Mock.Of<IRecordingDestinationService>();
        destMock.GetRecordingFileCandidate(Any<IOptionsService>(), Any<DateTime>(), Any<string?>())
            .Returns(new RecordingCandidate(DateTime.Now, 1, ".", "."));

        var commandLineMock = cmdLineMock ?? Mock.Of<ICommandLineService>();
        var copyMock = Mock.Of<ICopyRecordingsService>();
        var snackbarMock = Mock.Of<ISnackbarService>();
        var silenceMock = Mock.Of<ISilenceService>();

        return new RecordingPageViewModel(
            audioService,
            optionsMock.Object,
            commandLineMock.Object,
            destMock.Object,
            copyMock.Object,
            snackbarMock.Object,
            silenceMock.Object);
    }

    private sealed record ShowStopResult(bool ShowStopOnly, bool ShowStopAndPause);
}

#pragma warning restore CA1416 // Validate platform compatibility
