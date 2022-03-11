using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipc.Connect.Client
{
    public class IpcClientStream: Stream
    {
        private Stream m_InnerStream;
        private Action m_CloseAction;

        public IpcClientStream(Stream p_InnerStream, Action p_CloseAction)
        {
            m_InnerStream = p_InnerStream;
            m_CloseAction = p_CloseAction;
        }

        public override void Flush()
        {
            m_InnerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_InnerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_InnerStream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get
            {
                return m_InnerStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {

                return m_InnerStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_InnerStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return m_InnerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_InnerStream.Position;
            }
            set
            {
                m_InnerStream.Position = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            m_InnerStream.Dispose();
            m_CloseAction();
            m_InnerStream = null;

        }
    }
}
