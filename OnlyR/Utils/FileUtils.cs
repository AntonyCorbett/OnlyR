using System;
using System.IO;

namespace OnlyR.Utils
{
    public static class FileUtils
    {
        private static readonly string _appNamePathSegment = "OnlyR";
        private static readonly string _optionsFileName = "options.json";

        /// <summary>
        /// Creates directory if it doesn't exist. Throws if cannot be created
        /// </summary>
        /// <param name="folderPath">Directory to create</param>
        public static void CreateDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                if (!Directory.Exists(folderPath))
                {
                    // "Could not create folder {0}"
                    throw new Exception(string.Format(Properties.Resources.CREATE_FOLDER_ERROR, folderPath));
                }
            }
        }

        /// <summary>
        /// Gets system temp folder
        /// </summary>
        /// <returns>Temp folder</returns>
        public static string GetSystemTempFolder()
        {
            return Path.GetTempPath();
        }


        /// <summary>
        /// Gets number of free bytes available on the drive on which folderName resides
        /// </summary>
        /// <param name="folderName">A reference folder</param>
        /// <param name="freespace">Free space in bytes</param>
        /// <returns>True if the space could be calculated</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool DriveFreeBytes(string folderName, out ulong freespace)
        {
            freespace = 0;

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            if (NativeMethods.GetDiskFreeSpaceEx(folderName, out var free, out var dummy1, out var dummy2))
            {
                freespace = free;
                return true;
            }

            return false;
        }

        public static string GetLogFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                _appNamePathSegment,
                "Logs");
        }

        public static string GetDestinationFolder(DateTime dt, string commandLineIdentifier)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                _appNamePathSegment,
                commandLineIdentifier ?? string.Empty,
                dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("yyyy-MM-dd"));
        }

        public static string GetMonthlyDestinationFolder(DateTime dt, string commandLineIdentifier)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                _appNamePathSegment,
                commandLineIdentifier ?? string.Empty,
                dt.ToString("yyyy"), dt.ToString("MM"));
        }

        public static string GetRootDestinationFolder(string commandLineIdentifier)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                _appNamePathSegment,
                commandLineIdentifier ?? string.Empty);
        }

        public static string GetTempRecordingFolder()
        {
            string folder = Path.Combine(GetSystemTempFolder(), _appNamePathSegment, "Recordings");
            CreateDirectory(folder);
            return folder;
        }

        public static string GetUserOptionsFilePath(string commandLineIdentifier, int optionsVersion)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                _appNamePathSegment,
                commandLineIdentifier ?? string.Empty,
                optionsVersion.ToString(),
                _optionsFileName);
        }

    }
}
