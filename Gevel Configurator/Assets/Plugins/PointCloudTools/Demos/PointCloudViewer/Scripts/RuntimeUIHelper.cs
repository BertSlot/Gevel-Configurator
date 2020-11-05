using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace PointCloudRuntimeViewer
{

    public class RuntimeUIHelper : MonoBehaviour
    {
        public RuntimeViewerDX11 runtimeViewerDX11;
        public InputField filePathField;

        public void StartLoadingPointCloud()
        {
#if !UNITY_SAMSUNGTV && !UNITY_WEBGL

            // Set variables that you want to use for loading
            runtimeViewerDX11.fullPath = filePathField.text;

            // we could override other loader settings here also (for example if UI allows setting them)
            /*
//            runtimeViewerDX11.enablePicking = false;
            runtimeViewerDX11.fileFormat = 0; // 0="XYZ", 1="XYZRGB", 2="CGO", 3="ASC", 4="CATIA ASC", 5="PLY (ASCII)", 6="LAS", 7="PTS"
            runtimeViewerDX11.readRGB = true; // this will be automatically disabled, if the file doesnt contain RGB data
            runtimeViewerDX11.readIntensity = false;
            runtimeViewerDX11.useUnitScale = false;
            runtimeViewerDX11.unitScale = 0.001f;
            runtimeViewerDX11.flipYZ = true;
            runtimeViewerDX11.autoOffsetNearZero = true;
            runtimeViewerDX11.useManualOffset = true;
            runtimeViewerDX11.manualOffset = Vector3.zero;
            runtimeViewerDX11.plyHasNormals = false;*/

            // call actual loader

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            //runtimeViewerDX11.LoadRawPointCloud(); // non-threaded reader

            if (runtimeViewerDX11.IsLoading() == true)
            {
                runtimeViewerDX11.CallImporterThreaded(filePathField.text);
            }
            else
            {
                Debug.LogError("Its already loading..");
            }

            stopwatch.Stop();
            //Debug.Log("Loaded in: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Reset();
#endif        
        }

    }
}
