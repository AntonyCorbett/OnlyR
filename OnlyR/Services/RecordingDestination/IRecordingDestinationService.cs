using OnlyR.Services.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlyR.Model;

namespace OnlyR.Services.RecordingDestination
{
   public interface IRecordingDestinationService
   {
      RecordingCandidate GetRecordingFileCandidate(IOptionsService optionsService, DateTime dt, string commandLineIdentifier);
   }
}
