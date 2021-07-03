using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlyR.Core.Enums;
using OnlyR.Utils;

namespace OnlyR.Tests
{
    [TestClass]
    public class TestEnumExtensions
    {
        [TestMethod]
#pragma warning disable S2699 // Tests should include assertions
        public void ValidDescriptions()
#pragma warning restore S2699 // Tests should include assertions
        {
            foreach (RecordingStatus status in Enum.GetValues(typeof(RecordingStatus)))
            {
                if (status != RecordingStatus.Unknown)
                {
                    status.GetDescriptiveText();
                }
            }
        }
    }
}
