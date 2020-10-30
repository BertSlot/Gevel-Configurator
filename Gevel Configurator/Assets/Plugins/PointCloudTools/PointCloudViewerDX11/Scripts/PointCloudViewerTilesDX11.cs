// Point Cloud Binary Viewer DX11 with Tiles (v3)
// http://unitycoder.com

using UnityEngine;
using System.IO;
using System.Threading;
using PointCloudHelpers;
using System;
using System.Collections;
using Priority_Queue;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections.Generic;
using UnityLibrary;
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace unitycodercom_PointCloudBinaryViewer
{
    public class PointCloudViewerTilesDX11 : MonoBehaviour
    {
        [Header("Load")]
        public string rootFile = "StreamingAssets/.tiles/manu.pcroot";

        [Header("Settings")]
        public bool loadAtStart = true;
        [Tooltip("Use PointCloudColorSizeDX11v2.mat to get started, then experiment with other materials if needed")]
        public Material cloudMaterial;
        [Tooltip("Create copy of the material. Must enable if using multiple viewers with the same material")]
        public bool instantiateMaterial = false; // set True if using multiple viewers
#if UNITY_2019_1_OR_NEWER
        [Tooltip("Use native arrays (2019.1 or newer only)")]
        public bool useNativeArrays = false;
        [Tooltip("If using native arrays, memory can be released for far away tiles")]
        public bool releaseTileMemory = false;
#endif
        [Header("Visibility")]
        [Tooltip("Enable this if you have multiple cameras and only want to draw in MainCamera")]
        public bool renderOnlyMainCam = false;

        private Camera cam;
        string applicationStreamingAssetsPath;

        // cullingmanager
        [Header("LOD")]
        [Tooltip("All tiles below this distance to camera will have 100% points")]

        public float startDist = 10;
        [Tooltip("All tiles after this distance will be culled out (and tiles in between this and startdist will have % of points reduced")]
        public float endDist = 250;
        int lodSteps = 100;
        [Tooltip("If disabled, uses Linear falloff, if enabled uses faster falloff (points disappear faster, good for dense clouds)")]
        public bool useStrongFalloff = true;

        // TODO allow setting better distances, or use grid size?
        float[] cullingBandDistances = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 150, 200, float.PositiveInfinity };
        //float[] cullingBandDistances = new float[] { 8, 16, 32, 64, 128, 256, float.PositiveInfinity };
        CullingGroup cullGroup;

        [Tooltip("Make this 0 if you want to render all tiles, even if it has only 1 point. Make this value bigger to ignore tiles with less than x points")]
        public int minimumTilePointCount = 256;

        [Header("Rendering")]
        [Tooltip("1 = send data in big chunk (can cause spike), 32=send within 32 slices (smaller or non-noticeable spikes, but tiles appear bit slower)")]
        [Range(0, 48)]
        public int gpuUploadSteps = 16; // bigger value means, point data upload is spread to more frames (less laggy, but takes longer to appear), good values: 1 - 18
        [Tooltip("Global tile resolution multiplier: 1 = Keep original resolution, 0.5 = half resolution, 0 = 0 points visible. Resolution is updated only during tile update (not instantly)")]
        [Range(0f, 1f)]
        public float tileResolution = 1f;
        [Tooltip("Enable global point size multiplier (Not supported by most shaders, requires _SizeMultiplier variable in shader)")]
        public bool useSizeMultiplier = false;
        [Tooltip("Global point size multiplier: 1 = Keep original size, 0.5 = half size. NOTE: Requires shader with SizeMultiplier parameter!")]
        public float pointSizeMultiplier = 1f;

        [Header("Advanced")]
        [Tooltip("Force Garbage Collection after loading")]
        public bool forceGC = false;

        [Header("Experimental")]
        [Tooltip("Warning: Does not support colors! Use regular mesh renderers. Remember to use assign mesh prefab, with suitable material (CloudMaterial is not used)")]
        public bool useMeshRendering = false;
        [Tooltip("Prefab for mesh tiles")]
        public MeshFilter meshPrefab;


        // events
        public delegate void OnLoadComplete(string filename);
        public event OnLoadComplete OnLoadingComplete;

        PointCloudTile[] tiles;
        float gridSize = 5;
        int packMagic = 64; //64 = is default in packer

        // Threading
        bool abortReaderThread = false;

        int tempIndexA = 0;
        int tempIndexB = 0;

        System.Diagnostics.Stopwatch stopwatch;

        bool isInitializingBuffersA = false;
        bool isInitializingBuffersB = false;
        int bufID = Shader.PropertyToID("buf_Points");
        int bufColorID = Shader.PropertyToID("buf_Colors");

        //bool initDone = false;

        Thread importerThreadA;
        Thread importerThreadB;

        SimplePriorityQueue<int> loadQueueA = new SimplePriorityQueue<int>();
        SimplePriorityQueue<int> loadQueueB = new SimplePriorityQueue<int>();

        bool threadRunningA = false;
        bool threadRunningB = false;

        const char sep = '|';

        Bounds cloudBounds;
        Vector3 cloudOffset;

        int v3Version = -1;

        long totalPointCount = 0;
        int tilesCount = 0;

        bool rootLoaded = false;

        // point picking
        public delegate void PointSelected(Vector3 pointPos);
        public event PointSelected PointWasSelected;


        void Awake()
        {
            applicationStreamingAssetsPath = Application.streamingAssetsPath;
            FixMainThreadHelper();
        }

        //List<PointCloudMeshTile> meshPool;
        SimplePriorityQueue<int> meshUpdateQueue = new SimplePriorityQueue<int>();
        int[] indices;

        // init
        void Start()
        {
            cam = Camera.main;

            if (cam == null) { Debug.LogError("Camera Main is missing..", gameObject); }

            // create material clone, so can view multiple clouds
            if (instantiateMaterial == true)
            {
                cloudMaterial = new Material(cloudMaterial);
            }

            if (useMeshRendering == true)
            {
                // TODO set max point count here
                int bbb = 128000 * 3;
                indices = new int[bbb];
                for (int i = 0; i < bbb; i++)
                {
                    indices[i] = i;
                }

                StartCoroutine(MeshUpdater());
                //    // generate mesh pool
                //    meshPool = new List<PointCloudMeshTile>();

                //    // init with few
                //    for (int i = 0; i < 128; i++)
                //    {

                //        // create mesh
                //        var mesh = new Mesh();
                //        mesh.MarkDynamic();

                //        // create go
                //        var mf = Instantiate(meshPrefab) as MeshFilter;
                //        mf.gameObject.SetActive(false);
                //        mf.mesh = mesh;

                //        // create tile
                //        var meshTile = new PointCloudMeshTile();
                //        meshTile.mesh = mesh;
                //        meshTile.meshFilter = mf;

                //        meshPool.Add(meshTile);
                //    }
            }

            if (loadAtStart == true)
            {
                abortReaderThread = false;

                // all these are required to do
                rootLoaded = LoadRootFile(rootFile);
                if (rootLoaded == true) StartCoroutine(InitCullingSystem());
            }

            // add warnings if bad setttings
#if UNITY_2019_1_OR_NEWER
            if (useNativeArrays == false && releaseTileMemory == true)
            {
                Debug.LogWarning("useNativeArrays is not enabled, but releaseTileMemory is enabled - Cannot release memory for managed memory tiles");
            }
#endif
        } // Start

        int[] tempIndices = new int[128];

        // TODO use thread and new mesh stuff
        IEnumerator MeshUpdater()
        {
            while (true)
            {
                if (meshUpdateQueue.Count > 0)
                {
                    var meshIndex = meshUpdateQueue.Dequeue();
                    tiles[meshIndex].meshTile.mesh.vertices = tiles[meshIndex].points;

                    // TODO handle native array colors
                    // TODO try to replace with some better array copy, or convert in load? or use shader array for colors?

                    // FIXME too slow
                    //tiles[meshIndex].meshTile.mesh.colors = new Color[tiles[meshIndex].points.Length];
                    //for (int i = 0, length = tiles[meshIndex].points.Length; i < length; i++)
                    //{
                    //    var v = tiles[meshIndex].colors[i];
                    //    tiles[meshIndex].meshTile.mesh.colors[i] = new Color(v.x, v.y, v.z, 1);
                    //}

                    //tiles[meshIndex].meshTile.mesh.colors = tiles[meshIndex].colors; // NOTE cannot have colors, since it wants Color[] we have Vector3[]

                    // TODO no need to set these everytime? or slice, or they are preset?? NOTE can be just 3?? bug this resets bounds
                    // NOTE settings indices count affects how many points get drawn!
                    //tiles[meshIndex].meshTile.mesh.SetIndices(indices.Take(tiles[meshIndex].loadedPoints).ToArray(), MeshTopology.Points, 0, false);

                    tempIndices = new int[tiles[meshIndex].visiblePoints];
                    Buffer.BlockCopy(indices, 0, tempIndices, 0, tiles[meshIndex].visiblePoints);

                    tiles[meshIndex].meshTile.mesh.SetIndices(tempIndices, MeshTopology.Points, 0, false);

                    // needs this or cull off?
                    tiles[meshIndex].meshTile.mesh.bounds = new Bounds(tiles[meshIndex].center, new Vector3(tiles[meshIndex].maxX - tiles[meshIndex].minX, tiles[meshIndex].maxY - tiles[meshIndex].minY, tiles[meshIndex].maxZ - tiles[meshIndex].minZ));

                    //tiles[meshIndex].meshTile.mesh.triangles = indices;
                }
                yield return 0;
            }
        }

        //void Update()
        //{
        //}

        bool isPackedColors = false;
        string[] filenames;
        string[] filenamesRGB;
        float gridSizePackMagic = 0;

        // TODO this could run in a separate thread
        bool LoadRootFile(string filePath)
        {
            if (Path.IsPathRooted(filePath) == false)
            {
                filePath = Path.Combine(applicationStreamingAssetsPath, filePath);
            }

            if (PointCloudTools.CheckIfFileExists(filePath) == false)
            {
                Debug.LogError("File not found: " + filePath);
                return false;
            }

            if (Path.GetExtension(filePath).ToLower() != ".pcroot")
            {
                Debug.LogError("File is not V3 root file (.pcroot extension is required) : " + filePath);
                return false;
            }

            StartWorkerThreads();

            Debug.Log("(Tiles Viewer) Loading root file: " + filePath);
            var rootData = File.ReadAllLines(filePath);
            var rootFolder = Path.GetDirectoryName(filePath);

            // get global settings from first row : version | gridsize | totalpointcount | boundsx | boundsy | boundsz | autooffsetx | autooffsety | autooffsetz

            var globalData = rootData[0].Split(sep);

            if (globalData != null && globalData.Length >= 9)
            {
                v3Version = int.Parse(globalData[0], CultureInfo.InvariantCulture);

                if (v3Version < 1 || v3Version > 2)
                {
                    Debug.LogError("v3 header version (" + v3Version + ") is not supported in this viewer!");
                    return false;
                }

                if (v3Version == 2)
                {
                    isPackedColors = true;
                    Debug.LogWarning("(Tiles Viewer) V3 format #2 detected: Packed colors (Make sure you use material that supports PackedColors)");
                }

                gridSize = float.Parse(globalData[1], CultureInfo.InvariantCulture);
                totalPointCount = long.Parse(globalData[2], CultureInfo.InvariantCulture);
                Debug.Log("(Tiles Viewer) Total point count = " + totalPointCount + " (" + PointCloudTools.HumanReadableCount(totalPointCount) + ")");
                var minX = float.Parse(globalData[3], CultureInfo.InvariantCulture);
                var minY = float.Parse(globalData[4], CultureInfo.InvariantCulture);
                var minZ = float.Parse(globalData[5], CultureInfo.InvariantCulture);

                var maxX = float.Parse(globalData[6], CultureInfo.InvariantCulture);
                var maxY = float.Parse(globalData[7], CultureInfo.InvariantCulture);
                var maxZ = float.Parse(globalData[8], CultureInfo.InvariantCulture);

                var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
                var size = new Vector3(Mathf.Abs(maxX) + Mathf.Abs(minX), Mathf.Abs(maxY) + Mathf.Abs(minY), Mathf.Abs(maxZ) + Mathf.Abs(minZ));
                cloudBounds = new Bounds(center, size);

                var offX = float.Parse(globalData[9], CultureInfo.InvariantCulture);
                var offY = float.Parse(globalData[10], CultureInfo.InvariantCulture);
                var offZ = float.Parse(globalData[11], CultureInfo.InvariantCulture);

                if (v3Version == 2)
                {
                    packMagic = int.Parse(globalData[12], CultureInfo.InvariantCulture);
                }

                cloudOffset = new Vector3(offX, offY, offZ);

                //PointCloudTools.DrawBounds(cloudBounds, 100);
            }
            else
            {
                Debug.LogError("Failed to parse global values from " + filePath);
                return false;
            }

            int tileCount = rootData.Length - 1;
            tiles = new PointCloudTile[tileCount];
            tilesCount = tiles.Length;

            Debug.Log("(Tiles Viewer) Found " + tileCount + " tiles");
            if (tileCount <= 0)
            {
                Debug.LogError("Failed parsing V3 tiles root, no tiles found! Check this file in notepad, does it contain data? Usually this happens if your conversion scaling is wrong (not scaling to smaller), one cell only gets few points.." + filePath);

            }

            // arrays for filenames
            filenames = new string[tilesCount];
            filenamesRGB = new string[tilesCount];

            // get data, start from next row
            int i = 0;
            for (int rowIndex = 0; rowIndex < tileCount; rowIndex++)
            {
                var row = rootData[rowIndex + 1].Split('|');

                var t = new PointCloudTile();

                //t.filename = Path.Combine(rootFolder, row[0]);
                filenames[rowIndex] = Path.Combine(rootFolder, row[0]);

                //if (isPackedColors == false) t.filenameRGB = Path.Combine(rootFolder, row[0] + ".rgb");
                if (isPackedColors == false) filenamesRGB[rowIndex] = Path.Combine(rootFolder, row[0] + ".rgb");

                t.totalPoints = int.Parse(row[1], CultureInfo.InvariantCulture);
                t.visiblePoints = 0;
                t.loadedPoints = 0;

                // tile bounds
                t.minX = float.Parse(row[2], CultureInfo.InvariantCulture);
                t.minY = float.Parse(row[3], CultureInfo.InvariantCulture);
                t.minZ = float.Parse(row[4], CultureInfo.InvariantCulture);
                t.maxX = float.Parse(row[5], CultureInfo.InvariantCulture);
                t.maxY = float.Parse(row[6], CultureInfo.InvariantCulture);
                t.maxZ = float.Parse(row[7], CultureInfo.InvariantCulture);

                if (isPackedColors == true)
                {
                    t.cellX = int.Parse(row[8], CultureInfo.InvariantCulture);
                    t.cellY = int.Parse(row[9], CultureInfo.InvariantCulture);
                    t.cellZ = int.Parse(row[10], CultureInfo.InvariantCulture);
                }

                t.center = new Vector3((t.minX + t.maxX) * 0.5f, (t.minY + t.maxY) * 0.5f, (t.minZ + t.maxZ) * 0.5f);
                //Debug.Log("t.minX=" + t.minX + " row[2]=" + row[2]);

                t.isLoading = false;
                t.isReady = false;

                t.material = new Material(cloudMaterial);

                // TODO dont create all, uses memory? use pooling..
                if (useMeshRendering == true)
                {

                    // create mesh
                    var mesh = new Mesh();
                    mesh.MarkDynamic();
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    //mesh.SetIndices(indices, MeshTopology.Points, 0, false);
                    mesh.bounds = new Bounds(t.center, new Vector3(t.maxX - t.minX, t.maxY - t.minY, t.maxZ - t.minZ));
                    //mesh.bounds = 

                    if (i < 10)
                    {
                        PointCloudTools.DrawBounds(mesh.bounds, 99);
                    }

                    // create gameobject for mesh
                    var mf = Instantiate(meshPrefab) as MeshFilter;
                    mf.gameObject.SetActive(false);

                    // set pos
                    // without setpos works, but culling breaks
                    //mf.transform.position = new Vector3(t.cellX, t.cellY, t.cellZ); // wrong
                    //mf.transform.position = t.center/16; // wrong
                    //mf.transform.position = new Vector3(t.minX, t.minY, t.minZ); // wrong
                    //mf.transform.position = mesh.bounds.min; // wrong

                    mf.mesh = mesh;

                    // prepare material for packed
                    if (isPackedColors)
                    {
                        var mr = mf.GetComponent<MeshRenderer>();
                        t.material = new Material(mr.material);
                        mr.material = t.material;
                    }

                    // create tile
                    var meshTile = new PointCloudMeshTile();
                    meshTile.mesh = mesh;
                    meshTile.meshFilter = mf;

                    t.meshTile = meshTile;
                }

                // set offset for packed
                if (isPackedColors == true)
                {
                    t.material.SetVector("_Offset", new Vector3(t.cellX * gridSize, t.cellY * gridSize, t.cellZ * gridSize));
                    gridSizePackMagic = gridSize * packMagic;
                    t.material.SetFloat("_GridSizeAndPackMagic", gridSizePackMagic);
                }

                tiles[i] = t;
                i++;
            }
            return true;
        } // LoadRootFile

        void StartWorkerThreads()
        {
            if (threadRunningA == false)
            {
                threadRunningA = true;
                ParameterizedThreadStart start = new ParameterizedThreadStart(LoaderWorkerThreadA);
                importerThreadA = new Thread(start);
                importerThreadA.IsBackground = true;
                importerThreadA.Start(null);
            }

            if (threadRunningB == false)
            {
                threadRunningB = true;
                ParameterizedThreadStart startB = new ParameterizedThreadStart(LoaderWorkerThreadB);
                importerThreadB = new Thread(startB);
                importerThreadB.IsBackground = true;
                importerThreadB.Start(null);
            }
        }

        // keeps loading data, when have something in load queue
        void LoaderWorkerThreadA(System.Object temp)
        {
            while (abortReaderThread == false)
            {
                try
                {
                    if (loadQueueA.Count > 0)
                    {
                        int loadIndex = loadQueueA.Dequeue();
                        //Debug.Log("Loading queue=" + loadIndex);
                        ReadPointCloudThreadedNewA(loadIndex);
                        //Thread.Sleep(200);
                    }
                    else
                    {
                        // waiting for work
                        Thread.Sleep(2); // was 16
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

            }
            //Debug.Log("(Worker A) Thread ended.");
            threadRunningA = false;
        }

        void LoaderWorkerThreadB(System.Object temp)
        {
            while (abortReaderThread == false)
            {
                try
                {
                    if (loadQueueB.Count > 0)
                    {
                        int loadIndex = loadQueueB.Dequeue();
                        //Debug.Log("Loading queue=" + loadIndex);
                        ReadPointCloudThreadedNewB(loadIndex);
                        //Thread.Sleep(200);
                    }
                    else
                    {
                        // waiting for work
                        Thread.Sleep(2); // was 16
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            //Debug.Log("(Worker B) Thread ended.");
            threadRunningB = false;
        }

        byte[] dataPointsA = null;
        // v3 tiles format
        public void ReadPointCloudThreadedNewA(System.Object rawindex)
        {
            int index = (int)rawindex;
            tiles[index].isLoading = true;
            int newPointCount = tiles[index].totalPoints; // FIXME why whole count? but causes flicker if read only needed amount
            int dataBytesSize = newPointCount * 12;

            // points
#if UNITY_2019_1_OR_NEWER
            if (useNativeArrays == true)
            {
                if (tiles[index].pointsNative.IsCreated == true) tiles[index].pointsNative.Dispose();
                tiles[index].pointsNative = new NativeArray<byte>(dataBytesSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                // TODO add small amount reader
                dataPointsA = File.ReadAllBytes(filenames[index]);
                //Debug.Log(index + " pointCount=" + newPointCount + " points.len = " + tiles[index].points.Length + "  datapointsA.len = " + dataPointsA.Length);
                if (abortReaderThread || tiles[index].pointsNative.IsCreated == false) return;
                PointCloudMath.MoveFromByteArray<byte>(ref dataPointsA, ref tiles[index].pointsNative);
            }
            else
            {
                tiles[index].points = new Vector3[newPointCount];
                GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
                IntPtr pV = vectorPointer.AddrOfPinnedObject();
                // TODO add small amount reader
                dataPointsA = File.ReadAllBytes(filenames[index]);
                Marshal.Copy(dataPointsA, 0, pV, dataBytesSize);
                vectorPointer.Free();
            }
#else
            tiles[index].points = new Vector3[newPointCount];
            GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
            IntPtr pV = vectorPointer.AddrOfPinnedObject();

            // if need to load full cloud, TODO load full cloud also if near 80-90 % amount, if its faster..
            //if (1==1)//pointCount == tiles[index].totalPoints)
            //{
            dataPointsA = File.ReadAllBytes(filenames[index]);
            //}
            //else // read only required amount
            //{
            //    dataPointsA = new byte[dataBytesSize];

            //    using (var stream = new FileStream(filenames[index], FileMode.Open))
            //    {
            //        var reader = new BinaryReader(stream);
            //        stream.Position = 0;
            //        var bufferedReader = new BufferedBinaryReader(stream, 4096);
            //        var numBytesRead = stream.Read(dataPointsA, 0, dataBytesSize);
            //    }
            //}

            Marshal.Copy(dataPointsA, 0, pV, dataBytesSize);
            vectorPointer.Free();

            if (useMeshRendering == true)
            {
                // TODO add to update priorityqueue, to set points in mainthread
                //tiles[index].meshTile.mesh.vertices = tiles[index].points;
                meshUpdateQueue.Enqueue(index, 100); // TODO check priority from distance
            }

#endif
            tiles[index].loadedPoints = newPointCount;// tiles[index].totalPoints;

            if (forceGC == true) GC.Collect();

            // colors
            if (isPackedColors == false)
            {
#if UNITY_2019_1_OR_NEWER

                if (useNativeArrays == true)
                {
                    if (tiles[index].colorsNative.IsCreated == true) tiles[index].colorsNative.Dispose();
                    tiles[index].colorsNative = new NativeArray<byte>(dataBytesSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    dataPointsA = File.ReadAllBytes(filenamesRGB[index]);
                    if (abortReaderThread || tiles[index].colorsNative.IsCreated == false) return;
                    PointCloudMath.MoveFromByteArray<byte>(ref dataPointsA, ref tiles[index].colorsNative);
                    if (forceGC == true) GC.Collect();
                }
                else
                {
                    tiles[index].colors = new Vector3[newPointCount];
                    GCHandle vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
                    IntPtr pV = vectorPointer.AddrOfPinnedObject();
                    dataPointsA = File.ReadAllBytes(filenamesRGB[index]);
                    Marshal.Copy(dataPointsA, 0, pV, dataBytesSize);
                    vectorPointer.Free();
                }
#else
                tiles[index].colors = new Vector3[newPointCount];
                vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
                pV = vectorPointer.AddrOfPinnedObject();
                dataPointsA = File.ReadAllBytes(filenamesRGB[index]);
                Marshal.Copy(dataPointsA, 0, pV, dataBytesSize);
                vectorPointer.Free();
#endif
                if (forceGC == true) GC.Collect();
            }

            // refresh buffers, check if needed?
            isInitializingBuffersA = true;
            tempIndexA = index;
            MainThread.Call(CallInitDX11BufferA);

            while (isInitializingBuffersA == true && abortReaderThread == false)
            {
                Thread.Sleep(1);
            }

            tiles[index].isInQueue = false;
            tiles[index].isLoading = false;
            tiles[index].isReady = true;

        } // ReadPointCloudThreadedA

        byte[] dataPointsB = new byte[1];
        public void ReadPointCloudThreadedNewB(System.Object rawindex)
        {
            int index = (int)rawindex;
            tiles[index].isLoading = true;
            // TODO whole cloud gets read? but then faster to increase, no need to reload again?
            int newPointCount = tiles[index].totalPoints;
            //int newPointCount = tiles[index].visiblePoints;

            int dataBytesSize = newPointCount * 12;

            if (newPointCount == 0)
            {
                tiles[index].isLoading = false;
                return;
            }

            // read points
#if UNITY_2019_1_OR_NEWER
            if (useNativeArrays == true)
            {
                if (tiles[index].pointsNative.IsCreated == true) tiles[index].pointsNative.Dispose();
                tiles[index].pointsNative = new NativeArray<byte>(dataBytesSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                dataPointsB = File.ReadAllBytes(filenames[index]);
                if (abortReaderThread || tiles[index].pointsNative.IsCreated == false) return;
                PointCloudMath.MoveFromByteArray(ref dataPointsB, ref tiles[index].pointsNative);
            }
            else
            {
                tiles[index].points = new Vector3[newPointCount];
                GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
                IntPtr pV = vectorPointer.AddrOfPinnedObject();
                dataPointsB = File.ReadAllBytes(filenames[index]);
                //Debug.Log(index + " pointCount=" + pointCount + " points.len = " + tiles[index].points.Length + "  datapoints.len = " + dataPoints.Length);
                Marshal.Copy(dataPointsB, 0, pV, dataBytesSize);
                vectorPointer.Free();
            }
#else
            tiles[index].points = new Vector3[newPointCount];
            GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
            IntPtr pV = vectorPointer.AddrOfPinnedObject();

            // if need to load full cloud, TODO load full cloud also if near 80-90 % amount, if its faster..
            // FIXME should read bigger amount anyways, since otherwise need to load more points soon again to increase count
            //if (1==1)//newPointCount == tiles[index].totalPoints)
            //{
            dataPointsB = File.ReadAllBytes(filenames[index]);
            //}
            //else // read only required amount
            //{
            //    // TODO no need to completely erase, just resize would be needed..
            //    dataPointsB = new byte[dataBytesSize];
            //    //Array.Resize(ref dataPointsB, dataBytesSize);

            //    using (var stream = new FileStream(filenames[index], FileMode.Open))
            //    {
            //        var reader = new BinaryReader(stream);
            //        stream.Position = 0;
            //        var bufferedReader = new BufferedBinaryReader(stream, 4096);
            //        // read only missing points
            //        var missingPoints = newPointCount - tiles[index].loadedPoints;
            //        //if (missingPoints < 0)
            //        {
            //            //  Debug.LogWarning("index=" + index + "  MissingBytes = " + missingPoints + " loaded=" + tiles[index].loadedPoints + " visible=" + tiles[index].visiblePoints);
            //            //return;
            //        }
            //        //else
            //        {
            //            // read incrementally
            //            //var numBytesRead = stream.Read(dataPointsB, tiles[index].loadedPoints * 12, missingPoints * 12);
            //            // read from start
            //            var numBytesRead = stream.Read(dataPointsB, 0, dataBytesSize);
            //        }
            //    }
            //}

            //Debug.Log(index + " pointCount=" + pointCount + " points.len = " + tiles[index].points.Length + "  datapoints.len = " + dataPoints.Length);
            //Debug.Log("filelen= " + dataPointsB.Length + " dataneeded: " + (dataBytesSize));

            Marshal.Copy(dataPointsB, 0, pV, dataBytesSize);
            vectorPointer.Free();

            if (useMeshRendering == true)
            {
                // TODO add to update priorityqueue, to set points in mainthread
                //tiles[index].meshTile.mesh.vertices = tiles[index].points;
                meshUpdateQueue.Enqueue(index, 100); // TODO check priority from distance
            }

#endif
            // set current amount (TODO could keep loaded points in memory still!!)
            tiles[index].loadedPoints = newPointCount; // tiles[index].totalPoints;
            //tiles[index].visiblePoints = newPointCount;

            if (forceGC == true) GC.Collect();

            // colors
            if (isPackedColors == false)
            {
#if UNITY_2019_1_OR_NEWER
                if (useNativeArrays == true)
                {
                    if (tiles[index].colorsNative.IsCreated == true) tiles[index].colorsNative.Dispose();
                    tiles[index].colorsNative = new NativeArray<byte>(dataBytesSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    dataPointsB = File.ReadAllBytes(filenamesRGB[index]);
                    if (abortReaderThread || tiles[index].colorsNative.IsCreated == false) return;
                    PointCloudMath.MoveFromByteArray(ref dataPointsB, ref tiles[index].colorsNative);
                }
                else
                {
                    tiles[index].colors = new Vector3[newPointCount];
                    GCHandle vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
                    IntPtr pV = vectorPointer.AddrOfPinnedObject();
                    dataPointsB = File.ReadAllBytes(filenamesRGB[index]);
                    Marshal.Copy(dataPointsB, 0, pV, dataBytesSize);
                    vectorPointer.Free();
                }
#else
                tiles[index].colors = new Vector3[newPointCount];
                vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
                pV = vectorPointer.AddrOfPinnedObject();
                dataPointsB = File.ReadAllBytes(filenamesRGB[index]);
                Marshal.Copy(dataPointsB, 0, pV, dataBytesSize);
                vectorPointer.Free();
#endif
                if (forceGC == true) GC.Collect();
            }

            // refresh buffers, check if needed?
            isInitializingBuffersB = true;
            tempIndexB = index;
            MainThread.Call(CallInitDX11BufferB);

            while (isInitializingBuffersB == true && abortReaderThread == false)
            {
                Thread.Sleep(1);
            }

            //Debug.Log("Done loading index=" + index + " points=" + pointCount);
            tiles[index].isInQueue = false;
            tiles[index].isLoading = false;
            tiles[index].isReady = true;

        } // ReadPointCloudThreadedB

        void CallInitDX11BufferA()
        {
            StartCoroutine(InitDX11BufferA());
        }

        void CallInitDX11BufferB()
        {
            StartCoroutine(InitDX11BufferB());
        }

        IEnumerator InitDX11BufferA()
        {
            int nodeIndex = tempIndexA;

            int pointCount = tiles[nodeIndex].loadedPoints;
            if (pointCount == 0)
            {
                isInitializingBuffersA = false;
                yield break;
            }

            // init buffers on demand, otherwise grabs full memory
            if (tiles[nodeIndex].bufferPoints != null) tiles[nodeIndex].bufferPoints.Release();
            tiles[nodeIndex].bufferPoints = new ComputeBuffer(pointCount, 12);
            if (isPackedColors == false)
            {
                if (tiles[nodeIndex].bufferColors != null) tiles[nodeIndex].bufferColors.Release();
                tiles[nodeIndex].bufferColors = new ComputeBuffer(pointCount, 12);
            }

            int stepSize = pointCount / (gpuUploadSteps == 0 ? 1 : gpuUploadSteps);
            //if (stepSize == 0) Debug.LogError(nodeIndex + "  pointCount=" + pointCount + " stepSize=" + stepSize);
            int stepSizeBytes = stepSize;
            int stepCount = pointCount / stepSize;
            int startIndex = 0;
#if UNITY_2019_1_OR_NEWER
            if (useNativeArrays == true)
            {
                stepSizeBytes *= 12;
            }
#endif
            for (int i = 0; i < stepCount; i++)
            {
#if UNITY_2019_1_OR_NEWER
                if (useNativeArrays == true)
                {
                    if (tiles[nodeIndex].pointsNative.IsCreated == false) continue;
                    tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].pointsNative, startIndex, startIndex, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
                }
                else
                {
                    tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, startIndex, startIndex, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
                }
#else
                tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, startIndex, startIndex, stepSizeBytes);
                tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
#endif
                if (gpuUploadSteps > 0) yield return 0;

                if (isPackedColors == false)
                {
#if UNITY_2019_1_OR_NEWER
                    if (useNativeArrays == true)
                    {
                        if (tiles[nodeIndex].colorsNative.IsCreated == false) continue;
                        tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colorsNative, startIndex, startIndex, stepSizeBytes);
                        tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);

                    }
                    else
                    {
                        tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, startIndex, startIndex, stepSizeBytes);
                        tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
                    }
#else
                    tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, startIndex, startIndex, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
#endif
                    if (gpuUploadSteps > 0) yield return 0;
                }
                startIndex += stepSizeBytes;
            }
            //Debug.Log(nodeIndex+" total=" + total + " / " + pointCount * 12 + " dif=" + (pointCount * 12 - total));
            isInitializingBuffersA = false;
        }

        IEnumerator InitDX11BufferB()
        {
            int nodeIndex = tempIndexB;

            int pointCount = tiles[nodeIndex].loadedPoints;
            if (pointCount == 0)
            {
                isInitializingBuffersB = false;
                yield break;
            }

            // init buffers on demand, otherwise grabs full memory
            if (tiles[nodeIndex].bufferPoints != null) tiles[nodeIndex].bufferPoints.Release();
            tiles[nodeIndex].bufferPoints = new ComputeBuffer(pointCount, 12);
            if (isPackedColors == false)
            {
                if (tiles[nodeIndex].bufferColors != null) tiles[nodeIndex].bufferColors.Release();
                tiles[nodeIndex].bufferColors = new ComputeBuffer(pointCount, 12);
            }
            int stepSize = pointCount / (gpuUploadSteps == 0 ? 1 : gpuUploadSteps);
            //if (stepSize == 0) Debug.LogError(nodeIndex + "  pointCount=" + pointCount + " stepSize=" + stepSize);
            int stepSizeBytes = stepSize;
            int stepCount = pointCount / stepSize;
            int startIndex = 0;
#if UNITY_2019_1_OR_NEWER
            if (useNativeArrays == true)
            {
                stepSizeBytes *= 12;
            }
#endif
            for (int i = 0; i < stepCount; i++)
            {

#if UNITY_2019_1_OR_NEWER
                if (useNativeArrays == true)
                {
                    if (tiles[nodeIndex].pointsNative.IsCreated == false) continue;
                    tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].pointsNative, startIndex, startIndex, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
                }
                else
                {
                    tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, startIndex, startIndex, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
                }
#else
                tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, startIndex, startIndex, stepSizeBytes);
                tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
#endif
                if (gpuUploadSteps > 0) yield return 0;

                if (isPackedColors == false)
                {

#if UNITY_2019_1_OR_NEWER
                    if (useNativeArrays == true)
                    {
                        if (tiles[nodeIndex].colorsNative.IsCreated == false) continue;
                        tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colorsNative, startIndex, startIndex, stepSizeBytes);
                        tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);

                    }
                    else
                    {
                        tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, startIndex, startIndex, stepSizeBytes);
                        tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
                    }
#else
                    tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, startIndex, startIndex, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
#endif
                    if (gpuUploadSteps > 0) yield return 0;
                }
                startIndex += stepSizeBytes;
            }
            //Debug.Log(nodeIndex+" total=" + total + " / " + pointCount * 12 + " dif=" + (pointCount * 12 - total));
            isInitializingBuffersB = false;
        }

        public void ReleaseDX11Buffers()
        {
            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].bufferPoints != null) tiles[i].bufferPoints.Release();
                tiles[i].bufferPoints = null;
                if (tiles[i].bufferColors != null) tiles[i].bufferColors.Release();
                tiles[i].bufferColors = null;
#if UNITY_2019_1_OR_NEWER
                if (useNativeArrays == true)
                {
                    if (tiles[i].pointsNative.IsCreated == true) tiles[i].pointsNative.Dispose();
                    if (isPackedColors == false && tiles[i].colorsNative.IsCreated == true) tiles[i].colorsNative.Dispose();
                }
#endif
            }
        }

        void OnDestroy()
        {
            abortReaderThread = true;

            if (pointPickingThread != null && pointPickingThread.IsAlive == true) pointPickingThread.Abort();

            ReleaseDX11Buffers();

            if (rootLoaded == true)
            {
                cullGroup.onStateChanged -= OnCullingStateChange;
                cullGroup.Dispose();
                cullGroup = null;
            }
        }


        // drawing mainloop, for drawing the points
        //void OnPostRender() // < works also, BUT must have this script attached to Camera
        public void OnRenderObject()
        {
            if (rootLoaded == false || useMeshRendering == true || (renderOnlyMainCam == true && Camera.current.CompareTag("MainCamera") == false)) return;

            for (int i = 0, len = tilesCount; i < len; i++)
            {
                if (tiles[i].isReady == false || tiles[i].isLoading == true || tiles[i].visiblePoints == 0) continue;

                tiles[i].material.SetPass(0);

#if UNITY_2019_1_OR_NEWER
                Graphics.DrawProceduralNow(MeshTopology.Points, tiles[i].visiblePoints);
#else
                Graphics.DrawProcedural(MeshTopology.Points, tiles[i].visiblePoints);
#endif
            }
        }

        // called after all tiles have been loaded
        public void OnLoadingCompleteCallBack(System.Object a)
        {
            if (OnLoadingComplete != null) OnLoadingComplete((string)a);
            Debug.Log("(Tiles Viewer) Finished loading all tiles");

            // NOTE hackfix for cullingdata not refreshing..
            StartCoroutine(RefreshCameraCulling());

            // for now, can stop worker threads
            //abortReaderThread = true;

            //PointCloudTools.DrawBounds(cloudBounds, 100);
        }

        // Temporary fix for culling group not refreshing (unless camera moved)
        IEnumerator RefreshCameraCulling()
        {
            cullGroup.SetDistanceReferencePoint(new Vector3(0, 999999, 0));
            yield return 0;
            cullGroup.SetDistanceReferencePoint(cam.transform);
        }

        public void FixMainThreadHelper()
        {
            if (GameObject.Find("#MainThreadHelper") == null || UnityLibrary.MainThread.instanceCount == 0)
            {
                var go = new GameObject();
                go.name = "#MainThreadHelper";
                go.AddComponent<UnityLibrary.MainThread>();
            }
        }

        BoundingSphere[] boundingSpheres;
        IEnumerator InitCullingSystem()
        {
            // validate values
            if (startDist < 0)
            {
                Debug.LogError("startDist must be > 0. You have set it to " + startDist);
                startDist = 0;
            }
            if (endDist < startDist)
            {
                Debug.LogError("endDist must be > startDist. You have set it to " + endDist);
                endDist = startDist + 1;
            }

            if (lodSteps < 2 || lodSteps > 180)
            {
                Debug.LogError("lodSteps must be in between 2-180. You have set it to " + lodSteps);
                lodSteps = 100;
            }

            // calculate distances
            cullingBandDistances = new float[lodSteps + 1];
            for (int i = 0; i < lodSteps; i++)
            {
                cullingBandDistances[i] = Mathf.LerpUnclamped(startDist, endDist, ((float)i) / ((float)(lodSteps - 1)));
                //Debug.Log(i + "=" + distances[i]);
            }
            // add last point, to properly hide at maxdist
            cullingBandDistances[lodSteps] = endDist + 1;

            // create culling group
            cullGroup = new CullingGroup();
            cullGroup.targetCamera = cam;

            // measure distance to camera transform
            cullGroup.SetDistanceReferencePoint(cam.transform);

            // search distance "bands" starts from 0, so index=0 is from 0 to searchDistance
            cullGroup.SetBoundingDistances(cullingBandDistances);

            // get cloud pieces
            int objectCount = tiles.Length;
            boundingSpheres = new BoundingSphere[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                var t = tiles[i];
                boundingSpheres[i].position = t.center;
                boundingSpheres[i].radius = gridSize;
            }

            // set bounds that we track
            InitCullingValues();

            // subscribe to event
            cullGroup.onStateChanged += OnCullingStateChange;
            Debug.Log("(Tiles Viewer) Done init culling");

            // fix for not calling statechange on creation
            /*
            cam.enabled = false;
            yield return new WaitForSeconds(1f);
            cam.enabled = true;
            */

            /*            
                        cam.transform.Translate(0, 99999, 0);
                        yield return new WaitForSeconds(0.2f);
                        cam.transform.Translate(0, -99999, 0);
              */

            StartCoroutine(RefreshCameraCulling());

            //initDone = true;
            yield return 0;
        }

        void InitCullingValues()
        {
            cullGroup.SetBoundingSpheres(boundingSpheres);
            cullGroup.SetBoundingSphereCount(tiles.Length);
        }

        // NOTE not so nice, and tiles outside view are not visible anyways
        //bool useMinimumPointCount = true;
        //float minimumPointCountPercentage = 0.5f;
        // TODO this could be done in shader? (sizebydistance already does?) but what about relative to point count or tilesize?
        //public AnimationCurve pointSizeByDistance;
        //bool usePointSizeByDistance = false;

        // object state has changed in culling group
        void OnCullingStateChange(CullingGroupEvent e)
        {
            //Debug.Log(e.index + " " + e.isVisible);
            // FIXME this breaks initialization?
            //if (tiles[e.index].isReady == false) return;

            //if (e.hasBecomeInvisible)
            if (e.isVisible == false) // not visible tile TODO check if this breaks dispose?
            {
                tiles[e.index].visiblePoints = 0;

                if (useMeshRendering)
                {
                    tiles[e.index].meshTile.meshFilter.gameObject.SetActive(false);
                }

                // if too far, hide, and dispose if supported
                if (e.currentDistance == lodSteps + 1)
                {
                    //PointCloudTools.DrawMinMaxBounds(tiles[e.index].minX, tiles[e.index].minY, tiles[e.index].minZ, tiles[e.index].maxX, tiles[e.index].maxY, tiles[e.index].maxZ, 10);

#if UNITY_2019_1_OR_NEWER
                    if (tiles[e.index].isInQueue || tiles[e.index].isLoading) return;
                    tiles[e.index].loadedPoints = 0;
                    tiles[e.index].isReady = false;
                    if (useNativeArrays == true)
                    {
                        if (releaseTileMemory == true && tiles[e.index].pointsNative.IsCreated)
                        {
                            tiles[e.index].pointsNative.Dispose();
                            tiles[e.index].bufferPoints.Dispose();
                            if (isPackedColors == false && tiles[e.index].colorsNative.IsCreated)
                            {
                                tiles[e.index].colorsNative.Dispose();
                                tiles[e.index].bufferColors.Dispose();
                            }
                        }
                    }
#endif
                }
                return;
            }


            if (useMeshRendering == true)
            {
                tiles[e.index].meshTile.meshFilter.gameObject.SetActive(true);
            }

            int distanceBand = e.currentDistance;

            float distanceMultiplier = (1f - (float)distanceBand / (float)(cullingBandDistances.Length - 1)) * tileResolution;
            //int newpointcount = (int)((float)tiles[e.index].loadedPoints * (useStrongFalloff ? EaseInQuint(0f, 1f, multiplier) : multiplier));
            int newpointcount = (int)((float)tiles[e.index].totalPoints * (useStrongFalloff ? EaseInQuint(0f, 1f, distanceMultiplier) : distanceMultiplier));

            // full tile
            //if (distanceBand == 0)
            //{
            //    //Debug.Log("FullTile, newcount=" + newpointcount+" / "+ tiles[e.index].totalPoints);
            //}

            // no points will be visible, TODO add minimum pointcount variable
            if (newpointcount < minimumTilePointCount) newpointcount = 0;
            //if (newpointcount == 0 && tiles[e.index].visiblePoints == 0) return;

            // update multiplier size
            if (useSizeMultiplier == true)
            {
                tiles[e.index].material.SetFloat("_SizeMultiplier", pointSizeMultiplier);
            }

            // near smaller
            //tiles[e.index].material.SetFloat("_Size", 1-pointSizeByDistance.Evaluate(multiplier));

            // far smaller
            //tiles[e.index].material.SetFloat("_Size", pointSizeByDistance.Evaluate(multiplier));

            // based on amount visible/loaded
            //if (usePointSizeByDistance == true)
            //{
            //    //float amount = tiles[e.index].totalPoints / (float)newpointcount;
            //    //Debug.Log("amount=" + (multiplier));
            //    //tiles[e.index].material.SetFloat("_Size", 0.05f * (pointSizeByDistance.Evaluate((useStrongFalloff ? EaseInQuint(10f, 10f, 1-multiplier) : pointSizeByDistance.Evaluate(multiplier)))));
            //    tiles[e.index].material.SetFloat("_Size", 0.015f * (useStrongFalloff ? EaseInQuint(1f, 200f, 1 - multiplier) : pointSizeByDistance.Evaluate(multiplier)));
            //}

            //// TODO add minimum point count and pointsize by amount here?
            //if (useMinimumPointCount == true)
            //{
            //    int minCount = (int)(tiles[e.index].totalPoints * minimumPointCountPercentage);
            //    if (newpointcount < minCount)
            //    {
            //        newpointcount = minCount;
            //    }
            //}

            // TODO round or threshold newpointcount to nearest x amount (so that we dont queue tile just because 1 is missing)
            //float missingPointsPercentage = (float)(tiles[e.index].loadedPoints + 1) / (float)(newpointcount + 1);
            if (newpointcount > tiles[e.index].loadedPoints)

            //if (missingPointsPercentage < 0.1f || (distanceBand == 0 && newpointcount > tiles[e.index].loadedPoints))
            {
                // NOTE adjusting this here might cause race conditions in loader (since that value is used there at start)
                tiles[e.index].visiblePoints = newpointcount;
                //Debug.Log(missingPointsPercentage + " Not enought points loaded, index=" + e.index + " loaded=" + tiles[e.index].loadedPoints + " needed=" + newpointcount + " isinQUEUE=" + tiles[e.index].isInQueue);

                if (tiles[e.index].isInQueue == false)
                {
                    tiles[e.index].isInQueue = true;

                    if (e.index % 2 == 0)
                    {
                        loadQueueA.Enqueue(e.index, distanceBand);
                    }
                    else
                    {
                        loadQueueB.Enqueue(e.index, distanceBand);
                    }

                    var i = e.index;
                    //var testBounds = new Bounds(tiles[i].center, new Vector3(tiles[i].maxX - tiles[i].minX, tiles[i].maxY - tiles[i].minY, tiles[i].maxZ - tiles[i].minZ));
                    //PointCloudTools.DrawBounds(testBounds, 1);

                }
            }
            else // we have enough, or too many points loaded, or close enough amount, dont add to queue
            {
                //if (tiles[e.index].isInQueue == false)
                {
                    //if (e.index == 283)
                    //{
                    //    //Debug.Log("SetVisibleCount= " + newpointcount + " index=" + e.index);
                    //    var i = e.index;
                    //    var testBounds = new Bounds(tiles[i].center, new Vector3(tiles[i].maxX - tiles[i].minX, tiles[i].maxY - tiles[i].minY, tiles[i].maxZ - tiles[i].minZ));
                    //    //PointCloudTools.DrawBounds(testBounds, 3);
                    //}
                    tiles[e.index].visiblePoints = newpointcount;
                }
            }
        }

        // easing methods by https://gist.github.com/cjddmut/d789b9eb78216998e95c Created by C.J. Kimberlin The MIT License (MIT)
        float EaseInQuint(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value * value * value + start;
        }



        Thread pointPickingThread;

        // point picking initial "brute" version
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

            //int nearestIndex = -1;
            bool foundPoint = false;
            Vector3 nearestPoint = Vector3.zero;
            //float nearestDistanceToCamera = Mathf.Infinity;
            float nearestDistanceToCamera = Mathf.NegativeInfinity;

            Ray ray = (Ray)rawRay;

            Vector3 rayDirection = ray.direction;
            Vector3 rayOrigin = ray.origin;
            Vector3 rayInverted = new Vector3(1f / ray.direction.x, 1f / ray.direction.y, 1f / ray.direction.z);
            Vector3 point;

            // check all visible tiles
            for (int i = 0, nodesLen = tiles.Length; i < nodesLen; i++)
            {
                // if not loaded
                if (tiles[i].isReady == false) continue;

                // if ray intersects tile bounds, FIXME doesnt work if origin is inside the bounds?
                if (PointCloudMath.RayBoxIntersect2(rayOrigin, rayInverted, new Vector3(tiles[i].minX, tiles[i].minY, tiles[i].minZ), new Vector3(tiles[i].maxX, tiles[i].maxY, tiles[i].maxZ)) > 0)
                {
                    // then check all points from that node
                    for (int k = 0, visiblePointCount = tiles[i].visiblePoints; k < visiblePointCount; k++)
                    {

#if UNITY_2019_1_OR_NEWER
                        if (useNativeArrays == true)
                        {
                            point.x = tiles[i].pointsNative.GetSubArray(k * 3 * 4, 4).Reinterpret<float>(1)[0];
                            point.y = tiles[i].pointsNative.GetSubArray(k * 3 * 4 + 4, 4).Reinterpret<float>(1)[0];
                            point.z = tiles[i].pointsNative.GetSubArray(k * 3 * 4 + 4 + 4, 4).Reinterpret<float>(1)[0];
                        }
                        else
#endif
                        {
                            point = tiles[i].points[k];
                        }

                        if (isPackedColors == true)
                        {
                            // need to unpack to get proper xyz
                            var xr = PointCloudMath.SuperUnpacker(point.x, gridSizePackMagic);
                            var yg = PointCloudMath.SuperUnpacker(point.y, gridSizePackMagic);
                            var zb = PointCloudMath.SuperUnpacker(point.z, gridSizePackMagic);

                            point.x = xr.y + tiles[i].cellX * gridSize;
                            point.y = yg.y + tiles[i].cellY * gridSize;
                            point.z = zb.y + tiles[i].cellZ * gridSize;
                        }


                        // check ray hit
                        float dotAngle = Vector3.Dot(rayDirection, (point - rayOrigin).normalized);

                        // 1 would be exact hit?
                        if (dotAngle > 0.99999f)
                        {
                            //MainThread.Call(PointCloudMath.DebugHighLightPointGreen, point);

                            // try to take closest ones first
                            float camDist = 999999f * dotAngle - PointCloudMath.Distance(rayOrigin, point);

                            if (camDist > nearestDistanceToCamera)
                            {
                                nearestDistanceToCamera = camDist;
                                foundPoint = true;
                                nearestPoint = point;
                                //MainThread.Call(PointCloudMath.DebugHighLightPointYellow, point);
                            }
                        }
                        else // out of threshold
                        {
                            //MainThread.Call(PointCloudMath.DebugHighLightPointGray, point);
                        }
                    } // each point inside box
                } // if ray hits box
            } // all boxes


            if (foundPoint == true)
            {
                MainThread.Call(PointCallBack, nearestPoint);
                Debug.Log("(v3) Selected Point Position:" + nearestPoint);
                //MainThread.Call(PointCloudMath.DebugHighLightPointGreen, nearestPoint);
            }
            else
            {
                Debug.Log("(v3) No points found..");
            }

            stopwatch.Stop();
            Debug.Log("(v3) PickTimer: " + stopwatch.ElapsedMilliseconds + "ms");
            stopwatch.Reset();

            if (pointPickingThread != null && pointPickingThread.IsAlive == true) pointPickingThread.Abort();
        } // FindClosesPoint

        // this gets called after thread finds closest point
        void PointCallBack(System.Object a)
        {
            if (PointWasSelected != null) PointWasSelected((Vector3)a);
        }

        // PUBLIC API

        // return whole cloud bounds
        public Bounds GetBounds()
        {
            return cloudBounds;
        }

        // returns cloud (auto/manual) offset that was used in the converter
        public Vector3 GetOffset()
        {
            return cloudOffset;
        }

        // returns total pointcount
        public long GetTotalPointCount()
        {
            return totalPointCount;
        }

        // returns visible tiles count
        public int GetVisibleTileCount()
        {
            // FIXME not working?
            return cullGroup.QueryIndices(true, new int[tilesCount], 0);
        }

        // returns total visible point count
        public long GetVisiblePointCount()
        {
            // TODO init once
            int[] visibleTiles = new int[tilesCount];
            int visibleTileCount = cullGroup.QueryIndices(true, visibleTiles, 0);
            long counter = 0;
            for (int i = 0; i < visibleTileCount; i++)
            {
                counter += tiles[i].visiblePoints;
            }
            return counter;
        }

        public Bounds[] GetAllTileBounds()
        {
            Bounds[] results = new Bounds[tilesCount];

            for (int i = 0; i < tilesCount; i++)
            {
                results[i] = new Bounds(tiles[i].center, new Vector3(tiles[i].maxX - tiles[i].minX, tiles[i].maxY - tiles[i].minY, tiles[i].maxZ - tiles[i].minZ));
            }

            return results;
        }

        // TODO get node index by position, get node count, get visible nodes, get hidden nodes, ..

    } // class
} // namespace
