using System.Windows;

namespace OnlyR.Services.Tray;

/// <summary>
/// Shows the application's notification area (tray) icon and the commands
/// available from it.
/// </summary>
public interface ITrayIconService
{
    /// <summary>
    /// Gets a value indicating whether the user has asked the application to exit
    /// (as opposed to the window simply being hidden in the notification area).
    /// </summary>
    bool IsExiting { get; }

    /// <summary>
    /// Attaches the service to the main window. Must be called on the UI thread.
    /// </summary>
    /// <param name="mainWindow">The application's main window.</param>
    void Initialize(Window mainWindow);

    /// <summary>
    /// Stops any recording in progress and shuts the application down.
    /// </summary>
    void RequestExit();
}
