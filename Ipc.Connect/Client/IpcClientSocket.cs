using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Protocol.Exceptions;
using Ipc.Connect.Protocol.Messages;
using Ipc.Connect.Protocol.Utils;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Client
{
    public class IpcClientSocket : IDisposable, IIpcSocketStreamParent
    {
        private ILogger m_Logger;
        private IpcChannelFactory m_ChannelFactory;
        private String m_ConnectionTarget;

        private IpcReceiverChannel m_IpcReceiverChannel;
        private IpcSenderChannel m_IpcSenderChannel;
        private IpcClientSocketState m_SocketState = IpcClientSocketState.Closed;
        private object m_SocketStateLock = new object();


        private String m_ChannelName;
        private Byte[] m_SendBuffer;
        private object m_CloseLock = new object();

        public event EventHandler<EventArgs> SocketStateChanged;

        public IpcClientSocketState SocketState
        {
            get
            {
                return m_SocketState;
            }
        }

        public String ConnectionTarget
        {
            get
            {
                return m_ConnectionTarget;
            }
        }

        public String ChannelName
        {
            get
            {
                return m_ChannelName;
            }
        }

        private void SetSocketState(IpcClientSocketState p_State)
        {
            if (m_SocketState == p_State)
                return;

            m_SocketState = p_State;
            SocketStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool SetSocketStateSafe(IpcClientSocketState p_SourceState, IpcClientSocketState p_TargetSocketState, IpcClientSocketState p_WaitState, TimeSpan p_Timeout)
        {
            long l_Start = Environment.TickCount64;
            long l_TimeoutMS = (long)p_Timeout.TotalMilliseconds;

            while (m_SocketState == p_WaitState)
            {
                Thread.Yield();

                if (Environment.TickCount64 - l_Start > l_TimeoutMS)
                    return false;

            }

            if (m_SocketState != p_SourceState)
                return false;

            lock (m_SocketStateLock)
            {
                if (m_SocketState != p_SourceState)
                    return false;

                m_SocketState = p_TargetSocketState;
            }

            SocketStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }


        public IpcClientSocket(IpcChannelFactory p_ChannelFactory)
        {
            m_ChannelFactory = p_ChannelFactory;
            m_Logger = m_ChannelFactory.LoggerFactory.CreateLogger(GetType());
        }

        public void Connect(String p_Name, TimeSpan p_Timeout)
        {
            if (m_SocketState != IpcClientSocketState.Closed)
                throw new IpcException($"Client is already in used. State {m_SocketState}, TargetName {p_Name}");

            // Init
            SetSocketState(IpcClientSocketState.Closed);
            m_ConnectionTarget = p_Name;

            // Setup
            m_ChannelName = IpcNameUtils.CreateRandomName();
            m_IpcSenderChannel = m_ChannelFactory.CreateSenderChannel(IpcNameUtils.BuildName(m_ChannelName, "Request"));
            m_IpcReceiverChannel = m_ChannelFactory.CreateReceiverChannel(IpcNameUtils.BuildName(m_ChannelName, "Response"));
            

            // Start Handshake
            SetSocketState(IpcClientSocketState.WaitingForConnect);

            bool l_Successful;

            using (IpcSenderChannel l_SenderChannel = m_ChannelFactory.CreateSenderChannel(m_ConnectionTarget))
            {
                byte[] l_Bytes = Encoding.UTF8.GetBytes(m_ChannelName);
                l_Successful = l_SenderChannel.TrySendMessage(new IpcMessage(IpcMessageKind.StartHandshake, l_Bytes.Length, l_Bytes), p_Timeout);
            }

            if (!l_Successful)
            {
                Close();
                throw new IpcException($"Unable to connect to {p_Name} (Timeout: {p_Timeout})");
            }

            l_Successful = FinishHandShake(m_IpcReceiverChannel.WaitForMessage(p_Timeout));

            if (!l_Successful)
            {
                Close();
                throw new IpcException($"Unable to FinishHandShake to {p_Name} (Timeout: {p_Timeout})");
            }

            m_SendBuffer = new byte[m_IpcSenderChannel.MaxMessageDataSize];

            Task l_Task = new Task(KeepAliveTask);
            l_Task.Start();
        }

        private void KeepAliveTask()
        {
            while (SocketState != IpcClientSocketState.Closed)
            {
                try
                {
                    Task.Delay(m_ChannelFactory.IdleTimeOut.Divide(4)).Wait();
                    Ping(TimeSpan.FromSeconds(1));
                }
                catch (Exception e)
                {
                    m_Logger.Warning("KeepAlive failed: " + e.ToString());   
                }
            }
        }


        private bool FinishHandShake(IpcMessage p_Message)
        {
            if (p_Message.Kind != IpcMessageKind.FinishHandshake)
                return false;

            string l_ChannelName = Encoding.UTF8.GetString(p_Message.Content);

            if (l_ChannelName != m_ChannelName)
                return false;

            SetSocketState(IpcClientSocketState.Idle);
            return true;
        }

        private bool Ping(TimeSpan p_Timeout)
        {
            if (!SetSocketStateSafe(IpcClientSocketState.Idle, IpcClientSocketState.SendingPing, IpcClientSocketState.SendingMessage, p_Timeout))
                return false;

            m_Logger.Debug("PingRequest -> Start sending");
            try
            {
                bool l_Sended = m_IpcSenderChannel.TrySendMessage(new IpcMessage(IpcMessageKind.Ping, 0, ReadOnlySpan<byte>.Empty), p_Timeout);

                if (!l_Sended)
                    throw new IpcException("Unable to Send Ping Request");

                IpcMessage l_Message = m_IpcReceiverChannel.WaitForMessage(p_Timeout);

                if (l_Message.Kind != IpcMessageKind.Pong)
                {
                    throw new IpcException($"Invalid Response format for Ping. Expected {IpcMessageKind.Pong} got {l_Message.Kind}");
                }

                m_Logger.Debug("PingRequest -> Successful");
                SetSocketState(IpcClientSocketState.Idle);
            }
            catch (Exception)
            {
                m_Logger.Debug("PingRequest -> Failed");
                Close();
                throw;
            }

            return true;
        }

        public Stream Send(IIpcData p_Data, TimeSpan p_Timeout)
        {
            if (!SetSocketStateSafe(IpcClientSocketState.Idle, IpcClientSocketState.SendingMessage, IpcClientSocketState.SendingPing, p_Timeout))
            {
                throw new IpcException($"Socket has invalid State for Sending Message. Current State {SocketState}");
            }


            m_Logger.Debug($"Request -> Start Send");

            SetSocketState(IpcClientSocketState.SendingMessage);
            try
            {
                bool l_Sended = IpcSocketSenderUtils.SendStream(p_Data, m_IpcSenderChannel, m_IpcReceiverChannel, m_SendBuffer, p_Timeout);

                if (!l_Sended)
                {
                    Close();
                    throw new IpcException($"Unable to send Message. Send Request was not successful (Timout {p_Timeout})");
                }


                SetSocketState(IpcClientSocketState.WaitingForResponse);

                m_Logger.Debug($"Request -> Waiting for Response");

                IpcMessage l_Message = m_IpcReceiverChannel.WaitForMessage(p_Timeout);
                SetSocketState(IpcClientSocketState.ReadingResponse);

                m_Logger.Debug($"Request -> Start Reading Response");

                if (l_Message.Kind == IpcMessageKind.MessageEnd)
                {
                    MemoryStream l_Stream = new MemoryStream(l_Message.Content.ToArray());
                    m_Logger.Debug($"Request -> End Reading Response");
                    SetSocketState(IpcClientSocketState.Idle);
                    return l_Stream;
                }
                else if (l_Message.Kind == IpcMessageKind.Message)
                {
                    IpcSocketStream l_IpcSocketStream = new IpcSocketStream(this, m_Logger, l_Message);
                    l_IpcSocketStream.ReadTimeout = (int)p_Timeout.TotalMilliseconds;
                    return l_IpcSocketStream;
                }
                else
                {
                    throw new IpcException($"Unable to send Message. Invalid Response Message format {l_Message.Kind}");
                }

            }
            catch (Exception)
            {

                Close();
                throw;
            }
        }

        public Stream Send(Stream p_Stream, TimeSpan p_Timeout)
        {
            return Send(new IpcDataStream(p_Stream, false), p_Timeout);
        }

        public void Close()
        {

            lock (m_CloseLock)
            {
                if (m_SocketState != IpcClientSocketState.Closed)
                {
                    m_IpcSenderChannel.TrySendMessage(new IpcMessage(IpcMessageKind.Close, 0, ReadOnlySpan<byte>.Empty), TimeSpan.FromMilliseconds(250));
                    m_Logger.Debug("Close Connection");
                }

                SetSocketState(IpcClientSocketState.Closed);

                m_ConnectionTarget = null;
                m_ChannelName = null;
                m_IpcReceiverChannel?.Dispose();
                m_IpcReceiverChannel = null;

                m_IpcSenderChannel?.Dispose();
                m_IpcSenderChannel = null;
            }

            
        }


        public void Dispose()
        {
            Close();
        }

        public IpcReceiverChannel GetReceiverChannel()
        {
            return m_IpcReceiverChannel;
        }

        public string GetChannelName()
        {
            return m_ChannelName;
        }

        public IpcSenderChannel GetIpcSenderChannel()
        {
            return m_IpcSenderChannel;
        }

        public void StreamClosed(IpcSocketStream p_IpcSocketStream)
        {
            m_Logger.Debug($"Request -> End Reading Response");
            SetSocketState(IpcClientSocketState.Idle);
        }

        public void ConnectionClosed()
        {
            m_Logger.Debug($"Request -> Reading aborted because Remote connection closed");
            Close();
        }
    }
}