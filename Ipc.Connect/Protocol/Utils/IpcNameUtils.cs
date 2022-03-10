using System;
using System.IO;

namespace Ipc.Connect.Protocol.Utils
{
    public static class IpcNameUtils
    {
        public static String BuildName(String p_Name, String p_Suffix)
        {
            return p_Name + "_" + p_Suffix;
        }

        public static String CreateRandomName()
        {
            return Path.GetRandomFileName();
        }

    }
}
