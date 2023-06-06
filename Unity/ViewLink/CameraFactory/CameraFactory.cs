using UnityEngine;

namespace BlenderBridge.ViewLink.CameraFactory
{
    /// <summary>
    /// Inherit from this class and assign an instance to MeshSync Render Engine's project settings
    /// to configure the Camera, its GameObject, and its render textures for each Blender view.
    /// </summary>
    /// <remarks>
    /// The default implementations are <see cref="HDRPCameraFactory"/> and <see cref="URPCameraFactory"/>.
    /// <see cref="CameraPrefabFactory"/> is a code-free alternative. 
    /// </remarks>
    public class CameraFactory : ScriptableObject
    {
        /// <summary>
        /// Override this method to configure the Camera, its GameObject, and its render textures for a Blender view.
        /// </summary>
        /// <param name="camera">The Camera.</param>
        /// <param name="colorBuffer">The Camera's color render texture descriptor.</param>
        /// <param name="depthBuffer">The Camera's depth render texture descriptor.</param>
        public virtual void ConfigureCamera(Camera camera, ref RenderTextureDescriptor colorBuffer, ref RenderTextureDescriptor depthBuffer)
        {

        }
    }
}
