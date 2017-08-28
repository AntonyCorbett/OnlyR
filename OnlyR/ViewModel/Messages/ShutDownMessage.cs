namespace OnlyR.ViewModel.Messages
{
    // ReSharper disable once UnusedMember.Global
    internal class ShutDownMessage
    {
        public string CurrentPageName { get; }
        public ShutDownMessage(string currentPageName)
        {
            CurrentPageName = currentPageName;
        }
    }
}
