using OnlyR.Model;
using OnlyR.Services.Options;
using System;

namespace OnlyR.Services.RecordingDestination
{
    public interface IRecordingDestinationService
    {
        // ReSharper disable once UnusedMember.Global
        RecordingCandidate GetRecordingFileCandidate(IOptionsService optionsService, DateTime dt, string? commandLineIdentifier);
    }
}