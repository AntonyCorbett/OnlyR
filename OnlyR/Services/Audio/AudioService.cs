using System;
using System.Collections.Generic;
using System.IO;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Recorder;
using OnlyR.Model;
using OnlyR.Services.Options;
using Serilog;

namespace OnlyR.Services.Audio
{
    // ReSharper disable once UnusedMember.Global
    public sealed class AudioService : IAudioService, IDisposable
    {
        private AudioRecorder _audioRecorder;
        private RecordingCandidate _currentRecording;

        public event EventHandler StartedEvent;
        public event EventHandler StoppedEvent;
        public event EventHandler StopRequested;
        public event EventHandler<RecordingProgressEventArgs> RecordingProgressEvent;

        public AudioService()
        {
            _audioRecorder = new AudioRecorder();
            _audioRecorder.RecordingStatusChangeEvent += AudioRecorderOnRecordingStatusChangeHandler;
            _audioRecorder.ProgressEvent += AudioRecorderOnProgressHandler;
        }

        private void AudioRecorderOnProgressHandler(object sender, RecordingProgressEventArgs e)
        {
            OnRecordingProgressEvent(e);
        }

        private void AudioRecorderOnRecordingStatusChangeHandler(object sender, RecordingStatusChangeEventArgs recordingStatusChangeEventArgs)
        {
            switch(recordingStatusChangeEventArgs.RecordingStatus)
            {
                case RecordingStatus.NotRecording:
                    OnStoppedEvent();
                    break;
                case RecordingStatus.Recording:
                    OnStartedEvent();
                    break;
                case RecordingStatus.StopRequested:
                    OnStopRequested();
                    break;

                // ReSharper disable once RedundantCaseLabel
                case RecordingStatus.Unknown:
                default:
                    break;
            }
        }

        public void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService)
        {
            _currentRecording = candidateFile;

            RecordingConfig recordingConfig = new RecordingConfig
            {
                RecordingDevice = optionsService.Options.RecordingDevice,
                RecordingDate = candidateFile.RecordingDate,
                TrackNumber = candidateFile.TrackNumber,
                DestFilePath = candidateFile.TempPath,
                SampleRate = optionsService.Options.SampleRate,
                ChannelCount = optionsService.Options.ChannelCount,
                Mp3BitRate = optionsService.Options.Mp3BitRate,
                TrackTitle = GetTrackTitle(candidateFile),
                AlbumName = GetAlbumName(candidateFile),
                Genre = optionsService.Options.Genre
            };

            _audioRecorder.Start(recordingConfig);
        }

        private static string GetAlbumName(RecordingCandidate candidate)
        {
            return candidate.RecordingDate.ToString("MMM yyyy");
        }

        private static string GetTrackTitle(RecordingCandidate candidate)
        {
            return Path.GetFileNameWithoutExtension(candidate.FinalPath);
        }

        public void StopRecording(bool fadeOut)
        {
            _audioRecorder.Stop(fadeOut);
        }

        private void OnStartedEvent()
        {
            StartedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnStoppedEvent()
        {
            StoppedEvent?.Invoke(this, EventArgs.Empty);
            CopyFileToFinalDestination();
            _currentRecording = null;
        }

        private void CopyFileToFinalDestination()
        {
            Log.Logger.Information("Copying {Source} to {Target}", _currentRecording.TempPath, _currentRecording.FinalPath);
            var path = Path.GetDirectoryName(_currentRecording.FinalPath);
            if (path != null)
            {
                Directory.CreateDirectory(path);
                File.Move(_currentRecording.TempPath, _currentRecording.FinalPath);
            }
        }

        private void OnStopRequested()
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _audioRecorder.Dispose();
            _audioRecorder = null;
        }

        private void OnRecordingProgressEvent(RecordingProgressEventArgs e)
        {
            RecordingProgressEvent?.Invoke(this, e);
        }

        public IEnumerable<RecordingDeviceItem> GetRecordingDeviceList()
        {
            var devices = AudioRecorder.GetRecordingDeviceList();
            return AutoMapper.Mapper.Map<List<RecordingDeviceItem>>(devices);
        }
    }
}
