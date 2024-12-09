using System.Runtime.Serialization;

namespace OnlyR.Core.Enums
{
    public enum AudioCodec
    {
        [EnumMember(Value = "MP3")]
        Mp3,
        [EnumMember(Value = "WAV")]
        Wav
    }
}