using System;
using Ipc.Connect.Protocol.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ipc.Connect.Protocol.Channels
{
    public class IpcChannelFactory
    {
        private ILoggerFactory m_LoggerFactory;

        public long BufferSize { get; }
        public long MaxMessageSize { get; }
        public TimeSpan IdleTimeOut { get; set; } = TimeSpan.FromMinutes(1);

        public ILoggerFactory LoggerFactory
        {
            get
            {
                return m_LoggerFactory;
            }
        }

        public IpcChannelFactory(ILoggerFactory p_LoggerFactory)
        {
            m_LoggerFactory = p_LoggerFactory;
            BufferSize = 2 * 1024 * 1024; // 2 MB
            MaxMessageSize = 64 * 1024;
        }

        public IpcChannelFactory()
        {
            m_LoggerFactory = NullLoggerFactory.Instance;
            BufferSize = 2 * 1024 * 1024; // 2 MB
            MaxMessageSize = 64 * 1024;
        }


        public IpcChannelFactory(ILoggerFactory p_LoggerFactory, long p_BufferSize)
        {
            m_LoggerFactory = p_LoggerFactory;
            BufferSize = p_BufferSize;
            MaxMessageSize = p_BufferSize / 10;
        }

        public IpcChannelFactory(long p_BufferSize)
        {
            m_LoggerFactory = NullLoggerFactory.Instance;
            BufferSize = p_BufferSize;
            MaxMessageSize = p_BufferSize / 10;
        }

        public IpcChannelFactory(ILoggerFactory p_LoggerFactory, long p_BufferSize, long p_MaxMessageSize)
        {
            m_LoggerFactory = p_LoggerFactory;
            BufferSize = p_BufferSize;
            MaxMessageSize = p_MaxMessageSize;

            if (BufferSize < MaxMessageSize)
                throw new IpcException(
                    $"MaxMessageSize must be smaller than Buffersize. {BufferSize} < {MaxMessageSize}");
        }

        public IpcChannelFactory(long p_BufferSize, long p_MaxMessageSize)
        {
            m_LoggerFactory = NullLoggerFactory.Instance;
            BufferSize = p_BufferSize;
            MaxMessageSize = p_MaxMessageSize;

            if (BufferSize < MaxMessageSize)
                throw new IpcException(
                    $"MaxMessageSize must be smaller than Buffersize. {BufferSize} < {MaxMessageSize}");
        }

        public IpcReceiverChannel CreateReceiverChannel(String p_Name)
        {
            return new IpcReceiverChannel(p_Name, BufferSize, MaxMessageSize, m_LoggerFactory);
        }

        public IpcSenderChannel CreateSenderChannel(String p_Name)
        {
            return new IpcSenderChannel(p_Name, BufferSize, MaxMessageSize, m_LoggerFactory);
        }
    }
}
