using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Exceptions;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingCopies;

namespace OnlyR.Tests;

public sealed class TestCopyRecordingsService
{
    private string tempDir = string.Empty;

    [Before(Test)]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "OnlyRTests_Copy_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetRecordingsFolderReturnsNullWhenNonExistent()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"OnlyRTest_{Guid.NewGuid():N}");
        var service = CreateService(nonExistent);

        var result = service.GetRecordingsFolder();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetRecordingsFolderReturnsFolderWhenExists()
    {
        var todayFolder = CreateTodayRecordingsFolder();
        var service = CreateService(tempDir);

        var result = service.GetRecordingsFolder();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEqualTo(todayFolder);
    }

    [Test]
    public async Task CopyThrowsNoRecordingsWhenOnlyNonAudioFiles()
    {
        var recordingsFolder = CreateTodayRecordingsFolder();
        await File.WriteAllTextAsync(Path.Combine(recordingsFolder, "readme.md"), "not audio");
        await File.WriteAllTextAsync(Path.Combine(recordingsFolder, "data.json"), "{}");

        var service = CreateService(tempDir);

        await Assert.That(() => service.Copy(new List<char> { 'Z' }))
            .Throws<NoRecordingsException>();
    }

    [Test]
    public async Task CopyThrowsNoRecordingsWhenFolderMissing()
    {
        var nonExistentDir = Path.Combine(tempDir, "does_not_exist");
        var service = CreateService(nonExistentDir);

        await Assert.That(() => service.Copy(new List<char> { 'Z' }))
            .Throws<NoRecordingsException>();
    }

    [Test]
    public async Task CopyThrowsNoRecordingsWhenNoAudioFiles()
    {
        var recordingsFolder = CreateTodayRecordingsFolder();
        await File.WriteAllTextAsync(Path.Combine(recordingsFolder, "notes.txt"), "not audio");
        await File.WriteAllTextAsync(Path.Combine(recordingsFolder, "readme.txt"), "also not audio");

        var service = CreateService(tempDir);

        await Assert.That(() => service.Copy(new List<char> { 'Z' }))
            .Throws<NoRecordingsException>();
    }

    [Test]
    public async Task CanCopyFileReturnsTrueForAccessibleFile()
    {
        var filePath = Path.Combine(tempDir, "accessible.mp3");
        await File.WriteAllTextAsync(filePath, "dummy audio data");

        var result = CopyRecordingsService.CanCopyFile(filePath);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanCopyFileReturnsFalseForMissingFile()
    {
        var filePath = Path.Combine(tempDir, "nonexistent.mp3");

        var result = CopyRecordingsService.CanCopyFile(filePath);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsFileLockedReturnsTrueForLockedFile()
    {
        var filePath = Path.Combine(tempDir, "locked.mp3");
        await File.WriteAllTextAsync(filePath, "dummy audio data");

        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var result = CopyRecordingsService.IsFileLocked(filePath);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFileLockedReturnsFalseForUnlockedFile()
    {
        var filePath = Path.Combine(tempDir, "unlocked.mp3");
        await File.WriteAllTextAsync(filePath, "dummy audio data");

        var result = CopyRecordingsService.IsFileLocked(filePath);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetSpaceNeedSumsFileSizes()
    {
        var file1 = Path.Combine(tempDir, "file1.mp3");
        var file2 = Path.Combine(tempDir, "file2.mp3");
        await File.WriteAllTextAsync(file1, "abcdef");
        await File.WriteAllTextAsync(file2, "ghijklmnop");

        var expectedSize = new FileInfo(file1).Length + new FileInfo(file2).Length;
        var files = new[] { file1, file2 };

        var result = CopyRecordingsService.GetSpaceNeed(files);

        await Assert.That(result).IsEqualTo(expectedSize);
    }

    private sealed class StubDriveEjectionService : IDriveEjectionService
    {
        public bool Eject(char driveLetter) => true;
    }

    private static CopyRecordingsService CreateService(string destinationFolder)
    {
        var cmdMock = Mock.Of<ICommandLineService>();
        cmdMock.OptionsIdentifier.Returns((string?)null);

        var optsMock = Mock.Of<IOptionsService>();
        optsMock.Options.Returns(new Options { DestinationFolder = destinationFolder });

        return new CopyRecordingsService(cmdMock.Object, optsMock.Object, new StubDriveEjectionService());
    }

    private string CreateTodayRecordingsFolder()
    {
        var today = DateTime.Today;
        var folder = Path.Combine(
            tempDir,
            today.ToString("yyyy", CultureInfo.InvariantCulture),
            today.ToString("MM", CultureInfo.InvariantCulture),
            today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);
        return folder;
    }
}
