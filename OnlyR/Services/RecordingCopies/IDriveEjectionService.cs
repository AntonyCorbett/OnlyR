namespace OnlyR.Services.RecordingCopies
{
    internal interface IDriveEjectionService
    {
        bool Eject(char driveLetter);
    }
}
