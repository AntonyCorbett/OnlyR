﻿using System;
using System.Globalization;
using System.IO;
using OnlyR.Model;
using OnlyR.Services.Options;
using OnlyR.Utils;
using Serilog;

namespace OnlyR.Services.RecordingDestination
{
    /// <summary>
    /// Service to analyse recording destination folder and generate a recording candidate
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RecordingDestinationService : IRecordingDestinationService
    {
        /// <summary>
        /// Gets the full path of the next recording file (non-existent)
        /// </summary>
        /// <param name="optionsService">Options service</param>
        /// <param name="dt">Recording date</param>
        /// <param name="commandLineIdentifier">identifier passed on commandline to differentiate settings and folders</param>
        /// <returns>Candidate path</returns>
        public RecordingCandidate GetRecordingFileCandidate(
            IOptionsService optionsService,
            DateTime dt,
            string? commandLineIdentifier)
        {
            var destFolder = FileUtils.GetDestinationFolder(dt, commandLineIdentifier, optionsService.Options.DestinationFolder);
            var finalPathAndTrack = GetNextAvailableFile(optionsService, destFolder, dt);

            if (finalPathAndTrack == null)
            {
                throw new NotSupportedException("Unable to get recording candidate!");
            }

            var result = new RecordingCandidate(
                dt, 
                finalPathAndTrack.TrackNumber, 
                GetTempRecordingFile(),
                finalPathAndTrack.FilePath);

            Log.Logger.Information("New candidate = {@Candidate}", result);
            return result;
        }

        private static PathAndTrackNumber? GetNextAvailableFile(IOptionsService optionsService, string folder, DateTime dt)
        {
            PathAndTrackNumber? result = null;

            var path = Directory.Exists(folder) ? null : GenerateCandidateFilePath(folder, dt, 1);
            if (path != null)
            {
                result = new PathAndTrackNumber(path, 1);
            }
            else
            {
                var maxFileCount = optionsService.Options.MaxRecordingsInOneFolder;

                for (int increment = 1; increment <= maxFileCount && result == null; ++increment)
                {
                    var candidateFile = GenerateCandidateFilePath(folder, dt, increment);
                    if (!File.Exists(candidateFile))
                    {
                        result = new PathAndTrackNumber(candidateFile, increment);
                    }
                }
            }

            return result;
        }

        private static string GenerateCandidateFilePath(string folder, DateTime dt, int increment)
        {
            return Path.Combine(
                folder,
                $"{CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dt.DayOfWeek]} {dt:dd MMMM yyyy} - {increment:D3}.mp3");
        }

        /// <summary>
        /// Gets a file name that can be used to temporarily store recording data
        /// </summary>
        /// <returns>File name (full path)</returns>
        private static string GetTempRecordingFile()
        {
            var folder = FileUtils.GetTempRecordingFolder();
            var file = string.Concat(Guid.NewGuid().ToString("N"), ".mp3");
            return Path.Combine(folder, file);
        }
    }
}
