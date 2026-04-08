using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OnlyR.Model;
using OnlyR.Services.Options;

namespace OnlyR.Tests
{
    [TestClass]
    public class TestOptions
    {
        private readonly int _badIntValue = -100;

        [TestMethod]
        public void TestSanitize()
        {
            var options = new Options
            {
                ChannelCount = _badIntValue,
                MaxRecordingTimeSeconds = _badIntValue,
                MaxRecordingsInOneFolder = _badIntValue,
                Mp3BitRate = _badIntValue,
                RecordingDevice = _badIntValue,
                SampleRate = _badIntValue,
            };

            options.Sanitize();

            Assert.AreNotEqual(options.ChannelCount, _badIntValue);
            Assert.AreNotEqual(options.MaxRecordingTimeSeconds, _badIntValue);
            Assert.AreNotEqual(options.MaxRecordingsInOneFolder, _badIntValue);
            Assert.AreNotEqual(options.Mp3BitRate, _badIntValue);
            Assert.AreNotEqual(options.RecordingDevice, _badIntValue);
            Assert.AreNotEqual(options.SampleRate, _badIntValue);
        }

        [TestMethod]
        public void TestLegacyDarkModeFalseMigratesToSystem()
        {
            const string json = """{ "SampleRate": 44100, "AlwaysOnTop": true }""";

            var options = JsonConvert.DeserializeObject<Options>(json);

            Assert.IsNotNull(options);
            Assert.IsNull(options.AppTheme);

            options.Sanitize();

            Assert.AreEqual(AppTheme.System, options.AppTheme);
        }

        [TestMethod]
        public void TestLegacyDarkModeTrueMigratesToDark()
        {
            const string json = """{ "DarkMode": true, "SampleRate": 44100 }""";

            var options = JsonConvert.DeserializeObject<Options>(json);

            Assert.IsNotNull(options);
            Assert.IsNull(options.AppTheme);

            options.Sanitize();

            Assert.AreEqual(AppTheme.Dark, options.AppTheme);
        }

        [TestMethod]
        public void TestExplicitAppThemePreserved()
        {
            const string json = """{ "DarkMode": false, "AppTheme": 0 }""";

            var options = JsonConvert.DeserializeObject<Options>(json);

            Assert.IsNotNull(options);
            Assert.AreEqual(AppTheme.Light, options.AppTheme);

            options.Sanitize();

            Assert.AreEqual(AppTheme.Light, options.AppTheme);
        }

        [TestMethod]
        public void TestAppThemeRoundTrips()
        {
            var original = new Options { AppTheme = AppTheme.Dark };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<Options>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(AppTheme.Dark, restored.AppTheme);
        }

        [TestMethod]
        public void TestInvalidAppThemeSanitizedToSystem()
        {
            var options = new Options { AppTheme = (AppTheme)99 };

            options.Sanitize();

            Assert.AreEqual(AppTheme.System, options.AppTheme);
        }
    }
}
