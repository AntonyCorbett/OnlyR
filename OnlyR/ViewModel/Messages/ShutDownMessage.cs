namespace OnlyR.ViewModel.Messages
{
    /// <summary>
    /// MVVM message used by the MainViewModel to signal app shutdown
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class ShutDownMessage
    {
        /// <summary>
        /// Name of the current page
        /// </summary>
        public string CurrentPageName { get; }

        public ShutDownMessage(string currentPageName)
        {
            CurrentPageName = currentPageName;
        }
    }
}
