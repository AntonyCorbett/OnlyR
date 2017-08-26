using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlyR.Core.Enums;

namespace OnlyR.Utils
{
    public static class EnumExtensions
    {
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
                    throw new ArgumentException(nameof(value));
            }
        }

    }
}
