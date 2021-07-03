using System.Windows;

namespace OnlyR.ViewModel.Messages
{
    internal class SessionEndingMessage
    {
        public SessionEndingMessage(SessionEndingCancelEventArgs e)
        {
            SessionEndingArgs = e;
        }

        public SessionEndingCancelEventArgs SessionEndingArgs { get; }
    }
}
