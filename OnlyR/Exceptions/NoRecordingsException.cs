using System;

namespace OnlyR.Exceptions
{
    [Serializable]
    public sealed class NoRecordingsException : Exception
    {
        public NoRecordingsException()
            : base(Properties.Resources.NO_RECORDINGS)
        {
        }

        public NoRecordingsException(string msg)
            : base(msg)
        {
        }

        public NoRecordingsException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }
    }
}