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
    public class IpcPerformanceTest
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

            ThreadPool.SetMinThreads(50, 25);
        }

        [TestMethod]
        public void SmallRequests100000Test()
        {

            byte[] l_Bytes = Encoding.UTF8.GetBytes("Hello World");

            using (IpcServer l_Server = new IpcServer(m_ChannelFactory))
            {
                l_Server.Listen("PerformanceTest", new IpcServerRequestHandlerDelegate(EmptyResponse));

                using (IpcClient l_Client = new IpcClient(m_ChannelFactory, "PerformanceTest"))
                {

                    for (int i = 0; i < 100000; i++)
                    {
                        using (Stream l_ResponseStream = l_Client.Send(new IpcDataBytes(l_Bytes), TimeSpan.FromSeconds(2)))
                        {
                            ReadStreamToEnd(l_ResponseStream);
                        }
                    }

                }
            }

        }

        [TestMethod]
        public void StreamClientToServer10GBTest()
        {

            byte[] l_Bytes = Encoding.UTF8.GetBytes("Hello World");

            using (IpcServer l_Server = new IpcServer(m_ChannelFactory))
            {
                l_Server.Listen("PerformanceTest", new IpcServerRequestHandlerDelegate(EmptyResponse));

                using (IpcClient l_Client = new IpcClient(m_ChannelFactory, "PerformanceTest"))
                {

                    using (Stream l_ResponseStream = l_Client.Send(new DataGeneratorStream(10L * 1024 * 1024 * 1024), TimeSpan.FromSeconds(2)))
                    {
                        ReadStreamToEnd(l_ResponseStream);
                    }

                }
            }

        }

        [TestMethod]
        public void StreamServerToClient10GBTest()
        {

            byte[] l_Bytes = Encoding.UTF8.GetBytes("Hello World");

            using (IpcServer l_Server = new IpcServer(m_ChannelFactory))
            {
                l_Server.Listen("PerformanceTest", new IpcServerRequestHandlerDelegate(Response10GB));

                using (IpcClient l_Client = new IpcClient(m_ChannelFactory, "PerformanceTest"))
                {
                    using (Stream l_ResponseStream = l_Client.Send(new IpcDataEmpty(), TimeSpan.FromSeconds(2)))
                    {
                        ReadStreamToEnd(l_ResponseStream);
                    }

                }
            }

        }

        private IIpcData EmptyResponse(Stream p_Stream)
        {
            ReadStreamToEnd(p_Stream);
            return new IpcDataEmpty();
        }

        private IIpcData Response10GB(Stream p_Stream)
        {
            ReadStreamToEnd(p_Stream);
            return new IpcDataStream(new DataGeneratorStream(10L * 1024 * 1024 * 1024), true);
        }

        private void ReadStreamToEnd(Stream p_Stream)
        {
            Byte[] l_Buffer = new byte[64 * 1024];

            while (p_Stream.Read(l_Buffer, 0, l_Buffer.Length) == l_Buffer.Length)
            {
            }
        }
    }
}
