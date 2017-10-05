namespace OnlyR.ViewModel.Messages
{
    /// <summary>
    /// MVVM message used by the MainViewModel to navigate 
    /// between the Settings Page and the Recording Page
    /// </summary>
    internal class NavigateMessage
    {
        /// <summary>
        /// Name of the target page
        /// </summary>
        public string TargetPage { get; }

        /// <summary>
        /// Optional context-specific state
        /// </summary>
        public object State { get; }

        public NavigateMessage(string targetPage, object state)
        {
            TargetPage = targetPage;
            State = state;
        }
    }
}
