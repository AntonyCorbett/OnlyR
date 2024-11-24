using OnlyR.Core.Enums;

namespace OnlyR.Model
{
    /// <summary>
    /// Represents an audio codec option in the settings
    /// </summary>
    public class CodecItem
    {
        public CodecItem(string name, AudioCodec codec)
        {
            Name = name;
            Codec = codec;
        }

        public string Name { get; }

        public AudioCodec Codec { get; }
    }
}
