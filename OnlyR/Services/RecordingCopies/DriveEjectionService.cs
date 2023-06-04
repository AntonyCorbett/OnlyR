using System;
using Serilog;

namespace OnlyR.Services.RecordingCopies
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class DriveEjectionService : IDriveEjectionService
    {
        public bool Eject(char driveLetter)
        {
            try
            {
                return DriveEjectionServiceNativeMethods.EjectDrive($"{driveLetter}:");
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Ejecting device");
            }

            return false;
        }
    }
}
