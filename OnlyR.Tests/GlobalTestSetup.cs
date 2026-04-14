using System;
using System.Runtime.Versioning;
using System.Windows;

[assembly: SupportedOSPlatform("windows7.0")]

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
