namespace OnlyR.Tests
{
    using AutoMapper;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Model;

    [TestClass]
    public class TestMappings
    {
        [TestMethod]
        public void TestAutoMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ObjectMappingProfile>();
            });
            
            config.AssertConfigurationIsValid();
        }
    }
}
