namespace OnlyR.Services.Options
{
    using Model;

    public interface IOptionsService
    {
        Options Options { get; }

        string Culture { get; set; }

        SampleRateItem[] GetSupportedSampleRates();

        ChannelItem[] GetSupportedChannels();

        BitRateItem[] GetSupportedMp3BitRates();

        void Save();
    }
}
