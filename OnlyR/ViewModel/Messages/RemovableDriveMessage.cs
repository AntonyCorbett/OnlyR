namespace OnlyR.ViewModel.Messages;

internal sealed class RemovableDriveMessage
{
    public char DriveLetter { get; set; }

    public bool Added { get; set; }
}