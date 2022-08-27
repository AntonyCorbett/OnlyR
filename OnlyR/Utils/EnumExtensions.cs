using System;
using OnlyR.Core.Enums;

namespace OnlyR.Utils
{
    // ReSharper disable once UnusedMember.Global
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets descriptive text for the specified enum value (or throws if not found)
        /// </summary>
        /// <param name="value">Enum value</param>
        /// <returns>Descriptive text</returns>
        // ReSharper disable once UnusedMember.Global
        public static string GetDescriptiveText(this RecordingStatus value)
        {
            return value switch
            {
                RecordingStatus.NotRecording => Properties.Resources.NOT_RECORDING,
                RecordingStatus.Recording => Properties.Resources.RECORDING,
                RecordingStatus.StopRequested => Properties.Resources.STOPPING,
                RecordingStatus.Unknown => throw new ArgumentException($"Unknown: {value}"),
                _ => throw new ArgumentException($"Unknown: {value}")
            };
        }
    }
}
