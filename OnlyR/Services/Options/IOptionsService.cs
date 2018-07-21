namespace OnlyR.Services.Options
{
    using System.Collections.Generic;
    using Model;

    public interface IOptionsService
    {
        Options Options { get; }

        void Save();

        IEnumerable<SampleRateItem> GetSupportedSampleRates();

        IEnumerable<ChannelItem> GetSupportedChannels();

        IEnumerable<BitRateItem> GetSupportedMp3BitRates();
    }
}
