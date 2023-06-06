using System;
using System.Net;

namespace BlenderBridge.Comms.Net
{
    static class Utils
    {
        public static byte[] BytePrefix(byte prefix, ReadOnlyMemory<byte> data)
        {
            var full = new byte[1 + data.Length];
            full[0] = prefix;

            var span = data.Span;

            for (int i = 0; i < data.Length; i++)
                full[i + 1] = span[i];

            return full;
        }

        public static byte[] IntPrefix(int prefix, ReadOnlyMemory<byte> data)
        {
            var prefixEndian = IPAddress.HostToNetworkOrder(prefix);
            var prefixBytes = BitConverter.GetBytes(prefixEndian);
            var full = new byte[prefixBytes.Length + data.Length];

            var span = data.Span;

            int idx = 0;
            for (int i = 0; i < prefixBytes.Length; i++)
                full[idx++] = prefixBytes[i];
            for (int j = 0; j < data.Length; j++)
                full[idx++] = span[j];

            return full;
        }
    }
}
