using System;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace OnlyR.Services.Snackbar;

#pragma warning disable CA1416 // Validate platform compatibility

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SnackbarService : ISnackbarService, IDisposable
{

    public ISnackbarMessageQueue TheSnackbarMessageQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(4));

    public void Enqueue(object content, object actionContent, Action actionHandler, bool promote = false)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.WindowState != WindowState.Minimized)
            {
                TheSnackbarMessageQueue.Enqueue(content, actionContent, actionHandler, promote);
            }
        });
    }

    public void Enqueue(
        object content,
        object actionContent,
        Action<object?>? actionHandler,
        object? actionArgument,
        bool promote,
        bool neverConsiderToBeDuplicate)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.WindowState != WindowState.Minimized)
            {
                TheSnackbarMessageQueue.Enqueue(
                    content,
                    actionContent,
                    actionHandler,
                    actionArgument,
                    promote,
                    neverConsiderToBeDuplicate);
            }
        });
    }

    public void Enqueue(object content)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.WindowState != WindowState.Minimized)
            {
                TheSnackbarMessageQueue.Enqueue(content);
            }
        });
    }

    public void EnqueueWithOk(object content)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.WindowState != WindowState.Minimized)
            {
                TheSnackbarMessageQueue.Enqueue(content, Properties.Resources.OK, () => { });
            }
        });
    }

    public void Dispose()
    {
        ((SnackbarMessageQueue)TheSnackbarMessageQueue)?.Dispose();
    }
}

#pragma warning restore CA1416 // Validate platform compatibility