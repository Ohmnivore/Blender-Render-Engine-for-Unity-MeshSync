using System;
using BlenderBridge.ViewLink.CameraFactory;
using UnityEditor;
using UnityEngine;

namespace BlenderBridge.Settings
{
    [FilePath("ProjectSettings/MeshSyncRenderEngine.asset", FilePathAttribute.Location.ProjectFolder)]
    public class Settings : ScriptableSingleton<Settings>
    {
        public const int DefaultServerPort = 51451;
        
        [Serializable]
        struct HealthMonitor
        {
            public ServerHealth ServerHealth;
            public ViewLinkHealth ViewLinkHealth;
        }
        
        [Serializable]
        struct ServerHealth
        {
            public bool LogConnection;
            public bool LogSent;
            public bool LogReceived;
        }
        
        [Serializable]
        struct ViewLinkHealth
        {
            public bool LogViews;
            public bool LogObjectVisibility;
            public bool ShowGameObjects;
        }

        public int ServerPort => m_ServerPort;
        
        [SerializeField]
        int m_ServerPort = DefaultServerPort;

        [SerializeField]
#pragma warning disable 0414
        HealthMonitor m_HealthMonitor;
#pragma warning restore 0414

        [SerializeField]
        CameraFactory m_CameraFactory;

        CameraFactory m_DefaultCameraFactory;

        void Awake()
        {
            hideFlags &= ~HideFlags.NotEditable;
        }

        void OnEnable()
        {
            hideFlags &= ~HideFlags.NotEditable;
        }

        public void Modify()
        {
            Save(true);
        }

        public void Reset()
        {
            m_ServerPort = DefaultServerPort;
            m_HealthMonitor = new HealthMonitor();
            Modify();
        }

        public CameraFactory CameraFactory
        {
            get
            {
                if (m_CameraFactory == null)
                {
#if HDRP
                    if (m_DefaultCameraFactory == null)
                        m_DefaultCameraFactory = ScriptableObject.CreateInstance<HDRPCameraFactory>();
#elif URP
                    if (m_DefaultCameraFactory == null)
                        m_DefaultCameraFactory = ScriptableObject.CreateInstance<URPCameraFactory>();
#else
                    Debug.Assert(false, "Unsupported render pipeline");
#endif
                    
                    return m_DefaultCameraFactory;
                }

                return m_CameraFactory;
            }
        }
    }
}
