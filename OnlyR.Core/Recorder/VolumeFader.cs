using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlyR.Core.Recorder
{
    internal sealed class VolumeFader
    {
        public event EventHandler FadeComplete;
        private readonly int _sampleRate;

        private readonly int _fadeTimeSecs = 4;
        private bool _active;
        private int _sampleCountToModify;
        private int _sampleCountModified;

        public bool Active => _active;

        public VolumeFader(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public void Start()
        {
            _sampleCountToModify = _fadeTimeSecs * _sampleRate; // num samples to modify in order to fade over _fadeTimeSecs
            _sampleCountModified = 0;
            _active = true;
        }

        private void OnFadeComplete()
        {
            FadeComplete?.Invoke(this, System.EventArgs.Empty);
            _active = false;
        }

        public void FadeBuffer(byte[] buffer, int bytesRecorded)
        {
            _sampleCountModified += bytesRecorded;
            float volumeAdjustmentFraction = 1 - (float)_sampleCountModified / _sampleCountToModify;
            
            for (int index = 0; index < bytesRecorded; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) | buffer[index + 0]);
                
                short modifiedSample = (short) (sample * volumeAdjustmentFraction);
                buffer[index + 1] = (byte)(modifiedSample >> 8);
                buffer[index + 0] = (byte)(modifiedSample & 0xFF);
            }

            if(volumeAdjustmentFraction <= 0)
            {
                OnFadeComplete();
            }
        }
    }
}
