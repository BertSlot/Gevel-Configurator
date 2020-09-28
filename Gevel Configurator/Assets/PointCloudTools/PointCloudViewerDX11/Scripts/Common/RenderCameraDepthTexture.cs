using UnityEngine;

namespace unitycodercom_pointcloud_extras
{
//    [ExecuteInEditMode]
    public class RenderCameraDepthTexture : MonoBehaviour
    {
        void Awake()
        {
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.depthTextureMode |= DepthTextureMode.Depth;
            }
        }
    }
}
