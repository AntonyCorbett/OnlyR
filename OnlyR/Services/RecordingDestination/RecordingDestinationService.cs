using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using OnlyR.Core.Enums;
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
                GetTempRecordingFile(optionsService.Options.Codec),
                finalPathAndTrack.FilePath);

            Log.Logger.Information("New candidate = {@Candidate}", result);
            return result;
        }

        private static PathAndTrackNumber? GetNextAvailableFile(IOptionsService optionsService, string folder, DateTime dt)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var extensions = Enum.GetValues<AudioCodec>()
                .Select(codec => codec.GetExtensionFormat())
                .ToArray();

            var files = Directory.EnumerateFiles(
                    folder,
                    $"{GenerateCoreCandidateFileName(dt)} - *.*")
                .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

            var highestTrack = 0;
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var trackStr = fileName[^3..];
                if (!int.TryParse(trackStr, out var trackNum))
                {
                    continue;
                }

                highestTrack = Math.Max(highestTrack, trackNum);
                if (highestTrack + 1 >= optionsService.Options.MaxRecordingsInOneFolder)
                {
                    return null;
                }
            }

            var nextTrack = highestTrack + 1;
            var filePath = GenerateCandidateFilePath(folder, dt, nextTrack, optionsService.Options.Codec);
            return new PathAndTrackNumber(filePath, nextTrack);
        }

        private static string GenerateCandidateFilePath(string folder, DateTime dt, int increment, AudioCodec codec) =>
            Path.Combine(
                folder,
                $"{GenerateCoreCandidateFileName(dt)} - {increment:D3}.{codec.GetExtensionFormat()}");
        
        private static string GenerateCoreCandidateFileName(DateTime dt)
            => $"{CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dt.DayOfWeek]} {dt:dd MMMM yyyy}";

        /// <summary>
        /// Gets a file name that can be used to temporarily store recording data
        /// </summary>
        /// <returns>File name (full path)</returns>
        private static string GetTempRecordingFile(AudioCodec codec)
        {
            var folder = FileUtils.GetTempRecordingFolder();
            var file = string.Concat(Guid.NewGuid().ToString("N"), $".{codec.GetExtensionFormat()}");
            return Path.Combine(folder, file);
        }
    }
}
