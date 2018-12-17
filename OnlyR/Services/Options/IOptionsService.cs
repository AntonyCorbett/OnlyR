namespace OnlyR.Services.Options
{
    using System.Collections.Generic;
    using Model;

    public interface IOptionsService
    {
        Options Options { get; }

        void Save();

        SampleRateItem[] GetSupportedSampleRates();

        ChannelItem[] GetSupportedChannels();

        BitRateItem[] GetSupportedMp3BitRates();
    }
}
