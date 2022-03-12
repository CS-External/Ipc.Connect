using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipc.Connect.Client
{
    public class IpcClientOptions
    {
        public TimeSpan ConnectTimeOut { get; set; }

        public IpcClientOptions()
        {
            ConnectTimeOut = TimeSpan.FromSeconds(1);
        }
    }
}
