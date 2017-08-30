using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlyR.Core.Recorder
{
   /// <summary>
   /// To indicate there are no audio recording devices
   /// </summary>
   [Serializable]
   public class NoDevicesException : Exception
   {
      public NoDevicesException()
          : base("No recording devices found")
      {

      }
   }
}
