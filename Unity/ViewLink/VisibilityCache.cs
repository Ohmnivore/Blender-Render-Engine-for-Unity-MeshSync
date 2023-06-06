using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlenderBridge.ViewLink
{
    [Serializable]
    class BaseVisibilityCache
    {
        protected class Entry
        {
            // Object names are guaranteed to be unique in Blender
            public string Name;
            public bool Visible;
        }

        [SerializeField]
        protected readonly List<Entry> m_Entries = new List<Entry>();

        [SerializeField]
        protected int m_Version;
        
        public void HandleMessage(ObjectVisibility msg)
        {
            var idx = m_Entries.FindIndex(x => x.Name == msg.Name);
            if (idx >= 0)
            {
                var entry = m_Entries[idx];
                
                if (msg.Obsolete)
                {
                    SetVisibility(entry.Name, true);
                    m_Entries.RemoveAt(idx);
                }
                else
                {
                    entry.Visible = msg.Visible;
                }
            }
            else if (!msg.Obsolete)
            {
                var entry = new Entry()
                {
                    Name = msg.Name,
                    Visible = msg.Visible,
                };
                
                m_Entries.Add(entry);
            }
        }
        
        public void Apply()
        {
            foreach (var entry in m_Entries)
            {
                SetVisibility(entry.Name, entry.Visible);
            }
        }

        protected virtual void SetVisibility(string name, bool visible)
        {
            
        }
    }
    
#if USE_MESH_SYNC
    class MeshSyncVisibilityCache : BaseVisibilityCache
    {
        protected override void SetVisibility(string name, bool visible)
        {
            base.SetVisibility(name, visible);
            
            var root = MeshSyncInterop.MeshSyncInterop.instance.RootObject;
            if (root != null)
            {
                var obj = root.Find(name);
                if (obj != null)
                    obj.gameObject.SetActive(visible);
            }
        }
    }
#endif
}
