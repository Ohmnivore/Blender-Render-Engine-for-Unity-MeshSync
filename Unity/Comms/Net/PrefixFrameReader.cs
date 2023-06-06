using System;
using System.Net;

namespace BlenderBridge.Comms.Net
{
    public class PrefixFrameReader : IFrameReader
    {
        public Action<ReadOnlyMemory<byte>> Received { get; set; } = delegate { };

        bool m_GatheringMsg;
        int m_MsgLength;
        int m_MsgBytesProcessed;
        byte[] m_MsgBuffer;

        int m_PrefixBytesProcessed;
        byte[] m_PrefixBuffer = new byte[4];

        public PrefixFrameReader()
        {

        }

        public void Process(ReadOnlyMemory<byte> data)
        {
            var dataLength = data.Length;
            var bytesProcessed = 0;

            while (bytesProcessed != dataLength)
            {
                if (!m_GatheringMsg)
                {
                    var prefixBytesRemaining = 4 - m_PrefixBytesProcessed;
                    var bytesRemaining = dataLength - bytesProcessed;
                    var readLength = Math.Min(bytesRemaining, prefixBytesRemaining);

                    if (bytesProcessed + readLength > data.Length)
                        throw new DataMisalignedException($"Message framing error, processed {bytesProcessed + readLength} bytes for data length {data.Length}");

                    if (m_PrefixBytesProcessed + readLength > 4)
                        throw new DataMisalignedException($"Message framing error, processed {m_PrefixBytesProcessed + readLength} bytes for message length {4}");

                    data.Slice(bytesProcessed, readLength).CopyTo(new Memory<byte>(m_PrefixBuffer, m_PrefixBytesProcessed, readLength));

                    bytesProcessed += readLength;
                    m_PrefixBytesProcessed += readLength;

                    if (m_PrefixBytesProcessed == 4)
                    {
                        m_MsgLength = BitConverter.ToInt32(m_PrefixBuffer);
                        m_MsgLength = IPAddress.NetworkToHostOrder(m_MsgLength);
                        m_MsgBytesProcessed = 0;
                        m_MsgBuffer = new byte[m_MsgLength];
                        m_GatheringMsg = true;
                    }
                }

                var bytesRemaining2 = dataLength - bytesProcessed;
                var msgBytesRemaining = m_MsgLength - m_MsgBytesProcessed;
                var readLength2 = Math.Min(bytesRemaining2, msgBytesRemaining);

                if (bytesProcessed + readLength2 > data.Length)
                    throw new DataMisalignedException($"Message framing error, processed {bytesProcessed + readLength2} bytes for data length {data.Length}");

                if (m_MsgBytesProcessed + readLength2 > m_MsgLength)
                    throw new DataMisalignedException($"Message framing error, processed {m_MsgBytesProcessed + readLength2} bytes for message length {m_MsgLength}");

                data.Slice(bytesProcessed, readLength2).CopyTo(new Memory<byte>(m_MsgBuffer, m_MsgBytesProcessed, readLength2));

                bytesProcessed += readLength2;
                m_MsgBytesProcessed += readLength2;

                if (m_MsgBytesProcessed == m_MsgLength)
                {
                    m_PrefixBytesProcessed = 0;
                    m_GatheringMsg = false;
                    Received?.Invoke(m_MsgBuffer);
                }
            }
        }
    }
}
