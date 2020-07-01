namespace OnlyR.ViewModel
{
    using AutoMapper;
    using CommonServiceLocator;
    using GalaSoft.MvvmLight.Ioc;
    using OnlyR.Model;
    using Services.Audio;
    using Services.AudioSilence;
    using Services.Options;
    using Services.PurgeRecordings;
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
            SimpleIoc.Default.Register(MapperFactory);
            SimpleIoc.Default.Register<ICopyRecordingsService, CopyRecordingsService>();
            SimpleIoc.Default.Register<IDriveEjectionService, DriveEjectionService>();
            SimpleIoc.Default.Register<ISnackbarService, SnackbarService>();
            SimpleIoc.Default.Register<IPurgeRecordingsService, PurgeRecordingsService>();
            SimpleIoc.Default.Register<ISilenceService, SilenceService>();
            SimpleIoc.Default.Register<MainViewModel>();
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        private IMapper MapperFactory()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ObjectMappingProfile>();
            });
            
            return new Mapper(config);
        }
    }
}