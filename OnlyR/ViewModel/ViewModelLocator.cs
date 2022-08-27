#nullable enable
using Microsoft.Toolkit.Mvvm.DependencyInjection;

namespace OnlyR.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        public virtual MainViewModel? Main => Ioc.Default.GetService<MainViewModel>();
    }
}