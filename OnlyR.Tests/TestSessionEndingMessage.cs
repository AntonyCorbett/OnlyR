#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Reflection;
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
        var message = await StaThreadHelper.RunOnSta(() =>
        {
            var args = CreateSessionEndingCancelEventArgs();
            return new SessionEndingMessage(args);
        });

        await Assert.That(message.SessionEndingArgs).IsNotNull();
    }

    [Test]
    public async Task SessionEndingArgsReturnsSameInstance()
    {
        var (args, message) = await StaThreadHelper.RunOnSta(() =>
        {
            var a = CreateSessionEndingCancelEventArgs();
            return (a, new SessionEndingMessage(a));
        });

        await Assert.That(message.SessionEndingArgs).IsSameReferenceAs(args);
    }

    [Test]
    public async Task CancelPropertyIsPropagated()
    {
        var message = await StaThreadHelper.RunOnSta(() =>
        {
            var args = CreateSessionEndingCancelEventArgs();
            args.Cancel = true;
            return new SessionEndingMessage(args);
        });

        await Assert.That(message.SessionEndingArgs.Cancel).IsTrue();
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
