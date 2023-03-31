using System.Windows;

namespace OnlyR.ViewModel.Messages;

internal sealed class SessionEndingMessage
{
    public SessionEndingMessage(SessionEndingCancelEventArgs e)
    {
        SessionEndingArgs = e;
    }

    public SessionEndingCancelEventArgs SessionEndingArgs { get; }
}