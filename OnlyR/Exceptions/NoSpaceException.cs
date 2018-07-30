namespace OnlyR.Exceptions
{
    using System;

    internal class NoSpaceException : Exception
    {
        public NoSpaceException(char driveLetter)
            : base(string.Format(Properties.Resources.NO_SPACE, driveLetter.ToString()))
        {
        }
    }
}
