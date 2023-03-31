namespace OnlyR.ViewModel.Messages;

/// <summary>
/// MVVM message used by the MainViewModel to signal app shutdown
/// </summary>
// ReSharper disable once UnusedMember.Global
internal sealed class BeforeShutDownMessage
{
    public BeforeShutDownMessage(string? currentPageName)
    {
        CurrentPageName = currentPageName;
    }

    public string? CurrentPageName { get; }
}