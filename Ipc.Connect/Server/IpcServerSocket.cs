using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Protocol.Exceptions;
using Ipc.Connect.Protocol.Messages;
using Ipc.Connect.Protocol.Utils;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Server
{
    public class IpcServerSocket: IDisposable, IIpcSocketStreamParent
    {

        private ILogger m_Logger;
        private String m_ChannelName;

        private IpcChannelFactory m_ChannelFactory;
        private IIpcServerRequestHandler m_RequestHandler;

        private IpcReceiverChannel m_IpcReceiverChannel;
        private IpcSenderChannel m_IpcSenderChannel;

        public event EventHandler<EventArgs> Closed;

        private object m_CloseLock = new object();

        public Boolean IsConnected { get; private set; }

        public String ChannelName
        {
            get
            {
                return m_ChannelName;
            }
        }
        
        public IpcServerSocket(IpcChannelFactory p_ChannelFactory, IIpcServerRequestHandler p_RequestHandler)
        {
            m_Logger = p_ChannelFactory.LoggerFactory.CreateLogger(GetType());
            m_ChannelFactory = p_ChannelFactory;
            m_RequestHandler = p_RequestHandler;
        }


        public void FinishHandShakes(String p_ChannelName, TimeSpan p_Timeout)
        {
            try
            {
                m_ChannelName = p_ChannelName;
                m_IpcReceiverChannel = m_ChannelFactory.CreateReceiverChannel(IpcNameUtils.BuildName(m_ChannelName, "Request"));
                m_IpcSenderChannel = m_ChannelFactory.CreateSenderChannel(IpcNameUtils.BuildName(m_ChannelName, "Response"));

                StartListenForRequests(p_Timeout);

                byte[] l_Bytes = Encoding.UTF8.GetBytes(m_ChannelName);
                bool l_Sended = m_IpcSenderChannel.TrySendMessage(new IpcMessage(IpcMessageKind.FinishHandshake, l_Bytes.Length, new ReadOnlySpan<byte>(l_Bytes, 0, l_Bytes.Length)), p_Timeout);

                if (!l_Sended)
                    throw new IpcException($"Unable to Finish Handshake. Message not sended");

                IsConnected = true;
            }
            catch (Exception)
            {
                Close();
                throw;
            }
            
        }

        private void StartListenForRequests(TimeSpan p_Timeout)
        {
            Task l_Task = new Task(() => ListenForRequests(p_Timeout), TaskCreationOptions.LongRunning);
            l_Task.Start();
        }

        private void ListenForRequests(TimeSpan p_Timeout)
        {
            Byte[] l_SendBuffer = new byte[m_IpcSenderChannel.MaxMessageDataSize];

            while (m_IpcReceiverChannel != null)
            {
                try
                {
                    IpcMessage l_Message;

                    if (!m_IpcReceiverChannel.WaitForMessageSafe(out l_Message, m_ChannelFactory.IdleTimeOut))
                    {
                        Close();
                        continue;
                    }

                    Stream l_Stream = null;
                    try
                    {
                        if (l_Message.Kind == IpcMessageKind.MessageEnd)
                        {
                            m_Logger.Debug("Handle Request -> Readed");
                            l_Stream = new MemoryStream(l_Message.Content.ToArray());
                        }
                        else if (l_Message.Kind == IpcMessageKind.Message)
                        {
                            m_Logger.Debug("Handle Request -> Start reading");
                            l_Stream = new IpcSocketStream(this, m_Logger, l_Message);
                            l_Stream.ReadTimeout = (int)p_Timeout.TotalMilliseconds;
                        }
                        else if (l_Message.Kind == IpcMessageKind.Ping)
                        {
                            m_Logger.Debug("Handle PingRequest -> Send Response");
                            bool l_PongSended = m_IpcSenderChannel.TrySendMessage(new IpcMessage(IpcMessageKind.Pong, 0, ReadOnlySpan<byte>.Empty), p_Timeout);

                            if (!l_PongSended)
                            {
                                Close();
                                throw new IpcException($"Unable to send PingResponse (Timeout {p_Timeout})");
                            }

                            continue;
                        }
                        else if (l_Message.Kind == IpcMessageKind.Close)
                        {
                            Close();
                            return;
                        }
                        else
                        {
                            // Invalid message
                            m_Logger.Warning($"Invalid Message Kind {l_Message.Kind}. ChannelName {m_ChannelName} ");
                            continue;
                        }

                        m_Logger.Debug("Handle Request -> Handle");
                        IIpcData l_IpcData = m_RequestHandler.HandleRequest(l_Stream);

                        m_Logger.Debug("Handle Request -> Start Sending Response");
                        if (m_IpcSenderChannel == null)
                            continue;
                        
                        bool l_Sended = IpcSocketSenderUtils.SendStream(l_IpcData, m_IpcSenderChannel, m_IpcReceiverChannel, l_SendBuffer, p_Timeout);
                        m_Logger.Debug("Handle Request -> End Sending Response");

                        if (!l_Sended)
                        {
                            Close();
                            throw new IpcException($"Unable to send Response (Timeout {p_Timeout})");
                        }
                            

                    }
                    finally
                    {
                        if (l_Stream != null)
                            l_Stream.Dispose();
                    }
                    

                }
                catch (Exception e)
                {
                    m_Logger.Error($"Error while Listen for Message. Channel {m_ChannelName}", e);
                }
            }
        }

        public void Close()
        {

            lock (m_CloseLock)
            {


                if (IsConnected)
                {
                    m_IpcSenderChannel.TrySendMessage(new IpcMessage(IpcMessageKind.Close, 0, ReadOnlySpan<byte>.Empty), TimeSpan.FromMilliseconds(250));
                }

                m_IpcSenderChannel?.Dispose();
                m_IpcSenderChannel = null;
                m_IpcReceiverChannel?.Dispose();
                m_IpcReceiverChannel = null;

                if (IsConnected)
                {
                    m_Logger.Debug("Close Connection");
                    Closed?.Invoke(this, EventArgs.Empty);
                    IsConnected = false;
                }

                m_ChannelName = null;
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
            m_Logger.Debug("Handle Request -> Reading finished");
        }

        public void ConnectionClosed()
        {
            m_Logger.Debug("Handle Request -> Reading aborted. Connection Closed");
            Close();
        }
    }
}
