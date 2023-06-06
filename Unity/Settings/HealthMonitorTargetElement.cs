using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlenderBridge.Settings
{
    public class HealthMonitorTargetElement : VisualElement
    {
        public const string StatusIconClass = "status-icon";
        public const string StatusIconInactiveClass = "status-icon-inactive";
        public const string StatusIconActiveClass = "status-icon-active";
        
        public new class UxmlFactory : UxmlFactory<HealthMonitorTargetElement, UxmlTraits> { }

        HealthMonitor.ITarget m_Target;
        readonly Foldout m_Foldout;
        readonly VisualElement m_StatusIcon;
        readonly List<Toggle> m_Toggles = new List<Toggle>();
        
        public HealthMonitorTargetElement()
        {
            m_Foldout = new Foldout();
            Add(m_Foldout);

            m_StatusIcon = new VisualElement();
            m_StatusIcon.AddToClassList(StatusIconClass);

            var foldoutRow = m_Foldout.Q<VisualElement>(null, "unity-foldout__input");
            foldoutRow.Insert(1, m_StatusIcon);
        }

        public HealthMonitor.ITarget Target
        {
            get => m_Target;
            set
            {
                if (value == m_Target)
                    return;
                
                m_Target = value;

                foreach (var toggle in m_Target.Toggles)
                {
                    var uiToggle = new Toggle(toggle.Name);
                    uiToggle.userData = toggle;
                    uiToggle.RegisterCallback<ChangeEvent<bool>>(OnToggle);
                    m_Toggles.Add(uiToggle);
                    m_Foldout.Add(uiToggle);
                }
                
                Update();
            }
        }

        public void Update()
        {
            var toggles = m_Target.Toggles;
            Debug.Assert(toggles.Count == m_Toggles.Count);
            
            m_Foldout.text = m_Target.Name;

            SetClassList(m_StatusIcon, StatusIconInactiveClass, m_Target.Status == HealthMonitor.Status.Inactive);
            SetClassList(m_StatusIcon, StatusIconActiveClass, m_Target.Status == HealthMonitor.Status.Active);

            for (var i = 0; i < toggles.Count && i < m_Toggles.Count; i++)
            {
                m_Toggles[i].SetValueWithoutNotify(toggles[i].Value);
            }
        }

        void OnToggle(ChangeEvent<bool> evt)
        {
            var toggle = evt.target as VisualElement;
            var toggleTarget = toggle.userData as HealthMonitor.IToggle;
            toggleTarget.Value = evt.newValue;
        }

        static void SetClassList(VisualElement e, string className, bool enabled)
        {
            if (enabled)
                e.AddToClassList(className);
            else
                e.RemoveFromClassList(className);
        }
    }
}
