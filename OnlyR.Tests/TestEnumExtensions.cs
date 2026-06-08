using OnlyR.Core.Enums;
using OnlyR.Utils;
using System;
using System.Threading.Tasks;

namespace OnlyR.Tests;

public sealed class TestEnumExtensions
{
    [Test]
    public async Task ValidDescriptions()
    {
        foreach (var status in Enum.GetValues<RecordingStatus>())
        {
            if (status == RecordingStatus.Unknown)
            {
                continue;
            }

            var description = status.GetDescriptiveText();
            await Assert.That(description).IsNotNull().And.IsNotEmpty();
        }
    }
}