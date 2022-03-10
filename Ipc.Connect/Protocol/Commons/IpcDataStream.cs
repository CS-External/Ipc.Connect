using System;
using System.IO;

namespace Ipc.Connect.Protocol.Commons
{
    public class IpcDataStream: IIpcData, IDisposable
    {
        private Stream m_Stream;
        private Boolean m_CloseStream = false;

        public IpcDataStream(Stream p_Stream, bool p_CloseStream)
        {
            m_Stream = p_Stream;
            m_CloseStream = p_CloseStream;
        }

        public IpcDataStream(Stream p_Stream)
        {
            m_Stream = p_Stream;
        }

        public int Read(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            return m_Stream.Read(p_Buffer, p_Offset, p_Count);
        }

        public void Dispose()
        {
            if (m_CloseStream)
            {
                m_Stream?.Dispose();
            }

            m_Stream = null;

        }
    }
}
