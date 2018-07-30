namespace OnlyR.Tests
{
    using System;
    using System.Windows;
    using Core.Enums;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mocks;
    using ViewModel;

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
            // open on recording page...
            var vm = CreateMainViewModel();

            Assert.IsTrue(vm.CurrentPage != null);
            Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
            Assert.IsTrue(vm.CurrentPage.DataContext is RecordingPageViewModel);
        }

        [TestMethod]
        public void NavToOptionsPage()
        {
            var vm = CreateMainViewModel();

            var rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
            rvm.NavigateSettingsCommand.Execute(null);

            Assert.IsTrue(vm.CurrentPage is Pages.SettingsPage);
        }

        [TestMethod]
        public void NavToRecordingPage()
        {
            MainViewModel vm = CreateMainViewModel();

            RecordingPageViewModel rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
            rvm.NavigateSettingsCommand.Execute(null);

            // ReSharper disable once PossibleInvalidCastException
            SettingsPageViewModel svm = (SettingsPageViewModel)vm.CurrentPage.DataContext;
            svm.NavigateRecordingCommand.Execute(null);

            Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
        }

        [TestMethod]
        public void StartRecording()
        {
            MainViewModel vm = CreateMainViewModel();

            RecordingPageViewModel rvm = (RecordingPageViewModel)vm.CurrentPage.DataContext;
            Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.NotRecording);

            Assert.AreEqual(rvm.ElapsedTimeStr, TimeSpan.Zero.ToString("hh\\:mm\\:ss"));
            rvm.StartRecordingCommand.Execute(null);

            Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.Recording);

            rvm.StopRecordingCommand.Execute(null);
            Assert.IsTrue(rvm.RecordingStatus == RecordingStatus.NotRecording);
        }

        private MainViewModel CreateMainViewModel()
        {
            var audioService = MockGenerator.CreateAudioService();
            var optionsService = MockGenerator.CreateOptionsService();
            var destService = MockGenerator.CreateRecordingsDestinationService();
            var commandLineService = MockGenerator.CreateCommandLineService();
            var copyService = MockGenerator.CreateCopyRecordingsService();

            return new MainViewModel(
                audioService,
                optionsService.Object,
                commandLineService.Object,
                destService.Object,
                copyService.Object);
        }
    }
}
