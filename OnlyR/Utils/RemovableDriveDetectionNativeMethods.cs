using System;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using OnlyR.ViewModel.Messages;

// Ensure this is built x86!

namespace OnlyR.Utils
{
#pragma warning disable S101 // Types should be named in PascalCase
    // ReSharper disable StyleCop.SA1307
    // ReSharper disable MemberCanBePrivate.Local
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    // ReSharper disable StyleCop.SA1310
    // ReSharper disable InconsistentNaming

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
                var vol = (DEV_BROADCAST_VOLUME?)Marshal.PtrToStructure(lparam, typeof(DEV_BROADCAST_VOLUME));

                if (vol != null && vol.Value.dbcv_devicetype == DBT_DEVTYPVOLUME)
                {
                    var driveLetter = DriveMaskToLetter(vol.Value.dbcv_unitmask);

                    switch ((int)wparam)
                    {
                        case DBT_DEVICEARRIVAL:
                            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage
                            {
                                Added = true,
                                DriveLetter = driveLetter,
                            });
                            break;

                        case DBT_DEVICEREMOVALCOMPLETE:
                            WeakReferenceMessenger.Default.Send(new RemovableDriveMessage
                            {
                                Added = false,
                                DriveLetter = driveLetter,
                            });
                            break;
                    }
                }
            }
        }

        private static char DriveMaskToLetter(int mask)
        {
            const string Drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var cnt = 0;
            var pom = mask / 2;
            while (pom != 0)
            {
                pom /= 2;
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
#pragma warning restore S101 // Types should be named in PascalCase
}
