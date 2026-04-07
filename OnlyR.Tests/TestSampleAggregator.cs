using System.Collections.Generic;
using System.Threading.Tasks;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Samples;

namespace OnlyR.Tests;

public sealed class TestSampleAggregator
{
    [Test]
    public async Task ConstructorClampsLowInterval()
    {
        // reportingIntervalMs=5 is below 20, so clamped to 20.
        // reportCount = 1000 * 20 / 1000 = 20.
        var aggregator = new SampleAggregator(1000, 5);
        var fired = 0;

        aggregator.ReportEvent += (_, _) => fired++;

        for (var i = 0; i < 20; i++)
        {
            aggregator.Add(0.5f);
        }

        await Assert.That(fired).IsEqualTo(1);
    }

    [Test]
    public async Task ConstructorCalculatesReportCount()
    {
        // reportCount = 44100 * 40 / 1000 = 1764.
        var aggregator = new SampleAggregator(44100, 40);
        var fired = 0;

        aggregator.ReportEvent += (_, _) => fired++;

        for (var i = 0; i < 1764; i++)
        {
            aggregator.Add(0.1f);
        }

        await Assert.That(fired).IsEqualTo(1);
    }

    [Test]
    public async Task AddTracksMaxValue()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);
        SamplesReportEventArgs? report = null;

        aggregator.ReportEvent += (_, e) => report = e;

        var values = new[] { 0.1f, 0.5f, 0.9f, 0.3f, 0.2f, 0.7f, 0.4f, 0.6f, 0.8f, 0.05f };

        foreach (var value in values)
        {
            aggregator.Add(value);
        }

        await Assert.That(report).IsNotNull();
        await Assert.That(report!.MaxSample).IsEqualTo(0.9f);
    }

    [Test]
    public async Task AddTracksMinValue()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);
        SamplesReportEventArgs? report = null;

        aggregator.ReportEvent += (_, e) => report = e;

        var values = new[] { -0.1f, -0.5f, -0.9f, -0.3f, -0.2f, -0.7f, -0.4f, -0.6f, -0.8f, -0.05f };

        foreach (var value in values)
        {
            aggregator.Add(value);
        }

        await Assert.That(report).IsNotNull();
        await Assert.That(report!.MinSample).IsEqualTo(-0.9f);
    }

    [Test]
    public async Task ReportEventFiresAtThreshold()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);
        var fired = 0;

        aggregator.ReportEvent += (_, _) => fired++;

        for (var i = 0; i < 10; i++)
        {
            aggregator.Add(0.1f);
        }

        await Assert.That(fired).IsEqualTo(1);
    }

    [Test]
    public async Task ReportEventResetsState()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);
        var reports = new List<SamplesReportEventArgs>();

        aggregator.ReportEvent += (_, e) => reports.Add(e);

        // First cycle: values with max 0.9, min -0.5.
        for (var i = 0; i < 10; i++)
        {
            aggregator.Add(i < 5 ? 0.9f : -0.5f);
        }

        // Second cycle: values with max 0.2, min -0.1.
        for (var i = 0; i < 10; i++)
        {
            aggregator.Add(i < 5 ? 0.2f : -0.1f);
        }

        await Assert.That(reports.Count).IsEqualTo(2);
        await Assert.That(reports[1].MaxSample).IsEqualTo(0.2f);
        await Assert.That(reports[1].MinSample).IsEqualTo(-0.1f);
    }

    [Test]
    public async Task NoEventBeforeThreshold()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);
        var fired = 0;

        aggregator.ReportEvent += (_, _) => fired++;

        for (var i = 0; i < 9; i++)
        {
            aggregator.Add(0.1f);
        }

        await Assert.That(fired).IsEqualTo(0);
    }

    [Test]
    public async Task ConstructorClampsSmallReportCount()
    {
        // reportCount = 100 * 50 / 1000 = 5, clamped to 10.
        var aggregator = new SampleAggregator(100, 50);
        var fired = 0;

        aggregator.ReportEvent += (_, _) => fired++;

        for (var i = 0; i < 10; i++)
        {
            aggregator.Add(0.1f);
        }

        await Assert.That(fired).IsEqualTo(1);
    }

    [Test]
    public void NoExceptionWithoutSubscriber()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);

        // Add enough samples to exceed threshold with no event handler — should not throw.
        for (var i = 0; i < 10; i++)
        {
            aggregator.Add(0.5f);
        }
    }

    [Test]
    public async Task MultipleReportCycles()
    {
        // reportCount = 100 * 100 / 1000 = 10.
        var aggregator = new SampleAggregator(100, 100);
        var reports = new List<SamplesReportEventArgs>();

        aggregator.ReportEvent += (_, e) => reports.Add(e);

        // First cycle: all positive, max should be 0.8.
        var firstCycleValues = new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.05f, 0.15f };

        foreach (var value in firstCycleValues)
        {
            aggregator.Add(value);
        }

        // Second cycle: all negative, min should be -0.9.
        var secondCycleValues = new[] { -0.1f, -0.2f, -0.3f, -0.4f, -0.5f, -0.6f, -0.7f, -0.8f, -0.9f, -0.05f };

        foreach (var value in secondCycleValues)
        {
            aggregator.Add(value);
        }

        await Assert.That(reports.Count).IsEqualTo(2);

        await Assert.That(reports[0].MaxSample).IsEqualTo(0.8f);
        await Assert.That(reports[0].MinSample).IsEqualTo(0.0f);

        await Assert.That(reports[1].MaxSample).IsEqualTo(0.0f);
        await Assert.That(reports[1].MinSample).IsEqualTo(-0.9f);
    }
}