#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Model;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.PurgeRecordings;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;

namespace OnlyR.Tests;

public sealed class TestMainViewModel
{
    [Test]
    [NotInParallel("Messenger")]
    public async Task TestNavigation()
    {
        var result = await StaThreadHelper.RunOnSta(() => RunNavigationTest());

        // Initial page assertions
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.InitialPageIsNotNull).IsTrue();
        await Assert.That(result.InitialPageIsRecordingPage).IsTrue();
        await Assert.That(result.InitialDataContextIsRecordingVm).IsTrue();

        // Navigation assertions
        await Assert.That(result.AfterNavToSettingsIsSettingsPage).IsTrue();
        await Assert.That(result.AfterNavBackIsRecordingPage).IsTrue();

        // Recording assertions
        await Assert.That(result.ElapsedTimeBeforeRecording).IsEqualTo(TimeSpan.Zero.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture));
        await Assert.That(result.StatusAfterStart).IsEqualTo(RecordingStatus.Recording);
        await Assert.That(result.StatusAfterStop).IsEqualTo(RecordingStatus.NotRecording);
    }

    private static NavigationTestResult RunNavigationTest()
    {
        var vm = CreateMainViewModel();

        // Open on recording page
        var initialPageIsNotNull = vm.CurrentPage != null;
        var initialPageIsRecordingPage = vm.CurrentPage is Pages.RecordingPage;
        var initialDataContextIsRecordingVm = vm.CurrentPage?.DataContext is RecordingPageViewModel;

        // Navigate to options
        var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
        rvm.NavigateSettingsCommand.Execute(null);
        var afterNavToSettingsIsSettingsPage = vm.CurrentPage is Pages.SettingsPage;

        // Navigate back to recording
        var svm = (SettingsPageViewModel)vm.CurrentPage!.DataContext!;
        svm.NavigateRecordingCommand.Execute(null);
        var afterNavBackIsRecordingPage = vm.CurrentPage is Pages.RecordingPage;

        // Start and stop recording
        rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
        var elapsedTimeBeforeRecording = rvm.ElapsedTimeStr;
        rvm.StartRecordingCommand.Execute(null);
        var statusAfterStart = rvm.RecordingStatus;
        rvm.StopRecordingCommand.Execute(null);
        var statusAfterStop = rvm.RecordingStatus;

        return new NavigationTestResult(
            initialPageIsNotNull,
            initialPageIsRecordingPage,
            initialDataContextIsRecordingVm,
            afterNavToSettingsIsSettingsPage,
            afterNavBackIsRecordingPage,
            elapsedTimeBeforeRecording,
            statusAfterStart,
            statusAfterStop);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CurrentPageNameMatchesRecordingPage()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            return vm.CurrentPageName;
        });

        await Assert.That(result).IsEqualTo(RecordingPageViewModel.PageName);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task AlwaysOnTopReturnsOptionsValue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel(new Options { AlwaysOnTop = true });
            return vm.AlwaysOnTop;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ClosingWhenNotRecordingDoesNotCancel()
    {
        var cancelValue = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            var args = new System.ComponentModel.CancelEventArgs();
            vm.Closing(this, args);
            return args.Cancel;
        });

        await Assert.That(cancelValue).IsFalse();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ClosingWhenRecordingAndAllowCloseCancels()
    {
        var cancelValue = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { AllowCloseWhenRecording = true };
            var vm = CreateMainViewModel(options);

            // Start recording
            var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
            rvm.StartRecordingCommand.Execute(null);

            var args = new System.ComponentModel.CancelEventArgs();
            vm.Closing(this, args);
            var cancel = args.Cancel;

            // Stop recording to clean up
            rvm.StopRecordingCommand.Execute(null);
            return cancel;
        });

        await Assert.That(cancelValue).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ClosingWhenRecordingAndDefaultOptionsBlocksClose()
    {
        var cancelValue = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();

            // Start recording
            var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
            rvm.StartRecordingCommand.Execute(null);

            var args = new System.ComponentModel.CancelEventArgs();
            vm.Closing(this, args);
            var cancel = args.Cancel;

            // Stop recording to clean up
            rvm.StopRecordingCommand.Execute(null);
            return cancel;
        });

        await Assert.That(cancelValue).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task IsNotRecordingInitially()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
            return rvm.RecordingStatus;
        });

        await Assert.That(result).IsEqualTo(RecordingStatus.NotRecording);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CurrentPagePropertyChangedOnNavigation()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            var properties = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != null)
                {
                    properties.Add(e.PropertyName);
                }
            };

            var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
            rvm.NavigateSettingsCommand.Execute(null);
            return properties;
        });

        await Assert.That(changedProperties).Contains("CurrentPage");
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CurrentPageNameUpdatesOnNavigation()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
            rvm.NavigateSettingsCommand.Execute(null);
            return vm.CurrentPageName;
        });

        await Assert.That(result).IsEqualTo(SettingsPageViewModel.PageName);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task AlwaysOnTopFalseByDefault()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            return vm.AlwaysOnTop;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task TheSnackbarMessageQueueIsNotNull()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateMainViewModel();
            return vm.TheSnackbarMessageQueue != null;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task StartRecordingOnLaunchStartsRecording()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { StartRecordingOnLaunch = true };
            var vm = CreateMainViewModel(options);
            var rvm = (RecordingPageViewModel)vm.CurrentPage!.DataContext!;
            var status = rvm.RecordingStatus;
            rvm.StopRecordingCommand.Execute(null);
            return status;
        });

        await Assert.That(result).IsEqualTo(RecordingStatus.Recording);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task StartMinimizedSuppressesSplash()
    {
        var success = await StaThreadHelper.RunOnSta(() =>
        {
            var options = new Options { StartMinimized = true };
            var vm = CreateMainViewModel(options);
            return vm.CurrentPage != null;
        });

        await Assert.That(success).IsTrue();
    }

    // ========================================================================
    // ShouldShowUpdateNotification
    // ========================================================================

    [Test]
    public async Task ShouldShowUpdateWhenNewerAvailable() =>
        await Assert.That(MainViewModel.ShouldShowUpdateNotification(
            new Version(1, 0, 0, 0), new Version(2, 0, 0, 0))).IsTrue();

    [Test]
    public async Task ShouldNotShowUpdateWhenSameVersion() =>
        await Assert.That(MainViewModel.ShouldShowUpdateNotification(
            new Version(1, 0, 0, 0), new Version(1, 0, 0, 0))).IsFalse();

    [Test]
    public async Task ShouldNotShowUpdateWhenOlderAvailable() =>
        await Assert.That(MainViewModel.ShouldShowUpdateNotification(
            new Version(2, 0, 0, 0), new Version(1, 0, 0, 0))).IsFalse();

    [Test]
    public async Task ShouldNotShowUpdateWhenLatestNull() =>
        await Assert.That(MainViewModel.ShouldShowUpdateNotification(
            new Version(1, 0, 0, 0), null)).IsFalse();

    [Test]
    public async Task ShouldNotShowUpdateWhenCurrentNull() =>
        await Assert.That(MainViewModel.ShouldShowUpdateNotification(
            null, new Version(2, 0, 0, 0))).IsFalse();

    [Test]
    public async Task ShouldNotShowUpdateWhenBothNull() =>
        await Assert.That(MainViewModel.ShouldShowUpdateNotification(null, null)).IsFalse();

    private static MainViewModel CreateMainViewModel(Options? options = null)
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Reset();
        var audioService = new MockAudioService();

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(options ?? new Options());

        var destMock = Mock.Of<IRecordingDestinationService>();
        destMock.GetRecordingFileCandidate(Any<IOptionsService>(), Any<DateTime>(), Any<string?>())
            .Returns(new RecordingCandidate(DateTime.Now, 1, ".", "."));

        var commandLineMock = Mock.Of<ICommandLineService>();
        var copyMock = Mock.Of<ICopyRecordingsService>();
        var snackbarMock = Mock.Of<ISnackbarService>();
        var purgeMock = Mock.Of<IPurgeRecordingsService>();
        var silenceMock = Mock.Of<ISilenceService>();

        return new MainViewModel(
            audioService,
            optionsMock.Object,
            commandLineMock.Object,
            destMock.Object,
            copyMock.Object,
            snackbarMock.Object,
            purgeMock.Object,
            silenceMock.Object);
    }

    private sealed record NavigationTestResult(
        bool InitialPageIsNotNull,
        bool InitialPageIsRecordingPage,
        bool InitialDataContextIsRecordingVm,
        bool AfterNavToSettingsIsSettingsPage,
        bool AfterNavBackIsRecordingPage,
        string ElapsedTimeBeforeRecording,
        RecordingStatus StatusAfterStart,
        RecordingStatus StatusAfterStop);
}

#pragma warning restore CA1416 // Validate platform compatibility