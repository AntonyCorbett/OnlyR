namespace OnlyR.Core.Models;

/// <summary>
/// Describes a Windows playback device that can be captured through WASAPI loopback.
/// </summary>
public sealed class PlaybackDeviceInfo
{
    public PlaybackDeviceInfo(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }
}