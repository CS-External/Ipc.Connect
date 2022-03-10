namespace Ipc.Connect.Protocol.Messages
{
    public enum IpcMessageKind: byte
    {
        Invalid,
        StartHandshake,
        FinishHandshake,
        Message,
        MessageEnd,
        Close,
        Ping,
        Pong,
    }
}
