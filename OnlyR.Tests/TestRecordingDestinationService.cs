using System;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingDestination;

namespace OnlyR.Tests;

public class TestRecordingDestinationService
{
    private string _tempDir = string.Empty;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "OnlyRTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public async Task GetRecordingFileCandidateCreatesPaths()
    {
        // Arrange
        var optionsMock = Mock.Of<IOptionsService>();
        var options = new Options { DestinationFolder = _tempDir, Codec = AudioCodec.Mp3 };
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
        var options = new Options { DestinationFolder = _tempDir, Codec = AudioCodec.Mp3 };
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
        var options = new Options { DestinationFolder = _tempDir, Codec = AudioCodec.Mp3 };
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

        File.Create(firstCandidate.FinalPath).Dispose();

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
        var options = new Options { DestinationFolder = _tempDir, Codec = AudioCodec.Mp3 };
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
        var options = new Options { DestinationFolder = _tempDir, Codec = AudioCodec.Wav };
        optionsMock.Options.Returns(options);

        var service = new RecordingDestinationService();
        var testDate = new DateTime(2026, 4, 7, 10, 30, 0);

        // Act
        var candidate = service.GetRecordingFileCandidate(optionsMock.Object, testDate, null);

        // Assert
        await Assert.That(candidate.FinalPath).EndsWith(".wav");
    }
}