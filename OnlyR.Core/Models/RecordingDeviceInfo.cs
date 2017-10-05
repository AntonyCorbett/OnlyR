namespace OnlyR.Core.Models
{
    /// <summary>
    /// Represents a Windows recording device
    /// </summary>
    public class RecordingDeviceInfo
    {
        /// <summary>
        /// Id of the device
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// name of the device (unfortunately trimmed to 31 characters by the Win API that we are using)
        /// </summary>
        public string Name { get; set; }
    }
}
