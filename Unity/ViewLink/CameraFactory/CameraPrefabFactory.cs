using UnityEngine;

namespace BlenderBridge.ViewLink.CameraFactory
{
    /// <summary>
    /// An implementation of <see cref="CameraFactory"/> that copies Camera settings from a specified prefab.
    /// Also it optionally instantiates a child prefab as a child of the created Camera's GameObject.
    /// </summary>
    [CreateAssetMenu(menuName = "MeshSync Render Engine/Camera Prefab Factory")]
    public class CameraPrefabFactory : CameraFactory
    {
        /// <summary>
        /// The prefab whose Camera settings should be copied.
        /// </summary>
        public GameObject CameraPrefab;

        /// <summary>
        /// Optional: A prefab to instantiate as a child of the created Camera's GameObject.
        /// </summary>
        public GameObject ChildPrefab;

        /// <inheritdoc/>
        public override void ConfigureCamera(Camera camera, ref RenderTextureDescriptor colorBuffer, ref RenderTextureDescriptor depthBuffer)
        {
            var prefabCamera = CameraPrefab.GetComponent<Camera>();

            camera.CopyFrom(prefabCamera);

#if HDRP
            {
                var prefabData = prefabCamera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
                var data = camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
                prefabData.CopyTo(data);
            }
#endif

            if (ChildPrefab != null)
                Instantiate(ChildPrefab, camera.transform);
        }
    }
}
