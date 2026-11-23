namespace OnlyR.ViewModel;

/// <summary>
/// Result of a request to exit the application from the notification area menu.
/// </summary>
internal enum ExitRequestResult
{
    /// <summary>
    /// The shutdown has been started, or is waiting for a recording to be finalised.
    /// </summary>
    Requested,

    /// <summary>
    /// The exit is not allowed because a recording is in progress and the
    /// "Allow close when recording" option is off.
    /// </summary>
    BlockedWhileRecording,

    /// <summary>
    /// The recording could not be stopped, so the application did not exit.
    /// </summary>
    StopFailed,
}
