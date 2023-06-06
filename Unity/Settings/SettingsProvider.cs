using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlenderBridge.Settings
{
    class BlenderBridgeSettingsProvider : SettingsProvider
    {
        const string StyleSheetPath = "Packages/com.ohmnivore.meshsyncrenderengine/Unity/Settings/Settings.uss";
        const string PluginsPath = "Packages/com.ohmnivore.meshsyncrenderengine/BlenderAddons~/Blender 3.3 & 3.4";
        
        SerializedObject m_Settings;
        public BlenderBridgeSettingsProvider(string label, string path, SettingsScope scope)
            : base(path, scope)
        {
            this.label = label;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_Settings = new SerializedObject(Settings.instance);
            keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(m_Settings);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            rootElement.styleSheets.Add(styleSheet);
            
            var title = new Label(label);
            title.AddToClassList("blender-bridge-settings-title");
            rootElement.Add(title);

            var container = new VisualElement();
            container.AddToClassList("blender-bridge-settings-container");
            rootElement.Add(container);

            var pluginFolderButton = new Button();
            pluginFolderButton.AddToClassList("blender-bridge-settings-wide-button");
            pluginFolderButton.text = "Open Blender Addons folder";
            pluginFolderButton.clicked += () =>
            {
                EditorUtility.RevealInFinder(PluginsPath);
            };
            container.Add(pluginFolderButton);

            var serverPortField = new PropertyField(m_Settings.FindProperty("m_ServerPort"));
            var cameraFactoryField = new PropertyField(m_Settings.FindProperty("m_CameraFactory"));
            var healthMonitorField = new PropertyField(m_Settings.FindProperty("m_HealthMonitor"));
            healthMonitorField.AddToClassList("blender-bridge-settings-health-monitor");
            
            serverPortField.RegisterValueChangeCallback(OnPropertyValueChanged);
            cameraFactoryField.RegisterValueChangeCallback(OnPropertyValueChanged);
            healthMonitorField.RegisterValueChangeCallback(OnPropertyValueChanged);
            
            // This field doesn't trigger any RegisterValueChangeCallback.
            // However we know it consists entirely of bool fields, so we listen to ChangeEvent instead.
            healthMonitorField.RegisterCallback<ChangeEvent<bool>>(OnBoolValueChanged);
            
            container.Add(serverPortField);
            container.Add(cameraFactoryField);
            container.Add(healthMonitorField);

            var resetButton = new Button();
            resetButton.AddToClassList("blender-bridge-settings-wide-button");
            resetButton.text = "Reset to defaults";
            resetButton.clicked += () =>
            {
                Settings.instance.Reset();
                m_Settings.UpdateIfRequiredOrScript();
            };
            container.Add(resetButton);
            
            container.Bind(m_Settings);
            
            base.OnActivate(searchContext, rootElement);
        }

        public override void OnInspectorUpdate()
        {
            m_Settings.UpdateIfRequiredOrScript();
            
            base.OnInspectorUpdate();
        }

        void OnPropertyValueChanged(SerializedPropertyChangeEvent evt)
        {
            Settings.instance.Modify();
        }

        void OnBoolValueChanged(ChangeEvent<bool> evt)
        {
            Settings.instance.Modify();
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new BlenderBridgeSettingsProvider("MeshSync Render Engine", "Project/MeshSync Render Engine", SettingsScope.Project);
        }
    }
}
