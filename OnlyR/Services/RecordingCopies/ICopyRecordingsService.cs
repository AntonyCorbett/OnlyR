using System.Collections.Generic;

namespace OnlyR.Services.RecordingCopies
{
    public interface ICopyRecordingsService
    {
        void Copy(IReadOnlyCollection<char> drives);
    }
}
