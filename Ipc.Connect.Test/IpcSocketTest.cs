using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ipc.Connect.Client;
using Ipc.Connect.Protocol.Channels;
using Ipc.Connect.Protocol.Commons;
using Ipc.Connect.Protocol.Exceptions;
using Ipc.Connect.Server;
using Ipc.Connect.Test.Classes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipc.Connect.Test
{
    [TestClass]
    public class IpcSocketTest
    {
        private IpcChannelFactory m_ChannelFactory;
        private IpcClientSocket m_Client;
        private IpcServerSocket m_Server;
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
            m_Client = new IpcClientSocket(m_ChannelFactory);
            m_Server = new IpcServerSocket(m_ChannelFactory, new IpcServerRequestHandlerDelegate(RemoteCallHandler));

            m_Client.SocketStateChanged += (p_Sender, p_Args) =>
            {
                if (m_Client.SocketState == IpcClientSocketState.WaitingForConnect)
                    m_Server.FinishHandShakes(m_Client.ChannelName, TimeSpan.FromMilliseconds(2000));
            };
            m_Client.Connect("Test123", TimeSpan.FromMilliseconds(2000));
            Assert.IsTrue(m_Server.IsConnected);
            Assert.AreEqual(m_Client.SocketState, IpcClientSocketState.Idle);

        }

        [TestCleanup]
        public void Cleanup()
        {
            m_Client.Close();
            m_Server.Close();
            m_ChannelFactory = null;
        }


        private IIpcData RemoteCallHandler(Stream p_Stream)
        {
            MemoryStream l_Stream = new MemoryStream();

            p_Stream.CopyTo(l_Stream);
            l_Stream.Position = 0;

            return new IpcDataStream(l_Stream, true);

        }

        [TestMethod]
        public void CloseBecauseOfInterruptedServerTest()
        {

            IpcClientSocket l_Client = new IpcClientSocket(m_ChannelFactory);
            IpcServerSocket l_Server = new IpcServerSocket(m_ChannelFactory, new IpcServerRequestHandlerDelegate(
                p_Stream =>
                {
                    p_Stream.Dispose();
                    return new IpcDataBytes(new[] { Byte.MinValue });
                }));

            l_Client.SocketStateChanged += (p_Sender, p_Args) =>
            {
                if (l_Client.SocketState == IpcClientSocketState.WaitingForConnect)
                    l_Server.FinishHandShakes(l_Client.ChannelName, TimeSpan.FromMilliseconds(2000));
            };
            l_Client.Connect("CloseBecauseOfInterruptedServerTest", TimeSpan.FromMilliseconds(2000));

            try
            {
                l_Client.Send(new DataGeneratorStream(10 * 1024 * 1024), TimeSpan.FromSeconds(2));
                Assert.Fail("should never reached");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(IpcException));
            }
            
            

            Assert.AreEqual(l_Client.SocketState, IpcClientSocketState.Closed);
            Assert.IsFalse(l_Server.IsConnected);
        }


        [TestMethod]
        public void CloseBecauseOfInterruptedClientTest()
        {

            IpcClientSocket l_Client = new IpcClientSocket(m_ChannelFactory);
            IpcServerSocket l_Server = new IpcServerSocket(m_ChannelFactory, new IpcServerRequestHandlerDelegate(RemoteCallHandler));

            l_Client.SocketStateChanged += (p_Sender, p_Args) =>
            {
                if (l_Client.SocketState == IpcClientSocketState.WaitingForConnect)
                    l_Server.FinishHandShakes(l_Client.ChannelName, TimeSpan.FromMilliseconds(2000));
            };
            l_Client.Connect("CloseBecauseOfInterruptedClientTest", TimeSpan.FromMilliseconds(2000));

            Stream l_Stream = l_Client.Send(new DataGeneratorStream(1 * 1024 * 1024), TimeSpan.FromSeconds(2000));
            l_Stream.Dispose();

            Assert.AreEqual(l_Client.SocketState, IpcClientSocketState.Closed);
            Assert.IsFalse(l_Server.IsConnected);
        }

        [TestMethod]
        public void SmallPicesTest()
        {
            int l_Count = 100000;

            for (int i = 0; i < l_Count; i++)
            {
                string l_Chars = "Test " + i;

                byte[] l_Bytes = Encoding.UTF8.GetBytes(l_Chars);
                Stream l_Stream = m_Client.Send(new MemoryStream(l_Bytes), TimeSpan.FromSeconds(2));

                using (StreamReader l_StreamReader = new StreamReader(l_Stream, Encoding.UTF8, false, -1, false))
                {
                    string l_ReadToEnd = l_StreamReader.ReadToEnd();
                    Assert.AreEqual(l_Chars, l_ReadToEnd);
                }


            }

        }


        [TestMethod]
        public void TestManyCallesTest()
        {
            int l_Count = 100000;

            for (int i = 0; i < l_Count; i++)
            {
                string l_Chars = "TestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTest " +
                               i;

                byte[] l_Bytes = Encoding.UTF8.GetBytes(l_Chars);
                Stream l_Stream = m_Client.Send(new MemoryStream(l_Bytes), TimeSpan.FromSeconds(2));

                using (StreamReader l_StreamReader = new StreamReader(l_Stream, Encoding.UTF8, false, -1, false))
                {
                    string l_ReadToEnd = l_StreamReader.ReadToEnd();
                    Assert.AreEqual(l_Chars, l_ReadToEnd);
                }

                
            }

        }

        [TestMethod]
        public void LargePicesTest()
        {
            int l_Count = 100000;

            for (int i = 0; i < l_Count; i++)
            {
                string l_Chars = "TestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTest " +
                               i;

                byte[] l_Bytes = Encoding.UTF8.GetBytes(l_Chars);
                Stream l_Stream = m_Client.Send(new MemoryStream(l_Bytes), TimeSpan.FromSeconds(2));

                using (StreamReader l_StreamReader = new StreamReader(l_Stream, Encoding.UTF8, false, -1, false))
                {
                    string l_ReadToEnd = l_StreamReader.ReadToEnd();
                    Assert.AreEqual(l_Chars, l_ReadToEnd);
                }


            }

        }

        [TestMethod]
        public void Send10MBTest()
        {


            DataGeneratorStream l_Stream = new DataGeneratorStream(10 * 1024 * 1024);
            CheckStreaming(l_Stream);

        }

        [TestMethod]
        public void Send1MBTest()
        {


            DataGeneratorStream l_Stream = new DataGeneratorStream(1 * 1024 * 1024);
            CheckStreaming(l_Stream);

        }

        [TestMethod]
        public void Send100MBTest()
        {
            

            DataGeneratorStream l_Stream = new DataGeneratorStream(100 * 1024 * 1024);
            CheckStreaming(l_Stream);

        }

        [TestMethod]
        public void Send32KbTest()
        {


            DataGeneratorStream l_Stream = new DataGeneratorStream(32 * 1024);
            CheckStreaming(l_Stream);

        }

        [TestMethod]
        public void ClosedByClientTest()
        {

            m_Client.Close();
            Assert.AreEqual(m_Client.SocketState, IpcClientSocketState.Closed);
            Assert.IsFalse(m_Server.IsConnected);

        }

        [TestMethod]
        public void ClosedByServerTest()
        {

            m_Server.Close();
            Task.Delay(m_ChannelFactory.IdleTimeOut.Divide(3)).Wait();
            Assert.AreEqual(m_Client.SocketState, IpcClientSocketState.Closed);
            Assert.IsFalse(m_Server.IsConnected);

        }

        [TestMethod]
        public void KeepAliveWorkingTest()
        {

            Task.Delay(m_ChannelFactory.IdleTimeOut.Add(TimeSpan.FromSeconds(5))).Wait();
            Assert.AreEqual(m_Client.SocketState, IpcClientSocketState.Idle);
            Assert.IsTrue(m_Server.IsConnected);

        }

        [TestMethod]
        public void Send1GBTest()
        {


            DataGeneratorStream l_Stream = new DataGeneratorStream(1 * 1024 * 1024 * 1024);
            CheckStreaming(l_Stream);

        }


        private void CheckStreaming(DataGeneratorStream p_Stream)
        {
            bool l_CheckAll = true;

            Byte[] l_Bytes = new byte[12 * 1024];
            Byte[] l_FileBytes = new byte[12 * 1024];

            long l_Count = 0;

            using (Stream l_Result = m_Client.Send(p_Stream, TimeSpan.FromSeconds(120)))
            {

                p_Stream.Position = 0;

                while (true)
                {

                    int l_Read = l_Result.Read(l_Bytes, 0, l_Bytes.Length);
                    long l_CountBefore = l_Count;
                    l_Count = l_Count + l_Read;

                    if (l_CheckAll)
                    {
                        int l_FilesRead = p_Stream.Read(l_FileBytes, 0, l_FileBytes.Length);

                        Assert.AreEqual(l_Read, l_FilesRead);

                        if (!l_FileBytes.SequenceEqual(l_Bytes))
                        {
                            for (int i = 0; i < l_FileBytes.Length; i++)
                            {
                                if (l_FileBytes[i] != l_Bytes[i])
                                {
                                    Assert.AreEqual(l_FileBytes[i], l_Bytes[i], $"Count {l_CountBefore}, Postion {i}");
                                }
                            }
                        }



                        if (l_FilesRead == 0)
                            break;
                    }

                    if (l_Read == 0)
                        break;
                }
            }

            Assert.AreEqual(p_Stream.Length, l_Count);
            Assert.AreEqual(p_Stream.Length, p_Stream.Position);
        }
    }
}
