using System.Threading;

namespace OnlyR.Services.RecordingCopies
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class CopyRecordingsService : ICopyRecordingsService
    {
        public void Copy(IReadOnlyCollection<char> drives)
        {
            Parallel.ForEach(
                drives, 
                drive =>
                {
                    Copy(drive);
                });
        }

        private void Copy(char driveLetter)
        {
            Thread.Sleep(2000);
        }
    }
}
