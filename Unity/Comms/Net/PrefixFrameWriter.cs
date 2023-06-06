using System;
using System.Net;

namespace BlenderBridge.Comms.Net
{
    public class PrefixFrameWriter : IFrameWriter
    {
        public PrefixFrameWriter()
        {

        }

        public byte[] Process(ReadOnlyMemory<byte> data)
        {
            return Utils.IntPrefix(data.Length, data);
        }
    }
}
