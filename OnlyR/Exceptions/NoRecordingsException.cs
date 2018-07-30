namespace OnlyR.Exceptions
{
    using System;

    [Serializable]
    internal class NoRecordingsException : Exception
    {
        public NoRecordingsException()
        : base(Properties.Resources.NO_RECORDINGS)
        {
        }
    }
}
