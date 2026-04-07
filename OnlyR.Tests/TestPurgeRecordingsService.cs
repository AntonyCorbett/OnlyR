#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
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

    private static async Task<int> InvokePurgeFilesInternal(PurgeRecordingsService service, int days)
    {
        var method = typeof(PurgeRecordingsService).GetMethod(
            "PurgeFilesInternal",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<int>)method!.Invoke(service, [days])!;
        return await task;
    }

    private static async Task<int> InvokeRemoveEmptyFolders(PurgeRecordingsService service)
    {
        var method = typeof(PurgeRecordingsService).GetMethod(
            "RemoveEmptyFolders",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<int>)method!.Invoke(service, Array.Empty<object>())!;
        return await task;
    }

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
        var success = false;

        try
        {
            var t = new Thread(() =>
            {
                using var service = CreateService(dir);
                success = true;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
        finally
        {
            CleanupTempDir(dir);
        }

        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task CloseDoesNotThrow()
    {
        var dir = CreateTempTestDir();
        var success = false;

        try
        {
            var t = new Thread(() =>
            {
                var service = CreateService(dir);
                service.Close();
                service.Dispose();
                success = true;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
        finally
        {
            CleanupTempDir(dir);
        }

        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task DisposeDoesNotThrow()
    {
        var dir = CreateTempTestDir();
        var success = false;

        try
        {
            var t = new Thread(() =>
            {
                var service = CreateService(dir);
                service.Dispose();
                success = true;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
        finally
        {
            CleanupTempDir(dir);
        }

        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task PurgeDeletesOldFiles()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var oldFilePath = CreateRecordingFile(dir, oldDate);

            PurgeRecordingsService? service = null;
            var t = new Thread(() => { service = CreateService(dir, recordingsLifeTimeDays: 30); });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service!, 30);
                await Assert.That(deletedCount).IsGreaterThanOrEqualTo(1);
                await Assert.That(File.Exists(oldFilePath)).IsFalse();
            }
            finally
            {
                service!.Close();
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

            PurgeRecordingsService? service = null;
            var t = new Thread(() => { service = CreateService(dir, recordingsLifeTimeDays: 30); });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service!, 30);
                await Assert.That(deletedCount).IsEqualTo(0);
                await Assert.That(File.Exists(todayFilePath)).IsTrue();
            }
            finally
            {
                service!.Close();
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

            PurgeRecordingsService? service = null;
            var t = new Thread(() => { service = CreateService(dir, recordingsLifeTimeDays: 30); });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            try
            {
                await InvokePurgeFilesInternal(service!, 30);
                var foldersDeleted = await InvokeRemoveEmptyFolders(service!);
                await Assert.That(foldersDeleted).IsGreaterThanOrEqualTo(1);
                await Assert.That(Directory.Exists(dateFolderPath)).IsFalse();
            }
            finally
            {
                service!.Close();
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

        PurgeRecordingsService? service = null;
        var t = new Thread(() => { service = CreateService(nonExistentDir, recordingsLifeTimeDays: 30); });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();

        try
        {
            var deletedCount = await InvokePurgeFilesInternal(service!, 30);
            await Assert.That(deletedCount).IsEqualTo(0);

            var foldersDeleted = await InvokeRemoveEmptyFolders(service!);
            await Assert.That(foldersDeleted).IsEqualTo(0);
        }
        finally
        {
            service!.Close();
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

            PurgeRecordingsService? service = null;
            var t = new Thread(() => { service = CreateService(dir, recordingsLifeTimeDays: 30); });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            service!.Close();

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

            PurgeRecordingsService? service = null;
            var t = new Thread(() => service = CreateService(dir, recordingsLifeTimeDays: 30));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service!, 30);
                await Assert.That(deletedCount).IsEqualTo(0);
                await Assert.That(File.Exists(txtFilePath)).IsTrue();
            }
            finally
            {
                service!.Close();
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

            PurgeRecordingsService? service = null;
            var t = new Thread(() => { service = CreateService(dir, recordingsLifeTimeDays: 30, optionsIdentifier: "testId"); });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            try
            {
                var deletedCount = await InvokePurgeFilesInternal(service!, 30);
                await Assert.That(deletedCount).IsGreaterThanOrEqualTo(1);
            }
            finally
            {
                service!.Close();
                service.Dispose();
            }
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task RemoveEmptyFoldersKeepsNonEmptyFolder()
    {
        var dir = CreateTempTestDir();

        try
        {
            var oldDate = DateTime.Now.AddDays(-60);
            var oldFilePath = CreateRecordingFile(dir, oldDate);
            var dateFolderPath = Path.GetDirectoryName(oldFilePath)!;

            PurgeRecordingsService? service = null;
            var t = new Thread(() => { service = CreateService(dir, recordingsLifeTimeDays: 30); });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            try
            {
                var foldersDeleted = await InvokeRemoveEmptyFolders(service!);
                await Assert.That(foldersDeleted).IsEqualTo(0);
                await Assert.That(Directory.Exists(dateFolderPath)).IsTrue();
            }
            finally
            {
                service!.Close();
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
