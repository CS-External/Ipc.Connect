using System;
using System.Runtime.InteropServices;

namespace Ipc.Connect.Utils;

public class IpcLinuxUtils
{
    [DllImport("librt.so.1")]
    private static extern IntPtr sem_open([MarshalAs((UnmanagedType) 0)] string name, int oflag, uint mode, uint value);
}