namespace OnlyR.Model
{
    /// <summary>
    /// Model for "Bit rate" combo in Settings page
    /// </summary>
    public class BitRateItem
    {
        public BitRateItem(string name, int actualBitRate)
        {
            Name = name;
            ActualBitRate = actualBitRate;
        }

        public string Name { get; }

        public int ActualBitRate { get; }
    }
}
