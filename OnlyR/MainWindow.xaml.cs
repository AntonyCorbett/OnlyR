namespace OnlyR
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using CommonServiceLocator;
    using GalaSoft.MvvmLight.Messaging;
    using Services.Options;
    using Utils;
    using ViewModel;
    using ViewModel.Messages;

    /// <summary>
    /// MainWindow.xaml code-behind
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            
            Messenger.Default.Register<ShutDownApplicationMessage>(this, OnShutDownApplication);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var optionsService = ServiceLocator.Current.GetInstance<IOptionsService>();
            if (!string.IsNullOrEmpty(optionsService.Options.AppWindowPlacement))
            {
                this.SetPlacement(optionsService.Options.AppWindowPlacement);
            }

            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            RemovableDriveDetectionNativeMethods.WndProc(msg, wparam, lparam);
            return IntPtr.Zero;
        }

        private void OnShutDownApplication(ShutDownApplicationMessage obj)
        {
            Close();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPos();
            ((MainViewModel)DataContext).Closing(sender, e);
        }

        private void SaveWindowPos()
        {
            var optionsService = ServiceLocator.Current.GetInstance<IOptionsService>();
            optionsService.Options.AppWindowPlacement = this.GetPlacement();
            optionsService.Save();
        }
    }
}
