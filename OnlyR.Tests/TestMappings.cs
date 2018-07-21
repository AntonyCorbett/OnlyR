namespace OnlyR.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Model;

    [TestClass]
    public class TestMappings
    {
        [TestMethod]
        public void TestAutoMapper()
        {
            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<ObjectMappingProfile>();
            });

            AutoMapper.Mapper.AssertConfigurationIsValid();
        }
    }
}
