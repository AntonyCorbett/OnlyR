using System.Threading;
using System;
using System.Windows;
using OnlyR.Core.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;

namespace OnlyR.Tests
{
#pragma warning disable CA1416 // Validate platform compatibility

    [TestClass]
    public class TestMainViewModel
    {
        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            Application.LoadComponent(
                new Uri("/OnlyR;component/App.xaml", UriKind.Relative));
        }

        [TestMethod]
        public void TestNavigation()
        {
            var success = false;

            var t = new Thread(() =>
            {
                var vm = CreateMainViewModel();

                OpenOnRecordingPage(vm);
                NavToOptionsPage(vm);
                NavToRecordingsPage(vm);
                StartRecording(vm);

                success = true;
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            Assert.IsTrue(success);
        }

        private static void StartRecording(MainViewModel vm)
        {
            Assert.IsNotNull(vm.CurrentPage);

            var rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
            
            Assert.AreEqual(rvm.ElapsedTimeStr, TimeSpan.Zero.ToString("hh\\:mm\\:ss"));
            rvm.StartRecordingCommand.Execute(null);

            Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.Recording);

            rvm.StopRecordingCommand.Execute(null);
            Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.NotRecording);
        }

        private static void NavToRecordingsPage(MainViewModel vm)
        {
            Assert.IsNotNull(vm.CurrentPage);

            var svm = (SettingsPageViewModel)vm.CurrentPage.DataContext;
            svm.NavigateRecordingCommand.Execute(null);

            Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
        }

        private static void NavToOptionsPage(MainViewModel vm)
        {
            Assert.IsNotNull(vm.CurrentPage);

            var rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
            rvm.NavigateSettingsCommand.Execute(null);

            Assert.IsTrue(vm.CurrentPage is Pages.SettingsPage);
        }

        private static void OpenOnRecordingPage(MainViewModel vm)
        {
            // open on recording page...
            
            Assert.IsTrue(vm.CurrentPage != null);
            Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
            Assert.IsTrue(vm.CurrentPage.DataContext is RecordingPageViewModel);
        }

        private static MainViewModel CreateMainViewModel()
        {
            var audioService = MockGenerator.CreateAudioService();
            var optionsService = MockGenerator.CreateOptionsService();
            var destService = MockGenerator.CreateRecordingsDestinationService();
            var commandLineService = MockGenerator.CreateCommandLineService();
            var copyService = MockGenerator.CreateCopyRecordingsService();
            var snackbarService = MockGenerator.CreateSnackbarService();
            var purgeRecordingsService = MockGenerator.CreatePurgeRecordingsService();
            var silenceService = MockGenerator.CreateSilenceService();

            return new MainViewModel(
                audioService,
                optionsService.Object,
                commandLineService.Object,
                destService.Object,
                copyService.Object,
                snackbarService.Object,
                purgeRecordingsService.Object,
                silenceService.Object);
        }
    }

#pragma warning restore CA1416 // Validate platform compatibility
}
