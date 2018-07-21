namespace OnlyR.Services.RecordingDestination
{
    using System;
    using Model;
    using Options;

    public interface IRecordingDestinationService
    {
        // ReSharper disable once UnusedMember.Global
        RecordingCandidate GetRecordingFileCandidate(IOptionsService optionsService, DateTime dt, string commandLineIdentifier);
    }
}
