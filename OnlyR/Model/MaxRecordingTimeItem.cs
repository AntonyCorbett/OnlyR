namespace OnlyR.Model
{
    /// <summary>
    /// Model for "Max recording time" combo in Settings page
    /// </summary>
    public class MaxRecordingTimeItem
    {
        public MaxRecordingTimeItem(string name, int actualSeconds)
        {
            Name = name;
            ActualSeconds = actualSeconds;
        }

        public string Name { get; }

        public int ActualSeconds { get; }
    }
}
