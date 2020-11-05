using PointCloudViewer;
using System.Collections.Generic;
using unitycodercom_PointCloudBinaryViewer;
using UnityEngine;

namespace Unitycoder
{
    public class AreaSelectExample : MonoBehaviour
    {
        [Tooltip("Selection area is determined from child transforms of this transform. Note: Keep root transfrom at 0,0,0")]
        public Transform selectionRoot;

        [Header("Options")]
        [Tooltip("Press this key to update point selection")]
        public KeyCode selectPointsKey = KeyCode.F5;
        [Tooltip("If enabled, selected points are drawn twice (as a separate cloud)")]
        public bool createSeparateCloudFromSelection = false;
        [Tooltip("This is required if you use separate cloud")]
        public PointCloudViewerDX11 tempPointCloudViewerDX11;
        [Tooltip("Color for selected points, if createSeparateCloudFromSelection is enabled")]
        public Color selectedPointColor = Color.green;

        // results from selection
        List<CollectedPoint> selectedPoints;

        // reference to manager, which enabled point picking
        PointCloudManager pointCloudManager;

        private void Start()
        {
            // use singleton from manager
            pointCloudManager = PointCloudManager.instance;

            // check separate viewer, needed if use separate cloud to show selected points
            if (createSeparateCloudFromSelection == true && tempPointCloudViewerDX11 == null) Debug.LogError("Missing tempPointCloudViewerDX11 reference at " + gameObject.name, gameObject);
        }


        void Update()
        {
            // update area selection on F5
            if (Input.GetKeyDown(selectPointsKey))
            {
                // we need to keep track which clouds contributed into selection (since its possible to measure/select from multiple clouds)
                var uniqueClouds = new List<int>();

                if (createSeparateCloudFromSelection == true)
                {
                    // TODO clear previously generated cloud or just overwrite later?
                }
                else // use original points
                {
                    // first invert old selected point colors back to original (if we had something selected)
                    if (selectedPoints != null && selectedPoints.Count > 0)
                    {
                        for (int i = 0, len = selectedPoints.Count; i < len; i++)
                        {
                            var pdata = selectedPoints[i];
                            int cloudIndex = pointCloudManager.clouds[pdata.cloudIndex].viewerIndex;
                            var c = pointCloudManager.viewers[cloudIndex].pointColors[pdata.pointIndex];
                            // restore inverted colors
                            c.x = 1 - c.x;
                            c.y = 1 - c.y;
                            c.z = 1 - c.z;
                            pointCloudManager.viewers[cloudIndex].pointColors[pdata.pointIndex] = c;

                            // collect clouds that we need to refresh colors for
                            if (uniqueClouds.Contains(cloudIndex) == false) uniqueClouds.Add(cloudIndex);
                        }
                    }
                }

                // collect area selection points (child objects of the root)
                var points = new List<Vector3>();
                foreach (Transform t in selectionRoot)
                {
                    points.Add(t.position);
                }

                // get selection results form BoxSelect
                selectedPoints = pointCloudManager.ConvexHullSelectPoints(gameObject, points);

                if (selectedPoints != null)
                {
                    Debug.Log("Selected " + selectedPoints.Count + " points");
                    if (createSeparateCloudFromSelection == true)
                    {
                        // build new cloud from selection points
                        var selectedPointsTemp = new Vector3[selectedPoints.Count];
                        for (int i = 0, len = selectedPoints.Count; i < len; i++)
                        {
                            var pdata = selectedPoints[i];
                            int cloudIndex = pointCloudManager.clouds[pdata.cloudIndex].viewerIndex;
                            var p = pointCloudManager.viewers[cloudIndex].points[pdata.pointIndex];
                            selectedPointsTemp[i] = p;
                        }

                        // create new cloud on temporary viewer
                        tempPointCloudViewerDX11.InitDX11Buffers();
                        tempPointCloudViewerDX11.points = selectedPointsTemp;
                        tempPointCloudViewerDX11.UpdatePointData();

                        // set custom color for points, NOTE should use special material/shader that uses fixed color AND zoffset?
                        tempPointCloudViewerDX11.cloudMaterial.SetColor("_Color", selectedPointColor);
                        // NOTE we take pointsize from first cloud only
                        //tempPointCloudViewerDX11.cloudMaterial.SetFloat("_Size", pointCloudManager.viewers[0].cloudMaterial.GetFloat("_Size"));

                    }
                    else // we are using original cloud points
                    {
                        for (int i = 0, len = selectedPoints.Count; i < len; i++)
                        {
                            // set point colors (but really would be faster to create new owndata cloud for those usually..)
                            var pdata = selectedPoints[i];

                            // get point index from that cloudo
                            int cloudIndex = pointCloudManager.clouds[pdata.cloudIndex].viewerIndex;

                            // TODO, if want to use custom color create new cloud or point mesh, otherwise invert color or swap colors
                            var c = pointCloudManager.viewers[cloudIndex].pointColors[pdata.pointIndex];
                            c.x = 1 - c.x;
                            c.y = 1 - c.y;
                            c.z = 1 - c.z;
                            pointCloudManager.viewers[cloudIndex].pointColors[pdata.pointIndex] = c;

                            // get list of unique clouds that we need to update
                            if (uniqueClouds.Contains(cloudIndex) == false) uniqueClouds.Add(cloudIndex);
                        }
                    }
                }

                // refresh colors for each existing cloud
                foreach (int cloudIndex in uniqueClouds)
                {
                    pointCloudManager.viewers[cloudIndex].UpdateColorData();
                }


            }
        } // Update

    } // class

}
