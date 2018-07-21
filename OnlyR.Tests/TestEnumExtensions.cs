namespace OnlyR.Tests
{
    using System;
    using Core.Enums;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Utils;

    [TestClass]
    public class TestEnumExtensions
    {
        [TestMethod]
        public void ValidDescriptions()
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
