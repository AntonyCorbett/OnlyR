using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlyR.Core.Enums;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;

namespace OnlyR.Tests
{
    [TestClass]
    public class TestMainViewModel
    {
        private MainViewModel CreateMainViewModel()
        {
            var audioService = MockGenerator.CreateAudioService();
            var optionsService = MockGenerator.CreateOptionsService();
            var destService = MockGenerator.CreateRecordingsDestinationService();
            
            return new MainViewModel(audioService, optionsService.Object, destService.Object);
        }

        [TestMethod]
        public void OpenOnRecordingPage()
        {
            // open on recording page...
            MainViewModel vm = CreateMainViewModel();

            Assert.IsTrue(vm.CurrentPage != null);
            Assert.IsTrue(vm.CurrentPage is Pages.RecordingPage);
            Assert.IsTrue(vm.CurrentPage.DataContext is RecordingPageViewModel);
        }

        [TestMethod]
        public void NavToOptionsPage()
        {
            MainViewModel vm = CreateMainViewModel();

            RecordingPageViewModel rvm = (RecordingPageViewModel) vm.CurrentPage.DataContext;
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
            SettingsPageViewModel svm = (SettingsPageViewModel) vm.CurrentPage.DataContext;
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
    }
}
