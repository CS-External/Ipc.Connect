using System;
using System.IO;
using Ipc.Connect.Protocol.Commons;

namespace Ipc.Connect.Server
{
    public class IpcServerRequestHandlerDelegate: IIpcServerRequestHandler
    {
        private Func<Stream, IIpcData> m_Delegate;

        public IpcServerRequestHandlerDelegate(Func<Stream, IIpcData> p_Delegate)
        {
            m_Delegate = p_Delegate;
        }

        public IIpcData HandleRequest(Stream p_Data)
        {
            return m_Delegate(p_Data);
        }
    }
}
