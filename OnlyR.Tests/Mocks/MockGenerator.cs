﻿using Moq;
using OnlyR.Services.Audio;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.PurgeRecordings;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;

namespace OnlyR.Tests.Mocks
{
    public static class MockGenerator
    {
        public static IAudioService CreateAudioService()
        {
            return new MockAudioService();
        }

        public static Mock<IOptionsService> CreateOptionsService()
        {
            var m = new Mock<IOptionsService>();
            m.Setup(o => o.Options).Returns(new Options());

            return m;
        }

        public static Mock<IRecordingDestinationService> CreateRecordingsDestinationService()
        {
            return new Mock<IRecordingDestinationService>();
        }

        public static Mock<ICommandLineService> CreateCommandLineService()
        {
            return new Mock<ICommandLineService>();
        }

        public static Mock<ICopyRecordingsService> CreateCopyRecordingsService()
        {
            return new Mock<ICopyRecordingsService>();
        }

        public static Mock<ISnackbarService> CreateSnackbarService()
        {
            return new Mock<ISnackbarService>();
        }

        public static Mock<IPurgeRecordingsService> CreatePurgeRecordingsService()
        {
            return new Mock<IPurgeRecordingsService>();
        }

        public static Mock<ISilenceService> CreateSilenceService()
        {
            return new Mock<ISilenceService>();
        }
    }
}
