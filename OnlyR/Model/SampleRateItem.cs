namespace OnlyR.Model
{
    /// <summary>
    /// Model for the "MP3 Sample rate" combo in Settings page
    /// </summary>
    public class SampleRateItem
    {
        public SampleRateItem(string name, int actualSampleRate)
        {
            Name = name;
            ActualSampleRate = actualSampleRate;
        }

        public string Name { get; }

        public int ActualSampleRate { get; }
    }
}
