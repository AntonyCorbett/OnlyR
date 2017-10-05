using System.Collections.Generic;
using OnlyR.Model;

namespace OnlyR.Services.Options
{
    public interface IOptionsService
    {
        Options Options { get; }
        void Save();

        IEnumerable<SampleRateItem> GetSupportedSampleRates();
        IEnumerable<ChannelItem> GetSupportedChannels();
        IEnumerable<BitRateItem> GetSupportedMp3BitRates();
    }
}
