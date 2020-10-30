using UnityEngine;
using System.Collections;
using unitycodercom_PointCloudBinaryViewer;

// Multiple Cloud Spawner (Instantiates new DX11Viewer for each cloud)

namespace PointCloudExtras
{

    public class MultiCloudManager : MonoBehaviour
    {
        public KeyCode loadKey = KeyCode.Alpha1; // press this key to load next cloud

        public GameObject dx11ViewerPrefab;
        public Material pointCloudMaterial;

        public string[] cloudsToLoad;
        int cloudIndex = 0;

        // mainloop
        void Update()
        {
            // spawn new binaryviewers with keypress
            if (Input.GetKeyDown(loadKey))
            {
                if (cloudIndex < cloudsToLoad.Length)
                {
                    Debug.Log("Spawning new viewer for file and overriding prefab values:" + cloudsToLoad[cloudIndex], gameObject);
                    var go = Instantiate(dx11ViewerPrefab);
                    var viewer = go.GetComponent<PointCloudViewerDX11>();

                    // for demo, we move the cloud a bit, since its same..note not all shaders support transform offset
                    viewer.transform.position += Vector3.up * 10 * cloudIndex;

                    // we override new cloudviewer parameters for the binaryviewer component (instead of using the prefab values)
                    viewer.loadAtStart = false;
                    // pc.fileName = cloudsToLoad[cloudIndex];

                    // set new material (REQUIRED to be unique material for each cloud)
                    viewer.cloudMaterial = new Material(pointCloudMaterial);

                    // enable and load new cloud
                    viewer.useThreading = true; // should use threading, so that it wont hang this main thread..

                    viewer.enabled = true;
                    viewer.CallReadPointCloudThreaded(cloudsToLoad[cloudIndex]);

                    cloudIndex++;
                } else
                {
                    print("No more clouds to load..");
                }
            }
        }

    }
}