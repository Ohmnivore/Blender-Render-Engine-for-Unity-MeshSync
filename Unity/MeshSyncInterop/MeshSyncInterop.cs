using System.Reflection;
using Unity.MeshSync;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlenderBridge.MeshSyncInterop
{
    public class MeshSyncInterop : ScriptableSingleton<MeshSyncInterop>
    {
        public Transform RootObject => m_Server != null ? m_RootFieldInfo.GetValue(m_Server) as Transform : null;
        
        [SerializeField]
        MeshSyncServer m_Server;

        FieldInfo m_RootFieldInfo;
        
        void OnEnable()
        {
            EditorApplication.update += Update;

            SetupReflection();
            FindServer();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        void Update()
        {
            FindServer();
        }

        void SetupReflection()
        {
            m_RootFieldInfo = typeof(BaseMeshSync).GetField("m_rootObject", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(m_RootFieldInfo != null);
        }

        void FindServer()
        {
            MeshSyncServer[] servers = Object.FindObjectsByType<MeshSyncServer>(FindObjectsSortMode.None);

            var hasOldServer = false;
            foreach (var server in servers)
            {
                if (server == m_Server)
                {
                    hasOldServer = true;
                    break;
                }
            }

            if (!hasOldServer && servers.Length > 0)
                m_Server = servers[0];
        }
    }
}
