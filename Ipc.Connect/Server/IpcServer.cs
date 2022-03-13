using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Exceptions;
using Ipc.Connect.Protocol.Messages;
using Ipc.Connect.Protocol.Utils;
using Microsoft.Extensions.Logging;

namespace Ipc.Connect.Server
{
    public class IpcServer: IDisposable
    {
        private ILogger m_Logger;
        private IpcChannelFactory m_ChannelFactory;
        private Task m_ListingTask;
        private CancellationTokenSource m_Cancellation;
        private IIpcServerRequestHandler m_Handler;
        private String m_Name;
        private ReaderWriterLockSlim m_ConnectionsLock;
        private List<IpcServerSocket> m_Connections;
        private IpcServerOptions m_Options;

        public IpcServer(IpcChannelFactory p_ChannelFactory): this(p_ChannelFactory, new IpcServerOptions())
        {
        }

        public IpcServer(IpcChannelFactory p_ChannelFactory, IpcServerOptions p_Options)
        {
            m_ChannelFactory = p_ChannelFactory;
            m_Logger = m_ChannelFactory.LoggerFactory.CreateLogger(GetType());
            m_ConnectionsLock = new ReaderWriterLockSlim();
            m_Connections = new List<IpcServerSocket>();
            m_Options = p_Options;
        }

        public void Listen(String p_Name, IIpcServerRequestHandler p_Handler)
        {
            if (IsListing())
                throw new IpcException("Listen is already in Progress");

            m_Name= p_Name;
            m_Handler = p_Handler;
            m_Cancellation = new CancellationTokenSource();
            m_ListingTask = new Task(DoListen, TaskCreationOptions.LongRunning);
            m_ListingTask.Start();
        }

        private void DoListen()
        {
            try
            {
                IpcReceiverChannel l_Channel = m_ChannelFactory.CreateReceiverChannel(m_Name);


                while (!m_Cancellation.IsCancellationRequested)
                {
                    try
                    {
                        IpcMessage l_Message;

                        if (!l_Channel.WaitForMessageSafe(out l_Message, m_Cancellation.Token))
                        {
                            continue;
                        }

                        if (l_Message.Kind == IpcMessageKind.StartHandshake)
                        {
                            string l_ChannelName = Encoding.UTF8.GetString(l_Message.Content);

                            if (String.IsNullOrWhiteSpace(l_ChannelName))
                            {
                                m_Logger.Warning("Message with invalid ChannelName is ignored");
                                continue;
                            }

                            IpcServerSocket l_ServerSocket = null;
                            try
                            {
                                l_ServerSocket = new IpcServerSocket(m_ChannelFactory, m_Handler);
                                l_ServerSocket.FinishHandShakes(l_ChannelName, TimeSpan.FromSeconds(2));

                                AddConnection(l_ServerSocket);
                            }
                            catch (Exception)
                            {
                                if (l_ServerSocket != null)
                                    l_ServerSocket.Dispose();

                                throw;
                            }

                        }
                        else
                        {
                            m_Logger.Warning($"Invalid MessageKing {l_Message.Kind}");
                        }

                    }
                    catch (Exception e)
                    {
                        m_Logger.Error("Error while Listen for new Connections", e);
                    }

                }
            }
            catch (Exception e)
            {
                m_Logger.Error("Error while Listen for new Connections", e);
            }

        }

        private void AddConnection(IpcServerSocket p_ServerSocket)
        {
            m_ConnectionsLock.EnterWriteLock();
            try
            {
                if (m_Options.ConnectionLimit > -1)
                {
                    if (m_Connections.Count >= m_Options.ConnectionLimit)
                        throw new IpcException($"Connection rejected because max ConnectionCount {m_Options.ConnectionLimit} reached");
                }

                m_Connections.Add(p_ServerSocket);
                p_ServerSocket.Closed += ConnectionOnClosed;
            }
            finally
            {
                m_ConnectionsLock.ExitWriteLock();
            }
        }

        private void ConnectionOnClosed(object p_Sender, EventArgs p_Args)
        {
            if (!IsListing())
                return;

            m_ConnectionsLock.EnterWriteLock();
            try
            {
                m_Connections.Remove((IpcServerSocket)p_Sender);
            }
            finally
            {
                m_ConnectionsLock.ExitWriteLock();
            }
        }

        public bool IsListing()
        {
            return m_ListingTask != null;
        }

        public void Close()
        {
            if (!IsListing())
                return;

            m_Cancellation.Cancel();
            Task l_ListingTask = m_ListingTask;
            m_ListingTask = null;
            l_ListingTask.Wait();
            m_Cancellation = null;

            m_ConnectionsLock.EnterWriteLock();
            try
            {
                foreach (IpcServerSocket l_Connection in m_Connections)
                {
                    try
                    {
                        l_Connection.Close();
                    }
                    catch (Exception e)
                    {
                        m_Logger.LogWarning(e, $"Error while try to close Connection {l_Connection.ChannelName}");    
                    }
                }

                m_Connections.Clear();
            }
            finally
            {
                m_ConnectionsLock.ExitWriteLock();
            }

        }

        public List<IpcServerSocket> GetConnections()
        {
            m_ConnectionsLock.EnterReadLock();
            try
            {
                return new List<IpcServerSocket>(m_Connections);
            }
            finally
            {
                m_ConnectionsLock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
