namespace OnlyR.Core.Recorder
{
    using System;

    /// <summary>
    /// To indicate there are no audio recording devices
    /// </summary>
    [Serializable]
    public class NoDevicesException : Exception
    {
        public NoDevicesException()
            : base("No recording devices found")
        {
        }
    }
}
