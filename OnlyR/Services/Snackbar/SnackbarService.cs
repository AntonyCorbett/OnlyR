namespace OnlyR.Services.Snackbar
{
    using System;
    using System.Windows;
    using GalaSoft.MvvmLight.Threading;
    using MaterialDesignThemes.Wpf;

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class SnackbarService : ISnackbarService, IDisposable
    {
        public ISnackbarMessageQueue TheSnackbarMessageQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(4));

        public void Enqueue(object content, object actionContent, Action actionHandler, bool promote = false)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
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
            Action<object> actionHandler,
            object actionArgument,
            bool promote,
            bool neverConsiderToBeDuplicate)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
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
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (Application.Current.MainWindow?.WindowState != WindowState.Minimized)
                {
                    TheSnackbarMessageQueue.Enqueue(content);
                }
            });
        }

        public void EnqueueWithOk(object content)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
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
}
