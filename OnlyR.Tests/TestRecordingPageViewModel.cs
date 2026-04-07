#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Exceptions;
using OnlyR.Model;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using CommunityToolkit.Mvvm.Messaging;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;
using OnlyR.ViewModel.Messages;

namespace OnlyR.Tests;

public sealed class TestRecordingPageViewModel
{
    [Test]
    public async Task InitialStatusIsNotRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.RecordingStatus;
        });

        await Assert.That(result).IsEqualTo(RecordingStatus.NotRecording);
    }

    [Test]
    public async Task IsNotRecordingTrueInitially()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.IsNotRecording;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRecordingFalseInitially()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.IsRecording;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task RecordingStatusSetterFiresPropertyChanged()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };

            vm.RecordingStatus = RecordingStatus.Recording;
            return props;
        });

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
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { ShowPauseRecordingButton = false };
            var vm = CreateViewModel(options);
            vm.RecordingStatus = RecordingStatus.Recording;
            return new ShowStopResult(vm.ShowStopOnly, vm.ShowStopAndPause);
        });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShowStopOnly).IsTrue();
        await Assert.That(result.ShowStopAndPause).IsFalse();
    }

    [Test]
    public async Task ShowStopAndPauseWhenPauseEnabled()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { ShowPauseRecordingButton = true };
            var vm = CreateViewModel(options);
            vm.RecordingStatus = RecordingStatus.Recording;
            return new ShowStopResult(vm.ShowStopOnly, vm.ShowStopAndPause);
        });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShowStopAndPause).IsTrue();
        await Assert.That(result.ShowStopOnly).IsFalse();
    }

    [Test]
    public async Task ClosingPreventsCloseWhenRecording()
    {
        var cancelValue = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Recording;
            var args = new CancelEventArgs();
            vm.Closing(this, args);
            return args.Cancel;
        });

        await Assert.That(cancelValue).IsTrue();
    }

    [Test]
    public async Task ClosingAllowsCloseWhenNotRecording()
    {
        var cancelValue = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var args = new CancelEventArgs();
            vm.Closing(this, args);
            return args.Cancel;
        });

        await Assert.That(cancelValue).IsFalse();
    }

    [Test]
    public async Task MaxRecordingTimeStringNullWhenZero()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { MaxRecordingTimeSeconds = 0 };
            var vm = CreateViewModel(options);
            return vm.MaxRecordingTimeString;
        });

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task MaxRecordingTimeStringFormattedWhenSet()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { MaxRecordingTimeSeconds = 3661 };
            var vm = CreateViewModel(options);
            return vm.MaxRecordingTimeString;
        });

        await Assert.That(result).IsEqualTo("01:01:01");
    }

    [Test]
    public async Task ElapsedTimeStrZeroWhenNotRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.ElapsedTimeStr;
        });

        await Assert.That(result).IsEqualTo("00:00:00");
    }

    [Test]
    public async Task IsPausedWhenStatusPaused()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Paused;
            return vm.IsPaused;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRecordingOrPausedWhenRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Recording;
            return vm.IsRecordingOrPaused;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRecordingOrPausedWhenPaused()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Paused;
            return vm.IsRecordingOrPaused;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsMaxRecordingTimeSpecifiedWhenSet()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel(new Options { MaxRecordingTimeSeconds = 300 });
            return vm.IsMaxRecordingTimeSpecified;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsMaxRecordingTimeSpecifiedWhenZero()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel(new Options { MaxRecordingTimeSeconds = 0 });
            return vm.IsMaxRecordingTimeSpecified;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task NoSettingsReturnsCommandLineValue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var commandLineMock = Mock.Of<ICommandLineService>();
            commandLineMock.NoSettings.Returns(true);
            var vm = CreateViewModel(cmdLineMock: commandLineMock);
            return vm.NoSettings;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NoFolderReturnsCommandLineValue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var commandLineMock = Mock.Of<ICommandLineService>();
            commandLineMock.NoFolder.Returns(true);
            var vm = CreateViewModel(cmdLineMock: commandLineMock);
            return vm.NoFolder;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NoSaveReturnsCommandLineValue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var commandLineMock = Mock.Of<ICommandLineService>();
            commandLineMock.NoSave.Returns(true);
            var vm = CreateViewModel(cmdLineMock: commandLineMock);
            return vm.NoSave;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSaveEnabledWhenNotCopyingNotRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.IsSaveEnabled;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsSaveEnabledFalseWhenCopying()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.IsCopying = true;
            return vm.IsSaveEnabled;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ErrorMsgSetterNotifiesPropertyChanged()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.ErrorMsg = "Test error";
            return props;
        });

        await Assert.That(changedProperties).IsNotNull();
        await Assert.That(changedProperties!).Contains("ErrorMsg");
    }

    [Test]
    public async Task StatusStrSetterUpdatesValue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.StatusStr = "Recording...";
            return vm.StatusStr;
        });

        await Assert.That(result).IsEqualTo("Recording...");
    }

    [Test]
    public async Task IsSaveVisibleFalseWhenNoSaveTrue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var commandLineMock = Mock.Of<ICommandLineService>();
            commandLineMock.NoSave.Returns(true);
            var vm = CreateViewModel(cmdLineMock: commandLineMock);
            return vm.IsSaveVisible;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task SaveHintEmptyWhenNoDrives()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.SaveHint;
        });

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task RemovableDriveMessageAddsMakesVisible()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var commandLineMock = Mock.Of<ICommandLineService>();
            commandLineMock.NoSave.Returns(false);
            var vm = CreateViewModel(cmdLineMock: commandLineMock);
            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage { DriveLetter = 'E', Added = true });
            return vm.IsSaveVisible;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task RemovableDriveRemovedMakesInvisible()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var commandLineMock = Mock.Of<ICommandLineService>();
            commandLineMock.NoSave.Returns(false);
            var vm = CreateViewModel(cmdLineMock: commandLineMock);
            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage { DriveLetter = 'E', Added = true });
            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage { DriveLetter = 'E', Added = false });
            return vm.IsSaveVisible;
        });

        await Assert.That(result).IsFalse();
    }

    private static RecordingPageViewModel CreateViewModel(Options? options = null, Mock<ICommandLineService>? cmdLineMock = null)
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Reset();
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

    [Test]
    public async Task VolumeLevelAsPercentageSetterFiresPropertyChanged()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.VolumeLevelAsPercentage = 50;
            return props;
        });

        await Assert.That(changedProperties!).Contains("VolumeLevelAsPercentage");
    }

    [Test]
    public async Task VolumeLevelAsPercentageNoChangeWhenSameValue()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.VolumeLevelAsPercentage = 50;
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.VolumeLevelAsPercentage = 50;
            return props;
        });

        await Assert.That(changedProperties!).DoesNotContain("VolumeLevelAsPercentage");
    }

    [Test]
    public async Task IsCopyingSetterFiresPropertyChanged()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.IsCopying = true;
            return props;
        });

        await Assert.That(changedProperties!).Contains("IsCopying");
        await Assert.That(changedProperties!).Contains("IsSaveEnabled");
    }

    [Test]
    public async Task IsCopyingNoChangeWhenSameValue()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.IsCopying = false; // already false
            return props;
        });

        await Assert.That(changedProperties!).DoesNotContain("IsCopying");
    }

    [Test]
    public async Task StatusStrSetterNoChangeWhenSameValue()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var initial = vm.StatusStr;
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.StatusStr = initial;
            return props;
        });

        await Assert.That(changedProperties!).DoesNotContain("StatusStr");
    }

    [Test]
    public async Task ErrorMsgNoChangeWhenSameValue()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.ErrorMsg = null; // already null
            return props;
        });

        await Assert.That(changedProperties!).DoesNotContain("ErrorMsg");
    }

    [Test]
    public async Task RecordingStatusSetterNoChangeWhenSameValue()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    props.Add(e.PropertyName);
                }
            };
            vm.RecordingStatus = RecordingStatus.NotRecording; // same as initial
            return props;
        });

        await Assert.That(changedProperties!).DoesNotContain("RecordingStatus");
    }

    [Test]
    public async Task IsRecordingOrStoppingWhenStopRequested()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.StopRequested;
            return vm.IsRecordingOrStopping;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsReadyToRecordFalseWhenRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Recording;
            return vm.IsReadyToRecord;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsReadyToRecordFalseWhenPaused()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Paused;
            return vm.IsReadyToRecord;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsSaveEnabledFalseWhenRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingStatus = RecordingStatus.Recording;
            return vm.IsSaveEnabled;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task SaveHintShowsSingleDrive()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage { DriveLetter = 'E', Added = true });
            return vm.SaveHint;
        });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsNotEmpty();
        await Assert.That(result).Contains("E");
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task SaveHintShowsMultipleDrives()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage { DriveLetter = 'E', Added = true });
            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage { DriveLetter = 'F', Added = true });
            return vm.SaveHint;
        });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Contains(",");
    }

    [Test]
    public async Task ActivatedWithNullStateDoesNotThrow()
    {
        await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.Activated(null);
        });
    }

    [Test]
    public async Task ActivatedWithSplashStateDoesNotThrow()
    {
        await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.Activated(new RecordingPageNavigationState { ShowSplash = true });
        });
    }

    [Test]
    public async Task PageNameIsRecordingPage() =>
        await Assert.That(RecordingPageViewModel.PageName).IsEqualTo("RecordingPage");

    // ========================================================================
    // GetAutoStopReason
    // ========================================================================

    [Test]
    public async Task AutoStopNoneWhenUnderLimits() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(30, 60, 0, 30))
            .IsEqualTo(AutoStopReason.None);

    [Test]
    public async Task AutoStopNoneWhenLimitsDisabled() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(100, 0, 50, 0))
            .IsEqualTo(AutoStopReason.None);

    [Test]
    public async Task AutoStopTimeLimitWhenExceeded() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(61, 60, 0, 30))
            .IsEqualTo(AutoStopReason.TimeLimit);

    [Test]
    public async Task AutoStopTimeLimitNotWhenAtExactLimit() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(60, 60, 0, 30))
            .IsEqualTo(AutoStopReason.None);

    [Test]
    public async Task AutoStopSilenceWhenExceeded() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(10, 0, 31, 30))
            .IsEqualTo(AutoStopReason.Silence);

    [Test]
    public async Task AutoStopSilenceNotWhenDisabled() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(10, 0, 31, 0))
            .IsEqualTo(AutoStopReason.None);

    [Test]
    public async Task AutoStopTimeLimitTakesPriorityOverSilence() =>
        await Assert.That(RecordingPageViewModel.GetAutoStopReason(61, 60, 31, 30))
            .IsEqualTo(AutoStopReason.TimeLimit);

    // ========================================================================
    // GetCopyErrorMessages
    // ========================================================================

    [Test]
    public async Task CopyErrorNoRecordingsException()
    {
        var ex = new NoRecordingsException();
        var messages = RecordingPageViewModel.GetCopyErrorMessages(ex);
        await Assert.That(messages.Length).IsEqualTo(1);
        await Assert.That(messages[0]).IsEqualTo(ex.Message);
    }

    [Test]
    public async Task CopyErrorAggregateWithNoSpaceException()
    {
        var inner = new NoSpaceException('D');
        var agg = new AggregateException(inner);
        var messages = RecordingPageViewModel.GetCopyErrorMessages(agg);
        await Assert.That(messages.Length).IsEqualTo(1);
        await Assert.That(messages[0]).IsEqualTo(inner.Message);
    }

    [Test]
    public async Task CopyErrorAggregateWithOtherException()
    {
        var agg = new AggregateException(new InvalidOperationException("bad"));
        var messages = RecordingPageViewModel.GetCopyErrorMessages(agg);
        await Assert.That(messages.Length).IsEqualTo(1);
        await Assert.That(messages[0]).IsNotEmpty();
    }

    [Test]
    public async Task CopyErrorAggregateWithMixedExceptions()
    {
        var agg = new AggregateException(
            new NoSpaceException('E'),
            new InvalidOperationException("fail"));
        var messages = RecordingPageViewModel.GetCopyErrorMessages(agg);
        await Assert.That(messages.Length).IsEqualTo(2);
    }

    [Test]
    public async Task CopyErrorGenericException()
    {
        var ex = new InvalidOperationException("something broke");
        var messages = RecordingPageViewModel.GetCopyErrorMessages(ex);
        await Assert.That(messages.Length).IsEqualTo(1);
        await Assert.That(messages[0]).IsNotEmpty();
    }

    private sealed record ShowStopResult(bool ShowStopOnly, bool ShowStopAndPause);
}

#pragma warning restore CA1416 // Validate platform compatibility
