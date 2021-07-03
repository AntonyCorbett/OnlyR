namespace OnlyR.Model
{
    /// <summary>
    /// Model for "Channels" combo in Settings page
    /// </summary>
    public class ChannelItem
    {
        public ChannelItem(string name, int channelCount)
        {
            Name = name;
            ChannelCount = channelCount;
        }

        public string Name { get; }

        public int ChannelCount { get; }
    }
}
