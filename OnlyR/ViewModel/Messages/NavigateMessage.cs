namespace OnlyR.ViewModel.Messages
{
    internal class NavigateMessage
    {
        public string TargetPage { get; }
        public object State { get; }

        public NavigateMessage(string targetPage, object state)
        {
            TargetPage = targetPage;
            State = state;
        }
    }
}
