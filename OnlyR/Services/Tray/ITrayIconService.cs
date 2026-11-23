using System.Windows;

namespace OnlyR.Services.Tray;

/// <summary>
/// Shows the application's notification area (tray) icon and the commands
/// available from it.
/// </summary>
public interface ITrayIconService
{
    /// <summary>
    /// Attaches the service to the main window. Must be called on the UI thread.
    /// </summary>
    /// <param name="mainWindow">The application's main window.</param>
    void Initialize(Window mainWindow);

    /// <summary>
    /// Requests that the application exits, stopping any recording in progress first.
    /// </summary>
    void RequestExit();
}
