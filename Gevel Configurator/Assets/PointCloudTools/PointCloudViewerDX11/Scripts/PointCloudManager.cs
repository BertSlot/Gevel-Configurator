// point cloud manager (currently used for point picking)
// unitycoder.com

using GK;
using PointCloudHelpers;
using System.Collections.Generic;
using System.Threading;
using unitycodercom_PointCloudBinaryViewer;
using UnityEngine;

namespace PointCloudViewer
{
    public class PointCloudManager : MonoBehaviour
    {
        public List<PointCloudViewerDX11> viewers = new List<PointCloudViewerDX11>();
        public int slices = 16;

        [Header("Advanced Options")]
        // TODO move to measurement tool?
        [Tooltip("Vector3.Dot magnitude threshold, try to keep in very small values")]
        public float pointSearchThreshold = 0.0001f;
        [Tooltip("1 = Full precision, every point is indexed. Lower values decrease collect & picking speeds")]
        public PointIndexPrecision pointIndexPrecision = PointIndexPrecision.Full;

        internal List<Cloud> clouds; // all clouds
        Thread pointPickingThread;
        //Vector3 tempClosest = Vector3.zero;

        public delegate void PointSelected(Vector3 pointPos);
        public static event PointSelected PointWasSelected;

        public static PointCloudManager instance;

        private void Awake()
        {
            instance = this;

            if (UnityLibrary.MainThread.instanceCount == 0)
            {
                if (viewers != null)
                {
                    for (int i = 0; i < viewers.Count; i++)
                    {
                        if (viewers[i] != null) viewers[i].FixMainThreadHelper();
                    }
                }
            }

            clouds = new List<Cloud>();

            // wait for loading complete event (for automatic registration)
            if (viewers != null)
            {
                for (int i = 0; i < viewers.Count; i++)
                {
                    if (viewers[i] != null) viewers[i].OnLoadingComplete -= CloudIsReady;
                    if (viewers[i] != null) viewers[i].OnLoadingComplete += CloudIsReady;
                }
            }
        }

        public void RegisterCloudManually(PointCloudViewerDX11 newViewer)
        {
            for (int i = 0; i < viewers.Count; i++)
            {
                // remove previous same instance cloud, if already in the list  
                for (int vv = 0, viewerLen = viewers.Count; vv < viewerLen; vv++)
                {
                    if (viewers[vv].fileName == newViewer.fileName)
                    {
                        Debug.Log("Removed duplicate cloud from viewers: " + newViewer.fileName);
                        clouds.RemoveAt(vv);
                        break;
                    }
                }
            }

            // add new cloud
            viewers.Add(newViewer);

            // manually call cloud to be processed
            CloudIsReady(newViewer.fileName);
        }

        public void CloudIsReady(object cloudFilePath)
        {
            ProcessCloud((string)cloudFilePath);
            Debug.Log("Cloud is ready for picking: " + (string)cloudFilePath);
        }

        void ProcessCloud(string cloudPath)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            int viewerIndex = -1;
            // find index
            for (int vv = 0, viewerLen = viewers.Count; vv < viewerLen; vv++)
            {
                if (viewers[vv].fileName == cloudPath)
                {
                    viewerIndex = vv;
                    break;
                }
            }

            if (viewerIndex == -1)
            {
                Debug.LogError("Failed to find matching cloud for indexing..");
            }
            //Debug.Log("Adding: " + viewerIndex);

            var cloudBounds = viewers[viewerIndex].GetBounds();

            float minX = cloudBounds.min.x;
            float minY = cloudBounds.min.y;
            float minZ = cloudBounds.min.z;

            float maxX = cloudBounds.max.x;
            float maxY = cloudBounds.max.y;
            float maxZ = cloudBounds.max.z;

            // split cloud
            float width = maxX - minX;
            float height = maxY - minY;
            float depth = maxZ - minZ;

            float stepX = width / (float)slices;
            float stepY = height / (float)slices;
            float stepZ = depth / (float)slices;

            // NOTE need to clamp to minimum 1?
            //if (stepY < 1) stepY += 32;

            float stepXInverted = 1f / stepX;
            float stepYInverted = 1f / stepY;
            float stepZInverted = 1f / stepZ;

            int totalBoxes = slices * slices * slices;

            // create new cloud object
            var newCloud = new Cloud();
            // add to total clouds
            // init child node boxes
            newCloud.nodes = new NodeBox[totalBoxes];
            newCloud.viewerIndex = viewerIndex;
            newCloud.bounds = cloudBounds;

            float xs = minX;
            float ys = minY;
            float zs = minZ;

            float halfStepX = stepX * 0.5f;
            float halfStepY = stepY * 0.5f;
            float halfStepZ = stepZ * 0.5f;

            Vector3 p;
            Vector3 tempCenter = Vector3.zero;
            Vector3 tempSize = Vector3.zero;
            Bounds boxBoundes = new Bounds();

            // build node boxes
            for (int y = 0; y < slices; y++)
            {
                tempSize.y = stepY;
                tempCenter.y = ys + halfStepY;
                for (int z = 0; z < slices; z++)
                {
                    tempSize.z = stepZ;
                    tempCenter.z = zs + halfStepZ;
                    int slicesMulYZ = slices * (y + slices * z);
                    for (int x = 0; x < slices; x++)
                    {
                        tempSize.x = stepX;
                        tempCenter.x = xs + halfStepX;

                        var np = new NodeBox();
                        boxBoundes.center = tempCenter;
                        boxBoundes.size = tempSize;
                        np.bounds = boxBoundes;
                        np.points = new List<int>(); // for struct
                        //PointCloudHelpers.PointCloudTools.DrawBounds(np.bounds, 20);

                        newCloud.nodes[x + slicesMulYZ] = np;
                        xs += stepX;
                    }
                    xs = minX;
                    zs += stepZ;
                }
                zs = minZ;
                ys += stepY;
            }

            stopwatch.Stop();
            //            Debug.Log("Split: " + stopwatch.ElapsedTicks + " ticks");
            stopwatch.Reset();


            stopwatch.Start();

            /*
            Debug.Log("minx:" + minX + " maxx:" + maxX + " width:" + width);
            Debug.Log("miny:" + minY + " maxy:" + maxY + " height:" + height);
            Debug.Log("minz:" + minZ + " maxz:" + maxZ + " depth:" + depth);
            Debug.Log("stepX:" + stepX + " stepY:" + stepY + " stepZ:" + stepZ);
            Debug.Log("boxes:" + slices + " total=" + totalBoxes + " arrayboxes:" + nodeBoxes.Length);
            */

            // pick step resolution
            int pointStep = 1;
            switch (pointIndexPrecision)
            {
                case PointIndexPrecision.Full:
                    pointStep = 1;
                    break;
                case PointIndexPrecision.Half:
                    pointStep = 2;
                    break;
                case PointIndexPrecision.Quarter:
                    pointStep = 4;
                    break;
                case PointIndexPrecision.Eighth:
                    pointStep = 8;
                    break;
                case PointIndexPrecision.Sixteenth:
                    pointStep = 16;
                    break;
                case PointIndexPrecision.TwoHundredFiftySixth:
                    pointStep = 256;
                    break;
                default:
                    break;
            }

            // collect points to boxes
            for (int j = 0, pointLen = viewers[newCloud.viewerIndex].points.Length; j < pointLen; j += pointStep)
            {
                p = viewers[newCloud.viewerIndex].points[j];
                // http://www.reactiongifs.com/r/mgc.gif
                int sx = (int)((p.x - minX - 0.01f) * stepXInverted);
                int sy = (int)((p.y - minY - 0.01f) * stepYInverted);
                int sz = (int)((p.z - minZ - 0.01f) * stepZInverted);

                var boxIndex = sx + slices * (sy + slices * sz);
                //Debug.Log("sx,sy,sz=" + sx + "," + sy + "," + sz + " boxIndex=" + boxIndex + " p.y=" + p.y + " minY=" + minY + " newCloud.nodes=" + newCloud.nodes.Length);
                // NOTE bug with thin clouds?
                newCloud.nodes[boxIndex].points.Add(j);
            }
            // add to clouds list
            clouds.Add(newCloud);

            stopwatch.Stop();
            //Debug.Log("Collect: " + stopwatch.ElapsedMilliseconds + " ms");
            stopwatch.Reset();
        }

        public void RunPointPickingThread(Ray ray)
        {
            ParameterizedThreadStart start = new ParameterizedThreadStart(FindClosestPoint);
            pointPickingThread = new Thread(start);
            pointPickingThread.IsBackground = true;
            pointPickingThread.Start(ray);
        }

        public void FindClosestPoint(object rawRay)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            int nearestIndex = -1;
            float nearestDistanceToCamera = Mathf.Infinity;
            int viewerIndex = -1; // which viewer has this point data

            Ray ray = (Ray)rawRay;

            Vector3 rayDirection = ray.direction;
            Vector3 rayOrigin = ray.origin;
            Vector3 rayInverted = new Vector3(1f / ray.direction.x, 1f / ray.direction.y, 1f / ray.direction.z);
            Vector3 point;

            // check all clouds
            for (int cloudIndex = 0, cloudsLen = clouds.Count; cloudIndex < cloudsLen; cloudIndex++)
            {
                // check all nodes from each cloud
                for (int nodeIndex = 0, nodesLen = clouds[cloudIndex].nodes.Length; nodeIndex < nodesLen; nodeIndex++)
                {
                    // check if this box intersects ray
                    if (RayBoxIntersect2(rayOrigin, rayInverted, clouds[cloudIndex].nodes[nodeIndex].bounds.min, clouds[cloudIndex].nodes[nodeIndex].bounds.max) > 0)
                    {
                        //Debug.Log("Hit cloud: " + System.IO.Path.GetFileName(viewers[clouds[cloudIndex].viewerIndex].fileName) + " nodebox:" + nodeIndex + " with x points=" + clouds[cloudIndex].nodes[nodeIndex].points.Count);
                        // then check all points from that node
                        for (int nodePointIndex = 0, nodePointLen = clouds[cloudIndex].nodes[nodeIndex].points.Count; nodePointIndex < nodePointLen; nodePointIndex++)
                        {
                            int pointIndex = clouds[cloudIndex].nodes[nodeIndex].points[nodePointIndex];
                            point = viewers[clouds[cloudIndex].viewerIndex].points[pointIndex];
                            float dist = 1 - Vector3.Dot(rayDirection, (point - rayOrigin).normalized);

                            // 1 would be exact hit
                            if (dist < pointSearchThreshold)
                            {
                                float camDist = 4096 * dist + Distance(rayOrigin, point);

                                if (camDist < nearestDistanceToCamera)
                                {
                                    nearestDistanceToCamera = camDist;
                                    nearestIndex = pointIndex;
                                    viewerIndex = clouds[cloudIndex].viewerIndex;
                                    //pickResults[currentResults++] = index; // TEST array, so can sort later
                                }
                                //}
                            }
                        } // each point inside box
                    } // if ray hits box
                } // all boxes
                //UnityLibrary.MainThread.Call(viewers[clouds[cloudIndex].viewerIndex].UpdateColorData); // debug
            } // all clouds


            if (nearestIndex > -1)
            {
                // HighLightPoint(viewer.points[nearestIndex]);
                // UnityLibrary.MainThread.Call(HighLightPoint);
                UnityLibrary.MainThread.Call(PointCallBack, viewers[viewerIndex].points[nearestIndex]);
                Debug.Log("Selected Point #:" + (nearestIndex) + " Position:" + viewers[viewerIndex].points[nearestIndex] + " from " + (System.IO.Path.GetFileName(viewers[viewerIndex].fileName)));
            }
            else
            {
                Debug.Log("No points found..");
            }

            stopwatch.Stop();
            Debug.Log("PickTimer: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Reset();

            if (pointPickingThread != null && pointPickingThread.IsAlive == true) pointPickingThread.Abort();
        } // FindClosesPoint

        private void OnDestroy()
        {
            if (pointPickingThread != null && pointPickingThread.IsAlive == true) pointPickingThread.Abort();
        }

        // this gets called after thread finds closest point
        void PointCallBack(System.Object a)
        {
            if (PointWasSelected != null) PointWasSelected((Vector3)a);
        }

        public static float DistanceToRay(Ray ray, Vector3 point)
        {
            return Vector3.Cross(ray.direction, point - ray.origin).sqrMagnitude;
            //return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            float vecx = a.x - b.x;
            float vecy = a.y - b.y;
            float vecz = a.z - b.z;
            return vecx * vecx + vecy * vecy + vecz * vecz;
        }

        // checks if give AABB box collides with any point (point is inside the given box)
        public bool BoundsIntersectsCloud(Bounds box)
        {
            //PointCloudHelpers.PointCloudTools.DrawBounds(box);
            // all clouds
            for (int cloudIndex = 0, length2 = clouds.Count; cloudIndex < length2; cloudIndex++)
            {
                // exit if outside whole cloud bounds
                if (clouds[cloudIndex].bounds.Contains(box.center) == false) return false;

                // get full cloud bounds
                float minX = clouds[cloudIndex].bounds.min.x;
                float minY = clouds[cloudIndex].bounds.min.y;
                float minZ = clouds[cloudIndex].bounds.min.z;
                float maxX = clouds[cloudIndex].bounds.max.x;
                float maxY = clouds[cloudIndex].bounds.max.y;
                float maxZ = clouds[cloudIndex].bounds.max.z;

                // helpers
                float width = maxX - minX;
                float height = maxY - minY;
                float depth = maxZ - minZ;
                float stepX = width / slices;
                float stepY = height / slices;
                float stepZ = depth / slices;
                float stepXInverted = 1f / stepX;
                float stepYInverted = 1f / stepY;
                float stepZInverted = 1f / stepZ;

                // get collider box min node index
                int colliderX = (int)((box.center.x - minX - 0.01f) * stepXInverted);
                int colliderY = (int)((box.center.y - minY - 0.01f) * stepYInverted);
                int colliderZ = (int)((box.center.z - minZ - 0.01f) * stepZInverted);
                var BoxIndex = colliderX + slices * (colliderY + slices * colliderZ);
                //PointCloudHelpers.PointCloudTools.DrawBounds(clouds[cloudIndex].nodes[BoxIndex].bounds);

                // check if we hit within that area
                for (int j = 0, l = clouds[cloudIndex].nodes[BoxIndex].points.Count; j < l; j++)
                {
                    // each point
                    int pointIndex = clouds[cloudIndex].nodes[BoxIndex].points[j];
                    Vector3 p = viewers[clouds[cloudIndex].viewerIndex].points[pointIndex];

                    // check if within bounds distance
                    if (box.Contains(p) == true)
                    {
                        return true;
                    }
                }
            }
            return false;
        } // BoundsIntersectsCloud

        // area selection from multiple clouds, returns list of collectedpoints struct (which contains indexes to cloud and the actual point)
        public List<CollectedPoint> ConvexHullSelectPoints(GameObject go, List<Vector3> area)
        {
            // build area bounds
            var areaBounds = new Bounds();
            for (int i = 0, len = area.Count; i < len; i++)
            {
                areaBounds.Encapsulate(area[i]);
            }

            // check bounds
            //PointCloudTools.DrawBounds(areaBounds, 100);

            // build area hull
            var calc = new ConvexHullCalculator();

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var normals = new List<Vector3>();
            var mf = go.GetComponent<MeshFilter>();
            if (go == null) Debug.LogError("Missing MeshFilter from " + go.name, go);
            var mesh = new Mesh();
            mf.sharedMesh = mesh;

            calc.GenerateHull(area, false, ref verts, ref tris, ref normals);

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(normals);

            var results = new List<CollectedPoint>();
            // all clouds
            for (int cloudIndex = 0, length2 = clouds.Count; cloudIndex < length2; cloudIndex++)
            {
                // exit if outside whole cloud bounds
                if (clouds[cloudIndex].bounds.Intersects(areaBounds) == false) return null;

                // check all nodes from this cloud
                for (int nodeIndex = 0; nodeIndex < clouds[cloudIndex].nodes.Length; nodeIndex++)
                {
                    // early exit if bounds doesnt hit this node?
                    if (clouds[cloudIndex].nodes[nodeIndex].bounds.Intersects(areaBounds) == false) continue;

                    // loop points
                    for (int j = 0, l = clouds[cloudIndex].nodes[nodeIndex].points.Count; j < l; j++)
                    {
                        // check all points from that node
                        int pointIndex = clouds[cloudIndex].nodes[nodeIndex].points[j];
                        // get actual point
                        Vector3 p = viewers[clouds[cloudIndex].viewerIndex].points[pointIndex];

                        // check if inside hull
                        if (IsPointInsideMesh(mesh, p))
                        {
                            var temp = new CollectedPoint();
                            temp.cloudIndex = cloudIndex;
                            temp.pointIndex = pointIndex;
                            results.Add(temp);
                        }
                    }

                }

            } // for clouds
            return results;
        }

        // source http://answers.unity.com/answers/612014/view.html
        public bool IsPointInsideMesh(Mesh aMesh, Vector3 point)
        {
            var verts = aMesh.vertices;
            var tris = aMesh.triangles;
            int triangleCount = tris.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                var V1 = verts[tris[i * 3]];
                var V2 = verts[tris[i * 3 + 1]];
                var V3 = verts[tris[i * 3 + 2]];
                var P = new Plane(V1, V2, V3);
                if (P.GetSide(point)) return false;
            }
            return true;
        }

        // https://gamedev.stackexchange.com/a/103714/73429
        float RayBoxIntersect2(Vector3 rpos, Vector3 irdir, Vector3 vmin, Vector3 vmax)
        {
            float t1 = (vmin.x - rpos.x) * irdir.x;
            float t2 = (vmax.x - rpos.x) * irdir.x;
            float t3 = (vmin.y - rpos.y) * irdir.y;
            float t4 = (vmax.y - rpos.y) * irdir.y;
            float t5 = (vmin.z - rpos.z) * irdir.z;
            float t6 = (vmax.z - rpos.z) * irdir.z;
            float aMin = t1 < t2 ? t1 : t2;
            float aMax = t1 > t2 ? t1 : t2;
            float bMin = t3 < t4 ? t3 : t4;
            float bMax = t3 > t4 ? t3 : t4;
            float cMin = t5 < t6 ? t5 : t6;
            float cMax = t5 > t6 ? t5 : t6;
            float fMax = aMin > bMin ? aMin : bMin;
            float fMin = aMax < bMax ? aMax : bMax;
            float t7 = fMax > cMin ? fMax : cMin;
            float t8 = fMin < cMax ? fMin : cMax;
            float t9 = (t8 < 0 || t7 > t8) ? -1 : t7;
            return t9;
        }
    } // class
} // namespace