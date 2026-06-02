using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Ensure this is built x86!

namespace OnlyR.Services.RecordingCopies;
#pragma warning disable IDE0051 // unused member

// adapted from work by Armanisoft from here:
// https://www.codeproject.com/Articles/375916/How-to-Prepare-a-USB-Drive-for-Safe-Removal-2
[ExcludeFromCodeCoverage]
internal static class DriveEjectionServiceNativeMethods
{
    // from setupapi.h
    private const int DIGCF_PRESENT = 0x00000002;
    private const int DIGCF_DEVICEINTERFACE = 0x00000010;

    private const string GUID_DEVINTERFACE_VOLUME = "53f5630d-b6bf-11d0-94f2-00a0c91efb8b";
    private const string GUID_DEVINTERFACE_DISK = "53f56307-b6bf-11d0-94f2-00a0c91efb8b";
    private const string GUID_DEVINTERFACE_FLOPPY = "53f56311-b6bf-11d0-94f2-00a0c91efb8b";
    private const string GUID_DEVINTERFACE_CDROM = "53f56308-b6bf-11d0-94f2-00a0c91efb8b";

    private const int INVALID_HANDLE_VALUE = -1;
    // 0x80000000 overflows int, so the unchecked cast is required here.
    private const int GENERIC_READ = unchecked((int)0x80000000);
    private const int GENERIC_WRITE = 0x40000000;
    private const int FILE_SHARE_READ = 0x00000001;
    private const int FILE_SHARE_WRITE = 0x00000002;
    private const int OPEN_EXISTING = 3;
    private const int FSCTL_LOCK_VOLUME = 0x00090018;
    private const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
    private const int IOCTL_STORAGE_EJECT_MEDIA = 0x002D4808;
    private const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;
    private const int IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;

    private const int ERROR_NO_MORE_ITEMS = 259;
    private const int ERROR_INSUFFICIENT_BUFFER = 122;
    private const int ERROR_INVALID_DATA = 13;

    private enum DriveType : uint
    {
        /// <summary>The drive type cannot be determined.</summary>
        DriveUnknown = 0,

        /// <summary>The root path is invalid, for example, no volume is mounted at the path.</summary>
        DriveNoRootDir = 1,

        /// <summary>The drive is a type that has removable media, for example, a floppy drive or removable hard disk.</summary>
        DriveRemovable = 2,

        /// <summary>The drive is a type that cannot be removed, for example, a fixed hard drive.</summary>
        DriveFixed = 3,

        /// <summary>The drive is a remote (network) drive.</summary>
        DriveRemote = 4,

        /// <summary>The drive is a CD-ROM drive.</summary>
        DriveCdrom = 5,

        /// <summary>The drive is a RAM disk.</summary>
        DriveRamdisk = 6,
    }

    private enum PnpVetoType
    {
        Ok,
        TypeUnknown,
        LegacyDevice,
        PendingClose,
        WindowsApp,
        WindowsService,
        OutstandingOpen,
        Device,
        Driver,
        IllegalDeviceRequest,
        InsufficientPower,
        NonDisableable,
        LegacyDriver,
        InsufficientRights,
    }

    /// <summary>
    /// Call with "X:" or similar</summary>
    /// <param name="driveCharWithColon">Drive letter inc colon.</param>
    /// <returns>Whether the drive was ejected.</returns>
    public static bool EjectDrive(string driveCharWithColon)
    {
        // open the storage volume
        var hVolume = CreateFile(
            @"\\.\" + driveCharWithColon,
            0,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if ((int)hVolume == INVALID_HANDLE_VALUE)
        {
            return false;
        }

        // get the volume's device number
        var deviceNumber = GetDeviceNumber(hVolume);
        if (deviceNumber == -1)
        {
            return false;
        }

        // get the drive type which is required to match the device numbers correctly
        var rootPath = $"{driveCharWithColon}\\";
        var driveType = GetDriveType(rootPath);

        // get the dos device name (like \device\floppy0) to decide if it's a floppy or not
        var pathInformation = new StringBuilder(250);
        var res = QueryDosDevice(driveCharWithColon, pathInformation, 250);
        if (res == 0)
        {
            return false;
        }

        // get the device instance handle of the storage volume by means of a
        // SetupDi enum and matching the device number
        var devInst = GetDrivesDevInstByDeviceNumber(deviceNumber, driveType, pathInformation.ToString());
        if (devInst == 0)
        {
            return false;
        }

        // get drive's parent, e.g. the USB bridge, the SATA port, an IDE channel with two drives!
        var devInstParent = 0;

        // Native cleanup/lookup; the CR_* result is non-actionable here. If it fails,
        // DevInstParent stays 0 and the subsequent eject attempt simply returns failure.
        _ = CM_Get_Parent(ref devInstParent, (int)devInst, 0);

        for (int tries = 1; tries <= 3; tries++)
        {
            // sometimes we need to retry
            var r = CM_Request_Device_Eject_NoUi(
                devInstParent,
                IntPtr.Zero,
                null,
                0,
                0);

            if (r == 0)
            {
                return true;
            }

            Thread.Sleep(500);
        }

        return false;
    }

    // CharSet.Ansi makes the existing implicit-ANSI marshaling explicit (binds GetDriveTypeA),
    // matching the [MarshalAs(LPStr)] parameter and preserving behavior.
    // CA2101 cannot be satisfied without switching to the Unicode (W) ABI, which would change
    // the native binding; the ANSI marshaling is explicit and intentional here, so suppress.
#pragma warning disable CA2101 // ANSI marshaling is intentional to bind GetDriveTypeA; Unicode would change the ABI.
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern DriveType GetDriveType([MarshalAs(UnmanagedType.LPStr)] string lpRootPathName);
#pragma warning restore CA2101

    // CharSet.Ansi preserves the original implicit-ANSI binding (QueryDosDeviceA). The
    // dos device name is only compared against the literal "\\Floppy", so ANSI is correct.
    // CA2101 cannot be satisfied without switching to the Unicode (W) ABI, which would change
    // the native binding; the ANSI marshaling is explicit and intentional here, so suppress.
#pragma warning disable CA2101 // ANSI marshaling is intentional to bind QueryDosDeviceA; Unicode would change the ABI.
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
#pragma warning disable CA1838 // StringBuilder OUT param: perf-only for interop; converting to a char buffer risks regressions in working USB-ejection code.
    private static extern uint QueryDosDevice(
        [MarshalAs(UnmanagedType.LPStr)] string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
#pragma warning restore CA1838
#pragma warning restore CA2101

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        int dwShareMode,
        IntPtr lpSecurityAttributes,
        int dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        int dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("setupapi.dll")]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        int enumerator,
        IntPtr hwndParent,
        int flags);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        SpDevinfoData? deviceInfoData,
        ref Guid interfaceClassGuid,
        int memberIndex,
        SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        int deviceInterfaceDetailDataSize,
        ref int requiredSize,
        SpDevinfoData deviceInfoData);

    [DllImport("setupapi.dll")]
    private static extern uint SetupDiDestroyDeviceInfoList(
        IntPtr deviceInfoSet);

    [DllImport("setupapi.dll")]
    private static extern int CM_Get_Parent(
        ref int pdnDevInst,
        int dnDevInst,
        int ulFlags);

    // CharSet.Ansi preserves the original implicit-ANSI binding (CM_Request_Device_EjectA).
    // CA2101 cannot be satisfied without switching to the Unicode (W) ABI, which would change
    // the native binding; the ANSI marshaling is explicit and intentional here, so suppress.
#pragma warning disable CA2101 // ANSI marshaling is intentional to bind CM_Request_Device_EjectA; Unicode would change the ABI.
    [DllImport("setupapi.dll", CharSet = CharSet.Ansi)]
#pragma warning disable CA1838 // StringBuilder param: perf-only for interop; the working interop is left intact to avoid USB-ejection regressions.
    private static extern int CM_Request_Device_Eject(
        int dnDevInst,
        out PnpVetoType pVetoType,
        StringBuilder pszVetoName,
        int ulNameLength,
        int ulFlags);
#pragma warning restore CA1838
#pragma warning restore CA2101

    // CharSet.Ansi preserves the original implicit-ANSI binding (CM_Request_Device_EjectA).
    // CA2101 cannot be satisfied without switching to the Unicode (W) ABI, which would change
    // the native binding; the ANSI marshaling is explicit and intentional here, so suppress.
#pragma warning disable CA2101 // ANSI marshaling is intentional to bind CM_Request_Device_EjectA; Unicode would change the ABI.
    [DllImport("setupapi.dll", EntryPoint = "CM_Request_Device_Eject", CharSet = CharSet.Ansi)]
#pragma warning disable CA1838 // StringBuilder param: perf-only for interop; the working interop is left intact to avoid USB-ejection regressions.
    private static extern int CM_Request_Device_Eject_NoUi(
        int dnDevInst,
        IntPtr pVetoType,
        StringBuilder? pszVetoName,
        int ulNameLength,
        int ulFlags);
#pragma warning restore CA1838
#pragma warning restore CA2101

    private static long GetDeviceNumber(IntPtr handle)
    {
        // get the volume's device number
        long deviceNumber = -1;
        const int size = 0x400; // some big size
        var buffer = Marshal.AllocHGlobal(size);
        int bytesReturned;

        try
        {
            DeviceIoControl(
                handle,
                IOCTL_STORAGE_GET_DEVICE_NUMBER,
                IntPtr.Zero,
                0,
                buffer,
                size,
                out bytesReturned,
                IntPtr.Zero);
        }
        finally
        {
            CloseHandle(handle);
        }

        if (bytesReturned > 0)
        {
            var sdn = Marshal.PtrToStructure<StorageDeviceNumber>(buffer);
            deviceNumber = sdn.DeviceNumber;
        }

        Marshal.FreeHGlobal(buffer);

        return deviceNumber;
    }

    // returns the device instance handle of a storage volume or 0 on error
    private static long GetDrivesDevInstByDeviceNumber(long deviceNumber, DriveType driveType, string dosDeviceName)
    {
        var isFloppy = dosDeviceName.Contains("\\Floppy");
        Guid guid;

        switch (driveType)
        {
            case DriveType.DriveRemovable:
                guid = isFloppy
                    ? new Guid(GUID_DEVINTERFACE_FLOPPY)
                    : new Guid(GUID_DEVINTERFACE_DISK);
                break;

            case DriveType.DriveFixed:
                guid = new Guid(GUID_DEVINTERFACE_DISK);
                break;

            case DriveType.DriveCdrom:
                guid = new Guid(GUID_DEVINTERFACE_CDROM);
                break;

            default:
                return 0;
        }

        // Get device interface info set handle for all devices attached to system
        var hDevInfo = SetupDiGetClassDevs(
            ref guid,
            0,
            IntPtr.Zero,
            DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if ((int)hDevInfo == INVALID_HANDLE_VALUE)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Retrieve a context structure for a device interface of a device information set
        var dwIndex = 0;

        while (true)
        {
            var interfaceData = new SpDeviceInterfaceData();
            if (!SetupDiEnumDeviceInterfaces(hDevInfo, null, ref guid, dwIndex, interfaceData))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NO_MORE_ITEMS)
                {
                    throw new Win32Exception(error);
                }

                break;
            }

            var devData = new SpDevinfoData();
            var size = 0;
            if (!SetupDiGetDeviceInterfaceDetail(
                    hDevInfo,
                    interfaceData,
                    IntPtr.Zero,
                    0,
                    ref size,
                    devData))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception(error);
                }
            }

            var buffer = Marshal.AllocHGlobal(size);
            var detailData = default(SpDeviceInterfaceDetailData);
            detailData.cbSize = Marshal.SizeOf<SpDeviceInterfaceDetailData>();
            Marshal.StructureToPtr(detailData, buffer, false);

            if (!SetupDiGetDeviceInterfaceDetail(hDevInfo, interfaceData, buffer, size, ref size, devData))
            {
                Marshal.FreeHGlobal(buffer);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var pDevicePath = (IntPtr)((int)buffer + Marshal.SizeOf<int>());
            var devicePath = Marshal.PtrToStringAuto(pDevicePath);
            Marshal.FreeHGlobal(buffer);

            if (!string.IsNullOrEmpty(devicePath))
            {
                // open the disk or cdrom or floppy
                var hDrive = CreateFile(
                    devicePath,
                    0,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if ((int)hDrive != INVALID_HANDLE_VALUE)
                {
                    // get its device number
                    var driveDeviceNumber = GetDeviceNumber(hDrive);
                    if (deviceNumber == driveDeviceNumber)
                    {
                        // matched the given device number with the one of the current device
                        _ = SetupDiDestroyDeviceInfoList(hDevInfo); // native cleanup; result non-actionable
                        return devData.devInst;
                    }
                }
            }

            dwIndex++;
        }

        _ = SetupDiDestroyDeviceInfoList(hDevInfo); // native cleanup; result non-actionable
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]

    private struct StorageDeviceNumber
    {
        public int DeviceType;

        public int DeviceNumber;

        public int PartitionNumber;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct SpDeviceInterfaceDetailData
    {
        public int cbSize;
        public short devicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class SpDeviceInterfaceData
    {
        public int cbSize = Marshal.SizeOf<SpDeviceInterfaceData>();
        public Guid interfaceClassGuid = Guid.Empty; // temp
        public int flags;
        public int reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class SpDevinfoData
    {
        public int cbSize = Marshal.SizeOf<SpDevinfoData>();
        public Guid classGuid = Guid.Empty; // temp
        public int devInst; // dumy
        public int reserved;
    }
}

#pragma warning restore IDE0051 // unused member