using System;

namespace BlenderBridge.Comms.Net
{
    public interface IServer : IDisposable
    {
        Action ClientConnected { get; set; }
        Action ClientDisconnected { get; set; }
        Action<Message> Received { get; set; }

        void Start(int port);
        void Send(Message message);
    }
}
