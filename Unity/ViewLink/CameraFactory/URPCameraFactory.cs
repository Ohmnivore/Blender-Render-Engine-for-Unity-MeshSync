#if URP
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlenderBridge.ViewLink.CameraFactory
{
    /// <summary>
    /// A <see cref="CameraFactory"/> implementation used for URP when none is assigned to the MeshSync Render Engine settings.
    /// </summary>
    public class URPCameraFactory : CameraFactory
    {
        /// <inheritdoc/>
        public override void ConfigureCamera(Camera camera, ref RenderTextureDescriptor colorBuffer, ref RenderTextureDescriptor depthBuffer)
        {
            base.ConfigureCamera(camera, ref colorBuffer, ref depthBuffer);

            colorBuffer.msaaSamples = 8;
            colorBuffer.depthBufferBits = 32;

            camera.allowMSAA = true;
            
            var data = camera.GetUniversalAdditionalCameraData();
            data.antialiasing = AntialiasingMode.None;
        }
    }
}
#endif
