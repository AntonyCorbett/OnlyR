using System;
using Microsoft.Practices.ServiceLocation;
using OnlyR.Services.Options;
using OnlyR.Utils;
using OnlyR.ViewModel;

namespace OnlyR
{
   /// <summary>
   /// MainWindow.xaml code-behind
   /// </summary>
   public partial class MainWindow
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      protected override void OnSourceInitialized(EventArgs e)
      {
         var optionsService = ServiceLocator.Current.GetInstance<IOptionsService>();
         if (!string.IsNullOrEmpty(optionsService.Options.AppWindowPlacement))
         {
            this.SetPlacement(optionsService.Options.AppWindowPlacement);
         }
      }

      private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
      {
         SaveWindowPos();
         MainViewModel m = (MainViewModel)DataContext;
         m.Closing(sender, e);
      }

      private void SaveWindowPos()
      {
         var optionsService = ServiceLocator.Current.GetInstance<IOptionsService>();
         optionsService.Options.AppWindowPlacement = this.GetPlacement();
      }

   }
}
