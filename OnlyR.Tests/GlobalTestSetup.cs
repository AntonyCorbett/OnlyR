#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Windows;

namespace OnlyR.Tests;

public static class GlobalTestSetup
{
    [Before(Assembly)]
    public static void AssemblyInit(AssemblyHookContext context)
    {
        if (Application.Current == null)
        {
            Application.LoadComponent(
                new Uri("/OnlyR;component/App.xaml", UriKind.Relative));
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
