namespace OnlyR.ViewModel.Messages
{
    /// <summary>
    /// MVVM message used by the MainViewModel to signal app shutdown
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class BeforeShutDownMessage
    {
        /// <summary>
        /// Name of the current page
        /// </summary>
        public string CurrentPageName { get; }

        public BeforeShutDownMessage(string currentPageName)
        {
            CurrentPageName = currentPageName;
        }
    }
}
