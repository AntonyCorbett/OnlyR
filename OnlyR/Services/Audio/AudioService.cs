using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Recorder;
using OnlyR.Model;
using OnlyR.Services.Options;
using Serilog;

namespace OnlyR.Services.Audio
{
    /// <summary>
    /// Interface to the audio recording functions
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class AudioService : IAudioService, IDisposable
    {
        private readonly AudioRecorder _audioRecorder;
        private RecordingCandidate? _currentRecording;

        public AudioService()
        {
            _audioRecorder = new AudioRecorder();
            _audioRecorder.RecordingStatusChangeEvent += AudioRecorderOnRecordingStatusChangeHandler;
            _audioRecorder.ProgressEvent += AudioRecorderOnProgressHandler;
        }

        public event EventHandler? StartedEvent;

        public event EventHandler? StoppedEvent;

        public event EventHandler? StopRequested;

        public event EventHandler<RecordingProgressEventArgs>? RecordingProgressEvent;

        public void Dispose()
        {
            _audioRecorder?.Dispose();
        }

        /// <summary>
        /// Gets a list of Windows audio recording devices
        /// </summary>
        /// <returns>List of available devices</returns>
        public RecordingDeviceItem[] GetRecordingDeviceList()
        {
            var devices = AudioRecorder.GetRecordingDeviceList();
            return devices.Select(Convert).ToArray();
        }


        private RecordingDeviceItem Convert(RecordingDeviceInfo deviceInfo)
        {
            return new(deviceInfo.Id, deviceInfo.Name);
        }

        /// <summary>
        /// Starts recording
        /// </summary>
        /// <param name="candidateFile">The candidate recording file</param>
        /// <param name="optionsService">The options service</param>
        public void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService)
        {
            _currentRecording = candidateFile;

            var recordingConfig = new RecordingConfig
            {
                RecordingDevice = optionsService.Options.RecordingDevice,
                UseLoopbackCapture = optionsService.Options.UseLoopbackCapture,
                RecordingDate = candidateFile.RecordingDate,
                TrackNumber = candidateFile.TrackNumber,
                DestFilePath = candidateFile.TempPath,
                SampleRate = optionsService.Options.SampleRate,
                ChannelCount = optionsService.Options.ChannelCount,
                Mp3BitRate = optionsService.Options.Mp3BitRate,
                TrackTitle = GetTrackTitle(candidateFile),
                AlbumName = GetAlbumName(candidateFile),
                Genre = optionsService.Options.Genre,
            };

            _audioRecorder.Start(recordingConfig);
        }

        public void StopRecording(bool fadeOut)
        {
            _audioRecorder.Stop(fadeOut);
        }

        private static string GetAlbumName(RecordingCandidate candidate)
        {
            return candidate.RecordingDate.ToString("MMM yyyy");
        }

        private static string GetTrackTitle(RecordingCandidate candidate)
        {
            return Path.GetFileNameWithoutExtension(candidate.FinalPath);
        }

        private void AudioRecorderOnProgressHandler(object? sender, RecordingProgressEventArgs e)
        {
            OnRecordingProgressEvent(e);
        }

        private void AudioRecorderOnRecordingStatusChangeHandler(object? sender, RecordingStatusChangeEventArgs recordingStatusChangeEventArgs)
        {
            switch (recordingStatusChangeEventArgs.RecordingStatus)
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
            if (_currentRecording == null)
            {
                return;
            }

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

        private void OnRecordingProgressEvent(RecordingProgressEventArgs e)
        {
            RecordingProgressEvent?.Invoke(this, e);
        }
    }
}
