using UnityEngine;
using UnityEditor;
using BlenderBridge.Comms.Net;

namespace BlenderBridge
{
    [InitializeOnLoad]
    public class Bridge : ScriptableSingleton<Bridge>
    {
        static Bridge()
        {
            EditorApplication.delayCall += EntryPoint;
        }

        static void EntryPoint()
        {
            var _ = instance;
        }

        public enum Status
        {
            Disconnected,
            Connected,
        }

        public System.Action ClientConnected { get; set; } = delegate {};
        public System.Action ClientDisconnected { get; set; } = delegate { };
        public System.Action<Comms.IMessageHandle> Received { get; set; } = delegate { };

        public void Send(Comms.IMessage message)
        {
            m_Server.Send(Comms.JSONMessage.ToMessage(message));
        }

        public Settings.HealthMonitor Monitor => m_Monitor;
        public Status AppStatus => m_Status;

        Server<PrefixFrameWriter, PrefixFrameReader> m_Server;

        Settings.HealthMonitor m_Monitor;

        [SerializeField]
        ViewLink.Manager m_ViewManager;

        [SerializeField]
        Status m_Status;

        Bridge()
        {
            m_Monitor = new Settings.HealthMonitor();
            
            m_Status = Status.Disconnected;

            m_Server = new Server<PrefixFrameWriter, PrefixFrameReader>();
            m_Server.ClientConnected += OnClientConnected;
            m_Server.ClientDisconnected += OnClientDisconnected;
            m_Server.Received += OnReceived;

            m_ViewManager = new ViewLink.Manager(this);

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            m_Monitor.Register(m_Server);
            m_Monitor.Register(m_ViewManager);
        }

        void OnEnable() 
        {
            m_Server.Start(Settings.Settings.instance.ServerPort);
        }

        void OnClientConnected()
        {
            m_Status = Status.Connected;
            ClientConnected?.Invoke();

            Send(new Heartbeat());
        }

        void OnClientDisconnected()
        {
            m_Status = Status.Disconnected;
            ClientDisconnected?.Invoke();
        }

        void OnReceived(Comms.Message message)
        {
            Received?.Invoke(new Comms.JSONMessageHandle(message));
        }

        void OnBeforeAssemblyReload()
        {
            Send(new DomainReload());
            
            m_ViewManager.Dispose();
            m_Server.Dispose();
        }
    }
}
