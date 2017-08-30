/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:OnlyR"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingDestination;


namespace OnlyR.ViewModel
{
   /// <summary>
   /// This class contains static references to all the view models in the
   /// application and provides an entry point for the bindings.
   /// </summary>
   public class ViewModelLocator
   {
      /// <summary>
      /// Initializes a new instance of the ViewModelLocator class.
      /// </summary>
      public ViewModelLocator()
      {
         ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

         ////if (ViewModelBase.IsInDesignModeStatic)
         ////{
         ////    // Create design time view services and models
         ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
         ////}
         ////else
         ////{
         ////    // Create run time view services and models
         ////    SimpleIoc.Default.Register<IDataService, DataService>();
         ////}

         SimpleIoc.Default.Register<IOptionsService, OptionsService>();
         SimpleIoc.Default.Register<IRecordingDestinationService, RecordingDestinationService>();
         SimpleIoc.Default.Register<IAudioService, AudioService>();
         SimpleIoc.Default.Register<MainViewModel>();
      }

      public MainViewModel Main
      {
         get
         {
            return ServiceLocator.Current.GetInstance<MainViewModel>();
         }
      }

      public static void Cleanup()
      {
         // TODO Clear the ViewModels
      }
   }
}