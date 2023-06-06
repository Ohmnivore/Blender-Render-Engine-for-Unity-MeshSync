#if HDRP
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace BlenderBridge.ViewLink.CameraFactory
{
    /// <summary>
    /// A <see cref="CameraFactory"/> implementation used for HDRP when none is assigned to the MeshSync Render Engine settings.
    /// </summary>
    public class HDRPCameraFactory : CameraFactory
    {
        /// <inheritdoc/>
        public override void ConfigureCamera(Camera camera, ref RenderTextureDescriptor colorBuffer, ref RenderTextureDescriptor depthBuffer)
        {
            base.ConfigureCamera(camera, ref colorBuffer, ref depthBuffer);

            colorBuffer.msaaSamples = 1;
            colorBuffer.depthBufferBits = 32;

            camera.allowMSAA = false;

            var data = camera.GetComponent<HDAdditionalCameraData>();
            if (data != null)
            {
                data.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.Medium;
            }
        }
    }
}
#endif
