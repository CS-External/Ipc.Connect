using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Ipc.Connect.Test.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipc.Connect.Test
{
    [TestClass]
    public class DataGeneratorStreamTest
    {
        [TestMethod]
        public void Test()
        {
            DataGeneratorStream l_Stream = new DataGeneratorStream(10 * 1024 * 1024);
            l_Stream.Position = 0;

            MemoryStream l_Target1 = new MemoryStream();
            l_Stream.CopyTo(l_Target1);

            l_Stream.Position = 0;
            MemoryStream l_Target2 = new MemoryStream();
            l_Stream.CopyTo(l_Target2);

            Assert.AreEqual(l_Stream.Length, l_Target1.Length);
            Assert.AreEqual(l_Stream.Length, l_Target2.Length);

            Assert.IsTrue(Enumerable.SequenceEqual(l_Target1.ToArray(), l_Target2.ToArray()));

        }

        [TestMethod]
        public void Test2()
        {
            DataGeneratorStream l_Stream = new DataGeneratorStream(10 * 1024 * 1024);

            long l_Total = 0;
            Byte l_Current = Byte.MinValue;

            while (true)
            {
                if (l_Current == Byte.MaxValue)
                {
                    Debugger.Break();
                }

                int l_ReadByte = l_Stream.ReadByte();

                if (l_ReadByte < 0)
                    break;

                Assert.AreEqual(l_Current, l_ReadByte);
                unchecked
                {
                    l_Current++;
                }

                
                l_Total++;
            }

            Assert.AreEqual(l_Total, l_Stream.Position);

        }

    }
}
