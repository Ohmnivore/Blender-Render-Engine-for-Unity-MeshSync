using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlenderBridge.ViewLink
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
#if USE_SPOUT
    [RequireComponent(typeof(Klak.Spout.SpoutSender))]
#endif
#if HDRP
    [RequireComponent(typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData))]
#endif
    class View : MonoBehaviour
    {
#if USE_SPOUT
#if HDRP
        const string k_DepthCaptureShaderName = "Hidden/BlenderBridge/DepthCaptureHDRP";
#elif URP
        const string k_DepthCaptureShaderName = "Hidden/BlenderBridge/DepthCaptureURP";
#endif
        const string k_DepthCaptureShaderPass = "Depth Capture";
        const string k_DepthCaptureProfilerTag = "BlenderBridge Depth Capture";

        static Shader s_DepthCaptureShader;
        static Material s_DepthCaptureMaterial;
        static int s_DepthCapturePass;

        const string k_SpoutBlitShaderPath = "Packages/jp.keijiro.klak.spout/Runtime/Internal/Blit.shader";
        static Klak.Spout.SpoutResources s_SpoutResources;

        Camera m_Camera;
        Klak.Spout.SpoutSender m_Sender;
        Klak.Spout.SpoutSender m_DepthSender;

        int m_Width = -1;
        int m_Height = -1;
        RenderTexture m_ColorTexture;
        RenderTexture m_DepthTexture;

        void OnEnable()
        {
            if (s_SpoutResources == null)
            {
                // Can't load the default SpoutResources from "Packages/jp.keijiro.klak.spout/Editor/SpoutResources.asset",
                // don't know why. Create one from scratch instead.
                s_SpoutResources = ScriptableObject.CreateInstance<Klak.Spout.SpoutResources>();
                s_SpoutResources.blitShader = AssetDatabase.LoadAssetAtPath<Shader>(k_SpoutBlitShaderPath);
            }

            if (s_DepthCaptureShader == null)
            {
                s_DepthCaptureShader = Shader.Find(k_DepthCaptureShaderName);
                if (s_DepthCaptureShader != null)
                {
                    s_DepthCaptureMaterial = CoreUtils.CreateEngineMaterial(s_DepthCaptureShader);
                    s_DepthCapturePass = s_DepthCaptureMaterial.FindPass(k_DepthCaptureShaderPass);
                }
            }

            m_Camera = GetComponent<Camera>();

            var senders = GetComponents<Klak.Spout.SpoutSender>();
            if (senders.Length <= 0)
                m_Sender = gameObject.AddComponent<Klak.Spout.SpoutSender>();
            else
                m_Sender = senders[0];
            if (senders.Length <= 1)
                m_DepthSender = gameObject.AddComponent<Klak.Spout.SpoutSender>();
            else
                m_DepthSender = senders[1];

            m_Camera.enabled = false;
            m_Sender.enabled = false;
            m_DepthSender.enabled = false;

            m_Sender.captureMethod = Klak.Spout.CaptureMethod.Texture;
            m_Sender.SetResources(s_SpoutResources);
            m_DepthSender.captureMethod = Klak.Spout.CaptureMethod.Texture;
            m_DepthSender.keepAlpha = true;
            m_DepthSender.SetResources(s_SpoutResources);

            RenderPipelineManager.endCameraRendering += CaptureCamera;

#if URP
            var pipeline = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            Debug.Assert(pipeline != null);
            if (!pipeline.supportsCameraDepthTexture)
                Debug.LogError($"The Depth Texture checkbox isn't set in {nameof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)}, can't capture depth");
#endif
        }

        void OnDisable()
        {
            m_Camera.enabled = false;

            RenderPipelineManager.endCameraRendering -= CaptureCamera;
        }

        void OnDestroy()
        {

        }

        void LateUpdate()
        {
            m_Camera.Render();
        }

        void OnApplicationQuit()
        {
            // The camera will linger, while RTs created in playmode will not
            m_Camera.targetTexture = null;
        }

        void ReleaseTextures()
        {
            if (m_ColorTexture != null)
            {
                m_ColorTexture.Release();
                m_ColorTexture = null;
            }

            if (m_DepthTexture != null)
            {
                m_DepthTexture.Release();
                m_DepthTexture = null;
            }
        }

        void StartCapture(int width, int height)
        {
            if (width != m_Width || height != m_Height ||
                m_ColorTexture == null || m_DepthTexture == null)
            {
                ReleaseTextures();

                var colorDescriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 32, 1);
                var depthDescriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0, 1);

                var cameraFactory = Settings.Settings.instance.CameraFactory;
                if (cameraFactory != null)
                    cameraFactory.ConfigureCamera(m_Camera, ref colorDescriptor, ref depthDescriptor);

                m_ColorTexture = new RenderTexture(colorDescriptor);
                m_DepthTexture = new RenderTexture(depthDescriptor);

                m_Width = width;
                m_Height = height;
            }

            m_Camera.targetTexture = m_ColorTexture;

            m_Camera.enabled = true;
            m_Sender.enabled = true;
            m_DepthSender.enabled = true;
        }

        public void ApplyState(ViewState state)
        {
            StartCapture(state.Width, state.Height);

            m_Sender.spoutName = $"MeshSync Render Engine {state.ID}";
            m_DepthSender.spoutName = $"MeshSync Render Engine Depth {state.ID}";

            m_Camera.orthographic = !state.IsPerspective;
            m_Camera.projectionMatrix = state.WindowMatrix;
            
#if USE_MESH_SYNC
            var root = MeshSyncInterop.MeshSyncInterop.instance.RootObject;
            if (root != null)
            {
                m_Camera.transform.position = root.TransformPoint(state.ViewPose.position);
                m_Camera.transform.rotation = root.rotation * state.ViewPose.rotation;
            }
            else
            {
                m_Camera.transform.position = state.ViewPose.position;
                m_Camera.transform.rotation = state.ViewPose.rotation;
            }
#else
            m_Camera.transform.position = state.ViewPose.position;
            m_Camera.transform.rotation = state.ViewPose.rotation;
#endif

            EditorApplication.QueuePlayerLoopUpdate();
        }

        void CaptureCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != m_Camera)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_DepthCaptureProfilerTag);
            CoreUtils.DrawFullScreen(cmd, s_DepthCaptureMaterial, m_DepthTexture, shaderPassId: s_DepthCapturePass);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();

            CommandBufferPool.Release(cmd);

            m_Sender.sourceTexture = m_ColorTexture;
            m_DepthSender.sourceTexture = m_DepthTexture;
        }
#else
        public void ApplyState(ViewState state)
        {

        }
#endif
    }
}
