using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace OnlyR.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ViewModelLocator
    {
        public MainViewModel? Main => Ioc.Default.GetService<MainViewModel>();
    }
}