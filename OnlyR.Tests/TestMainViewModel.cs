#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Threading;
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

public class TestMainViewModel
{
    [Test]
    [NotInParallel("WpfApp")]
    public async Task TestNavigation()
    {
        NavigationTestResult? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                result = RunNavigationTest();
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

        // Initial page assertions
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.InitialPageIsNotNull).IsTrue();
        await Assert.That(result.InitialPageIsRecordingPage).IsTrue();
        await Assert.That(result.InitialDataContextIsRecordingVm).IsTrue();

        // Navigation assertions
        await Assert.That(result.AfterNavToSettingsIsSettingsPage).IsTrue();
        await Assert.That(result.AfterNavBackIsRecordingPage).IsTrue();

        // Recording assertions
        await Assert.That(result.ElapsedTimeBeforeRecording).IsEqualTo(TimeSpan.Zero.ToString("hh\\:mm\\:ss"));
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

    private static MainViewModel CreateMainViewModel()
    {
        var audioService = new MockAudioService();

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(new Options());

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