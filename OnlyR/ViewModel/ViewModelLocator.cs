using CommunityToolkit.Mvvm.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace OnlyR.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ViewModelLocator
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Data-bound in XAML: App.xaml defines this class as the 'Locator' resource and MainWindow.xaml binds DataContext to {Binding Main, Source={StaticResource Locator}}; the property must remain an instance member for WPF binding.")]
        public MainViewModel? Main => Ioc.Default.GetService<MainViewModel>();
    }
}