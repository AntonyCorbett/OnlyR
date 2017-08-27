namespace OnlyR.Core.EventArgs
{
    public class SamplesReportEventArgs : System.EventArgs
    {
        public float MaxSample { get; }
        public float MinSample { get; }

        public SamplesReportEventArgs(float minValue, float maxValue)
        {
            MaxSample = maxValue;
            MinSample = minValue;
        }
    }
}
