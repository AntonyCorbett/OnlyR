using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlyR.Model;

namespace OnlyR.Tests
{
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
