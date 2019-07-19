namespace OnlyR.ViewModel.Messages
{
    /// <summary>
    /// MVVM message used by the MainViewModel to navigate 
    /// between the Settings Page and the Recording Page.
    /// </summary>
    internal class NavigateMessage
    {
        public NavigateMessage(string originalPageName, string targetPageName, object state)
        {
            OriginalPageName = originalPageName ?? string.Empty;
            TargetPageName = targetPageName;
            State = state;
        }

        public string OriginalPageName { get; }

        /// <summary>
        /// Name of the target page.
        /// </summary>
        public string TargetPageName { get; }

        /// <summary>
        /// Optional context-specific state.
        /// </summary>
        public object State { get; }
    }
}
