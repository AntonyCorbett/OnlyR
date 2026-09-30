namespace OnlyR.Model;

/// <summary>
/// Model for the system-audio playback-device combo in Settings.
/// </summary>
public sealed class PlaybackDeviceItem
{
    public PlaybackDeviceItem(string deviceId, string deviceName)
    {
        DeviceId = deviceId.ToLowerInvariant();
        DeviceName = deviceName;
    }

    public string DeviceId { get; }

    public string DeviceName { get; }

    public override string ToString() => DeviceName;
}