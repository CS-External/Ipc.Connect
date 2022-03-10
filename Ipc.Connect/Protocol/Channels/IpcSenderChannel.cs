using System;
using System.Threading;
using Cloudtoid.Interprocess;
using Ipc.Connect.Protocol.Messages;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Protocol.Channels
{
    public class IpcSenderChannel: IDisposable
    {
        private ILogger m_Logger;
        private ILoggerFactory m_LoggerFactory;
        private IPublisher m_Publisher;
        private Byte[] m_SendBuffer;

        public String ChannelName { get; }
        public long BufferSize { get; }
        public long MaxMessageSize { get; }

        public long MaxMessageDataSize { get; }

        
        public IpcSenderChannel(string p_ChannelName, long p_BufferSize, long p_MaxMessageSize, ILoggerFactory p_LoggerFactory)
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
            QueueFactory l_Factory = new QueueFactory();
            m_Publisher = l_Factory.CreatePublisher(new QueueOptions(ChannelName, BufferSize));
            m_SendBuffer = new byte[MaxMessageSize];
        }

        public bool TrySendMessage(IpcMessage p_Message, TimeSpan p_Timeout)
        {
            int l_Bytes = IpcMessageUtils.SerializeMessage(p_Message, m_SendBuffer);

            long l_TimeoutTicks = (long)p_Timeout.TotalMilliseconds;
            long l_TickCount = Environment.TickCount64;

            while (!m_Publisher.TryEnqueue(new ReadOnlySpan<byte>(m_SendBuffer, 0, l_Bytes)))
            {

                Thread.Yield();
                
                if (Timeout.InfiniteTimeSpan == p_Timeout)
                {
                    continue;
                }

                if (Environment.TickCount64 - l_TickCount > l_TimeoutTicks)
                    return false;

            }

            return true;

        }

        public void Dispose()
        {
            m_Publisher?.Dispose();
            m_Publisher = null;
        }
    }
}
