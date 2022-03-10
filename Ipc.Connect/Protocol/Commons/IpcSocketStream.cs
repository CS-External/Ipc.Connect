using System;
using System.IO;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Exceptions;
using Ipc.Connect.Protocol.Messages;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Protocol.Commons
{
    public class IpcSocketStream: Stream
    {
        private IIpcSocketStreamParent m_StreamParent;
        private ILogger m_Logger;

        private Byte[] m_Buffer;
        private int m_BufferPosition;
        private int m_BufferLength;

        public IpcSocketStream(IIpcSocketStreamParent p_StreamParent, ILogger p_Logger, IpcMessage p_IpcMessage)
        {
            m_Logger = p_Logger;
            m_StreamParent = p_StreamParent;
            m_Buffer = new byte[m_StreamParent.GetReceiverChannel().MaxMessageSize];
            m_BufferPosition = 0;
            m_BufferLength = p_IpcMessage.ContentLength;

            if (p_IpcMessage.ContentLength > 0)
            {
                p_IpcMessage.Content.CopyTo(m_Buffer);
            }
            
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] p_Buffer, int p_Offset, int p_Count)
        {

            Span<Byte> l_TargetBuffer = new Span<byte>(p_Buffer, p_Offset, p_Count);
            ReadOnlySpan<Byte> l_ReadBuffer = new ReadOnlySpan<byte>(m_Buffer, m_BufferPosition, m_BufferLength - m_BufferPosition);
            
            int l_BytesToRead = p_Count - p_Offset;

            int l_BytesReaded = 0;

            while (l_BytesToRead != 0)
            {
                int l_BytesReadFromBuffer = Math.Min(l_ReadBuffer.Length, l_BytesToRead);

                if (l_BytesReadFromBuffer > 0)
                {
                    l_ReadBuffer.Slice(0, l_BytesReadFromBuffer).CopyTo(l_TargetBuffer.Slice(l_BytesReaded));
                    l_BytesToRead = l_BytesToRead - l_BytesReadFromBuffer;
                    m_BufferPosition = m_BufferPosition + l_BytesReadFromBuffer;
                    l_BytesReaded = l_BytesReaded + l_BytesReadFromBuffer;
                    l_ReadBuffer = new ReadOnlySpan<byte>(m_Buffer, m_BufferPosition, m_BufferLength - m_BufferPosition);
                }

                if (l_BytesToRead == 0)
                {
                    break;
                }

                if (m_StreamParent == null)
                {
                    break;
                }
                
                IpcReceiverChannel l_Channel = m_StreamParent.GetReceiverChannel();

                if (l_Channel == null)
                {
                    break;
                }

                IpcMessage l_Message = l_Channel.WaitForMessage(TimeSpan.FromMilliseconds(ReadTimeout));

                if (l_Message.Kind == IpcMessageKind.Message || l_Message.Kind == IpcMessageKind.MessageEnd)
                {
                    
                    int l_BytesReadFromMessage = Math.Min(l_Message.ContentLength, l_BytesToRead);

                    if (l_BytesReadFromMessage > 0)
                    {
                        l_Message.Content.Slice(0, l_BytesReadFromMessage).CopyTo(l_TargetBuffer.Slice(l_BytesReaded));
                        l_BytesToRead = l_BytesToRead - l_BytesReadFromMessage;
                        l_BytesReaded = l_BytesReaded + l_BytesReadFromMessage;
                    }

                    if (l_BytesReadFromMessage < l_Message.ContentLength)
                    {
                        m_BufferPosition = 0;
                        m_BufferLength = l_Message.ContentLength - l_BytesReadFromMessage;
                        l_ReadBuffer = new ReadOnlySpan<byte>(m_Buffer, 0, m_BufferLength);
                        l_Message.Content.Slice(l_BytesReadFromMessage).CopyTo(new Span<byte>(m_Buffer, 0, m_BufferLength));
                    }

                    if (l_Message.Kind == IpcMessageKind.MessageEnd)
                    {
                        m_StreamParent.StreamClosed(this);
                        m_StreamParent = null;
                    }
                }
                else if (l_Message.Kind == IpcMessageKind.Close)
                {
                    m_StreamParent.ConnectionClosed();
                    throw new IpcException($"Read aborted because Channel is closed. {m_StreamParent.GetChannelName()}");
                }
                else
                {
                    throw new IpcException($"Invalid Message Type {l_Message.Kind}, ChannelName: {m_StreamParent.GetChannelName()}");
                }
            }


            return l_BytesReaded;


        }

        public override int ReadTimeout { get; set; }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
                return false;
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
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_StreamParent != null)
            {
                m_StreamParent.ConnectionClosed();
                m_StreamParent = null;
                m_Buffer = null;
            }
            

            base.Dispose(disposing);
        }
    }
}
