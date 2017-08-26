using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace OnlyR.Utils
{
    public static class FileUtils
    {
        private static readonly string _appNamePathSegment = "OnlyR";

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

        public static string GetDestinationFolder(DateTime dt, string commandLineIdentifier)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                _appNamePathSegment,
                commandLineIdentifier,
                dt.ToString("yyyy"), dt.ToString("MM"), dt.ToString("yyyy-MM-dd"));
        }

        public static string GetTempRecordingFolder()
        {
            string folder = Path.Combine(FileUtils.GetSystemTempFolder(), _appNamePathSegment, "Recordings");
            CreateDirectory(folder);
            return folder;
        }

        public static string GetUserOptionsFilePath(string commandLineIdentifier, int optionsVersion)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                _appNamePathSegment,
                commandLineIdentifier,
                optionsVersion.ToString());
        }

    }
}
