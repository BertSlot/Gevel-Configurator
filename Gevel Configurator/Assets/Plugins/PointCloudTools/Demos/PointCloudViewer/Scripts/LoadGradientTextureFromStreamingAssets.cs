// Loads texture from Streaming Assets folder, with options for format/filter/target field

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#if !UNITY_SAMSUNGTV && !UNITY_WEBGL

namespace UnityLibrary
{
    public class LoadGradientTextureFromStreamingAssets : MonoBehaviour
    {
        public PointCloudRuntimeViewer.RuntimeViewerDX11 targetViewer;
        [Tooltip("Texture filename (StreamingAssets/ - prefix is automatically added)")]
        public string textureFileName = "PointCloudViewerSampleData/sample-gradient2.png";
        public TextureFormat textureFormat = TextureFormat.RGB24;
        public FilterMode filterMode = FilterMode.Point;
        public TextureWrapMode textureWrapMode = TextureWrapMode.Clamp;
        public bool hasMipMap = false;
        public bool isLinear = false;
        public string shaderTargetProperty = "_GradientMap";

        void Awake()
        {
            if (targetViewer == null) Debug.LogError("Missing RuntimeViewer reference", gameObject);
            StartCoroutine(LoadStreamingAsset(textureFileName));
        }

        public IEnumerator LoadStreamingAsset(string filename)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, filename);
            if (File.Exists(fullPath) == false)
            {
                Debug.LogError("File not found: " + fullPath);
                yield break;
            }

            //WWW www = new WWW("file://" + fullPath);
            var www = UnityWebRequestTexture.GetTexture("file://" + fullPath);

            while (!www.isDone)
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(www.error))
            {

                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Loaded Texture:" + fullPath);
                if (targetViewer.cloudMaterial.HasProperty(shaderTargetProperty))
                {
                    var tex = new Texture2D(2, 2, textureFormat, hasMipMap, isLinear);
                    tex = DownloadHandlerTexture.GetContent(www);
                    tex.filterMode = filterMode;
                    tex.wrapMode = textureWrapMode;
                    targetViewer.cloudMaterial.SetTexture(shaderTargetProperty, tex);
                }
                else
                {
                    Debug.LogError("Shader doesnt have property: " + shaderTargetProperty);
                }
            }
            yield return 0;
            www.Dispose();
            www = null;
        }
    }
}

#endif