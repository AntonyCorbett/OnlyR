using System;
using System.Runtime.Serialization;

namespace OnlyR.Exceptions
{
    [Serializable]
    public sealed class NoSpaceException : Exception
    {
        public NoSpaceException()
        {
        }

        public NoSpaceException(char driveLetter)
            : base(string.Format(Properties.Resources.NO_SPACE, driveLetter.ToString()))
        {
        }

        public NoSpaceException(string msg)
            : base(msg)
        {
        }

        public NoSpaceException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        private NoSpaceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
