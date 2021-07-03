namespace OnlyR.Model
{
    public class RecordingLifeTimeItem
    {
        public RecordingLifeTimeItem(string description, int days)
        {
            Description = description;
            Days = days;
        }

        public string Description { get; }

        public int Days { get; }
    }
}
