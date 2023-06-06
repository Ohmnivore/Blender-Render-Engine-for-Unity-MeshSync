using System;

namespace BlenderBridge.Comms
{
    public struct Message
    {
        public byte Type => m_Type;
        public ReadOnlyMemory<byte> Data => new ReadOnlyMemory<byte>(m_Data);

        byte m_Type;
        byte[] m_Data;

        internal static Message From(byte Type, byte[] data)
        {
            return new Message() { m_Type = Type, m_Data = data };
        }

        internal static Message From(byte Type, ReadOnlyMemory<byte> data)
        {
            return new Message() { m_Type = Type, m_Data = data.ToArray() };
        }
    }

    public interface IMessage
    {
        byte Type { get; }
    }

    public interface IMessageHandle
    {
        bool TryParse<T>(out T jsonMessage) where T : IMessage;
    }
}
