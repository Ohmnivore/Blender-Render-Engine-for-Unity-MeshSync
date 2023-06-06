using System.Collections.Generic;
using UnityEditor;

namespace BlenderBridge.Settings
{
    public class HealthMonitor
    {
        public enum Status
        {
            Inactive,
            Active
        }

        public interface IToggle
        {
            string Name { get; }

            bool Value { get; set; }
        }

        public class Toggle : IToggle
        {
            public string Name { get; }

            public bool Value { get; set; }

            public Toggle(string name)
            {
                Name = name;
            }
        }
        
        public class SettingsToggle : IToggle
        {
            public string Name { get; }

            public bool Value
            {
                get
                {
                    Update();
                    return m_Property.boolValue;
                }

                set
                {
                    Update();
                    m_Property.boolValue = value;
                    m_Object.ApplyModifiedProperties();
                }
            }
            
            readonly string[] m_Path;

            SerializedObject m_Object;
            SerializedProperty m_Property;
            
            public SettingsToggle(string name, string field)
            {
                Name = name;
                m_Path = field.Split(".");
            }

            void Update()
            {
                if (m_Object == null)
                {
                    m_Object = new SerializedObject(Settings.instance);
                    UpdateProperty();
                }
                else
                {
                    m_Object.UpdateIfRequiredOrScript();
                    
                    if (m_Property == null)
                        UpdateProperty();
                }
            }

            void UpdateProperty()
            {
                m_Property = m_Object.FindProperty("m_HealthMonitor");
                m_Property = m_Property.FindPropertyRelative(m_Path[0]);
                for (var i = 1; i < m_Path.Length; i++)
                {
                    m_Property = m_Property.FindPropertyRelative(m_Path[i]);
                }
            }
        }
        
        public interface ITarget
        {
            string Name { get; }
            
            Status Status { get; }
            
            ILogger Logger { get; set; }

            IReadOnlyList<IToggle> Toggles { get; }
        }
        
        public interface ILogger
        {
            void Log(ITarget target, string text);
        }

        class UnityConsoleLogger : ILogger
        {
            public void Log(ITarget target, string text)
            {
                UnityEngine.Debug.Log($"{target.Name}: {text}");
            }
        }

        public System.Action OnChange = delegate { };
        
        public IReadOnlyList<ITarget> Targets => m_Targets;

        readonly List<ITarget> m_Targets = new List<ITarget>();

        readonly UnityConsoleLogger m_Logger = new UnityConsoleLogger();

        public HealthMonitor()
        {
            
        }

        public void Register(ITarget target)
        {
            if (!m_Targets.Contains(target))
            {
                m_Targets.Add(target);
                target.Logger = m_Logger;
                OnChange?.Invoke();
            }
        }
        
        public void Unregister(ITarget target)
        {
            if (m_Targets.Contains(target))
            {
                m_Targets.Remove(target);
                OnChange?.Invoke();
            }
        }
    }
}
