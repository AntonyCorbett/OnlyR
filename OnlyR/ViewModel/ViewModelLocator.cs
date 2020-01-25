using OnlyR.Services.AudioSilence;

namespace OnlyR.ViewModel
{
    using CommonServiceLocator;
    using GalaSoft.MvvmLight.Ioc;
    using OnlyR.Services.PurgeRecordings;
    using Services.Audio;
    using Services.Options;
    using Services.RecordingCopies;
    using Services.RecordingDestination;
    using Services.Snackbar;

    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<IOptionsService, OptionsService>();
            SimpleIoc.Default.Register<ICommandLineService, CommandLineService>();
            SimpleIoc.Default.Register<IRecordingDestinationService, RecordingDestinationService>();
            SimpleIoc.Default.Register<IAudioService, AudioService>();
            SimpleIoc.Default.Register<ICopyRecordingsService, CopyRecordingsService>();
            SimpleIoc.Default.Register<IDriveEjectionService, DriveEjectionService>();
            SimpleIoc.Default.Register<ISnackbarService, SnackbarService>();
            SimpleIoc.Default.Register<IPurgeRecordingsService, PurgeRecordingsService>();
            SimpleIoc.Default.Register<ISilenceService, SilenceService>();
            SimpleIoc.Default.Register<MainViewModel>();
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
    }
}