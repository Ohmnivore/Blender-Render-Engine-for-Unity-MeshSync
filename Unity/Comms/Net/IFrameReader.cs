using System;

namespace BlenderBridge.Comms.Net
{
    public interface IFrameReader
    {
        Action<ReadOnlyMemory<byte>> Received { get; set; }

        void Process(ReadOnlyMemory<byte> data);
    }
}
