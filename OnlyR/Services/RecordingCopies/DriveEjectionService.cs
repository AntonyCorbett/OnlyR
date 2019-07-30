namespace OnlyR.Services.RecordingCopies
{
    using System;
    using Serilog;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DriveEjectionService : IDriveEjectionService
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
