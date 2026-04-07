using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
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
        File.WriteAllText(Path.Combine(recordingsFolder, "notes.txt"), "not audio");
        File.WriteAllText(Path.Combine(recordingsFolder, "readme.txt"), "also not audio");

        var service = CreateService(tempDir);

        await Assert.That(() => service.Copy(new List<char> { 'Z' }))
            .Throws<NoRecordingsException>();
    }

    [Test]
    public async Task CanCopyFileReturnsTrueForAccessibleFile()
    {
        var method = typeof(CopyRecordingsService).GetMethod(
            "CanCopyFile",
            BindingFlags.NonPublic | BindingFlags.Static);

        var filePath = Path.Combine(tempDir, "accessible.mp3");
        File.WriteAllText(filePath, "dummy audio data");

        var result = (bool)method!.Invoke(null, [filePath])!;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanCopyFileReturnsFalseForMissingFile()
    {
        var method = typeof(CopyRecordingsService).GetMethod(
            "CanCopyFile",
            BindingFlags.NonPublic | BindingFlags.Static);

        var filePath = Path.Combine(tempDir, "nonexistent.mp3");

        var result = (bool)method!.Invoke(null, [filePath])!;

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsFileLockedReturnsTrueForLockedFile()
    {
        var method = typeof(CopyRecordingsService).GetMethod(
            "IsFileLocked",
            BindingFlags.NonPublic | BindingFlags.Static);

        var filePath = Path.Combine(tempDir, "locked.mp3");
        File.WriteAllText(filePath, "dummy audio data");

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var result = (bool)method!.Invoke(null, [filePath])!;

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsFileLockedReturnsFalseForUnlockedFile()
    {
        var method = typeof(CopyRecordingsService).GetMethod(
            "IsFileLocked",
            BindingFlags.NonPublic | BindingFlags.Static);

        var filePath = Path.Combine(tempDir, "unlocked.mp3");
        File.WriteAllText(filePath, "dummy audio data");

        var result = (bool)method!.Invoke(null, [filePath])!;

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetSpaceNeedSumsFileSizes()
    {
        var method = typeof(CopyRecordingsService).GetMethod(
            "GetSpaceNeed",
            BindingFlags.NonPublic | BindingFlags.Static);

        var file1 = Path.Combine(tempDir, "file1.mp3");
        var file2 = Path.Combine(tempDir, "file2.mp3");
        File.WriteAllText(file1, "abcdef"); // 6 bytes
        File.WriteAllText(file2, "ghijklmnop"); // 10 bytes

        var expectedSize = new FileInfo(file1).Length + new FileInfo(file2).Length;
        var files = new[] { file1, file2 };

        var result = (long)method!.Invoke(null, [files])!;

        await Assert.That(result).IsEqualTo(expectedSize);
    }
}
