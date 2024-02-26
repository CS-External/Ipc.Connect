using System;
using System.Runtime.InteropServices;

namespace Ipc.Connect.Utils;

public class IpcUtils
{
    private static Boolean? m_IsSupported;

    public static bool IsSupported()
    {
        if (!m_IsSupported.HasValue)
            m_IsSupported = CalcIsSupported();

        return m_IsSupported.Value;
    }

    private static bool CalcIsSupported()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                Marshal.PrelinkAll(typeof(IpcLinuxUtils));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                Marshal.PrelinkAll(typeof(IpcLMacOSUtils));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Always supported
        return true;
    }
}