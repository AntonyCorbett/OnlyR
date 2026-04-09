#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Threading;
using System.Threading.Tasks;

namespace OnlyR.Tests;

internal static class StaThreadHelper
{
    public static async Task RunOnSta(Action action)
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await tcs.Task;
    }

    public static async Task<T> RunOnSta<T>(Func<T> func)
    {
        T? result = default;
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                result = func();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await tcs.Task;
        return result!;
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
