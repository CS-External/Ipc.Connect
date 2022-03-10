using System;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Protocol.Messages;

namespace Ipc.Connect.Protocol.Utils
{
    public static class IpcSocketSenderUtils
    {
        public static bool SendStream(IIpcData p_Stream, IpcSenderChannel p_IpcSenderChannel, IpcReceiverChannel p_IpcReceiverChannel, byte[] p_SendBuffer, TimeSpan p_Timeout)
        {
            try
            {
                IpcMessageKind l_Kind = IpcMessageKind.Message;
                do
                {
                    int l_ReadCount = p_Stream.Read(p_SendBuffer, 0, p_SendBuffer.Length);

                    if (l_ReadCount < p_SendBuffer.Length)
                        l_Kind = IpcMessageKind.MessageEnd;
                    else
                        l_Kind = IpcMessageKind.Message;


                    for (int i = 0; i < 10; i++)
                    {
                        bool l_Sended = p_IpcSenderChannel.TrySendMessage(new IpcMessage(l_Kind, l_ReadCount, new ReadOnlySpan<byte>(p_SendBuffer, 0, l_ReadCount)), p_Timeout);

                        if (l_Sended)
                            break;

                        IpcMessage l_IpcMessage = p_IpcReceiverChannel.WaitForMessage(TimeSpan.FromMilliseconds(50));

                        if (l_IpcMessage.Kind == IpcMessageKind.Close)
                        {
                            return false;
                        }
                    }

                } while (l_Kind == IpcMessageKind.Message);

                return true;
            }
            finally
            {
                IDisposable l_Disposable = p_Stream as IDisposable;

                if (l_Disposable != null)
                {
                    l_Disposable.Dispose();
                }
            }
        }
    }
}
