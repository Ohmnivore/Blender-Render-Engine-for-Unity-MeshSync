using System;

namespace BlenderBridge.Comms.Net
{
    public interface IFrameWriter
    {
        byte[] Process(ReadOnlyMemory<byte> data);
    }
}
