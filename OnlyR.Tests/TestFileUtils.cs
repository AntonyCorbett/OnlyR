using System;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Utils;

namespace OnlyR.Tests;

public sealed class TestFileUtils
{
    private string tempDir = string.Empty;

    [Before(Test)]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "OnlyRTests_" + Guid.NewGuid().ToString("N"));
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

    // ========================================================================
    // ParseYearFromFolderName
    // ========================================================================

    [Test]
    public async Task ParseYearValidYear()
    {
        var result = FileUtils.ParseYearFromFolderName("2026");
        await Assert.That(result).IsEqualTo(2026);
    }

    [Test]
    public async Task ParseYearTooShort()
    {
        var result = FileUtils.ParseYearFromFolderName("202");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseYearTooLong()
    {
        var result = FileUtils.ParseYearFromFolderName("20260");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseYearNonNumeric()
    {
        var result = FileUtils.ParseYearFromFolderName("abcd");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseYearBelowRange()
    {
        var result = FileUtils.ParseYearFromFolderName("1999");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseYearAboveRange()
    {
        var result = FileUtils.ParseYearFromFolderName("3001");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseYearBoundaryLow()
    {
        var result = FileUtils.ParseYearFromFolderName("2000");
        await Assert.That(result).IsEqualTo(2000);
    }

    [Test]
    public async Task ParseYearBoundaryHigh()
    {
        var result = FileUtils.ParseYearFromFolderName("3000");
        await Assert.That(result).IsEqualTo(3000);
    }

    // ========================================================================
    // ParseMonthFromFolderName
    // ========================================================================

    [Test]
    public async Task ParseMonthValid()
    {
        var result = FileUtils.ParseMonthFromFolderName("04");
        await Assert.That(result).IsEqualTo(4);
    }

    [Test]
    public async Task ParseMonthTooShort()
    {
        var result = FileUtils.ParseMonthFromFolderName("4");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseMonthTooLong()
    {
        var result = FileUtils.ParseMonthFromFolderName("042");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseMonthNonNumeric()
    {
        var result = FileUtils.ParseMonthFromFolderName("ab");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseMonthZero()
    {
        var result = FileUtils.ParseMonthFromFolderName("00");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseMonthAboveRange()
    {
        var result = FileUtils.ParseMonthFromFolderName("13");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseMonthBoundaryLow()
    {
        var result = FileUtils.ParseMonthFromFolderName("01");
        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task ParseMonthBoundaryHigh()
    {
        var result = FileUtils.ParseMonthFromFolderName("12");
        await Assert.That(result).IsEqualTo(12);
    }

    // ========================================================================
    // ParseDateFromFolderName
    // ========================================================================

    [Test]
    public async Task ParseDateValid()
    {
        var result = FileUtils.ParseDateFromFolderName("2026-04-07", 2026, 4);
        await Assert.That(result).IsEqualTo(new DateTime(2026, 4, 7));
    }

    [Test]
    public async Task ParseDateWrongLength()
    {
        var result = FileUtils.ParseDateFromFolderName("2026-4-7", 2026, 4);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseDateInvalidFormat()
    {
        var result = FileUtils.ParseDateFromFolderName("07-04-2026", 2026, 4);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseDateYearMismatch()
    {
        var result = FileUtils.ParseDateFromFolderName("2025-04-07", 2026, 4);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseDateMonthMismatch()
    {
        var result = FileUtils.ParseDateFromFolderName("2026-05-07", 2026, 4);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseDateInvalidDay()
    {
        var result = FileUtils.ParseDateFromFolderName("2026-04-32", 2026, 4);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseDateLeapYear()
    {
        var result = FileUtils.ParseDateFromFolderName("2024-02-29", 2024, 2);
        await Assert.That(result).IsEqualTo(new DateTime(2024, 2, 29));
    }

    // ========================================================================
    // Path Composition
    // ========================================================================

    [Test]
    public async Task GetDestinationFolderFormat()
    {
        var dt = new DateTime(2026, 4, 7);
        var result = FileUtils.GetDestinationFolder(dt, null, tempDir);

        await Assert.That(result).Contains("2026");
        await Assert.That(result).Contains("04");
        await Assert.That(result).Contains("2026-04-07");
    }

    [Test]
    public async Task GetMonthlyDestinationFolderFormat()
    {
        var dt = new DateTime(2026, 4, 7);
        var result = FileUtils.GetMonthlyDestinationFolder(dt, null, tempDir);

        await Assert.That(result).Contains("2026");
        await Assert.That(result).Contains("04");
    }

    [Test]
    public async Task GetRootDestinationFolderWithIdentifier()
    {
        var result = FileUtils.GetRootDestinationFolder("myId", tempDir);
        await Assert.That(result).Contains("myId");
    }

    [Test]
    public async Task GetRootDestinationFolderWithoutIdentifier()
    {
        var result = FileUtils.GetRootDestinationFolder(null, tempDir);
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
    }

    // ========================================================================
    // File System Operations
    // ========================================================================

    [Test]
    public async Task CreateDirectoryNew()
    {
        var newDir = Path.Combine(tempDir, "newSubDir");
        FileUtils.CreateDirectory(newDir);
        await Assert.That(Directory.Exists(newDir)).IsTrue();
    }

    [Test]
    public async Task CreateDirectoryExisting()
    {
        FileUtils.CreateDirectory(tempDir);
        await Assert.That(Directory.Exists(tempDir)).IsTrue();
    }

    [Test]
    public async Task IsDirectoryEmptyTrue()
    {
        var result = FileUtils.IsDirectoryEmpty(tempDir);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsDirectoryEmptyFalseWithFile()
    {
        await File.WriteAllTextAsync(Path.Combine(this.tempDir, "test.txt"), "content");
        var result = FileUtils.IsDirectoryEmpty(tempDir);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsDirectoryEmptyFalseWithSubdir()
    {
        Directory.CreateDirectory(Path.Combine(tempDir, "subDir"));
        var result = FileUtils.IsDirectoryEmpty(tempDir);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public void SafeDeleteFolderExisting()
    {
        var folderToDelete = Path.Combine(tempDir, "toDelete");
        Directory.CreateDirectory(folderToDelete);
        FileUtils.SafeDeleteFolder(folderToDelete);
    }

    [Test]
    public void SafeDeleteFolderNullPath()
    {
        FileUtils.SafeDeleteFolder(null!);
    }

    [Test]
    public void SafeDeleteFolderEmptyPath()
    {
        FileUtils.SafeDeleteFolder(string.Empty);
    }

    [Test]
    public void SafeDeleteFolderNonExistent()
    {
        FileUtils.SafeDeleteFolder(Path.Combine(tempDir, "doesNotExist"));
    }

    [Test]
    public void SafeDeleteFileExisting()
    {
        var filePath = Path.Combine(tempDir, "toDelete.txt");
        File.WriteAllText(filePath, "content");
        FileUtils.SafeDeleteFile(filePath);
    }

    [Test]
    public void SafeDeleteFileNullPath()
    {
        FileUtils.SafeDeleteFile(null!);
    }

    [Test]
    public void SafeDeleteFileEmptyPath()
    {
        FileUtils.SafeDeleteFile(string.Empty);
    }

    [Test]
    public void SafeDeleteFileNonExistent()
    {
        FileUtils.SafeDeleteFile(Path.Combine(tempDir, "doesNotExist.txt"));
    }

    [Test]
    public async Task MoveFileSuccess()
    {
        var sourcePath = Path.Combine(tempDir, "source.txt");
        await File.WriteAllTextAsync(sourcePath, "content");
        var destPath = Path.Combine(tempDir, "dest", "moved.txt");

        FileUtils.MoveFile(sourcePath, destPath);

        await Assert.That(File.Exists(sourcePath)).IsFalse();
        await Assert.That(File.Exists(destPath)).IsTrue();
    }

    [Test]
    public async Task MoveFileSourceMissing()
    {
        var sourcePath = Path.Combine(tempDir, "nonexistent.txt");
        var destPath = Path.Combine(tempDir, "dest", "moved.txt");

        await Assert.That(() => FileUtils.MoveFile(sourcePath, destPath))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task GetTempRecordingFolderReturnsValidPath()
    {
        var result = FileUtils.GetTempRecordingFolder();
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
        await Assert.That(Directory.Exists(result)).IsTrue();
    }

    [Test]
    public async Task GetSystemTempFolderReturnsValidPath()
    {
        var result = FileUtils.GetSystemTempFolder();
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
    }
}
