using System;

namespace OnlyR.Core.Recorder
{
    /// <summary>
    /// To indicate there are no audio recording devices
    /// </summary>
    [Serializable]
    public class NoDevicesException : Exception
    {
        public NoDevicesException()
            : base(Properties.Resources.NO_RECORDING_DEVICE)
        {
        }

        protected NoDevicesException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}
