using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Moq;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;

namespace OnlyR.Tests.Mocks
{
   /// <summary>
   /// A mock audio service
   /// </summary>
   class MockAudioService : IAudioService
   {
      private RecordingStatus _status;
      private readonly DispatcherTimer _timer;
      private readonly Random _random;

      public event EventHandler StartedEvent;
      public event EventHandler StoppedEvent;
      public event EventHandler StopRequested;
      public event EventHandler<RecordingProgressEventArgs> RecordingProgressEvent;


      public MockAudioService()
      {
         _status = RecordingStatus.NotRecording;
         _random = new Random();

         _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
         _timer.Tick += RecordingTimer;
      }

      private void RecordingTimer(object sender, EventArgs e)
      {
         OnRecordingProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = _random.Next(0, 101) });
      }

      public IEnumerable<RecordingDeviceItem> GetRecordingDeviceList()
      {
         return new List<RecordingDeviceItem>
            {
                new RecordingDeviceItem {DeviceName = "Dev1", DeviceId = 1},
                new RecordingDeviceItem {DeviceName = "Dev1", DeviceId = 2}
            };
      }

      public void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService)
      {
         if (_status == RecordingStatus.NotRecording)
         {
            _status = RecordingStatus.Recording;
            OnStartedEvent();
            _timer.Start();
         }
      }

      public void StopRecording(bool fadeOut)
      {
         _status = RecordingStatus.StopRequested;
         OnStopRequested();

         _timer.Stop();
         OnStoppedEvent();
      }

      protected virtual void OnStartedEvent()
      {
         StartedEvent?.Invoke(this, EventArgs.Empty);
      }

      protected virtual void OnStopRequested()
      {
         StopRequested?.Invoke(this, EventArgs.Empty);
      }

      protected virtual void OnStoppedEvent()
      {
         StoppedEvent?.Invoke(this, EventArgs.Empty);
      }

      protected virtual void OnRecordingProgressEvent(RecordingProgressEventArgs e)
      {
         RecordingProgressEvent?.Invoke(this, e);
      }
   }
}
