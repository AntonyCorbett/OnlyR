using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Exceptions;
using OnlyR.Services.Options;
using OnlyR.Utils;
using Serilog;

namespace OnlyR.Services.RecordingCopies;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class CopyRecordingsService : ICopyRecordingsService
{
    private readonly ICommandLineService _commandLineService;
    private readonly IOptionsService _optionsService;
    private readonly IDriveEjectionService _driveEjectionService;

    public CopyRecordingsService(
        ICommandLineService commandLineService,
        IOptionsService optionsService,
        IDriveEjectionService driveEjectionService)
    {
        _commandLineService = commandLineService;
        _optionsService = optionsService;
        _driveEjectionService = driveEjectionService;
    }

    public void Copy(IReadOnlyCollection<char> drives)
    {
        var recordings = GetRecordings();
        var spaceNeeded = GetSpaceNeed(recordings);

        ProcessInParallel(drives, recordings, spaceNeeded);
    }

    private void ProcessInParallel(IReadOnlyCollection<char> drives, string[] recordings, long spaceNeeded)
    {
        var exceptions = new ConcurrentQueue<Exception>();

        Parallel.ForEach(
            drives, 
            drive =>
            {
                try
                {
                    Copy(drive, recordings, spaceNeeded);
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException(exceptions);
        }
    }

    private static long GetSpaceNeed(IEnumerable<string> srcFiles)
    {
        long totalSize = 0;

        foreach (var file in srcFiles)
        {
            var fi = new FileInfo(file);
            totalSize += fi.Length;
        }

        return totalSize;
    }

    private string[] GetRecordings()
    {
        var folder = GetRecordingsFolder();
        if (folder == null)
        {
            throw new NoRecordingsException();
        }

        var files = Directory.GetFiles(folder, "*.mp3");
        if (files.Length == 0)
        {
            throw new NoRecordingsException();
        }

        Log.Logger.Debug($"Recordings found = {files.Length}");

        var result = new ConcurrentBag<string>();

        Parallel.ForEach(
            files, 
            file =>
            {
                if (CanCopyFile(file))
                {
                    result.Add(file);
                }
            });

        return result.ToArray();
    }
        
    private static bool CanCopyFile(string filePath)
    {
        return File.Exists(filePath) && !IsFileLocked(filePath);
    }

    private static bool IsFileLocked(string filePath)
    {
        FileStream? stream = null;

        try
        {
            stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (IOException)
        {
            return true;
        }
        finally
        {
            stream?.Close();
        }

        return false;
    }

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private void Copy(char driveLetter, string[] recordings, long spaceNeeded)
    {
        var di = new DriveInfo(driveLetter.ToString());
        if (spaceNeeded * 1.05 > di.AvailableFreeSpace)
        {
            throw new NoSpaceException(driveLetter);
        }

        Parallel.ForEach(
            recordings, 
            file =>
            {
                var fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var destinationFile = Path.Combine($"{driveLetter}:\\", fileName);

                    Log.Logger.Debug($"Copying {file} to {destinationFile}");
                    File.Copy(file, destinationFile, overwrite: true);
                }
            });

        Eject(driveLetter);
    }

    private void Eject(char driveLetter)
    {
        // so the device can be safely removed.
        try
        {
            _driveEjectionService.Eject(driveLetter);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Ejecting device");
        }
    }

    private string? GetRecordingsFolder()
    {
        Log.Logger.Debug("Getting recordings folder for today");

        var today = DateTime.Today;

        // first try today's folder...
        var folder = FileUtils.GetDestinationFolder(
            today, 
            _commandLineService.OptionsIdentifier,
            _optionsService.Options.DestinationFolder);

        if (!Directory.Exists(folder))
        {
            return null;
        }

        Log.Logger.Debug($"Recordings folder = {folder}");

        return folder;
    }
}