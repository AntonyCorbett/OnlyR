using System;
using OnlyR.Services.Options;

namespace OnlyR.Services.AudioSilence
{
    internal sealed class SilenceService : ISilenceService
    {
        private readonly IOptionsService _optionsService;
        private DateTime _nonSilenceLastDetected;

        public SilenceService(IOptionsService optionsService)
        {
            _optionsService = optionsService;
        }

        public int GetSecondsOfSilence()
        {
            if (_nonSilenceLastDetected == default)
            {
                return 0;
            }

            return (int)(DateTime.UtcNow - _nonSilenceLastDetected).TotalSeconds;
        }

        public void ReportVolume(int volumeLevelAsPercentage)
        {
            if (volumeLevelAsPercentage > _optionsService.Options.SilenceAsVolumePercentage)
            {
                _nonSilenceLastDetected = DateTime.UtcNow;
            }
        }

        public void Reset()
        {
            _nonSilenceLastDetected = DateTime.UtcNow;
        }
    }
}
