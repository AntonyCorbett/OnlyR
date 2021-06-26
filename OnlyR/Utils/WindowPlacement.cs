namespace OnlyR.Utils
{
#pragma warning disable S101 // Types should be named in PascalCase

    // ReSharper disable InconsistentNaming
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable StyleCop.SA1307
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Interop;
    using System.Xml;
    using System.Xml.Serialization;

    //// adapted from david Rickard's Tech Blog

    public static class WindowPlacement
    {
        private const int SwShowNormal = 1;
        private const int SwShowMinimized = 2;

        private static readonly Encoding Encoding = new UTF8Encoding();
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(WINDOWPLACEMENT));

        public static void SetPlacement(this Window window, string placementJson, bool showMinimized)
        {
            SetPlacement(
                new WindowInteropHelper(window).Handle, 
                placementJson, 
                window.Width, 
                window.Height, 
                showMinimized);
        }

        public static string GetPlacement(this Window window)
        {
            return GetPlacement(new WindowInteropHelper(window).Handle);
        }

        private static void SetPlacement(
            IntPtr windowHandle, 
            string placementJson, 
            double width, 
            double height, 
            bool showMinimized)
        {
            if (!string.IsNullOrEmpty(placementJson))
            {
                byte[] xmlBytes = Encoding.GetBytes(placementJson);
                try
                {
                    WINDOWPLACEMENT placement;
                    using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                    {
                        placement = (WINDOWPLACEMENT)Serializer.Deserialize(memoryStream);
                    }

                    var adjustedDimensions = GetAdjustedWidthAndHeight(width, height);

                    if (showMinimized)
                    {
                        placement.showCmd = SwShowMinimized;
                    }
                    else if (placement.showCmd == SwShowMinimized)
                    {
                        placement.showCmd = SwShowNormal;
                    }

                    placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                    placement.flags = 0;
                    placement.normalPosition.Right = placement.normalPosition.Left + (int)adjustedDimensions.Item1;
                    placement.normalPosition.Bottom = placement.normalPosition.Top + (int)adjustedDimensions.Item2;
                    NativeMethods.SetWindowPlacement(windowHandle, ref placement);
                }
                catch (InvalidOperationException)
                {
                    // Parsing placement XML failed. Fail silently.
                }
            }
        }

        private static Tuple<double, double> GetAdjustedWidthAndHeight(double width, double height)
        {
            var dpi = GetDpiSettings();

            var adjustedWidth = (width * dpi.Item1) / 96.0;
            var adjustedHeight = (height * dpi.Item2) / 96.0;

            return new Tuple<double, double>(adjustedWidth, adjustedHeight);
        }

        private static string GetPlacement(IntPtr windowHandle)
        {
            NativeMethods.GetWindowPlacement(windowHandle, out var placement);

            using (var memoryStream = new MemoryStream())
            {
                var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                Serializer.Serialize(xmlTextWriter, placement);
                var xmlBytes = memoryStream.ToArray();
                return Encoding.GetString(xmlBytes);
            }
        }

        private static Tuple<int, int> GetDpiSettings()
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            return new Tuple<int, int>(
                (int)(dpiXProperty?.GetValue(null, null) ?? 0),
                (int)(dpiYProperty?.GetValue(null, null) ?? 0));
        }
    }
    
    // RECT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
#pragma warning disable U2U1004 // Public value types should implement equality
    public struct RECT
#pragma warning restore U2U1004 // Public value types should implement equality
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        // ReSharper disable once UnusedMember.Global
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
#pragma warning disable U2U1004 // Public value types should implement equality
    public struct POINT
#pragma warning restore U2U1004 // Public value types should implement equality
    {
        public int X;
        public int Y;

        // ReSharper disable once UnusedMember.Global
        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    //// WINDOWPLACEMENT stores the position, size, and state of a window
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
#pragma warning disable U2U1004 // Public value types should implement equality
    public struct WINDOWPLACEMENT
#pragma warning restore U2U1004 // Public value types should implement equality
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;
    }

#pragma warning restore S101 // Types should be named in PascalCase
}
