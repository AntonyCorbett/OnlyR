namespace OnlyR.Core.Models
{
    /// <summary>
    /// Represents a Windows recording device
    /// </summary>
    public class RecordingDeviceInfo
    {
        public RecordingDeviceInfo(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Id of the device
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// name of the device (unfortunately trimmed to 31 characters by the Win API that we are using)
        /// </summary>
        public string Name { get; }
    }
}
