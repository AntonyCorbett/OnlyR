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
        public static void ClassInit(TestContext ctx)
        {
            Application.LoadComponent(
                new Uri("/OnlyR;component/App.xaml", UriKind.Relative));
        }

        [TestMethod]
        public void OpenOnRecordingPage()
        {
            var t = new Thread(delegate ()
            { 
                // open on recording page...
                var vm = CreateMainViewModel();

                Assert.IsTrue(vm.CurrentPage != null);
                Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
                Assert.IsTrue(vm.CurrentPage.DataContext is RecordingPageViewModel);
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [TestMethod]
        public void NavToOptionsPage()
        {
            var t = new Thread(delegate()
            {
                var vm = CreateMainViewModel();

                var rvm = (RecordingPageViewModel) vm.CurrentPage.DataContext;
                rvm.NavigateSettingsCommand.Execute(null);

                Assert.IsTrue(vm.CurrentPage is Pages.SettingsPage);
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [TestMethod]
        public void NavToRecordingPage()
        {
            var t = new Thread(delegate()
            {
                MainViewModel vm = CreateMainViewModel();

                RecordingPageViewModel rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
                rvm.NavigateSettingsCommand.Execute(null);

                // ReSharper disable once PossibleInvalidCastException
                SettingsPageViewModel svm = (SettingsPageViewModel)vm.CurrentPage.DataContext;
                svm.NavigateRecordingCommand.Execute(null);

                Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [TestMethod]
        public void StartRecording()
        {
            var t = new Thread(delegate ()
            {
                MainViewModel vm = CreateMainViewModel();

                RecordingPageViewModel rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
                Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.NotRecording);

                Assert.AreEqual(rvm.ElapsedTimeStr, TimeSpan.Zero.ToString("hh\\:mm\\:ss"));
                rvm.StartRecordingCommand.Execute(null);

                Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.Recording);

                rvm.StopRecordingCommand.Execute(null);
                Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.NotRecording);
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
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
