using System.Runtime.Serialization;

namespace OnlyR.Exceptions
{
    using System;

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

        private NoRecordingsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
