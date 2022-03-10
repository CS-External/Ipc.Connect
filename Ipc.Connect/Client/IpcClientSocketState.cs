namespace Ipc.Connect.Client
{
    public enum IpcClientSocketState
    {
        Closed,
        WaitingForConnect,
        Idle,
        SendingMessage,
        SendingPing,
        WaitingForResponse,
        ReadingResponse


    }
}
