using System;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingDestination;
using OnlyR.Core.Enums;
using OnlyR.Utils;
using Serilog;
using System.IO;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using OnlyR.Pages;
using OnlyR.ViewModel.Messages;

namespace OnlyR.ViewModel
{
   /// <summary>
   /// Main View model. The main viuew is just a container for pages
   /// <para>
   /// See http://www.galasoft.ch/mvvm for details of the mvvm light framework
   /// </para>
   /// </summary>
   public class MainViewModel : ViewModelBase
   {
      private Dictionary<string, FrameworkElement> _pages;
      private string _currentPageName;
      private IOptionsService _optionsService;
      private IAudioService _audioService;

      public MainViewModel(
         IAudioService audioService,
         IOptionsService optionsService,
         IRecordingDestinationService destService)
      {
         Messenger.Default.Register<NavigateMessage>(this, OnNavigate);
         _pages = new Dictionary<string, FrameworkElement>();

         _optionsService = optionsService;
         _audioService = audioService;

         // set up pages...
         SetupPage(RecordingPageViewModel.PageName, new RecordingPage(),
             new RecordingPageViewModel(audioService, optionsService, destService));

         SetupPage(SettingsPageViewModel.PageName, new SettingsPage(),
             new SettingsPageViewModel(audioService, optionsService));

         var state = new RecordingPageNavigationState
         {
            ShowSplash = true,
            StartRecording = optionsService.Options.StartRecordingOnLaunch
         };

         Messenger.Default.Send(new NavigateMessage(RecordingPageViewModel.PageName, state));
      }

      private void SetupPage(string pageName, FrameworkElement page, ViewModelBase pageModel)
      {
         page.DataContext = pageModel;
         _pages.Add(pageName, page);
      }

      private void OnNavigate(NavigateMessage message)
      {
         CurrentPage = _pages[message.TargetPage];
         _currentPageName = message.TargetPage;
         ((IPage)CurrentPage.DataContext).Activated(message.State);
      }

      private FrameworkElement _currentPage;
      public FrameworkElement CurrentPage
      {
         get
         {
            return _currentPage;
         }
         set
         {
            if (_currentPage != value)
            {
               _currentPage = value;
               RaisePropertyChanged(nameof(CurrentPage));
            }
         }
      }

      public void Closing(object sender, CancelEventArgs e)
      {
         // prevent window closing when recording...
         var recordingPageModel = (RecordingPageViewModel)_pages[RecordingPageViewModel.PageName].DataContext;
         recordingPageModel.Closing(sender, e);
         
         Messenger.Default.Send(new ShutDownMessage(_currentPageName));
         (_audioService as IDisposable)?.Dispose();
      }
   }
}