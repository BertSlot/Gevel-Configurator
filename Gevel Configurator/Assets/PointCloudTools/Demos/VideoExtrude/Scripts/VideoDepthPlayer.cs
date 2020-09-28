using UnityEngine;
using System.Collections;

namespace unitycoder_extras
{

    public class VideoDepthPlayer : MonoBehaviour
    {
        public bool usePoints = false;

        void Start()
        {
#if UNITY_2019_1_OR_NEWER
            Debug.LogError("VideoDepthPlayer example is not supported in 2019_1 or later, you could use new VideoPlayer component though");
#else
            PlayMovie();
#endif
        }

#if !UNITY_2019_1_OR_NEWER
        void PlayMovie()
        {
            // no movietexture in mobiles
#if !UNITY_IPHONE && !UNITY_ANDROID && !UNITY_SAMSUNGTV && !UNITY_WEBGL

            if (usePoints)
            {
                Mesh mesh = GetComponent<MeshFilter>().mesh;
                int[] tris = mesh.triangles;
                mesh.SetIndices(tris, MeshTopology.Points, 0);
            }

            //for now we use 2 separate videos, 1 for color, 1 for depth (grayscale)
            var r = GetComponent<Renderer>();
            MovieTexture mainTex = r.material.mainTexture as MovieTexture;
            MovieTexture depthTex = r.material.GetTexture("_ExtrudeTex") as MovieTexture;

            if (mainTex == null || depthTex == null)
            {
                Debug.LogError("Both textures should be movie textures to see animated depth map", gameObject);
                return;
            }

            mainTex.loop = true;
            depthTex.loop = true;

            mainTex.Play();
            depthTex.Play();
#endif
        }
#endif
    }

}

