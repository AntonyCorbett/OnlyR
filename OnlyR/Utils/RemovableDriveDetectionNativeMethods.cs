namespace OnlyR.Utils
{
    // ReSharper disable StyleCop.SA1307
    // ReSharper disable MemberCanBePrivate.Local
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    // ReSharper disable StyleCop.SA1310
    // ReSharper disable InconsistentNaming
    using System;
    using System.Runtime.InteropServices;
    using GalaSoft.MvvmLight.Messaging;
    using ViewModel.Messages;

    internal static class RemovableDriveDetectionNativeMethods
    {
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVALCOMPLETE = 0x8004;
        private const int DBT_DEVTYPVOLUME = 0x00000002;

        public static void WndProc(int msg, IntPtr wparam, IntPtr lparam)
        {
            if (msg == WM_DEVICECHANGE && lparam != IntPtr.Zero)
            {
                DEV_BROADCAST_VOLUME vol = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(lparam, typeof(DEV_BROADCAST_VOLUME));

                if (vol.dbcv_devicetype == DBT_DEVTYPVOLUME)
                {
                    char driveLetter = DriveMaskToLetter(vol.dbcv_unitmask);

                    switch (wparam.ToInt32())
                    {
                        case DBT_DEVICEARRIVAL:
                            Messenger.Default.Send(new RemovableDriveMessage
                            {
                                Added = true,
                                DriveLetter = driveLetter 
                            });
                            break;

                        case DBT_DEVICEREMOVALCOMPLETE:
                            Messenger.Default.Send(new RemovableDriveMessage
                            {
                                Added = false,
                                DriveLetter = driveLetter
                            });
                            break;
                    }
                }
            }
        }

        private static char DriveMaskToLetter(int mask)
        {
            const string Drives = @"ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var cnt = 0;
            var pom = mask / 2;
            while (pom != 0)
            {
                pom = pom / 2;
                ++cnt;
            }

            return cnt < Drives.Length ? Drives[cnt] : '?';
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size;
            public int dbcv_devicetype;
            public int dbcv_reserved;
            public int dbcv_unitmask;
        }
    }
}
