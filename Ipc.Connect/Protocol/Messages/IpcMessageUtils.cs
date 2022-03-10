using System;

namespace Ipc.Connect.Protocol.Messages
{
    public static class IpcMessageUtils
    {
        public const int CONST_INTERNAL_IPC_LIB_HEADER_SIZE = 8;

        public const int CONST_MESSAGE_HEADER_SIZE = 1 + sizeof(int);

        public static int SerializeMessage(IpcMessage p_Message, Byte[] p_Target)
        {
            p_Target[0] = (byte)p_Message.Kind;
            byte[] l_Bytes = BitConverter.GetBytes(p_Message.ContentLength);
            p_Target[1] = l_Bytes[0];
            p_Target[2] = l_Bytes[1];
            p_Target[3] = l_Bytes[2];
            p_Target[4] = l_Bytes[3];

            if (p_Message.ContentLength < 1)
            {
                return CONST_MESSAGE_HEADER_SIZE;
            }

            p_Message.Content.CopyTo(new Span<byte>(p_Target, CONST_MESSAGE_HEADER_SIZE, p_Target.Length - CONST_MESSAGE_HEADER_SIZE));

            return CONST_MESSAGE_HEADER_SIZE + p_Message.ContentLength;
        }

        public static IpcMessage ParseMessage(ReadOnlySpan<byte> p_Data)
        {
            if (p_Data.Length < CONST_MESSAGE_HEADER_SIZE)
            {
                return new IpcMessage(IpcMessageKind.Invalid, 0, ReadOnlySpan<byte>.Empty);
            }
                

            IpcMessageKind l_Kind = (IpcMessageKind)p_Data[0];
            int l_ContentLength = BitConverter.ToInt32(p_Data.Slice(1));

            if (l_ContentLength > 0)
                return new IpcMessage(l_Kind, l_ContentLength, p_Data.Slice(CONST_MESSAGE_HEADER_SIZE, l_ContentLength));
                
            
            return new IpcMessage(l_Kind, 0, ReadOnlySpan<byte>.Empty);
        }
    }
}
