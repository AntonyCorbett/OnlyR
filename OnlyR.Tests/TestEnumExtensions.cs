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
