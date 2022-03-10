using Ipc.Connect.Protocol.Channels;

namespace Ipc.Connect.Protocol.Commons
{
    public interface IIpcSocketStreamParent
    {
        IpcReceiverChannel GetReceiverChannel();
        string GetChannelName();
        IpcSenderChannel GetIpcSenderChannel();
        void StreamClosed(IpcSocketStream p_IpcSocketStream);
        void ConnectionClosed();
    }
}
