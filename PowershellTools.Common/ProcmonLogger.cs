using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PowerShellTools.Common
{
    /// <summary>
    /// Appender for writing to the Procmon profiling log.
    /// </summary>
    public class ProcMonLogger : IDisposable
    {
        private IntPtr _deviceHandle;
        private static readonly uint FILE_DEVICE_PROCMON_LOG = 0x00009535;
        private static readonly uint METHOD_BUFFERED = 0;
        private const uint FILE_WRITE_ACCESS = 0x0002;
        private static ProcMonLogger _instance;

        public static ProcMonLogger Instance
        {
            get { return _instance ?? (_instance = new ProcMonLogger()); }
        }

        private readonly uint IOCTL_EXTERNAL_LOG_DEBUGOUT = CTL_CODE(FILE_DEVICE_PROCMON_LOG, 0x81, METHOD_BUFFERED,
            FILE_WRITE_ACCESS);

        private static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
        {
            return ((deviceType) << 16) | ((access) << 14) | ((function) << 2) | (method);
        }

        private readonly object _lockObject = new object();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFileW(
             [MarshalAs(UnmanagedType.LPTStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize,
        IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Appends the logging event to the procmon device.
        /// </summary>
        /// <param name="msg"></param>
        public void Log(string msg)
        {
            lock (_lockObject)
            {
                if (_deviceHandle == IntPtr.Zero)
                {
                    _deviceHandle = CreateFileW("\\\\.\\Global\\ProcmonDebugLogger", FileAccess.ReadWrite,
                        FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open, FileAttributes.Normal,
                        IntPtr.Zero);

                    if (_deviceHandle == new IntPtr(-1))
                    {
                        _deviceHandle = IntPtr.Zero;
                    }
                }

                var len = (uint)Encoding.Unicode.GetByteCount(msg);
                if (len > 4094)
                {
                    len = 4094;
                }

                var buffer = Marshal.StringToHGlobalUni(msg);
                uint bytesReturned;
                if (
                    !DeviceIoControl(_deviceHandle, IOCTL_EXTERNAL_LOG_DEBUGOUT, buffer, len, IntPtr.Zero, 0,
                        out bytesReturned,
                        IntPtr.Zero))
                {
                    throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                CloseHandle(_deviceHandle);
            }
        }
    }
}
