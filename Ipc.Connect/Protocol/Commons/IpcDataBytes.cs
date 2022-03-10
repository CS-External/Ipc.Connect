using System;

namespace Ipc.Connect.Protocol.Commons
{
    public class IpcDataBytes: IIpcData
    {
        private Byte[] m_Bytes;
        private int m_Pos;

        public IpcDataBytes(byte[] p_Bytes)
        {
            m_Bytes = p_Bytes;
            m_Pos = 0;
        }

        public int Read(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            int l_Copyed = Math.Min(p_Count, m_Bytes.Length - m_Pos);
            Array.Copy(m_Bytes, m_Pos, p_Buffer, p_Offset, l_Copyed);
            m_Pos = m_Pos + l_Copyed;
            return l_Copyed;
        }
    }
}
