using System;

namespace Ipc.Connect.Protocol.Messages
{
    public readonly ref struct IpcMessage
    {
        public IpcMessage(IpcMessageKind p_Kind, int p_ContentLength, ReadOnlySpan<byte> p_Content)
        {
            Kind = p_Kind;
            ContentLength = p_ContentLength;
            Content = p_Content;
        }

        public IpcMessageKind Kind { get; }
        public int ContentLength { get; }
        
        public ReadOnlySpan<Byte> Content { get;}
    }
}
