// Example script showing how to load different point cloud
// (to override existing loaded cloud, or to load first new cloud when [ ] load at start is not enabled in BinaryViewerDX11)

using System.IO;
using UnityEngine;

namespace unitycoder_examples
{
    public class LoadDifferentCloudExample : MonoBehaviour
    {
        public unitycodercom_PointCloudBinaryViewer.PointCloudViewerDX11 binaryViewer;

        [Tooltip("Filename inside StreamingAssets/ folder")]
        public string fileName = "sample2.bin";

        // this method is called from canvas button click
        public void LoadAnotherCloud()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);

            // can override settings here if needed
            //binaryViewer.useThreading = true;

            // load with threading
            binaryViewer.CallReadPointCloudThreaded(fullPath);
        }
    }

}