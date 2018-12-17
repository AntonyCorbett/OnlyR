namespace OnlyR.ViewModel.Messages
{
    /// <summary>
    /// MVVM message used by the MainViewModel to navigate 
    /// between the Settings Page and the Recording Page
    /// </summary>
    internal class NavigateMessage
    {
        public NavigateMessage(string targetPage, object state)
        {
            TargetPage = targetPage;
            State = state;
        }

        public string TargetPage { get; }

        public object State { get; }
    }
}
