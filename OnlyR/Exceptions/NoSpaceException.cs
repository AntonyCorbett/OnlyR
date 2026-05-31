using System;
using System.Globalization;

namespace OnlyR.Exceptions
{
    [Serializable]
    public sealed class NoSpaceException : Exception
    {
        public NoSpaceException()
        {
        }

        public NoSpaceException(char driveLetter)
            : base(string.Format(CultureInfo.CurrentCulture, Properties.Resources.NO_SPACE, driveLetter.ToString(CultureInfo.CurrentCulture)))
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
    }
}