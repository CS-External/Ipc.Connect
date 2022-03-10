using System;

namespace Ipc.Connect.Protocol.Exceptions
{
    public class IpcException: Exception
    {
        public IpcException()
        {
        }

        public IpcException(string p_Message) : base(p_Message)
        {
        }

        public IpcException(string p_Message, Exception p_InnerException) : base(p_Message, p_InnerException)
        {
        }
    }
}
