using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlenderBridge.Settings
{
    public class HealthMonitorWindow : EditorWindow
    {
        const string Menu = "Window/MeshSync RenderEngine/";
        const string Title = "Health Monitor";
        const string IconPath = "Packages/com.ohmnivore.meshsyncrenderengine/Unity/Icons/Logo.png";
        static readonly Vector2 MinSize = new Vector2(320f, 240f);

        // Default reference should be set to "Packages/com.ohmnivore.meshsyncrenderengine/Unity/Settings/HealthMonitor.uss" in the inspector
        [SerializeField]
        StyleSheet StyleSheet;

        ScrollView m_ScrollView;
        
        [MenuItem(Menu + Title, priority = 200)]
        static void Open()
        {
            SetupWindow(CreateWindow<HealthMonitorWindow>());
        }

        static void SetupWindow(HealthMonitorWindow window)
        {
            var cachedContent = EditorGUIUtility.IconContent(IconPath);
            window.titleContent = new GUIContent(Title, cachedContent.image);

            window.minSize = MinSize;

            window.Show();
        }

        readonly List<HealthMonitorTargetElement> m_Elements = new List<HealthMonitorTargetElement>();
        bool m_OnMonitorChangedRegistered;

        void OnDestroy()
        {
            if (m_OnMonitorChangedRegistered)
                Bridge.instance.Monitor.OnChange -= OnMonitorChanged;
        }

        void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(StyleSheet);

            m_ScrollView = new ScrollView(ScrollViewMode.Vertical);
            rootVisualElement.Add(m_ScrollView);

            OnMonitorChanged();

            Bridge.instance.Monitor.OnChange += OnMonitorChanged;
            m_OnMonitorChangedRegistered = true;
        }
        
        void Update()
        {
            foreach (var element in m_Elements)
            {
                element.Update(); 
            }
        }

        void OnMonitorChanged()
        {
            m_Elements.Clear();
            m_ScrollView.Clear();
            foreach (var target in Bridge.instance.Monitor.Targets)
            {
                var element = new HealthMonitorTargetElement();
                element.Target = target;
                
                m_Elements.Add(element);
                m_ScrollView.Add(element);
            }
        }
    }
}
