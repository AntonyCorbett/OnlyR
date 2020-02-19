namespace OnlyR.ViewModel.Messages
{
    using System.Windows;

    internal class SessionEndingMessage
    {
        public SessionEndingMessage(SessionEndingCancelEventArgs e)
        {
            SessionEndingArgs = e;
        }

        public SessionEndingCancelEventArgs SessionEndingArgs { get; }
    }
}
