// example code for getting tile and point count from V3 viewer

using UnityEngine;

namespace unitycodercom_PointCloudBinaryViewer
{
    public class ViewerStats : MonoBehaviour
    {
        public PointCloudViewerTilesDX11 viewer;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                Debug.Log("Visible tiles=" + viewer.GetVisibleTileCount() + " Visible points=" + viewer.GetVisiblePointCount() + " Total cloud points=" + viewer.GetTotalPointCount());
            }

        }
    }
}