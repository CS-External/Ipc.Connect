using System;
using System.IO;
using System.Threading.Tasks;
using Ipc.Connect.Client;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipc.Connect.Test
{



    [TestClass]
    public class IpcServerTest
    {

        private IpcChannelFactory m_ChannelFactory;
        private ILoggerFactory m_Logger;


        [TestInitialize]
        public void Init()
        {

            if (m_Logger == null)
            {
                m_Logger = new LoggerFactory();
                m_Logger.AddProvider(new DebugLoggerProvider());

            }

            m_ChannelFactory = new IpcChannelFactory(m_Logger);
            

        }

        [TestMethod]
        public void TwoConnectionsTest()
        {
            IpcServer l_Server = new IpcServer(m_ChannelFactory);
            Assert.IsFalse(l_Server.IsListing());
            l_Server.Listen("Test1234", new IpcServerRequestHandlerDelegate(RemoteCallHandler));
            Assert.IsTrue(l_Server.IsListing());

            Assert.AreEqual(0, l_Server.GetConnections().Count);

            IpcClientSocket l_Client = new IpcClientSocket(m_ChannelFactory);
            l_Client.Connect("Test1234", TimeSpan.FromSeconds(2));
            Assert.AreEqual(IpcClientSocketState.Idle, l_Client.SocketState);
            Assert.AreEqual(1, l_Server.GetConnections().Count);

            IpcClientSocket l_Client2 = new IpcClientSocket(m_ChannelFactory);
            l_Client2.Connect("Test1234", TimeSpan.FromSeconds(2));
            Assert.AreEqual(IpcClientSocketState.Idle, l_Client2.SocketState);
            Assert.AreEqual(2, l_Server.GetConnections().Count);

            l_Client.Close();
            Assert.AreEqual(IpcClientSocketState.Closed, l_Client.SocketState);
            Task.Delay(100).Wait();
            Assert.AreEqual(1, l_Server.GetConnections().Count);

            l_Server.Close();
            Assert.IsFalse(l_Server.IsListing());
            Task.Delay(m_ChannelFactory.IdleTimeOut.Add(TimeSpan.FromSeconds(1))).Wait();
            Assert.AreEqual(IpcClientSocketState.Closed, l_Client2.SocketState);

        }

        private IIpcData RemoteCallHandler(Stream p_Stream)
        {
            MemoryStream l_Stream = new MemoryStream();

            p_Stream.CopyTo(l_Stream);
            l_Stream.Position = 0;

            return new IpcDataStream(l_Stream, true);

        }
    }
}
