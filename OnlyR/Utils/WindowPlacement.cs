using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Serialization;

namespace OnlyR.Utils
{
   // adapted from david Rickard's Tech Blog

   // RECT structure required by WINDOWPLACEMENT structure
   [Serializable]
   [StructLayout(LayoutKind.Sequential)]
   public struct RECT
   {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;

      public RECT(int left, int top, int right, int bottom)
      {
         Left = left;
         Top = top;
         Right = right;
         Bottom = bottom;
      }
   }

   // POINT structure required by WINDOWPLACEMENT structure
   [Serializable]
   [StructLayout(LayoutKind.Sequential)]
   public struct POINT
   {
      public int X;
      public int Y;

      public POINT(int x, int y)
      {
         X = x;
         Y = y;
      }
   }

   // WINDOWPLACEMENT stores the position, size, and state of a window
   [Serializable]
   [StructLayout(LayoutKind.Sequential)]
   public struct WINDOWPLACEMENT
   {
      public int length;
      public int flags;
      public int showCmd;
      public POINT minPosition;
      public POINT maxPosition;
      public RECT normalPosition;
   }

   public static class WindowPlacement
   {
      private static readonly Encoding encoding = new UTF8Encoding();
      private static readonly XmlSerializer serializer = new XmlSerializer(typeof(WINDOWPLACEMENT));
      
      private const int SW_SHOWNORMAL = 1;
      private const int SW_SHOWMINIMIZED = 2;

      private static void SetPlacement(IntPtr windowHandle, string placementXml)
      {
         if (!string.IsNullOrEmpty(placementXml))
         {
            byte[] xmlBytes = encoding.GetBytes(placementXml);
            try
            {
               WINDOWPLACEMENT placement;
               using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
               {
                  placement = (WINDOWPLACEMENT) serializer.Deserialize(memoryStream);
               }

               placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
               placement.flags = 0;
               placement.showCmd = (placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd);
               NativeMethods.SetWindowPlacement(windowHandle, ref placement);
            }
            catch (InvalidOperationException)
            {
               // Parsing placement XML failed. Fail silently.
            }
         }
      }

      private static string GetPlacement(IntPtr windowHandle)
      {
         NativeMethods.GetWindowPlacement(windowHandle, out var placement);

         using (MemoryStream memoryStream = new MemoryStream())
         {
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
            serializer.Serialize(xmlTextWriter, placement);
            byte[] xmlBytes = memoryStream.ToArray();
            return encoding.GetString(xmlBytes);
         }
      }

      public static void SetPlacement(this Window window, string placementXml)
      {
         SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
      }

      public static string GetPlacement(this Window window)
      {
         return GetPlacement(new WindowInteropHelper(window).Handle);
      }
   }

}
