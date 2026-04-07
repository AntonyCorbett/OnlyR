using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingDestination;
using OnlyR.Utils;

namespace OnlyR.Tests;

public class TestRecordingDestinationService
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

    [Test]
    public async Task GetRecordingFileCandidateCreatesPaths()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Mp3 };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Act
        var candidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert
        await Assert.That(candidate.TempPath).IsNotNull().And.IsNotEmpty();
        await Assert.That(candidate.FinalPath).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task TrackNumberStartsAtOne()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Mp3 };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Act
        var candidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert
        await Assert.That(candidate.TrackNumber).IsEqualTo(1);
    }

    [Test]
    public async Task TrackNumberIncrements()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Mp3 };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Act - first call to get track 1
        var firstCandidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Create the file at the first candidate's FinalPath to simulate it existing
        var dir = Path.GetDirectoryName(firstCandidate.FinalPath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.Create(firstCandidate.FinalPath).DisposeAsync();

        // Act - second call should detect existing file and increment
        var secondCandidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert
        await Assert.That(firstCandidate.TrackNumber).IsEqualTo(1);
        await Assert.That(secondCandidate.TrackNumber).IsEqualTo(2);
    }

    [Test]
    public async Task CorrectFileExtensionForMp3()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Mp3 };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Act
        var candidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert
        await Assert.That(candidate.FinalPath).EndsWith(".mp3");
    }

    [Test]
    public async Task CorrectFileExtensionForWav()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Wav };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Act
        var candidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert
        await Assert.That(candidate.FinalPath).EndsWith(".wav");
    }

    [Test]
    public async Task ThrowsWhenMaxRecordingsReached()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Mp3, MaxRecordingsInOneFolder = 10 };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Pre-create destination folder and files for tracks 001-009.
        var destFolder = FileUtils.GetDestinationFolder(testDate, null, tempDir);
        Directory.CreateDirectory(destFolder);

        var coreName = $"{CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)testDate.DayOfWeek]} {testDate:dd MMMM yyyy}";

        for (var i = 1; i <= 9; i++)
        {
            await File.Create(Path.Combine(destFolder, $"{coreName} - {i:D3}.mp3")).DisposeAsync();
        }

        // Act & Assert
        await Assert.That(() => service.GetRecordingFileCandidate(optionsMock.Object, testDate, null))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task MalformedFilenameIsSkipped()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = tempDir, Codec = AudioCodec.Mp3 };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Pre-create destination folder with a malformed filename (non-numeric track).
        var destFolder = FileUtils.GetDestinationFolder(testDate, null, tempDir);
        Directory.CreateDirectory(destFolder);

        var coreName = $"{CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)testDate.DayOfWeek]} {testDate:dd MMMM yyyy}";
        await File.Create(Path.Combine(destFolder, $"{coreName} - XYZ.mp3")).DisposeAsync();

        // Act
        var candidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert - malformed file is skipped, so track starts at 1.
        await Assert.That(candidate.TrackNumber).IsEqualTo(1);
    }
}