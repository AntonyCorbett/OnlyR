﻿using System;
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
            switch (value)
            {
                case RecordingStatus.NotRecording:
                    return Properties.Resources.NOT_RECORDING;
                case RecordingStatus.Recording:
                    return Properties.Resources.RECORDING;
                case RecordingStatus.StopRequested:
                    return Properties.Resources.STOPPING;

                default:
                // ReSharper disable once RedundantCaseLabel
                case RecordingStatus.Unknown:
                    throw new ArgumentException(nameof(value));
            }
        }
    }
}
