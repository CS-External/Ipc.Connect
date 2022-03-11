using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipc.Connect.Client;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Server;
using Ipc.Connect.Test.Classes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipc.Connect.Test
{
    [TestClass]
    public class IpcClientTest
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
        public void ClientLargetMessageTest()
        {
            IpcServer l_Server = new IpcServer(m_ChannelFactory);
            Assert.IsFalse(l_Server.IsListing());
            l_Server.Listen("ClientTest", new IpcServerRequestHandlerDelegate(RemoteCallHandler));

            using (IpcClient l_Client = new IpcClient(m_ChannelFactory, "ClientTest"))
            {
                

                for (int i = 0; i < 10; i++)
                {
                    DataGeneratorStream l_DataGenerator = new DataGeneratorStream(100 * 1024 * 1024);

                    using (Stream l_ResponseStream = l_Client.Send(l_DataGenerator, TimeSpan.FromSeconds(2)))
                    {
                        using (MemoryStream l_Stream = new MemoryStream())
                        {
                            l_ResponseStream.CopyTo(l_Stream);
                            Assert.AreEqual(100 * 1024 * 1024, l_Stream.Length);
                        }
                    }
                }

                Assert.AreEqual(1, l_Client.PooledConnectionCount);
            }
        }

        [TestMethod]
        public void ClientTest()
        {
            IpcServer l_Server = new IpcServer(m_ChannelFactory);
            Assert.IsFalse(l_Server.IsListing());
            l_Server.Listen("ClientTest", new IpcServerRequestHandlerDelegate(RemoteCallHandler));

            using (IpcClient l_Client = new IpcClient(m_ChannelFactory, "ClientTest"))
            {
                byte[] l_Bytes = Encoding.UTF8.GetBytes("Hello World");

                for (int i = 0; i < 100000; i++)
                {
                    using (Stream l_ResponseStream = l_Client.Send(new IpcDataBytes(l_Bytes), TimeSpan.FromSeconds(2)))
                    {
                        using (StreamReader l_Reader = new StreamReader(l_ResponseStream, Encoding.UTF8, true, -1, true))
                        {
                            Assert.AreEqual("Hello World", l_Reader.ReadToEnd());
                        }

                    }
                }

                Assert.AreEqual(1, l_Client.PooledConnectionCount);
            }
        }

        [TestMethod]
        public void ClientMultiThreadTest()
        {
            ThreadPool.SetMinThreads(50, 25);

            IpcServer l_Server = new IpcServer(m_ChannelFactory);
            Assert.IsFalse(l_Server.IsListing());
            l_Server.Listen("ClientTest", new IpcServerRequestHandlerDelegate(RemoteCallHandler));

            using (IpcClient l_Client = new IpcClient(m_ChannelFactory, "ClientTest"))
            {
                List<Task> l_Tasks = new List<Task>();

                for (int i = 0; i < 10; i++)
                {
                    Task l_Task = new Task(() =>
                    {
                        byte[] l_Bytes = Encoding.UTF8.GetBytes("Hello World");

                        for (int i = 0; i < 100000; i++)
                        {
                            using (Stream l_ResponseStream = l_Client.Send(new IpcDataBytes(l_Bytes), TimeSpan.FromSeconds(2)))
                            {
                                using (StreamReader l_Reader = new StreamReader(l_ResponseStream, Encoding.UTF8, true, -1, true))
                                {
                                    Assert.AreEqual("Hello World", l_Reader.ReadToEnd());
                                }

                            }
                        }

                    });
                    l_Task.Start();

                    l_Tasks.Add(l_Task);
                }

                Task.WaitAll(l_Tasks.ToArray());

                Assert.AreEqual(10, l_Client.PooledConnectionCount);
            }
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
