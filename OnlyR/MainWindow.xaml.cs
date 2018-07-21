namespace OnlyR
{
    using System;
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
            var optionsService = ServiceLocator.Current.GetInstance<IOptionsService>();
            if (!string.IsNullOrEmpty(optionsService.Options.AppWindowPlacement))
            {
                this.SetPlacement(optionsService.Options.AppWindowPlacement);
            }
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
        }
    }
}
