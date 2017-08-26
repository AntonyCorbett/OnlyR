using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlyR.Model;
using OnlyR.Services.Options;
using OnlyR.Utils;
using Serilog;

namespace OnlyR.Services.RecordingDestination
{
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
            string commandLineIdentifier)
        {
            var destFolder = FileUtils.GetDestinationFolder(dt, commandLineIdentifier);

            var result = new RecordingCandidate
            {
                TempPath = GetTempRecordingFile(),
                FinalPath = GetNextAvailableFile(optionsService, destFolder, dt)
            };

            Log.Logger.Information("New candidate = {@Candidate}", result);
            return result;
        }

        private string GetNextAvailableFile(IOptionsService optionsService, string folder, DateTime dt)
        {
            string result = Directory.Exists(folder) ? null : GenerateCandidateFilePath(folder, dt, 1);

            if (result == null)
            {
                int maxFileCount = optionsService.Options.MaxRecordingsInOneFolder;

                for (int increment = 1; increment <= maxFileCount && result == null; ++increment)
                {
                    string candidateFile = GenerateCandidateFilePath(folder, dt, increment);
                    if (!File.Exists(candidateFile))
                    {
                        result = candidateFile;
                    }
                }
            }

            return result;
        }

        private string GenerateCandidateFilePath(string folder, DateTime dt, int increment)
        {
            return Path.Combine(folder,
               $"{CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dt.DayOfWeek]} {dt:dd MMMM yyyy} - {increment:D3}.mp3");
        }

        /// <summary>
        /// Gets a file name that can be used to temporarily store recording data
        /// </summary>
        /// <returns>File name (full path)</returns>
        public string GetTempRecordingFile()
        {
            string folder = FileUtils.GetTempRecordingFolder();
            string file = string.Concat(Guid.NewGuid().ToString("N"), ".mp3");
            return Path.Combine(folder, file);
        }

    }
}
