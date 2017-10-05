using System;
using OnlyR.Core.EventArgs;

namespace OnlyR.Core.Samples
{
    /// <summary>
    /// Aggregator of audio samples. Based on Mark Heath's work in NAudio sample applications
    /// </summary>
    public sealed class SampleAggregator
    {
        private float _maxValue;
        private float _minValue;
        private int _count;
        private readonly int _reportCount;
        private readonly int _minReportingIntervalMs = 20;

        public event EventHandler<SamplesReportEventArgs> ReportEvent;

        public SampleAggregator(int samplesPerSecond, int reportingIntervalMs)
        {
            if (reportingIntervalMs < 20)
            {
                reportingIntervalMs = _minReportingIntervalMs;
            }

            _reportCount = samplesPerSecond * reportingIntervalMs / 1000;

            if (_reportCount < 10)
            {
                _reportCount = 10;
            }
        }

        private void Reset()
        {
            _count = 0;
            _maxValue = _minValue = 0.0F;
        }

        public void Add(float value)
        {
            ++_count;

            _maxValue = Math.Max(_maxValue, value);
            _minValue = Math.Min(_minValue, value);

            if (_count >= _reportCount)
            {
                OnReportEvent(new SamplesReportEventArgs(_minValue, _maxValue));
                Reset();
            }
        }

        private void OnReportEvent(SamplesReportEventArgs e)
        {
            ReportEvent?.Invoke(this, e);
        }
    }
}
