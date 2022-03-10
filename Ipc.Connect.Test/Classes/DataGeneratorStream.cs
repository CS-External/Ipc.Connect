using System;
using System.IO;

namespace Ipc.Connect.Test.Classes
{
    public class DataGeneratorStream: Stream
    {
        private long m_Length;
        private long m_Postion;
        private Byte[] m_BaseData;
        public DataGeneratorStream(long p_Length)
        {
            m_Length = p_Length;
            m_BaseData = new byte[1024];

            int l_Pos = 0;

            while (l_Pos != m_BaseData.Length)
            {
                for (int i = Byte.MinValue; i <= Byte.MaxValue; i++)
                {
                    if (m_BaseData.Length <= l_Pos)
                        break;

                    m_BaseData[l_Pos] = (byte)i;
                    l_Pos++;
                }    
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long l_ToCopy = count - offset;
            l_ToCopy = Math.Min(Length - Position, l_ToCopy);
            long l_Coped = 0;

            while (Position != Length && l_ToCopy != 0)
            {
                long l_BaseDataPosition;

                if (Position == 0)
                    l_BaseDataPosition = 0;
                else
                {
                    l_BaseDataPosition = Position % m_BaseData.Length;

                    if (l_BaseDataPosition == m_BaseData.Length)
                        l_BaseDataPosition = 0;
                }
                    

                long l_CopyPart = Math.Min(l_ToCopy, m_BaseData.Length - l_BaseDataPosition);

                try
                {
                    Array.Copy(m_BaseData, l_BaseDataPosition, buffer, offset + l_Coped, l_CopyPart);
                }
                catch (Exception)
                {
                    throw;
                }

                l_ToCopy = l_ToCopy - l_CopyPart;
                l_Coped = l_Coped + l_CopyPart;
                Position = Position + l_CopyPart;

            }

            return (int)l_Coped;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return m_Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_Postion;
            }
            set
            {
                if (value < 0)
                    throw new InvalidOperationException($"Invalid Position {value}/{Length}");

                if (value > Length)
                    throw new InvalidOperationException($"Invalid Position {value}/{Length}");

                m_Postion = value;
            }
        }
    }
}
