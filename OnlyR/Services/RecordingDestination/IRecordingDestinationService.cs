using System;
using OnlyR.Model;
using OnlyR.Services.Options;

namespace OnlyR.Services.RecordingDestination
{
    public interface IRecordingDestinationService
    {
        // ReSharper disable once UnusedMember.Global
        RecordingCandidate GetRecordingFileCandidate(IOptionsService optionsService, DateTime dt, string? commandLineIdentifier);
    }
}
