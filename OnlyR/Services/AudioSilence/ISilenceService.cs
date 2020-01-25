namespace OnlyR.Services.AudioSilence
{
    public interface ISilenceService
    {
        int GetSecondsOfSilence();
        
        void ReportVolume(int volumeLevelAsPercentage);

        void Reset();
    }
}
