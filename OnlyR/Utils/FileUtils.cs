using Serilog;

namespace OnlyR.Utils
{
    using System;
    using System.IO;

    /// <summary>
    /// General file / folder utilities
    /// </summary>
    public static class FileUtils
    {
        private const string AppNamePathSegment = "OnlyR";

        private const string OptionsFileName = "options.json";

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

        /// <summary>
        /// Gets the log folder
        /// </summary>
        /// <returns>Log folder</returns>
        public static string GetLogFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AppNamePathSegment,
                "Logs");
        }

        /// <summary>
        /// Gets the application's MyDocs folder, e.g. "...MyDocuments\OnlyR"
        /// </summary>
        /// <returns>Folder path</returns>
        public static string GetDefaultMyDocsDestinationFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppNamePathSegment);
        }

        /// <summary>
        /// Gets the recording destination folder
        /// </summary>
        /// <param name="dt">Date of recording</param>
        /// <param name="commandLineIdentifier">Optional command-line id</param>
        /// <param name="rootFromOptions">Overriding root folder as manually specified in app's settings</param>
        /// <returns>Folder path</returns>
        public static string GetDestinationFolder(DateTime dt, string commandLineIdentifier, string rootFromOptions)
        {
            return Path.Combine(
                GetMonthlyDestinationFolder(dt, commandLineIdentifier, rootFromOptions),
                dt.ToString("yyyy-MM-dd"));
        }

        /// <summary>
        /// Gets the recording destination folder for the month
        /// </summary>
        /// <param name="dt">Date of recording</param>
        /// <param name="commandLineIdentifier">Optional command-line id</param>
        /// <param name="rootFromOptions">Overriding root folder as manually specified in app's settings</param>
        /// <returns>Folder path</returns>
        public static string GetMonthlyDestinationFolder(DateTime dt, string commandLineIdentifier, string rootFromOptions)
        {
            return Path.Combine(
                GetRootDestinationFolder(commandLineIdentifier, rootFromOptions),
                dt.ToString("yyyy"),
                dt.ToString("MM"));
        }

        /// <summary>
        /// Gets the recording destination folder
        /// </summary>
        /// <param name="commandLineIdentifier">Optional command-line id</param>
        /// <param name="rootFromOptions">Overriding root folder as manually specified in app's settings</param>
        /// <returns>Folder path</returns>
        public static string GetRootDestinationFolder(string commandLineIdentifier, string rootFromOptions)
        {
            return Path.Combine(
                GetRecordingFolderRoot(rootFromOptions),
                commandLineIdentifier ?? string.Empty);
        }

        /// <summary>
        /// Gets the temp recording folder (this is where recording files are initally created)
        /// </summary>
        /// <returns>Folder path</returns>
        public static string GetTempRecordingFolder()
        {
            var folder = Path.Combine(GetSystemTempFolder(), AppNamePathSegment, "Recordings");
            CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Gets the file path for storing the user options
        /// </summary>
        /// <param name="commandLineIdentifier">Optional command-line id</param>
        /// <param name="optionsVersion">The options schema version</param>
        /// <returns>Options file path.</returns>
        public static string GetUserOptionsFilePath(string commandLineIdentifier, int optionsVersion)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppNamePathSegment,
                commandLineIdentifier ?? string.Empty,
                optionsVersion.ToString(),
                OptionsFileName);
        }

        public static string FindSuitableRecordingFolderToShow(string commandLineIdentifier, string destFolder)
        {
            string folder = null;

            try
            {
                DateTime today = DateTime.Today;
                
                // first try today's folder...
                folder = GetDestinationFolder(today, commandLineIdentifier, destFolder);

                if (!Directory.Exists(folder))
                {
                    // try this month's folder...
                    folder = GetMonthlyDestinationFolder(today, commandLineIdentifier, destFolder);

                    if (!Directory.Exists(folder))
                    {
                        folder = GetRootDestinationFolder(commandLineIdentifier, destFolder);

                        if (!Directory.Exists(folder) && !string.IsNullOrEmpty(commandLineIdentifier))
                        {
                            folder = GetRootDestinationFolder(string.Empty, destFolder);

                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Could not find destination folder {folder}");
            }

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                folder = GetDefaultMyDocsDestinationFolder();
                Directory.CreateDirectory(folder);
            }

            return folder;
        }


        private static string GetRecordingFolderRoot(string rootFromOptions)
        {
            return DirectoryIsAvailable(rootFromOptions) 
                ? rootFromOptions 
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppNamePathSegment);
        }

        private static bool DirectoryIsAvailable(string dir)
        {
            if (string.IsNullOrEmpty(dir))
            {
                return false;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                return Directory.Exists(dir);
            }

            return true;
        }
    }
}
