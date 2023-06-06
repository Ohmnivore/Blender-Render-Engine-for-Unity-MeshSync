using System.Text;
using UnityEngine;

namespace BlenderBridge.Comms
{
    public static class JSONMessage
    {
        public static Message ToMessage(IMessage message)
        {
            var text = JsonUtility.ToJson(message);
            var data = Encoding.UTF8.GetBytes(text);

            return Message.From(message.Type, data);
        }

        public static bool TryParse<T>(Message message, out T jsonMessage) where T : IMessage
        {
            jsonMessage = default;

            if (message.Type == jsonMessage.Type)
            {
                var text = Encoding.UTF8.GetString(message.Data.Span);
                jsonMessage = JsonUtility.FromJson<T>(text);
                return true;
            }

            return false;
        }
    }

    public class JSONMessageHandle : IMessageHandle
    {
        Message m_Message;

        internal JSONMessageHandle(Message message)
        {
            m_Message = message;
        }

        bool IMessageHandle.TryParse<T>(out T jsonMessage)
        {
            return JSONMessage.TryParse(m_Message, out jsonMessage);
        }
    }
}
