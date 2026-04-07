#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Services.Options;
using OnlyR.Services.PurgeRecordings;

namespace OnlyR.Tests;

public sealed class TestPurgeRecordingsService
{
    private static PurgeRecordingsService CreateService(
        string destinationFolder,
        int recordingsLifeTimeDays = 0,
        string? optionsIdentifier = null)
    {
        var options = new Options
        {
            DestinationFolder = destinationFolder,
            RecordingsLifeTimeDays = recordingsLifeTimeDays,
        };

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(options);

        var commandLineMock = Mock.Of<ICommandLineService>();
        commandLineMock.OptionsIdentifier.Returns(optionsIdentifier);

        return new PurgeRecordingsService(optionsMock.Object, commandLineMock.Object);
    }

    private static string CreateRecordingFile(string rootFolder, DateTime date, int trackNum = 1)
    {
        var dayFolder = Path.Combine(
            rootFolder,
            date.ToString("yyyy", CultureInfo.InvariantCulture),
            date.ToString("MM", CultureInfo.InvariantCulture),
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        Directory.CreateDirectory(dayFolder);

        var dayName = date.ToString("dddd", CultureInfo.InvariantCulture);
        var fileName = $"{dayName} {date:dd MMMM yyyy} - {trackNum:D3}.mp3";
        var filePath = Path.Combine(dayFolder, fileName);
        File.WriteAllText(filePath, "dummy audio content");
        return filePath;
    }

    private static async Task<int> InvokePurgeFilesInternal(PurgeRecordingsService service, int days) =>
        await service.PurgeFilesInternal(days);

    private static async Task<int> InvokeRemoveEmptyFolders(PurgeRecordingsService service) =>
        await service.RemoveEmptyFolders();

    private static string CreateTempTestDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"OnlyRTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CleanupTempDir(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    [Test]
    public async Task ConstructorDoesNotThrow()
    {
        var dir = CreateTempTestDir();

        try
        {
            await StaThreadHelper.RunOnSta(() =>
            {
                using var service = CreateService(dir);
            });
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task CloseDoesNotThrow()
    {
        var dir = CreateTempTestDir();

        try
        {
            await StaThreadHelper.RunOnSta(() =>
            {
                var service = CreateService(dir);
                service.Close();
                service.Dispose();
            });
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task DisposeDoesNotThrow()
    {
        var dir = CreateTempTestDir();

        try
        {
            await StaThreadHelper.RunOnSta(() =>
            {
                var service = CreateService(dir);
                service.Dispose();
            });
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeDeletesOldFiles()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var oldFilePath = CreateRecordingFile(dir, oldDate);

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service, 30);
                await Assert.That(deletedCount).IsGreaterThanOrEqualTo(1);
                await Assert.That(File.Exists(oldFilePath)).IsFalse();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeKeepsRecentFiles()
    {
        var dir = CreateTempTestDir();

        try
        {
            var todayFilePath = CreateRecordingFile(dir, DateTime.Now);

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service, 30);
                await Assert.That(deletedCount).IsEqualTo(0);
                await Assert.That(File.Exists(todayFilePath)).IsTrue();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeDeletesEmptyFolders()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var oldFilePath = CreateRecordingFile(dir, oldDate);
            var dateFolderPath = Path.GetDirectoryName(oldFilePath)!;

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                await InvokePurgeFilesInternal(service, 30);
                var foldersDeleted = await InvokeRemoveEmptyFolders(service);
                await Assert.That(foldersDeleted).IsGreaterThanOrEqualTo(1);
                await Assert.That(Directory.Exists(dateFolderPath)).IsFalse();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeHandlesMissingDestinationFolder()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"OnlyRTest_NonExistent_{Guid.NewGuid():N}");
        CleanupTempDir(nonExistentDir);

        var service = await StaThreadHelper.RunOnSta(() => CreateService(nonExistentDir, recordingsLifeTimeDays: 30));

        try
        {
            var deletedCount = await InvokePurgeFilesInternal(service, 30);
            await Assert.That(deletedCount).IsEqualTo(0);

            var foldersDeleted = await InvokeRemoveEmptyFolders(service);
            await Assert.That(foldersDeleted).IsEqualTo(0);
        }
        finally
        {
            service.Close();
            service.Dispose();
            CleanupTempDir(nonExistentDir);
        }
    }

    [Test]
    public async Task CancellationStopsPurge()
    {
        var dir = CreateTempTestDir();

        try
        {
            for (var i = 1; i <= 5; i++)
            {
                CreateRecordingFile(dir, DateTime.Now.AddDays(-60 - i), i);
            }

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            service.Close();

            int deletedCount;
            try
            {
                deletedCount = await InvokePurgeFilesInternal(service, 30);
            }
            catch (TaskCanceledException)
            {
                deletedCount = 0;
            }

            await Assert.That(deletedCount).IsEqualTo(0);
            service.Dispose();
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeKeepsNonAudioFiles()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var dayFolder = Path.Combine(
                dir,
                oldDate.ToString("yyyy", CultureInfo.InvariantCulture),
                oldDate.ToString("MM", CultureInfo.InvariantCulture),
                oldDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(dayFolder);

            var txtFilePath = Path.Combine(dayFolder, "notes.txt");
            await File.WriteAllTextAsync(txtFilePath, "some text content");

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service, 30);
                await Assert.That(deletedCount).IsEqualTo(0);
                await Assert.That(File.Exists(txtFilePath)).IsTrue();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeWithOptionsIdentifier()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var subDir = Path.Combine(dir, "testId");
            Directory.CreateDirectory(subDir);
            CreateRecordingFile(subDir, oldDate);

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30, optionsIdentifier: "testId"));

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service, 30);
                await Assert.That(deletedCount).IsGreaterThanOrEqualTo(1);
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task PurgeBatchesMaxFileDeletions()
    {
        var dir = CreateTempTestDir();

        try
        {
            // Create 25 old files (more than MaxFileDeletionsInBatch = 20)
            for (var i = 1; i <= 25; i++)
            {
                CreateRecordingFile(dir, DateTime.Now.AddDays(-60), i);
            }

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service, 30);
                // Should not exceed 20 (MaxFileDeletionsInBatch)
                await Assert.That(deletedCount).IsLessThanOrEqualTo(20);
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task YearFolderMayContainCandidatesReturnsTrueForOldYear()
    {
        var result = PurgeRecordingsService.YearFolderMayContainCandidates(2020, new DateTime(2026, 4, 7));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task YearFolderMayContainCandidatesReturnsFalseForFutureYear()
    {
        var result = PurgeRecordingsService.YearFolderMayContainCandidates(2030, new DateTime(2026, 4, 7));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task MonthFolderMayContainCandidatesReturnsTrueForOlderMonth()
    {
        var result = PurgeRecordingsService.MonthFolderMayContainCandidates(2026, 1, new DateTime(2026, 4, 7));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task MonthFolderMayContainCandidatesReturnsFalseForFutureMonth()
    {
        var result = PurgeRecordingsService.MonthFolderMayContainCandidates(2026, 12, new DateTime(2026, 4, 7));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task MonthFolderMayContainCandidatesReturnsTrueForSameMonth()
    {
        var result = PurgeRecordingsService.MonthFolderMayContainCandidates(2026, 4, new DateTime(2026, 4, 7));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task MonthFolderMayContainCandidatesReturnsTrueForOlderYear()
    {
        var result = PurgeRecordingsService.MonthFolderMayContainCandidates(2025, 12, new DateTime(2026, 4, 7));
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task PurgeDeletesWavFilesToo()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var dayFolder = Path.Combine(
                dir,
                oldDate.ToString("yyyy", CultureInfo.InvariantCulture),
                oldDate.ToString("MM", CultureInfo.InvariantCulture),
                oldDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(dayFolder);

            var wavFile = Path.Combine(dayFolder, "recording - 001.wav");
            File.WriteAllText(wavFile, "dummy wav");

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service, 30);
                await Assert.That(deletedCount).IsGreaterThanOrEqualTo(1);
                await Assert.That(File.Exists(wavFile)).IsFalse();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task RemoveEmptyFoldersIgnoresInvalidYearFolders()
    {
        var dir = CreateTempTestDir();

        try
        {
            // Create an invalid year folder
            var invalidFolder = Path.Combine(dir, "notayear");
            Directory.CreateDirectory(invalidFolder);

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var foldersDeleted = await InvokeRemoveEmptyFolders(service);
                await Assert.That(foldersDeleted).IsEqualTo(0);
                await Assert.That(Directory.Exists(invalidFolder)).IsTrue();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    // ========================================================================
    // GetNextPurgeJob
    // ========================================================================

    [Test]
    public async Task GetNextPurgeJobFromNothingIsFilePurge() =>
        await Assert.That(PurgeRecordingsService.GetNextPurgeJob(PurgeServiceJob.Nothing))
            .IsEqualTo(PurgeServiceJob.FilePurge);

    [Test]
    public async Task GetNextPurgeJobFromFilePurgeIsFolderPurge() =>
        await Assert.That(PurgeRecordingsService.GetNextPurgeJob(PurgeServiceJob.FilePurge))
            .IsEqualTo(PurgeServiceJob.FolderPurge);

    [Test]
    public async Task GetNextPurgeJobFromFolderPurgeIsFilePurge() =>
        await Assert.That(PurgeRecordingsService.GetNextPurgeJob(PurgeServiceJob.FolderPurge))
            .IsEqualTo(PurgeServiceJob.FilePurge);

    // ========================================================================
    // ShouldDeleteEmptyFolder
    // ========================================================================

    [Test]
    public async Task ShouldDeleteEmptyFolderNullYear() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(null, null, null, DateTime.Now))
            .IsFalse();

    [Test]
    public async Task ShouldDeleteEmptyYearFolderFromPastYear() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(2020, null, null, new DateTime(2026, 4, 7)))
            .IsTrue();

    [Test]
    public async Task ShouldNotDeleteEmptyYearFolderFromCurrentYear() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(2026, null, null, new DateTime(2026, 4, 7)))
            .IsFalse();

    [Test]
    public async Task ShouldDeleteEmptyMonthFolderFromPastMonth() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(2026, 1, null, new DateTime(2026, 4, 7)))
            .IsTrue();

    [Test]
    public async Task ShouldNotDeleteEmptyMonthFolderFromCurrentMonth() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(2026, 4, null, new DateTime(2026, 4, 7)))
            .IsFalse();

    [Test]
    public async Task ShouldDeleteEmptyDateFolderFromPastDate() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(
            2026, 4, new DateTime(2026, 4, 1), new DateTime(2026, 4, 7)))
            .IsTrue();

    [Test]
    public async Task ShouldNotDeleteEmptyDateFolderFromToday() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(
            2026, 4, new DateTime(2026, 4, 7), new DateTime(2026, 4, 7)))
            .IsFalse();

    [Test]
    public async Task ShouldDeleteEmptyMonthFolderFromPastYear() =>
        await Assert.That(PurgeRecordingsService.ShouldDeleteEmptyFolder(2025, 12, null, new DateTime(2026, 4, 7)))
            .IsTrue();

    [Test]
    public async Task RemoveEmptyFoldersKeepsNonEmptyFolder()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var oldFilePath = CreateRecordingFile(dir, oldDate);
            var dateFolderPath = Path.GetDirectoryName(oldFilePath)!;

            var service = await StaThreadHelper.RunOnSta(() => CreateService(dir, recordingsLifeTimeDays: 30));

            try
            {
                var foldersDeleted = await InvokeRemoveEmptyFolders(service);
                await Assert.That(foldersDeleted).IsEqualTo(0);
                await Assert.That(Directory.Exists(dateFolderPath)).IsTrue();
            }
            finally
            {
                service.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
