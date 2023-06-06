using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BlenderBridge.Settings;

namespace BlenderBridge.Comms.Net
{
    public class Server<TWriter, TReader> : IServer, HealthMonitor.ITarget
        where TWriter : IFrameWriter, new()
        where TReader : IFrameReader, new()
    {
        const int k_MaxPendingConnections = 64;
        const int k_ReEntryPreventionTimeMS = 50;

        public string Name { get; } = "Server";
        public HealthMonitor.Status Status => m_Connected ? HealthMonitor.Status.Active : HealthMonitor.Status.Inactive;
        public HealthMonitor.ILogger Logger { get; set; }
        public IReadOnlyList<HealthMonitor.IToggle> Toggles { get; }

        public Action ClientConnected { get; set; } = delegate {};
        public Action ClientDisconnected { get; set; } = delegate {};
        public Action<Message> Received { get; set; } = delegate {};

        public HealthMonitor.SettingsToggle LogConnection { get; } = new HealthMonitor.SettingsToggle("Log Connection", "ServerHealth.LogConnection");
        public HealthMonitor.SettingsToggle LogSent { get; } = new HealthMonitor.SettingsToggle("Log Sent", "ServerHealth.LogSent");
        public HealthMonitor.SettingsToggle LogReceived { get; } = new HealthMonitor.SettingsToggle("Log Received", "ServerHealth.LogReceived");

        int m_Port;
        Socket m_Socket;
        Socket m_ClientSocket;
        bool m_Connected;
        TWriter m_FrameWriter = new TWriter();
        TReader m_FrameReader = new TReader();

        public Server()
        {
            Toggles = new[] { LogConnection, LogSent, LogReceived };

            m_FrameReader.Received = OnFrameRead;
        }

        public void Dispose()
        {
            DisposeClientSocket();
            DisposeServerSocket();
        }

        void DisposeClientSocket()
        {
            if (m_ClientSocket != null)
            {
                m_Connected = false;
                ClientDisconnected?.Invoke();
                if (m_ClientSocket.Connected)
                {
                    m_ClientSocket.Shutdown(SocketShutdown.Both);
                    DebugLog("m_ClientSocket.Shutdown", LogConnection.Value);
                    m_ClientSocket.Disconnect(false);
                    DebugLog("m_ClientSocket.Disconnect", LogConnection.Value);
                }
                m_ClientSocket.Close();
                DebugLog("m_ClientSocket.Close", LogConnection.Value);
                m_ClientSocket = null;
            }
        }

        void DisposeServerSocket()
        {
            if (m_Socket != null)
            {
                if (m_Socket.Connected)
                {
                    m_Socket.Shutdown(SocketShutdown.Both);
                    DebugLog("m_Socket.Shutdown", LogConnection.Value);
                    m_Socket.Disconnect(false);
                    DebugLog("m_Socket.Disconnect", LogConnection.Value);
                }
                m_Socket.Close();
                DebugLog("m_Socket.Close", LogConnection.Value);
                m_Socket = null;
            }
        }

        public void Start(int port)
        {
            m_Port = port;
            var endPoint = new IPEndPoint(IPAddress.Loopback, m_Port);

            m_Socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            m_Socket.Blocking = true;
            m_Socket.NoDelay = true;
            m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            m_Socket.LingerState = new LingerOption(false, 0);
            m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            m_Socket.Bind(endPoint);
            m_Socket.Listen(k_MaxPendingConnections);

            AcceptLoop();
            ReceiveLoop();
        }

        async void AcceptLoop()
        {
            while (true)
            {
                Socket newClientSocket;
                try
                {
                    newClientSocket = await m_Socket.AcceptAsync();
                }
                // Low budget cancellation token
                catch (ObjectDisposedException)
                {
                    await Task.Delay(k_ReEntryPreventionTimeMS); // Prevent hanging from re-entry
                    continue;
                }
                catch (NullReferenceException)
                {
                    await Task.Delay(k_ReEntryPreventionTimeMS);
                    continue;
                }

                if (m_ClientSocket != null)
                    DisposeClientSocket();

                m_ClientSocket = newClientSocket;
                m_ClientSocket.Blocking = true;
                DebugLog($"New client: {m_ClientSocket.RemoteEndPoint}", LogConnection.Value);
                m_Connected = true;
                ClientConnected?.Invoke();
            }
        }

        async void ReceiveLoop()
        {
            var buffer = new byte[8192];

            while (true)
            {
                if (m_ClientSocket == null)
                {
                    await Task.Delay(k_ReEntryPreventionTimeMS); // Prevent hanging from re-entry
                    continue;
                }

                if (!m_ClientSocket.Connected)
                {
                    OnClientDisconnected();
                    await Task.Delay(k_ReEntryPreventionTimeMS); // Prevent hanging from re-entry
                    continue;
                }

                int size = 0;

                try
                {
                    size = await m_ClientSocket.ReceiveAsync(buffer, SocketFlags.None);
                }
                catch (ObjectDisposedException)
                {
                    await Task.Delay(k_ReEntryPreventionTimeMS);
                    continue;
                }
                catch (SocketException)
                {
                    await Task.Delay(k_ReEntryPreventionTimeMS);
                    continue;
                }

                if (size > 0)
                {
                    m_FrameReader.Process(new ReadOnlyMemory<byte>(buffer, 0, size));
                }
                else
                {
                    // Receiving size 0 means the other side has closed the socket
                    OnClientDisconnected();
                    await Task.Delay(k_ReEntryPreventionTimeMS); // Prevent hanging from re-entry
                }
            }
        }

        void OnFrameRead(ReadOnlyMemory<byte> data)
        {
            var messageType = data.Span[0];

            Received?.Invoke(Message.From(messageType, data.Slice(1)));

            if (LogReceived.Value)
            {
                var str = Encoding.UTF8.GetString(data.Slice(1).Span);
                DebugLog($"Received type {messageType}: {str}");
            }
        }

        public void Send(Message message)
        {
            if (!CheckSocket())
                return;

            var messageWithType = Utils.BytePrefix(message.Type, message.Data);
            SendInternal(messageWithType);

            if (LogSent.Value)
            {
                var str = Encoding.UTF8.GetString(message.Data.Span).Trim();
                DebugLog($"Sent: {str}");
            }
        }

        void SendInternal(ReadOnlyMemory<byte> data)
        {
            var framed = m_FrameWriter.Process(data);
            int sent = 0;

            try
            {
                while (sent < framed.Length)
                {
                    var justSent = m_ClientSocket.Send(framed, sent, framed.Length - sent, SocketFlags.None);
                    sent += justSent;
                }
            }
            catch(SocketException)
            {

            }
        }

        bool CheckSocket()
        {
            if (m_ClientSocket == null || !m_ClientSocket.Connected)
                return false;

            return true;
        }

        void OnClientDisconnected()
        {
            DisposeClientSocket();
        }

        void DebugLog(string str, bool condition = true)
        {
            if (condition)
            {
                Logger?.Log(this, str);
            }
        }
    }
}
