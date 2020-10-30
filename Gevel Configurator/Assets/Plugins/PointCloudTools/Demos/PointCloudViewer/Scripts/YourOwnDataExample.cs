using unitycodercom_PointCloudBinaryViewer;
using UnityEngine;

namespace unitycoder_examples
{
    public class YourOwnDataExample : MonoBehaviour
    {
        public PointCloudViewerDX11 binaryViewerDX11;
        public int totalPoints = 50000;

        void Start()
        {
            // initialize viewer with 1 point and colordata, so that can resize/fill it later
            binaryViewerDX11.containsRGB = true;
            binaryViewerDX11.InitDX11Buffers();
        }

        void Update()
        {
            // demo: press space to randomize new data
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // generate random example dataset, instead of this, you would load/generate your own data
                var randomPoints = new Vector3[totalPoints];
                var randomColors = new Vector3[totalPoints];
                for (int i = 0; i < totalPoints; i++)
                {
                    randomPoints[i] = Random.insideUnitSphere * 15;
                    var c = Random.ColorHSV(0, 1, 0, 1, 0, 1);
                    randomColors[i] = new Vector3(c.r, c.g, c.b);
                }

                // after you have your own data, send them into viewer
                binaryViewerDX11.points = randomPoints;
                binaryViewerDX11.UpdatePointData();

                binaryViewerDX11.pointColors = randomColors;
                binaryViewerDX11.UpdateColorData();
            }
        }
    }
}