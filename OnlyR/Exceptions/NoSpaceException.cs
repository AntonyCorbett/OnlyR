using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Interop;

namespace OnlyR.Exceptions
{
    [Serializable]
    public sealed class NoSpaceException : Exception
    {
        public NoSpaceException()
        {
        }

        //todo: wjr - remove?
        /*public NoSpaceException(char driveLetter)
            : base(string.Format(Properties.Resources.NO_SPACE, driveLetter.ToString(CultureInfo.CurrentCulture)))
        {
        }*/
        public NoSpaceException(char driveLetter) : base(driveLetter.ToString(CultureInfo.CurrentCulture))
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
