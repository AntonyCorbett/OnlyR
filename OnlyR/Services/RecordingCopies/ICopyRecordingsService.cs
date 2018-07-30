namespace OnlyR.Services.RecordingCopies
{
    using System.Collections.Generic;

    public interface ICopyRecordingsService
    {
        void Copy(IReadOnlyCollection<char> drives);
    }
}
