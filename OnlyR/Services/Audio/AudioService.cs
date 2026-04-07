using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Recorder;
using OnlyR.Model;
using OnlyR.Services.Options;
using OnlyR.Utils;

namespace OnlyR.Services.Audio
{
    /// <summary>
    /// Interface to the audio recording functions
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    [ExcludeFromCodeCoverage]
    public sealed class AudioService : IAudioService, IDisposable
    {
        private readonly AudioRecorder _audioRecorder;
        private readonly IOptionsService _optionsService;
        private RecordingCandidate? _currentRecording;
        private bool _hasStarted;

        public AudioService(IOptionsService optionsService)
        {
            _optionsService = optionsService;
            _audioRecorder = new AudioRecorder();
            _audioRecorder.RecordingStatusChangeEvent += AudioRecorderOnRecordingStatusChangeHandler;
            _audioRecorder.ProgressEvent += AudioRecorderOnProgressHandler;
        }

        public event EventHandler? StartedEvent;

        public event EventHandler? StoppedEvent;

        public event EventHandler? StopRequested;

        public event EventHandler<RecordingProgressEventArgs>? RecordingProgressEvent;

        public event EventHandler? PausedEvent;

        public event EventHandler? ResumedEvent;

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
                FinalFilePath = candidateFile.FinalPath,
                SampleRate = optionsService.Options.SampleRate,
                ChannelCount = optionsService.Options.ChannelCount,
                Mp3BitRate = optionsService.Options.Mp3BitRate,
                Codec = optionsService.Options.Codec,
                TrackTitle = GetTrackTitle(candidateFile),
                AlbumName = GetAlbumName(candidateFile),
                Genre = optionsService.Options.Genre,
            };

            _audioRecorder.Start(recordingConfig);
        }

        /// <summary>
        /// Stops recording
        /// </summary>
        /// <param name="fadeOut">true - fade out the recording instead of stopping immediately.</param>
        public void StopRecording(bool fadeOut)
        {
            _audioRecorder.Stop(fadeOut);
        }

        /// <summary>
        /// Pauses the current recording.
        /// </summary>
        public void PauseRecording()
        {
            _audioRecorder.Pause();
        }

        /// <summary>
        /// Resumes a paused recording.
        /// </summary>
        public void ResumeRecording()
        {
            _audioRecorder.Resume();
        }

        private static string GetAlbumName(RecordingCandidate candidate)
        {
            return candidate.RecordingDate.ToString("MMM yyyy", CultureInfo.CurrentCulture);
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
                    _hasStarted = false;
                    ClearPathOfUnfinishedRecording();
                    OnStoppedEvent();
                    break;

                case RecordingStatus.Recording:
                    if (!_hasStarted)
                    {
                        _hasStarted = true;
                        StorePathOfUnfinishedRecording(recordingStatusChangeEventArgs);
                        OnStartedEvent();
                    }
                    else
                    {
                        OnResumedEvent();
                    }

                    break;

                case RecordingStatus.Paused:
                    OnPausedEvent();
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

        private void StorePathOfUnfinishedRecording(RecordingStatusChangeEventArgs args)
        {
            _optionsService.Options.UnfinishedRecordingTempPath = args.TempRecordingPath;
            _optionsService.Options.UnfinishedRecordingFinalPath = args.FinalRecordingPath;
            _optionsService.Save();
        }

        private void ClearPathOfUnfinishedRecording()
        {
            _optionsService.Options.UnfinishedRecordingTempPath = null;
            _optionsService.Options.UnfinishedRecordingFinalPath = null;
            _optionsService.Save();
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

            FileUtils.MoveFile(_currentRecording.TempPath, _currentRecording.FinalPath);
        }

        private void OnPausedEvent()
        {
            PausedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnResumedEvent()
        {
            ResumedEvent?.Invoke(this, EventArgs.Empty);
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
