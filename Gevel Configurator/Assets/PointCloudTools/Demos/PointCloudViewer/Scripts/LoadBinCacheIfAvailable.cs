// example script to check if cached .bin file exits
// and then load using PointCloudViewerDX11 instead of RuntimeViewerDX11

using unitycodercom_PointCloudBinaryViewer;
using PointCloudRuntimeViewer;
using UnityEngine;
#if !UNITY_SAMSUNGTV && !UNITY_WEBGL
using System.IO;
#endif

namespace unitycoder_examples
{
    public class LoadBinCacheIfAvailable : MonoBehaviour
    {
        public bool loadAtStart = true;

        [Tooltip("If this file has .bin cached file, we use PointCloudViewerDX11 instead of parsing with RuntimeViewerDX11")]
        public string fileName = "StreamingAssets/PointCloudViewerSampleData/sample.xyz";

        public PointCloudViewerDX11 pointCloudViewerDX11;
        public RuntimeViewerDX11 runtimeViewerDX11;

#if !UNITY_SAMSUNGTV && !UNITY_WEBGL
        void Start()
        {
            if (loadAtStart == true)
            {

                if (Path.IsPathRooted(fileName) == false)
                {
                    fileName = Path.Combine(Application.streamingAssetsPath, fileName);
                }

                var cacheFile = fileName + ".bin";
                if (File.Exists(cacheFile) == true)
                {
                    Debug.Log("Loading cached file: " + cacheFile);
                    pointCloudViewerDX11.CallReadPointCloudThreaded(cacheFile);
                }
                else
                {
                    Debug.Log("No cached file available, reading raw data: " + fileName);
                    runtimeViewerDX11.CallImporterThreaded(fileName);
                }
            }
        }
#endif

    } // class
} // namespace

