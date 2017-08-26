using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OnlyR.ViewModel;

namespace OnlyR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Random _random;
        private System.Timers.Timer _timer;
        private int _smoothedLevel;
        const int MAX_LEVEL = 100;
        const int VUSPEED = MAX_LEVEL / 10;

        public MainWindow()
        {
            InitializeComponent();
            _random = new Random();
            _timer = new System.Timers.Timer();
            _timer.Interval = 50;
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var vm = (MainViewModel) DataContext;
                var volLevel = _random.Next(0, 100);
                vm.VolumeLevelAsPercentage = GetSmoothedLevel(volLevel);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _timer.Enabled = !_timer.Enabled;
        }

        private int GetSmoothedLevel(int volLevel)
        {
            if (volLevel > _smoothedLevel)
            {
                _smoothedLevel = volLevel + VUSPEED;
            }

            _smoothedLevel -= VUSPEED;
            if (_smoothedLevel < 0)
            {
                _smoothedLevel = 0;
            }

            return _smoothedLevel;
        }

    }
}
