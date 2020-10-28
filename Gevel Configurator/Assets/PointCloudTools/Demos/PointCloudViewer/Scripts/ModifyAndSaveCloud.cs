using PointCloudConverter;
using PointCloudConverter.Writers;
using System.IO;
using unitycodercom_PointCloudBinaryViewer;
using UnityEngine;

namespace unitycoder_examples
{
    public class ModifyAndSaveCloud : MonoBehaviour
    {
        public PointCloudViewerDX11 binaryViewerDX11;
        bool isSaving = false;

        void Update()
        {
            // demo: press space to modify existing point positions and colors
            // TODO should do exporting in another thread, now it hangs mainthread while processing
            if (Input.GetKeyDown(KeyCode.Space) && !isSaving)
            {
                isSaving = true;
                // assign export settings
                var importSettings = new ImportSettings();
                importSettings.batch = false;
                importSettings.exportFormat = PointCloudConverter.Structs.ExportFormat.UCPC;
                var outputFile = Path.Combine(Application.streamingAssetsPath, "export.ucpc");
                importSettings.outputFile = outputFile;
                importSettings.packColors = false;
                importSettings.randomize = false;
                importSettings.writer = new UCPC();

                int pointCount = binaryViewerDX11.GetPointCount();
                importSettings.writer.InitWriter(importSettings, pointCount);

                // loop all points
                for (int i = 0, len = pointCount; i < len; i++)
                {
                    binaryViewerDX11.points[i] += Random.insideUnitSphere * 0.25f;
                    binaryViewerDX11.pointColors[i] = new Vector3(1 - binaryViewerDX11.pointColors[i].x, 1 - binaryViewerDX11.pointColors[i].y, 1 - binaryViewerDX11.pointColors[i].z);

                    float x = binaryViewerDX11.points[i].x;
                    float y = binaryViewerDX11.points[i].y;
                    float z = binaryViewerDX11.points[i].z;
                    float r = binaryViewerDX11.pointColors[i].x;
                    float g = binaryViewerDX11.pointColors[i].y;
                    float b = binaryViewerDX11.pointColors[i].z;

                    importSettings.writer.AddPoint(i, x, y, z, r, g, b);
                }

                // update data to gpu
                binaryViewerDX11.UpdatePointData();
                binaryViewerDX11.UpdateColorData();

                Debug.Log("Saving file: " + outputFile);
                importSettings.writer.Save();

                isSaving = false;
            }
        }
    }
}