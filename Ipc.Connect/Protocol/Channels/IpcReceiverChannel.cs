using System;
using System.Threading;
using Cloudtoid.Interprocess;
using Ipc.Connect.Protocol.Exceptions;
using Ipc.Connect.Protocol.Messages;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Protocol.Channels
{
    public class IpcReceiverChannel : IDisposable
    {
        private ILogger m_Logger;
        private ILoggerFactory m_LoggerFactory;
        private ISubscriber m_Subscriber;
        private Byte[] m_Buffer;

        public String ChannelName { get;  }
        public long BufferSize { get; }
        public long MaxMessageSize { get; }
        public long MaxMessageDataSize { get; }

        public IpcReceiverChannel(string p_ChannelName, long p_BufferSize, long p_MaxMessageSize, ILoggerFactory p_LoggerFactory)
        {
            m_LoggerFactory = p_LoggerFactory;
            m_Logger = m_LoggerFactory.CreateLogger(GetType());
            ChannelName = p_ChannelName;
            BufferSize = p_BufferSize;
            MaxMessageSize = p_MaxMessageSize;
            MaxMessageDataSize = p_MaxMessageSize - IpcMessageUtils.CONST_MESSAGE_HEADER_SIZE - IpcMessageUtils.CONST_INTERNAL_IPC_LIB_HEADER_SIZE;
            Init();
        }

        private void Init()
        {
            QueueFactory l_Factory = new QueueFactory(m_LoggerFactory);
            m_Subscriber = l_Factory.CreateSubscriber(new QueueOptions(ChannelName, BufferSize));
            m_Buffer = new byte[MaxMessageSize];
        }

        public void Dispose()
        {
            m_Subscriber?.Dispose();
            m_Subscriber = null;
        }

        public IpcMessage WaitForMessage(TimeSpan p_Timeout)
        {
            try
            {
                m_Subscriber.Dequeue(m_Buffer, new CancellationTokenSource(p_Timeout).Token);
            }
            catch (OperationCanceledException e)
            {
                throw new IpcTimeOutException($"WaitForMessage Aborted during timeout {p_Timeout}", e);
            }

            return IpcMessageUtils.ParseMessage(m_Buffer);
        }

        public bool WaitForMessageSafe(out IpcMessage p_Message, TimeSpan p_Timeout)
        {
            try
            {
                m_Subscriber.Dequeue(m_Buffer, new CancellationTokenSource(p_Timeout).Token);
            }
            catch (OperationCanceledException)
            {
                p_Message = default(IpcMessage);
                return false;
            }

            p_Message = IpcMessageUtils.ParseMessage(m_Buffer);
            return true;
        }

        public bool WaitForMessageSafe(out IpcMessage p_Message, CancellationToken p_Cancellation)
        {
            try
            {
                m_Subscriber.Dequeue(m_Buffer, p_Cancellation);
            }
            catch (OperationCanceledException)
            {
                p_Message = default(IpcMessage);
                return false;
            }

            p_Message = IpcMessageUtils.ParseMessage(m_Buffer);
            return true;
        }
    }
}
