using System.IO;
using Ipc.Connect.Protocol.Commons;

namespace Ipc.Connect.Server
{
    public interface IIpcServerRequestHandler
    {
        IIpcData HandleRequest(Stream p_Stream);
    }
}
