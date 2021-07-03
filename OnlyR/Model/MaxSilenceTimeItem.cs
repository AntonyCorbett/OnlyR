namespace OnlyR.Model
{
    public class MaxSilenceTimeItem
    {
        public MaxSilenceTimeItem(string name, int seconds)
        {

            Name = name;
            Seconds = seconds;
        }

        public string Name { get; }

        public int Seconds { get; }
    }
}
