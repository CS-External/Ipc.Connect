using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Protocol.Utils
{
    public static class IpcLogUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(this ILogger p_Logger, String p_Message)
        {
            p_Logger.Log(LogLevel.Debug, p_Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(this ILogger p_Logger, String p_Message)
        {
            p_Logger.Log(LogLevel.Warning, p_Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(this ILogger p_Logger, String p_Message)
        {
            p_Logger.Log(LogLevel.Error, p_Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(this ILogger p_Logger, String p_Message, Exception p_Exception)
        {
            p_Logger.Log(LogLevel.Error, p_Exception, p_Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(this ILogger p_Logger, String p_Message)
        {
            p_Logger.Log(LogLevel.Information, p_Message);
        }
    }
}
