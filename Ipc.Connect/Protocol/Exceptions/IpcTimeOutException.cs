using System;

namespace Ipc.Connect.Protocol.Exceptions
{
    public class IpcTimeOutException: IpcException
    {
        public IpcTimeOutException()
        {
        }

        public IpcTimeOutException(string p_Message) : base(p_Message)
        {
        }

        public IpcTimeOutException(string p_Message, Exception p_InnerException) : base(p_Message, p_InnerException)
        {
        }
    }
}
