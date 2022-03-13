using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Protocol.Utils;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Client
{
    public class IpcClient: IDisposable
    {
        private ILogger m_Logger;
        private IpcChannelFactory m_ChannelFactory;
        private String m_ChannelName;
        private ConcurrentBag<IpcClientSocket> m_Sockets;
        private IpcClientOptions m_Options;

        public IpcClient(IpcChannelFactory p_ChannelFactory, String p_Channel, IpcClientOptions p_Options)
        {
            m_ChannelName = p_Channel;
            m_ChannelFactory = p_ChannelFactory;
            m_Logger = m_ChannelFactory.LoggerFactory.CreateLogger(GetType());
            m_Sockets = new ConcurrentBag<IpcClientSocket>();
            m_Options = p_Options;
        }

        public IpcClient(IpcChannelFactory p_ChannelFactory, string p_ChannelName): this(p_ChannelFactory, p_ChannelName, new IpcClientOptions())
        {

        }


        public int PooledConnectionCount
        {
            get
            {
                return m_Sockets.Count;
            }
        }

        public Stream Send(Stream p_Stream, TimeSpan p_TimeOut)
        {
            return Send(new IpcDataStream(p_Stream, false), p_TimeOut);
        }

        public Stream Send(IIpcData p_Data, TimeSpan p_TimeOut)
        {
            IpcClientSocket l_Socket = GetSocket();
            try
            {
                Stream l_Stream = l_Socket.Send(p_Data, p_TimeOut);

                if (l_Stream is IpcSocketStream)
                {
                    // If its an SocketStream the connection is still open so we can only reuse the connection after
                    // the Stream is closed
                    return new IpcClientStream(l_Stream, () =>
                    {
                        ReleaseSocketBag(l_Socket);
                    });
                }
                else
                {
                    ReleaseSocketBag(l_Socket);
                    return l_Stream;
                }
                
            }
            catch (Exception)
            {
                ReleaseSocketBag(l_Socket);
                throw;
            }
        }

        private void ReleaseSocketBag(IpcClientSocket p_Socket)
        {
            if (p_Socket.SocketState == IpcClientSocketState.Closed)
                return;

            if (p_Socket.SocketState != IpcClientSocketState.Idle && p_Socket.SocketState != IpcClientSocketState.SendingPing)
            {
                m_Logger.Warning($"Socket With Invalid State push back. Maybe there is somewhere an memory leak. ChannelName {m_ChannelName} Name {p_Socket.ChannelName}, State {p_Socket.SocketState}");
            }
            
            m_Sockets.Add(p_Socket);
        }

        private IpcClientSocket GetSocket()
        {
            IpcClientSocket l_Socket = null;

            while (!m_Sockets.IsEmpty)
            {
                if (!m_Sockets.TryTake(out l_Socket))
                    continue;

                if (l_Socket.SocketState == IpcClientSocketState.Idle)
                    break;

                if (l_Socket.SocketState == IpcClientSocketState.Closed)
                    continue;

                if (l_Socket.SocketState == IpcClientSocketState.SendingPing)
                    continue;

                m_Logger.Warning($"Socket With Invalid State found. Maybe there is somewhere an memory leak. ChannelName {m_ChannelName} Name {l_Socket.ChannelName}, State {l_Socket.SocketState}");
                m_Sockets.Add(l_Socket);
            }

            if (l_Socket != null)
                return l_Socket;

            l_Socket = new IpcClientSocket(m_ChannelFactory);
            l_Socket.Connect(m_ChannelName, m_Options.ConnectTimeOut);
            return l_Socket;
        }

        public void Dispose()
        {
            while (!m_Sockets.IsEmpty)
            {
                IpcClientSocket l_Socket;

                if (m_Sockets.TryTake(out l_Socket))
                    l_Socket.Dispose();
            }
        }
    }

}
