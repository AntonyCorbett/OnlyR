using System;

namespace OnlyR.Model
{
   /// <summary>
   /// Represents a planned recording, with properties based on existing content of the recordings folder.
   /// </summary>
   public class RecordingCandidate
   {
      public DateTime RecordingDate { get; set; }
      public int TrackNumber { get; set; }
      public string TempPath { get; set; }
      public string FinalPath { get; set; }
   }
}
