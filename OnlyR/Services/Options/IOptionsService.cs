using OnlyR.Model;

namespace OnlyR.Services.Options
{
    public interface IOptionsService
    {
        Options Options { get; }

        string? Culture { get; set; }

        SampleRateItem[] GetSupportedSampleRates();

        ChannelItem[] GetSupportedChannels();

        BitRateItem[] GetSupportedMp3BitRates();

        void Save();
    }
}
