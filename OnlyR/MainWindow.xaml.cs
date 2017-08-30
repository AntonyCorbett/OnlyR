using OnlyR.ViewModel;

namespace OnlyR
{
   /// <summary>
   /// MainWindow.xaml code-behind
   /// </summary>
   public partial class MainWindow
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
      {
         MainViewModel m = (MainViewModel)DataContext;
         m.Closing(sender, e);
      }
   }
}
