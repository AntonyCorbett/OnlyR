namespace OnlyR.Services.RecordingCopies
{
    // ReSharper disable InconsistentNaming
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;

    internal static class CopyRecordingsServiceNativeMethods
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;

        public static void EjectDrive(char driveLetter)
        {
            string path = @"\\.\" + driveLetter + @":";

            IntPtr handle = CreateFile(
                path, 
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, 
                IntPtr.Zero, 
                0x3, 
                0, 
                IntPtr.Zero);

            if ((long)handle == -1)
            {
                MessageBox.Show("Unable to open drive " + driveLetter);
                return;
            }

            int dummy = 0;

            DeviceIoControl(
                handle, 
                IOCTL_STORAGE_EJECT_MEDIA, 
                IntPtr.Zero, 
                0,
                IntPtr.Zero, 
                0, 
                ref dummy, 
                IntPtr.Zero);

            CloseHandle(handle);
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string filename, 
            uint desiredAccess,
            uint shareMode, 
            IntPtr securityAttributes,
            int creationDisposition, 
            int flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32")]
        private static extern int DeviceIoControl(
            IntPtr deviceHandle, 
            uint ioControlCode,
            IntPtr inBuffer, 
            int inBufferSize,
            IntPtr outBuffer, 
            int outBufferSize,
            ref int bytesReturned, 
            IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
