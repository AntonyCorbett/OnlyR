using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Windows;
using System.Windows.Interop;
using OnlyR.Services.Options;
using OnlyR.Utils;
using OnlyR.ViewModel;
using OnlyR.ViewModel.Messages;

namespace OnlyR
{
    /// <summary>
    /// MainWindow.xaml code-behind.
    /// </summary>
    public partial class MainWindow
    {
        private const double MainWindowWidth = 268;
        private const double MainWindowHeight = 300;

        private const double SettingsWindowDefWidth = 400;
        private const double SettingsWindowDefHeight = 400;

        private const double SettingsWindowMaxWidth = 500;
        private const double SettingsWindowMaxHeight = 770;

        public MainWindow()
        {
            InitializeComponent();
            
            WeakReferenceMessenger.Default.Register<ShutDownApplicationMessage>(this, OnShutDownApplication);
            WeakReferenceMessenger.Default.Register<NavigateMessage>(this, OnNavigate);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            AdjustMainWindowPositionAndSize();

            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private void OnNavigate(object recipient, NavigateMessage message)
        {
            if (message.OriginalPageName.Equals(SettingsPageViewModel.PageName))
            {
                // store the size of the settings page...
                SaveSettingsWindowSize();
            }

            if (message.TargetPageName.Equals(RecordingPageViewModel.PageName))
            {
                // We don't allow the main window to be resized...
                ResizeMode = ResizeMode.CanMinimize;
                WindowState = WindowState.Normal;
                Width = MainWindowWidth;
                Height = MainWindowHeight;
            }
            else if (message.TargetPageName.Equals(SettingsPageViewModel.PageName))
            {
                // Settings window can be resized...
                ResizeMode = ResizeMode.CanResize;
                MinHeight = MainWindowHeight;
                MinWidth = MainWindowWidth;

                MaxHeight = SettingsWindowMaxHeight;
                MaxWidth = SettingsWindowMaxWidth;

                var optionsService = Ioc.Default.GetService<IOptionsService>();
                var sz = optionsService?.Options?.SettingsPageSize ?? default;
                if (sz != default)
                {
                    Width = sz.Width;
                    Height = sz.Height;
                }
                else
                {
                    Width = SettingsWindowDefWidth;
                    Height = SettingsWindowDefHeight;
                }
            }
        }

        private void SaveSettingsWindowSize()
        {
            var optionsService = Ioc.Default.GetService<IOptionsService>();
            if (optionsService != null)
            {
                optionsService.Options.SettingsPageSize = new Size(Width, Height);
            }
        }

        private void AdjustMainWindowPositionAndSize()
        {
            var optionsService = Ioc.Default.GetService<IOptionsService>();
            if (optionsService != null)
            {
                if (!string.IsNullOrEmpty(optionsService.Options.AppWindowPlacement))
                {
                    this.SetPlacement(optionsService.Options.AppWindowPlacement, optionsService.Options.StartMinimized);
                }
                else if (optionsService.Options.StartMinimized)
                {
                    WindowState = WindowState.Minimized;
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            RemovableDriveDetectionNativeMethods.WndProc(msg, wparam, lparam);
            return IntPtr.Zero;
        }

        private void OnShutDownApplication(object recipient, ShutDownApplicationMessage obj)
        {
            Close();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPos();

            var m = (MainViewModel)DataContext;

            if (m.CurrentPageName != null && 
                m.CurrentPageName.Equals(SettingsPageViewModel.PageName))
            {
                SaveSettingsWindowSize();
            }

            m.Closing(sender, e);
        }

        private void SaveWindowPos()
        {
            var optionsService = Ioc.Default.GetService<IOptionsService>();
            if (optionsService != null)
            {
                optionsService.Options.AppWindowPlacement = this.GetPlacement();
                optionsService.Save();
            }
        }
    }
}
