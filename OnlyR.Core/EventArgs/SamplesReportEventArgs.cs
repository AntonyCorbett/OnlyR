namespace OnlyR.Core.EventArgs
{
    /// <summary>
    /// Used to report to clients a summary of recent audio samples
    /// </summary>
    public class SamplesReportEventArgs : System.EventArgs
    {
        public SamplesReportEventArgs(float minValue, float maxValue)
        {
            MaxSample = maxValue;
            MinSample = minValue;
        }

        public float MaxSample { get; }

        public float MinSample { get; }
    }
}
