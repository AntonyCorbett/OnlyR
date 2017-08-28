using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlyR.Services.Options;

namespace OnlyR.Tests
{
    [TestClass]
    public class TestOptions
    {
        private readonly int _badIntValue = -100;

        [TestMethod]
        public void TestSantize()
        {
            Options options = new Options
            {
                ChannelCount = _badIntValue,
                MaxRecordingTimeMins = _badIntValue,
                MaxRecordingsInOneFolder = _badIntValue,
                Mp3BitRate = _badIntValue,
                RecordingDevice = _badIntValue,
                SampleRate = _badIntValue
            };

            options.Sanitize();

            Assert.AreNotEqual(options.ChannelCount, _badIntValue);
            Assert.AreNotEqual(options.MaxRecordingTimeMins, _badIntValue);
            Assert.AreNotEqual(options.MaxRecordingsInOneFolder, _badIntValue);
            Assert.AreNotEqual(options.Mp3BitRate, _badIntValue);
            Assert.AreNotEqual(options.RecordingDevice, _badIntValue);
            Assert.AreNotEqual(options.SampleRate, _badIntValue);
        }

    }
}
