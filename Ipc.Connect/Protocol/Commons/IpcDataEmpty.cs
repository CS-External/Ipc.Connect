using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipc.Connect.Protocol.Commons
{
    public class IpcDataEmpty: IIpcData
    {
        public int Read(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            return 0;
        }
    }
}
