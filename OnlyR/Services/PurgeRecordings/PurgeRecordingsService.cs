namespace OnlyR.Services.PurgeRecordings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using OnlyR.Services.Options;
    using OnlyR.Utils;
    using Serilog;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class PurgeRecordingsService : IPurgeRecordingsService
    {
        private const int MaxFileDeletionsInBatch = 100;

        private readonly IOptionsService _optionsService;
        private readonly ICommandLineService _commandLineService;
        private readonly DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Background);
        private readonly TimeSpan _initialTimerInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _backoffTimerInterval = TimeSpan.FromMinutes(15);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private PurgeServiceJob _lastJob = PurgeServiceJob.Nothing;
        private bool _allFilesDone;

        public PurgeRecordingsService(
            IOptionsService optionsService,
            ICommandLineService commandLineService)
        {
            _optionsService = optionsService;
            _commandLineService = commandLineService;

            InitTimer();
        }

        public void NotifyClosing()
        {
            // get a chance to clean up...
            _cancellationTokenSource.Cancel();
        }

        private void InitTimer()
        {
            _timer.Interval = _initialTimerInterval;
            _timer.Tick += HandleTimerTick;
            _timer.Start();
        }

        private async void HandleTimerTick(object sender, EventArgs e)
        {
            _timer.Stop();

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            var days = _optionsService.Options.RecordingsLifeTimeDays;

            if (days == 0)
            {
                _timer.Interval = _backoffTimerInterval;
                _timer.Start();
                return;
            }
            
            int itemsDeletedCount = 0;
            try
            {
                switch (_lastJob)
                {
                    case PurgeServiceJob.FolderPurge:
                    case PurgeServiceJob.Nothing:
                        Log.Logger.Information($"Starting purge of old recordings (older than {days} days)");
                        _lastJob = PurgeServiceJob.FilePurge;
                        itemsDeletedCount = await PurgeFilesInternal(days);
                        _allFilesDone = itemsDeletedCount == 0;
                        break;

                    case PurgeServiceJob.FilePurge:
                        Log.Logger.Information("Starting removal of empty folders");
                        _lastJob = PurgeServiceJob.FolderPurge;
                        itemsDeletedCount = await RemoveEmptyFolders();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Could not purge recordings");
            }
            finally
            {
                AfterPurge(itemsDeletedCount);
            }
        }

        private void AfterPurge(int itemsDeletedCount)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                Log.Logger.Information($"Purge cancelled: ({itemsDeletedCount} items deleted)");
            }
            else
            {
                Log.Logger.Information($"Completed purge: ({itemsDeletedCount} items deleted)");

                if (_lastJob == PurgeServiceJob.FolderPurge && _allFilesDone)
                {
                    // probably no need to do further work in this session
                    // (unless user changes settings)...
                    Log.Logger.Debug("Purge activity backing off in this session");
                    _timer.Interval = _backoffTimerInterval;
                }

                _timer.Start();
            }
        }
        
        private Task<int> RemoveEmptyFolders()
        {
            var t = Task.Run(
                () => DeleteFolders(GetEmptyFolders()),
                _cancellationTokenSource.Token);

            return t;
        }

        private int DeleteFolders(IReadOnlyCollection<string> emptyFolders)
        {
            if (_cancellationTokenSource.IsCancellationRequested || emptyFolders == null)
            {
                return 0;
            }

            int count = 0;

            foreach (var candidate in emptyFolders)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                Log.Logger.Debug($"Deleting folder: {candidate}");
                FileUtils.SafeDeleteFolder(candidate);

                if (!Directory.Exists(candidate))
                {
                    ++count;
                }
            }

            return count;
        }

        // returns number of files deleted
        private Task<int> PurgeFilesInternal(int recordingsLifeTimeDays)
        {
            var t = Task.Run(
                () =>
                {
                    var oldFileDate = DateTime.Now.AddDays(-recordingsLifeTimeDays);
                    return DeleteCandidates(GetPurgeCandidates(oldFileDate));
                }, 
                _cancellationTokenSource.Token);

            return t;
        }

        private int DeleteCandidates(IEnumerable<string> candidatePaths)
        {
            if (_cancellationTokenSource.IsCancellationRequested || candidatePaths == null)
            {
                return 0;
            }

            var count = 0;

            foreach (var candidate in candidatePaths)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                Log.Logger.Debug($"Deleting file: {candidate}");
                FileUtils.SafeDeleteFile(candidate);

                if (!File.Exists(candidate))
                {
                    ++count;

                    if (count == MaxFileDeletionsInBatch)
                    {
                        break;
                    }
                }
            }

            return count;
        }

        private IReadOnlyCollection<string> GetEmptyFolders()
        {
            var result = new List<string>();

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return result;
            }
            
            var rootFolder = FileUtils.GetRootDestinationFolder(
                _commandLineService.OptionsIdentifier,
                _optionsService.Options.DestinationFolder);

            if (!Directory.Exists(rootFolder))
            {
                return result;
            }
            
            var yearSubFolders = Directory.GetDirectories(rootFolder);
            foreach (var yearFolder in yearSubFolders)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                var yearOfFolder = FileUtils.ParseYearFromFolderName(Path.GetFileName(yearFolder));
                if (yearOfFolder == null)
                {
                    continue;
                }

                if (FileUtils.IsDirectoryEmpty(yearFolder) && yearOfFolder != DateTime.Now.Year)
                {
                    Log.Logger.Debug($"Found empty folder: {yearFolder}");
                    result.Add(yearFolder);
                }

                var monthSubFolders = Directory.GetDirectories(yearFolder);
                foreach (var monthFolder in monthSubFolders)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    var monthOfFolder = FileUtils.ParseMonthFromFolderName(Path.GetFileName(monthFolder));
                    if (monthOfFolder == null)
                    {
                        continue;
                    }

                    if (FileUtils.IsDirectoryEmpty(monthFolder) && 
                        (yearOfFolder != DateTime.Now.Year || monthOfFolder != DateTime.Now.Month))
                    {
                        Log.Logger.Debug($"Found empty folder: {monthFolder}");
                        result.Add(monthFolder);
                    }

                    var dateSubFolders = Directory.GetDirectories(monthFolder);
                    foreach (var dateFolder in dateSubFolders)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var dateOfFolder = FileUtils.ParseDateFromFolderName(
                            Path.GetFileName(dateFolder),
                            yearOfFolder.Value,
                            monthOfFolder.Value);

                        if (dateOfFolder == null)
                        {
                            continue;
                        }

                        if (FileUtils.IsDirectoryEmpty(dateFolder) &&
                            (yearOfFolder != DateTime.Now.Year || 
                             monthOfFolder != DateTime.Now.Month || 
                             dateOfFolder != DateTime.Today.Date))
                        {
                            Log.Logger.Debug($"Found empty folder: {dateFolder}");
                            result.Add(dateFolder);
                        }
                    }
                }
            }

            return result;
        }

        private IEnumerable<string> GetPurgeCandidates(DateTime oldFileDate)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                yield return null;
            }

            var folders = GetPurgeCandidateFolders(oldFileDate);

            foreach (var folder in folders)
            {
                var files = Directory.EnumerateFiles(folder, "*.mp3");
                foreach (var file in files)
                {
                    Log.Logger.Debug($"Found file: {file}");
                    yield return file;
                }
            }
        }

        private IEnumerable<string> GetPurgeCandidateFolders(DateTime oldFileDate)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                yield return null;
            }

            var rootFolder = FileUtils.GetRootDestinationFolder(
                _commandLineService.OptionsIdentifier,
                _optionsService.Options.DestinationFolder);

            if (!Directory.Exists(rootFolder))
            {
                yield return null;
            }

            var yearSubFolders = Directory.EnumerateDirectories(rootFolder);
            foreach (var yearFolder in yearSubFolders)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                var yearOfFolder = FileUtils.ParseYearFromFolderName(Path.GetFileName(yearFolder));
                if (yearOfFolder == null)
                {
                    continue;
                }

                if (!YearFolderMayContainCandidates(yearOfFolder.Value, oldFileDate))
                {
                    continue;
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                var monthSubFolders = Directory.EnumerateDirectories(yearFolder);
                foreach (var monthFolder in monthSubFolders)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    var monthOfFolder = FileUtils.ParseMonthFromFolderName(Path.GetFileName(monthFolder));
                    if (monthOfFolder == null)
                    {
                        continue;
                    }

                    if (!MonthFolderMayContainCandidates(yearOfFolder.Value, monthOfFolder.Value, oldFileDate))
                    {
                        continue;
                    }

                    var dateSubFolders = Directory.EnumerateDirectories(monthFolder);
                    foreach (var dateFolder in dateSubFolders)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var dateOfFolder = FileUtils.ParseDateFromFolderName(
                            Path.GetFileName(dateFolder), 
                            yearOfFolder.Value, 
                            monthOfFolder.Value);

                        if (dateOfFolder == null)
                        {
                            continue;
                        }

                        if (dateOfFolder.Value.Date < oldFileDate.Date)
                        {
                            Log.Logger.Debug($"Found folder: {dateFolder}");
                            yield return dateFolder;
                        }
                    }
                }
            }
        }

        private bool MonthFolderMayContainCandidates(int yearOfFolder, int monthOfFolder, DateTime oldFileDate)
        {
            return yearOfFolder < oldFileDate.Year ||
                   (yearOfFolder == oldFileDate.Year && monthOfFolder <= oldFileDate.Month);
        }

        private bool YearFolderMayContainCandidates(int yearOfFolder, DateTime oldFileDate)
        {
            return oldFileDate.Year >= yearOfFolder;
        }
    }
}
