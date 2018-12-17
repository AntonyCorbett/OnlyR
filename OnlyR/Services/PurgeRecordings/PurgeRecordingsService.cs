namespace OnlyR.Services.PurgeRecordings
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using OnlyR.Services.Options;
    using Serilog;

    internal class PurgeRecordingsService : IPurgeRecordingsService
    {
        private readonly IOptionsService _optionsService;
        private readonly DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Background);
        private readonly TimeSpan _timerInterval = TimeSpan.FromMinutes(1);

        public PurgeRecordingsService(IOptionsService optionsService)
        {
            _optionsService = optionsService;
            InitTimer();
        }

        private void InitTimer()
        {
            _timer.Interval = _timerInterval;
            _timer.Tick += HandleTimerTick;
            _timer.Start();
        }

        private async void HandleTimerTick(object sender, EventArgs e)
        {
            var days = _optionsService.Options.RecordingsLifeTimeDays;

            if (days == 0)
            {
                return;
            }

            _timer.Stop();
            int countFilesDeleted = 0;
            try
            {
                Log.Logger.Debug($"Starting purge of old recordings (older than {days} days)");
                countFilesDeleted = await PurgeInternal(days);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Could not purge recordings");
            }
            finally
            {
                Log.Logger.Debug($"Completed purge of old recordings ({countFilesDeleted} deleted)");
                _timer.Start();
            }
        }
        
        // returns number of files deleted
        private Task<int> PurgeInternal(int recordingsLifeTimeDays)
        {
            var t = Task.Run(() =>
            {
                Task.Delay(10000);
                return 10;
            });

            return t;
        }
    }
}
