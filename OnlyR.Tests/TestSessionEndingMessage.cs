#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OnlyR.ViewModel.Messages;

namespace OnlyR.Tests;

public sealed class TestSessionEndingMessage
{
    private static SessionEndingCancelEventArgs CreateSessionEndingCancelEventArgs()
    {
        var ctor = typeof(SessionEndingCancelEventArgs)
            .GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(ReasonSessionEnding)],
                null)!;

        return (SessionEndingCancelEventArgs)ctor.Invoke([ReasonSessionEnding.Logoff]);
    }

    [Test]
    public async Task ConstructorSetsArgs()
    {
        SessionEndingMessage? message = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var args = CreateSessionEndingCancelEventArgs();
                message = new SessionEndingMessage(args);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(message!.SessionEndingArgs).IsNotNull();
    }

    [Test]
    public async Task SessionEndingArgsReturnsSameInstance()
    {
        SessionEndingCancelEventArgs? args = null;
        SessionEndingMessage? message = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                args = CreateSessionEndingCancelEventArgs();
                message = new SessionEndingMessage(args);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(message!.SessionEndingArgs).IsSameReferenceAs(args!);
    }

    [Test]
    public async Task CancelPropertyIsPropagated()
    {
        SessionEndingMessage? message = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var args = CreateSessionEndingCancelEventArgs();
                args.Cancel = true;
                message = new SessionEndingMessage(args);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        await Assert.That(message!.SessionEndingArgs.Cancel).IsTrue();
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
