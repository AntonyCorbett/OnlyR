namespace OnlyR.Model
{
    /// <summary>
    /// Model for "Recording device" combo in Settings page
    /// </summary>
    public class RecordingDeviceItem
    {
        public RecordingDeviceItem(int deviceId, string deviceName)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
        }

        public int DeviceId { get; }

        public string DeviceName { get; }
    }
}
