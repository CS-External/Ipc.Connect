namespace Ipc.Connect.Protocol.Commons
{
    public interface IIpcData
    {
        int Read(byte[] p_Buffer, int p_Offset, int p_Count);
    }
}
