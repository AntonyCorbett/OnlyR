using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlyR.Utils;

namespace OnlyR.Tests
{

    [TestClass]
    public class TestCommandLineParser
    {
        [TestMethod]
        public void TestIdentifier()
        {
            string id = "A Special Id";

            string[] dummyCmdLine =
            {
                "OnlyR.exe",
                $"/id={id}"
            };

            var p = new CommandLineParser(dummyCmdLine);
            Assert.AreEqual(p.Parameters["/id"], id);
        }

        [TestMethod]
        public void TestNoGpuSwitch()
        {
            string[] dummyCmdLine =
            {
                "OnlyR.exe",
                "-nogpu"
            };

            var p = new CommandLineParser(dummyCmdLine);
            Assert.IsTrue(p.IsSwitchSet("-nogpu"));
        }
    }
}
