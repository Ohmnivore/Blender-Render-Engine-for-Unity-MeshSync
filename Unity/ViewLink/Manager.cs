using System;
using System.Collections.Generic;
using BlenderBridge.Settings;
using UnityEditor;
using UnityEngine;

namespace BlenderBridge.ViewLink
{
    [Serializable]
    public class Manager : HealthMonitor.ITarget
    {
        class InvalidViewIDException : Exception
        {
            public InvalidViewIDException(string viewID)
                : base($"Invalid view ID: {viewID}")
            {

            }
        }

        [Serializable]
        struct Item
        {
            [SerializeField]
            public ViewState State;

            [SerializeField]
            public View View;
        }

        [SerializeField]
        List<Item> m_Items = new List<Item>();
        
#if USE_MESH_SYNC
        [SerializeField]
        readonly MeshSyncVisibilityCache m_VisibilityCache = new MeshSyncVisibilityCache();
#endif
        
        public string Name { get; } = "ViewLink";
        public HealthMonitor.Status Status => m_Items.Count > 0 ? HealthMonitor.Status.Active : HealthMonitor.Status.Inactive;
        public HealthMonitor.ILogger Logger { get; set; }
        public IReadOnlyList<HealthMonitor.IToggle> Toggles { get; }
        
        public HealthMonitor.SettingsToggle LogViews { get; } = new HealthMonitor.SettingsToggle("Log Views", "ViewLinkHealth.LogViews");
        public HealthMonitor.SettingsToggle LogObjectVisibility { get; } = new HealthMonitor.SettingsToggle("Log Object Visibility", "ViewLinkHealth.LogObjectVisibility");
        public HealthMonitor.SettingsToggle ShowGameObjects { get; } = new HealthMonitor.SettingsToggle("Show GameObjects", "ViewLinkHealth.ShowGameObjects");

        public Manager(Bridge bridge)
        {
            Toggles = new[] { LogViews, LogObjectVisibility, ShowGameObjects };
            
            bridge.ClientConnected += OnClientConnected;
            bridge.ClientDisconnected += OnClientDisconnected;
            bridge.Received += OnReceived;

            EditorApplication.update += OnUpdate;

#if !USE_SPOUT
            Debug.LogError("[Render Engine for MeshSync]: Dependency KlakSpout needs to be installed manually following the instructions in <a href=\"https://github.com/keijiro/KlakSpout\">https://github.com/keijiro/KlakSpout</a>");
#endif
        }

        public void Dispose()
        {
            EditorApplication.update -= OnUpdate;
        }

        void OnUpdate()
        {
            EditorRefresh.EditorRefresh.SetEnabled(m_Items.Count > 0);

            foreach (var item in m_Items)
            {
                item.View.gameObject.hideFlags = ShowGameObjects.Value ? HideFlags.DontSave : HideFlags.HideAndDontSave;
                item.View.ApplyState(item.State);
            }
            
#if USE_MESH_SYNC
            m_VisibilityCache.Apply();
#endif
        }

        void OnClientConnected()
        {

        }

        void OnClientDisconnected()
        {
            foreach (var item in m_Items)
            {
                if (item.View != null)
                    UnityEngine.Object.DestroyImmediate(item.View.gameObject);
            }

            m_Items.Clear();
        }

        void OnReceived(Comms.IMessageHandle message)
        {
            if (message.TryParse<ViewUpdated>(out var viewUpdated))
            {
                HandleViewUpdated(viewUpdated);
            }
            else if (message.TryParse<ViewDestroyed>(out var viewDestroyed))
            {
                HandleViewDestroyed(viewDestroyed);
            }
            else if (message.TryParse<ObjectVisibility>(out var objectVisibility))
            {
                HandleObjectVisibility(objectVisibility);
            }
        }

        void HandleViewUpdated(ViewUpdated msg)
        {
            var idx = m_Items.FindIndex(x => x.State.ID == msg.ID);
            if (idx >= 0)
            {
                var item = m_Items[idx];
                ApplyUpdate(msg, ref item.State);
                m_Items[idx] = item;

                DebugLog($"Updated view {item.State.ID}", LogViews.Value);
            }
            else
            {
                var state = new ViewState();
                ApplyUpdate(msg, ref state);

                var go = new GameObject();
                go.name = $"Blender View {state.ID}";
                go.hideFlags = HideFlags.HideAndDontSave;
                var view = go.AddComponent<View>();

                m_Items.Add(new Item { State = state, View = view });

                DebugLog($"Created view {state.ID}", LogViews.Value);
                
                EditorApplication.QueuePlayerLoopUpdate(); 
            }
        }

        void HandleViewDestroyed(ViewDestroyed msg)
        {
            var idx = m_Items.FindIndex(x => x.State.ID == msg.ID);
            if (idx >= 0)
            {
                var item = m_Items[idx];

                if (item.View != null)
                    UnityEngine.Object.DestroyImmediate(item.View.gameObject);

                m_Items.RemoveAt(idx);

                DebugLog($"Destroyed view {item.State.ID}", LogViews.Value);
            }
            // Sync is not 100% perfect: don't throw for now
            //else
            //    throw new InvalidViewIDException(msg.ID);
        }

        void HandleObjectVisibility(ObjectVisibility msg)
        {
#if USE_MESH_SYNC
            if (LogObjectVisibility.Value)
            {
                var str = $"{msg.Name}: {(msg.Obsolete ? "Obsolete" : (msg.Visible ? "Visible" : "Hidden"))}";
                DebugLog(str);
            }
            
            m_VisibilityCache.HandleMessage(msg);
#endif
        }

        void ApplyUpdate(ViewUpdated msg, ref ViewState state)
        {
            state.ID = msg.ID;
            state.Width = msg.Width;
            state.Height = msg.Height;
            state.IsPerspective = msg.IsPerspective;

            state.ViewPose = CoordinateSystem.BlenderViewMatrixToUnityPose(msg.ConvertedViewMatrix);
            state.WindowMatrix = msg.ConvertedWindowMatrix;
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
