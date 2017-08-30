using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Lame;
using NAudio.Wave;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Samples;

namespace OnlyR.Core.Recorder
{
   /// <summary>
   /// The audio recorder. Uses NAudio for the heavy lifting, but it's isolated in this class
   /// so if we need to replace NAudio with another library we just need to modify this part
   /// of the application
   /// </summary>
   public sealed class AudioRecorder : IDisposable
   {
      private LameMP3FileWriter _mp3Writer;
      private WaveIn _waveSource;
      private SampleAggregator _sampleAggregator;
      private VolumeFader _fader;

      private int _dampedLevel;

      // use these 2 together. Experiment to get the best VU display...
      private readonly int _requiredReportingIntervalMs = 40;
      private readonly int _vuSpeed = 5;


      public event EventHandler<RecordingProgressEventArgs> ProgressEvent;

      public AudioRecorder()
      {
         _recordingStatus = RecordingStatus.NotRecording;
      }

      private void FadeCompleteHandler(object sender, System.EventArgs e)
      {
         _waveSource.StopRecording();
      }

      private RecordingStatus _recordingStatus;
      public event EventHandler<RecordingStatusChangeEventArgs> RecordingStatusChangeEvent;

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_mp3Writer")]
      public void Dispose()
      {
         Cleanup();
      }

      private void Cleanup()
      {
         _waveSource?.Dispose();
         _waveSource = null;

         _mp3Writer?.Dispose();
         _mp3Writer = null;
      }

      private static ID3TagData CreateTag(RecordingConfig recordingConfig)
      {
         // tag is embeded as MP3 metadata

         return new ID3TagData
         {
            Title = recordingConfig.TrackTitle,
            Album = recordingConfig.AlbumName,
            Track = recordingConfig.TrackNumber.ToString(),
            Genre = recordingConfig.Genre,
            Year = recordingConfig.RecordingDate.Year.ToString(),
            UserDefinedTags = new string[] { }     // fix bug in naudio.lame
         };
      }

      /// <summary>
      /// Starts recording
      /// </summary>
      /// <param name="recordingConfig">Recording configuration</param>
      public void Start(RecordingConfig recordingConfig)
      {
         if (_recordingStatus == RecordingStatus.NotRecording)
         {
            InitAggregator(recordingConfig.SampleRate);
            CheckRecordingDevice(recordingConfig);
            InitFader(recordingConfig.SampleRate);

            _waveSource = new WaveIn
            {
               WaveFormat = new WaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount),
               DeviceNumber = recordingConfig.RecordingDevice
            };

            _waveSource.DataAvailable += WaveSourceDataAvailableHandler;
            _waveSource.RecordingStopped += WaveSourceRecordingStoppedHandler;

            _mp3Writer = new LameMP3FileWriter(recordingConfig.DestFilePath, _waveSource.WaveFormat,
                recordingConfig.Mp3BitRate, CreateTag(recordingConfig));

            _waveSource.StartRecording();
            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Recording));
         }
      }

      private void InitFader(int sampleRate)
      {
         // used to optionally fade out a recording
         _fader = new VolumeFader(sampleRate);
         _fader.FadeComplete += FadeCompleteHandler;
      }

      private static void CheckRecordingDevice(RecordingConfig recordingConfig)
      {
         int deviceCount = WaveIn.DeviceCount;
         if (deviceCount == 0)
         {
            throw new NoDevicesException();
         }

         if (recordingConfig.RecordingDevice >= deviceCount)
         {
            recordingConfig.RecordingDevice = 0;
         }
      }

      private void InitAggregator(int sampleRate)
      {
         // the aggregator collects audio sample metrics 
         // and publishes the results at suitable intervals.
         // Used by the OnlyR volume meter
         if (_sampleAggregator != null)
         {
            _sampleAggregator.ReportEvent -= AggregatorReportHandler;
         }

         _sampleAggregator = new SampleAggregator(sampleRate, _requiredReportingIntervalMs);
         _sampleAggregator.ReportEvent += AggregatorReportHandler;
      }

      private void AggregatorReportHandler(object sender, SamplesReportEventArgs e)
      {
         float value = Math.Max(e.MaxSample, Math.Abs(e.MinSample)) * 100;
         OnProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = GetDampedVolumeLevel(value) });
      }

      private void WaveSourceRecordingStoppedHandler(object sender, StoppedEventArgs e)
      {
         Cleanup();
         OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.NotRecording));
         _fader = null;
      }

      private void WaveSourceDataAvailableHandler(object sender, WaveInEventArgs waveInEventArgs)
      {
         // as audio samples are provided by WaveIn, we hook in here 
         // and write them to disk, encoding to MP3 on the fly 
         // using the _mp3Writer...

         byte[] buffer = waveInEventArgs.Buffer;
         int bytesRecorded = waveInEventArgs.BytesRecorded;

         if (_fader != null && _fader.Active)
         {
            // we're fading out...
            _fader.FadeBuffer(buffer, bytesRecorded);
         }

         for (int index = 0; index < bytesRecorded; index += 2)
         {
            short sample = (short)((buffer[index + 1] << 8) | buffer[index + 0]);
            float sample32 = sample / 32768F;
            _sampleAggregator.Add(sample32);
         }

         _mp3Writer.Write(buffer, 0, bytesRecorded);
      }

      /// <summary>
      /// Stop recording
      /// </summary>
      /// <param name="fadeOut">true - fade out the recording instead of stopping immediately</param>
      public void Stop(bool fadeOut)
      {
         if (_recordingStatus == RecordingStatus.Recording)
         {
            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.StopRequested));

            if (fadeOut)
            {
               _fader.Start();
            }
            else
            {
               _waveSource.StopRecording();
            }
         }
      }

      private void OnRecordingStatusChangeEvent(RecordingStatusChangeEventArgs e)
      {
         _recordingStatus = e.RecordingStatus;
         RecordingStatusChangeEvent?.Invoke(this, e);
      }

      private void OnProgressEvent(RecordingProgressEventArgs e)
      {
         ProgressEvent?.Invoke(this, e);
      }

      private int GetDampedVolumeLevel(float volLevel)
      {
         // provide some "damping" of the volume meter

         if (volLevel > _dampedLevel)
         {
            _dampedLevel = (int)(volLevel + _vuSpeed);
         }

         _dampedLevel -= _vuSpeed;
         if (_dampedLevel < 0)
         {
            _dampedLevel = 0;
         }

         return _dampedLevel;
      }

      /// <summary>
      /// Gets a list of Windows recording devices
      /// </summary>
      /// <returns>Collection of devices</returns>
      public static IEnumerable<RecordingDeviceInfo> GetRecordingDeviceList()
      {
         List<RecordingDeviceInfo> result = new List<RecordingDeviceInfo>();

         int count = WaveIn.DeviceCount;
         for (int n = 0; n < count; ++n)
         {
            var caps = WaveIn.GetCapabilities(n);
            result.Add(new RecordingDeviceInfo { Id = n, Name = caps.ProductName });
         }

         return result;
      }
   }
}
