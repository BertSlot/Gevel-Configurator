// Point Cloud Binary Viewer DX11
// reads custom binary file and displays it with dx11 shader
// http://unitycoder.com

#if !UNITY_WEBPLAYER && !UNITY_SAMSUNGTV

using UnityEngine;
using System.IO;
using System.Threading;
using PointCloudHelpers;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System;

namespace unitycodercom_PointCloudBinaryViewer
{
    //[ExecuteInEditMode] // ** You can enable this, if you want to see DX11 cloud inside editor, without playmode NOTE: only works with V1 .bin and with threading disabled **
    public class PointCloudViewerDX11 : MonoBehaviour
    {
        [Header("Binary Source File")]
        [Tooltip("Note: New v2 format uses .ucpc extension")]
        public string fileName = "StreamingAssets/PointCloudViewerSampleData/sample.bin";

        [Header("Settings")]
        public bool loadAtStart = true;
        public Material cloudMaterial;
        [Tooltip("Create copy of the material. Must enable if viewing multiple clouds with same materials")]
        public bool instantiateMaterial = false; // set True if using multiple viewers
        [Tooltip("Load cloud at Start()")]
        public bool useThreading = false;
        public bool showDebug = false;

        [Header("Visibility")]
        public bool displayPoints = true;
        [Tooltip("Enable this if you have multiple cameras and only want to draw in MainCamera")]
        public bool renderOnlyMainCam = false;

        [Header("Animated Point Clouds")]
        public bool isAnimated = false; // Brekel binary cloud frames
        public float playbackDelay = 0.025F;

        // Brekel animated frames variables
        private int[] numberOfPointsPerFrame;
        private System.Int64[] frameBinaryPositionForEachFrame;
        private Vector3[] animatedPointArray;
        private Vector3[] animatedColorArray;
        private int totalNumberOfPoints; // total from each frame
        private int currentFrame = 0;
        private int[] animatedOffset;
        private float nextFrame = 0.0F;
        private float[] byteArrayToFloats;
        private Bounds cloudBounds;

        private byte binaryVersion = 0;
        private int numberOfFrames = 0;
        [HideInInspector]
        public bool containsRGB = false;
        [HideInInspector]
        public int totalPoints = 0;
        [HideInInspector]
        public int totalMaxPoints = 0; // new variable to keep total maximum point count
        private ComputeBuffer bufferPoints;
        //private ComputeBuffer bufferPointsDepth;
        private ComputeBuffer bufferColors;
        internal Vector3[] points; // actual point cloud points array
        internal Vector3[] pointColors;
        private Vector3 dataColor;
        private float r, g, b;

        private bool isLoading = false;
        private bool haveError = false;
        bool isLoadingNewData = false;

        // Threading
        bool abortReaderThread = false;
        Thread importerThread;

        // events
        public delegate void OnLoadComplete(string filename);
        public event OnLoadComplete OnLoadingComplete;

        [Header("Experimental")]
        [Tooltip("Shuffle points to use dynamic resolution adjuster *Cannot use if ReadCachedFile is enabled. **V2+ formats are usually already randomized")]
        public bool randomizeArray = false;
        [Tooltip("[v1 & v2 format only] Pack colors for GPU (Note: Use V4-packed material)")]
        public bool packColors = false;
        //[Tooltip("Pack colors for GPU (Note: Use V4-packed2 material) *Not working yet")]
        //public bool packColors2 = false;
        [Tooltip("[v2 format only] Read whole cloud (initialPointsToRead value is ignored)")]
        public bool readWholeCloud = true;
        [Tooltip("[v2 format only] Read only this many points initially")]
        public int initialPointsToRead = 10000;
        [HideInInspector] public bool isNewFormat = false;

        private Camera cam;

        [Header("Rendering")]
        [Tooltip("Draw using CommandBuffer instead of OnRenderObject")]
        public bool useCommandBuffer = false;
        [Tooltip("Default value: AfterForwardOpaque")]
        public CameraEvent camDrawPass = CameraEvent.AfterForwardOpaque;
        internal CommandBuffer commandBuffer;
        public bool forceDepthBufferPass = false;
        Material depthMaterial;
        [Tooltip("Changing CameraEvent takes effect only at Start(). Default value: AfterDepthTexture")]
        public CameraEvent camDepthPass = CameraEvent.AfterDepthTexture;
#if UNITY_EDITOR
        [Tooltip("Forces CommandBuffer to be rendered in Scene window also")]
        public bool commandBufferToSceneCamera = false;
#endif
        internal CommandBuffer commandBufferDepth;
        Vector3 transformPos;
        Matrix4x4 Matrix4x4identity = Matrix4x4.identity;

        [Header("Caching")]
        [Tooltip("Save .bin file again (to include randomizing or other changes to data during load *Not supported for new V2 format)")]
        public bool reSaveBinFile = false;

        string applicationStreamingAssetsPath;

        struct PackedPoint
        {
            public float x;
            public float y;
            public float z;
        };

        void Awake()
        {
            applicationStreamingAssetsPath = Application.streamingAssetsPath;
        }

        // init
        void Start()
        {
            transformPos = transform.position;

            cam = Camera.main;

            if (useCommandBuffer == true)
            {
                commandBuffer = new CommandBuffer();
                cam.AddCommandBuffer(camDrawPass, commandBuffer);

#if UNITY_EDITOR
                if (commandBufferToSceneCamera == true) UnityEditor.SceneView.GetAllSceneCameras()[0].AddCommandBuffer(camDrawPass, commandBuffer);
#endif

            }

            if (forceDepthBufferPass == true)
            {
                depthMaterial = cloudMaterial;
                commandBufferDepth = new CommandBuffer();
                cam.AddCommandBuffer(camDepthPass, commandBufferDepth);
            }

            if (cam == null) { Debug.LogError("Camera main is missing..", gameObject); }

            // create material clone, so can view multiple clouds
            if (instantiateMaterial == true)
            {
                cloudMaterial = new Material(cloudMaterial);
            }

            if (useThreading == true)
            {
                // check if MainThread script exists in scene, its required only for threading
                FixMainThreadHelper();
            }

            if (loadAtStart == true)
            {
                if (useThreading == true)
                {
                    abortReaderThread = false;
                    CallReadPointCloudThreaded(fileName);
                }
                else
                {
                    ReadPointCloud();
                }
            }
        }

        // ====================================== mainloop ======================================
        void Update()
        {
            if (isLoading == true || haveError == true) return;

            if (isAnimated == true) // animated point cloud frames (brekel, .bin)
            {
                if (Time.time > nextFrame)
                {
                    nextFrame = Time.time + playbackDelay;
                    System.Array.Copy(animatedPointArray, animatedOffset[currentFrame], points, 0, numberOfPointsPerFrame[currentFrame]);
                    bufferPoints.SetData(points);
                    if (bufferColors != null)
                    {
                        System.Array.Copy(animatedColorArray, animatedOffset[currentFrame], pointColors, 0, numberOfPointsPerFrame[currentFrame]);
                        bufferColors.SetData(pointColors);
                    }
                    currentFrame = (++currentFrame) % numberOfFrames;
                }
            }
        } // Update()


        // binary point cloud reader *OLD, non-threaded
        public void ReadPointCloud()
        {
            Debug.LogWarning("This old non-threaded reader will be removed later.. public void ReadPointCloud()");
            // if not full path, use streaming assets
            if (Path.IsPathRooted(fileName) == false)
            {
                fileName = Path.Combine(applicationStreamingAssetsPath, fileName);
            }

            if (PointCloudTools.CheckIfFileExists(fileName) == false)
            {
                Debug.LogError("File not found:" + fileName);
                haveError = true;
                return;
            }

            Debug.Log("Reading pointcloud from: " + fileName);

            if (isAnimated == true)
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                ReadAnimatedPointCloud();

                stopwatch.Stop();
                Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
                stopwatch.Reset();
                return;
            }

            isLoading = true;

            // new loader reads whole file at once
            byte[] data;

            try
            {
                data = File.ReadAllBytes(fileName);
            }
            catch
            {
                Debug.LogError(fileName + " cannot be opened with ReadAllBytes(), it might be too large >2gb");
                return;
            }

            System.Int32 byteIndex = 0;

            int binaryVersion = data[byteIndex];
            byteIndex += sizeof(System.Byte);


            if (binaryVersion > 1)
            {
                Debug.LogError("File binaryVersion should have value (0) or (1). Loading cancelled... founded:" + binaryVersion + " (Is this animated cloud instead?)");
                return;
            }

            totalPoints = System.BitConverter.ToInt32(data, byteIndex);
            byteIndex += sizeof(System.Int32);

            containsRGB = System.BitConverter.ToBoolean(data, byteIndex);
            byteIndex += sizeof(System.Boolean);

            points = new Vector3[totalPoints];
            Debug.Log("Loading " + totalPoints + " points..");

            float x, y, z;

            float minX = Mathf.Infinity;
            float minY = Mathf.Infinity;
            float minZ = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float maxY = Mathf.NegativeInfinity;
            float maxZ = Mathf.NegativeInfinity;

            if (containsRGB == true) pointColors = new Vector3[totalPoints];

            var byteSize = sizeof(System.Single);
            byteArrayToFloats = new float[(data.Length - byteIndex) / 4];
            System.Buffer.BlockCopy(data, byteIndex, byteArrayToFloats, 0, data.Length - byteIndex);

            int dataIndex = 0;
            for (int i = 0; i < totalPoints; i++)
            {
                x = byteArrayToFloats[dataIndex];
                dataIndex++;
                byteIndex += byteSize; // not used!
                y = byteArrayToFloats[dataIndex];
                dataIndex++;
                byteIndex += byteSize;
                z = byteArrayToFloats[dataIndex];
                dataIndex++;
                byteIndex += byteSize;

                // need to move rgb after xyz
                if (containsRGB == true)
                {
                    r = byteArrayToFloats[dataIndex];
                    dataIndex++;
                    g = byteArrayToFloats[dataIndex];
                    dataIndex++;
                    b = byteArrayToFloats[dataIndex];
                    dataIndex++;
                    pointColors[i].Set(r, g, b);
                }

                points[i].Set(x, y, z);

                // get bounds
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }

            // for testing load timer
            // stopwatch.Stop();
            // Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
            // stopwatch.Reset();
            totalMaxPoints = totalPoints;

            cloudBounds = new Bounds(new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f), new Vector3((maxX - minX), (maxY - minY), (maxZ - minZ)));


            if (randomizeArray == true)
            {
                PointCloudTools.Shuffle(new System.Random(), ref points, ref pointColors);
                Debug.Log("Randomizing array: Done");
            }

            InitDX11Buffers();
            isLoading = false;
            OnLoadingCompleteCallBack(fileName);
            Debug.Log("Finished loading..");
        }

        public void CallReadPointCloudThreaded(string fullPath)
        {
            if (Path.IsPathRooted(fullPath) == false)
            {
                fullPath = Path.Combine(applicationStreamingAssetsPath, fullPath);
            }

            transformPos = transform.position;

            // TEMP, needed later in loader, should pass instead
            fileName = fullPath;
            if (PointCloudTools.CheckIfFileExists(fullPath) == false)
            {
                Debug.LogError("File not found:" + fullPath);
                return;
            }

            if (Path.GetExtension(fullPath).ToLower() != ".bin" && Path.GetExtension(fullPath).ToLower() != ".ucpc")
            {
                Debug.LogError("File is not v1 or v2 file (.bin or .ucpc extension is required) : " + Path.GetExtension(fullPath).ToLower());
                return;
            }

            if (!isLoadingNewData) Debug.Log("(Viewer) Reading threaded pointcloud file: " + fullPath, gameObject);

            // pass in filename
            //ThreadReaderInfo threadReaderData = new ThreadReaderInfo();
            //threadReaderData.fileName = fullPath;
            //ThreadPool.QueueUserWorkItem(new WaitCallback(ReadPointCloudThreaded), threadReaderData);

            ParameterizedThreadStart start = new ParameterizedThreadStart(ReadPointCloudThreaded);
            //Debug.Log(Path.GetExtension(fullPath).ToLower());
            if (Path.GetExtension(fullPath).ToLower() == ".ucpc")
            {
                if (isAnimated == true)
                {
                    Debug.LogError("Reading Animated Point Clouds in Separate thread is not supported");
                    return;
                }
                start = new ParameterizedThreadStart(ReadPointCloudThreadedNew);
                isNewFormat = true;
            }
            else
            {
                start = new ParameterizedThreadStart(ReadPointCloudThreaded);
                isNewFormat = false;
            }


            importerThread = new Thread(start);
            importerThread.IsBackground = true;
            importerThread.Start(fullPath);
            // TODO need to close previous thread before loading new!
        }

        // v2 format
        public void ReadPointCloudThreadedNew(System.Object a)
        {
            if (showDebug)
            {
                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }
            isLoading = true;

            byte[] headerdata = null;
            byte[] dataPoints = null;
            byte[] dataColors = null;

            try
            {
                // load header
                // load x amount of points and colors
                using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                using (BufferedStream bs = new BufferedStream(fs))
                using (BinaryReader binaryReader = new BinaryReader(bs))
                {
                    int headerSizeTemp = 34;
                    headerdata = new byte[headerSizeTemp];
                    headerdata = binaryReader.ReadBytes(headerSizeTemp);
                    int byteIndexTemp = 4 + 1 + 1;
                    /*
                    int binaryVersionTemp = headerdata[byteIndexTemp];
                    byteIndexTemp += sizeof(System.Byte);
                    Debug.Log("(header) binaryVersionTemp:" + binaryVersionTemp);

                    int rgbTemp = headerdata[byteIndexTemp];
                    byteIndexTemp += sizeof(System.Byte);
                    Debug.Log("(header) rgbTemp:" + rgbTemp);
                    */
                    totalPoints = (int)System.BitConverter.ToInt32(headerdata, byteIndexTemp);
                    long totalMaxPointsTemp = totalPoints;

                    Debug.Log("(header) totalPoints:" + totalPoints);
                    byteIndexTemp += sizeof(System.Int32);

                    if (readWholeCloud == true)
                    {
                        initialPointsToRead = totalPoints;
                    }
                    else
                    {
                        totalPoints = Mathf.Clamp(initialPointsToRead, 0, totalPoints);
                    }
                    //Debug.Log("initialPointsToRead="+ initialPointsToRead);

                    int pointsChunkSize = totalPoints * (4 + 4 + 4);
                    //Debug.Log("pointsChunkSize=" + pointsChunkSize);
                    //int colorsChunkSize = initialPointsToRead * (4 + 4 + 4);
                    //dataPoints = new byte[initialPointsToRead];
                    //dataPoints = new byte[2130702268];
                    dataPoints = binaryReader.ReadBytes(pointsChunkSize);

                    //Debug.Log("dataPoints=" + dataPoints.Length);
                    //Debug.Log(binaryReader.BaseStream.Position);

                    // jump to colors
                    binaryReader.BaseStream.Flush();
                    binaryReader.BaseStream.Position = (long)(totalMaxPointsTemp * (4 + 4 + 4) + headerdata.Length);

                    //Debug.Log(binaryReader.BaseStream.Position);

                    //                        dataColors = new byte[initialPointsToRead];
                    dataColors = binaryReader.ReadBytes(pointsChunkSize);
                    //Debug.Log("dataColors=" + dataColors.Length);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(fileName + " cannot be opened, its probably too large.. Test with [x] Load Limited amount (max 178m), or try splitting your data into smaller parts (using external point cloud editing tools)");
                return;
            }

            System.Int32 byteIndex = 0;

            // magic
            var magic = new byte[] { headerdata[byteIndex++], headerdata[byteIndex++], headerdata[byteIndex++], headerdata[byteIndex++] };
            if (showDebug) Debug.Log("magic=" + System.Text.Encoding.ASCII.GetString(magic));

            int binaryVersion = headerdata[byteIndex];
            byteIndex += sizeof(System.Byte);
            if (showDebug) Debug.Log("binaryVersion=" + binaryVersion);


            // check format
            if (binaryVersion != 2)
            {
                Debug.LogError("File binaryVersion should have value (2). Was " + binaryVersion + " - Loading cancelled.");
                return;
            }

            containsRGB = System.BitConverter.ToBoolean(headerdata, byteIndex);
            byteIndex += sizeof(System.Boolean);

            if (containsRGB == false)
            {
                if (isNewFormat == true)
                {
                    Debug.LogError("v2 format requires RGB data - loading cancelled");
                    return;
                }
                else
                {
                    Debug.LogWarning("No RGB data in the file, cloud will be black..");
                }
            }

            if (showDebug) Debug.Log("containsRGB=" + containsRGB);


            totalPoints = (int)System.BitConverter.ToInt32(headerdata, byteIndex);
            totalMaxPoints = totalPoints;
            byteIndex += sizeof(System.Int32);
            //Debug.Log("totalPoints=" + totalPoints);
            //if (showDebug) Debug.Log("totalPoints from file=" + totalPoints);

            // TEST load initially less points
            totalPoints = (int)Mathf.Clamp(initialPointsToRead, 0, totalPoints);
            //if (showDebug) Debug.Log("totalPoints after clamp=" + totalPoints);

            // bounds
            float minX = System.BitConverter.ToSingle(headerdata, byteIndex);
            byteIndex += sizeof(System.Single);
            float minY = System.BitConverter.ToSingle(headerdata, byteIndex);
            byteIndex += sizeof(System.Single);
            float minZ = System.BitConverter.ToSingle(headerdata, byteIndex);
            byteIndex += sizeof(System.Single);
            float maxX = System.BitConverter.ToSingle(headerdata, byteIndex);
            byteIndex += sizeof(System.Single);
            float maxY = System.BitConverter.ToSingle(headerdata, byteIndex);
            byteIndex += sizeof(System.Single);
            float maxZ = System.BitConverter.ToSingle(headerdata, byteIndex);
            byteIndex += sizeof(System.Single);

            cloudBounds = new Bounds(new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f), new Vector3((maxX - minX), (maxY - minY), (maxZ - minZ)));

            points = new Vector3[totalPoints];
            if (showDebug) Debug.Log("cloudBounds=" + cloudBounds);


            if (showDebug && !isLoadingNewData) Debug.Log("Loading new (V2) format: " + totalPoints + " points..");

            GCHandle vectorPointer = GCHandle.Alloc(points, GCHandleType.Pinned);
            IntPtr pV = vectorPointer.AddrOfPinnedObject();
            Marshal.Copy(dataPoints, 0, pV, totalPoints * 4 * 3);
            vectorPointer.Free();

            if (containsRGB == true)
            {
                pointColors = new Vector3[totalPoints];
                var vectorPointer2 = GCHandle.Alloc(pointColors, GCHandleType.Pinned);
                var pV2 = vectorPointer2.AddrOfPinnedObject();
                Marshal.Copy(dataColors, 0, pV2, totalPoints * 4 * 3);
                vectorPointer2.Free();
            }

            //memcpy(dstData.Scan0, srcData.Scan0, new UIntPtr((uint)height * (uint)srcData.Stride));
            // 2018.x
            // UnsafeUtility.MemCpyStride

            // for testing load timer
            //            stopwatch.Stop();
            //            Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
            //            stopwatch.Reset();

            //totalMaxPoints = totalPoints;

            // if randomize enabled, and didnt read from cache, then randomize
            if (randomizeArray == true)
            {
                if (showDebug) Debug.Log("Randomizing cloud..");
                PointCloudTools.Shuffle(new System.Random(), ref points, ref pointColors);
            }

            // refresh buffers
            UnityLibrary.MainThread.Call(InitDX11Buffers);
            while (isInitializingBuffers == true && abortReaderThread == false)
            {
                Thread.Sleep(1);
            }

            // NOTE: disabled this, was it needed?
            //UnityLibrary.MainThread.Call(UpdatePointData);
            //if (containsRGB == true) UnityLibrary.MainThread.Call(UpdateColorData);

            // if caching, save as bin (except if already read from cache)
            // TODO dont save if no changes to data
            // TODO move to separate method, so can call save anytime, if modify or remove points manually  

            if (reSaveBinFile == true)
            {
                if (isNewFormat == true)
                {
                    Debug.LogError("Cannot use reSaveBinFile with new V2 format");
                }
                else
                {
                    var outputFile = fileName;
                    Debug.Log("saving " + fileName);

                    BinaryWriter writer = null;

                    try
                    {
                        writer = new BinaryWriter(File.Open(outputFile, FileMode.Create));

                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                    if (writer == null)
                    {
                        Debug.LogError("Cannot output file: " + outputFile);
                        return;
                    }

                    writer.Write((byte)binaryVersion);
                    writer.Write((System.Int32)totalMaxPoints);
                    writer.Write(containsRGB);

                    for (int i = 0, length = points.Length; i < length; i++)
                    {
                        writer.Write(points[i].x);
                        writer.Write(points[i].y);
                        writer.Write(points[i].z);
                        if (containsRGB == true)
                        {
                            writer.Write(pointColors[i].x);
                            writer.Write(pointColors[i].y);
                            writer.Write(pointColors[i].z);
                        }
                    }
                    writer.Close();
                    Debug.Log("Finished saving cached file: " + outputFile);
                } // cache
            }

            isLoading = false;
            UnityLibrary.MainThread.Call(OnLoadingCompleteCallBack, fileName);

            //data = null;

            if (!isLoadingNewData) Debug.Log("Finished Loading: " + initialPointsToRead + " / " + totalMaxPoints);

            if (showDebug)
            {
                stopwatch.Stop();
                //Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
                // 2243ms for 25m
                // 3528ms for 50m > 2900ms (no .set)
                // 380ms for 50m (new format)
                stopwatch.Reset();
            }

            isLoadingNewData = false;
        } // ReadPointCloudThreaded

        System.Diagnostics.Stopwatch stopwatch;
        // binary point cloud reader (using separate thread)
        public void ReadPointCloudThreaded(System.Object a)
        {
            if (isAnimated == true)
            {
                Debug.LogError("Reading Animated Point Clouds in Separate thread is not supported");
                haveError = true;
                return;
            }

            // for testing loading times
            if (showDebug)
            {
                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }

            isLoading = true;

            byte[] data;

            try
            {
                data = File.ReadAllBytes(fileName);
            }
            catch
            {
                Debug.LogError(fileName + " cannot be opened with ReadAllBytes(), it might be too large >2gb. Try splitting your data into smaller parts (using external point cloud editing tools)");

                /*
                // try reading in smaller parts
                long fileSize = new System.IO.FileInfo(fileName).Length;

                try
                {
                    data = new byte[fileSize];
                }
                catch (System.Exception)
                {

                }
                finally
                {
                    Debug.LogError("File is too large, cannot create array: " + fileSize);
                }

                FileStream sourceFile = new FileStream(fileName, FileMode.Open);
                BinaryReader reader = new BinaryReader(sourceFile);

                reader.Close();
                sourceFile.Close();
               */

                return;
            }



            System.Int32 byteIndex = 0;

            int binaryVersion = data[byteIndex];
            byteIndex += sizeof(System.Byte);

            // check format
            if (binaryVersion > 1)
            {
                Debug.LogError("File binaryVersion should have value (0-1). Was " + binaryVersion + " - Loading cancelled.");
                return;
            }

            totalPoints = (int)System.BitConverter.ToInt32(data, byteIndex);
            byteIndex += sizeof(System.Int32);
            //Debug.Log(totalPoints);

            containsRGB = System.BitConverter.ToBoolean(data, byteIndex);
            byteIndex += sizeof(System.Boolean);

            // TEST
            //totalPoints = totalPoints * 2;

            points = new Vector3[totalPoints];

            Debug.Log("Loading old format: " + totalPoints + " points..");

            float x, y, z;
            float minX = Mathf.Infinity;
            float minY = Mathf.Infinity;
            float minZ = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float maxY = Mathf.NegativeInfinity;
            float maxZ = Mathf.NegativeInfinity;

            if (containsRGB == true) pointColors = new Vector3[totalPoints];

            byteArrayToFloats = new float[(data.Length - byteIndex) / 4];
            System.Buffer.BlockCopy(data, byteIndex, byteArrayToFloats, 0, data.Length - byteIndex);

            int dataIndex = 0;
            for (int i = 0; i < totalPoints; i++)
            {
                x = byteArrayToFloats[dataIndex] + transformPos.x;
                dataIndex++;
                y = byteArrayToFloats[dataIndex] + transformPos.y;
                dataIndex++;
                z = byteArrayToFloats[dataIndex] + transformPos.z;
                dataIndex++;

                points[i].x = x;
                points[i].y = y;
                points[i].z = z;

                // get bounds
                if (x < minX) minX = x;
                else if (x > maxX) maxX = x;
                //((x < minX) ? ref minX : ref maxX) = x; // c#7
                if (y < minY) minY = y;
                else if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                else if (z > maxZ) maxZ = z;

                if (containsRGB == true)
                {
                    r = byteArrayToFloats[dataIndex];
                    dataIndex++;
                    g = byteArrayToFloats[dataIndex];
                    dataIndex++;
                    b = byteArrayToFloats[dataIndex];
                    dataIndex++;

                    pointColors[i].x = r;
                    pointColors[i].y = g;
                    pointColors[i].z = b;
                }

                if (abortReaderThread == true)
                {
                    return;
                }
            } // for all points

            // for testing load timer
            //            stopwatch.Stop();
            //            Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
            //            stopwatch.Reset();

            cloudBounds = new Bounds(new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f), new Vector3((maxX - minX), (maxY - minY), (maxZ - minZ)));

            totalMaxPoints = totalPoints;

            // if randomize enabled, and didnt read from cache, then randomize
            if (randomizeArray == true)
            {
                if (showDebug) Debug.Log("Randomizing cloud..");
                PointCloudTools.Shuffle(new System.Random(), ref points, ref pointColors);
            }

            // refresh buffers
            UnityLibrary.MainThread.Call(InitDX11Buffers);

            // NOTE: disabled this, was it needed?
            //UnityLibrary.MainThread.Call(UpdatePointData);
            //if (containsRGB == true) UnityLibrary.MainThread.Call(UpdateColorData);

            // if caching, save as bin (except if already read from cache)
            // TODO dont save if no changes to data
            // TODO move to separate method, so can call save anytime, if modify or remove points manually  

            if (reSaveBinFile == true)
            {
                if (isNewFormat == true)
                {
                    Debug.LogError("Cannot use reSaveBinFile with new V2 format");
                }
                else
                {
                    var outputFile = fileName; // Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(fileName) + "-cached.bin";
                    Debug.Log("saving cached file: " + fileName);

                    BinaryWriter writer = null;

                    try
                    {
                        writer = new BinaryWriter(File.Open(outputFile, FileMode.Create));

                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                    if (writer == null)
                    {
                        Debug.LogError("Cannot output file: " + outputFile);
                        return;
                    }

                    writer.Write((byte)binaryVersion);
                    writer.Write((System.Int32)totalMaxPoints);
                    writer.Write(containsRGB);

                    for (int i = 0, length = points.Length; i < length; i++)
                    {
                        writer.Write(points[i].x);
                        writer.Write(points[i].y);
                        writer.Write(points[i].z);
                        if (containsRGB == true)
                        {
                            writer.Write(pointColors[i].x);
                            writer.Write(pointColors[i].y);
                            writer.Write(pointColors[i].z);
                        }
                    }
                    writer.Close();
                    Debug.Log("Finished saving cached file: " + outputFile);
                }
            } // cache

            isLoading = false;
            UnityLibrary.MainThread.Call(OnLoadingCompleteCallBack, fileName);

            data = null;

            Debug.Log("Finished Loading.");

            if (showDebug)
            {
                stopwatch.Stop();
                Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
                // 2243ms for 25m
                // 3528ms for 50m > 2900ms (no .set)
                stopwatch.Reset();
            }
        } // ReadPointCloudThreaded

        void ReadNewFormat()
        {

        }

        // For Brekel animated binary data only
        void ReadAnimatedPointCloud()
        {
            if (isAnimated == false)
            {
                Debug.LogWarning("ReadAnimatedPointCloud() called, but isAnimated = false");
                return;
            }

            isLoading = true;

            int totalCounter = 0;

            // check file size
            long fileSize = new System.IO.FileInfo(fileName).Length;
            //Debug.Log("fileSize=" + fileSize);
            var tempPoint = Vector3.zero;
            var tempColor = Vector3.zero;

            byte[] data = null;
            if (fileSize >= 2147483647)
            {
                // large file
                FileStream sourceFile = new FileStream(fileName, FileMode.Open);
                BinaryReader reader = new BinaryReader(sourceFile);

                // parse header data
                System.Int32 byteIndex = 0;
                binaryVersion = reader.ReadByte();
                byteIndex += sizeof(System.Byte);
                if (binaryVersion != 2) { Debug.LogError("For Animated point cloud, file binaryVersion should have value (2) or bigger. received=" + binaryVersion); isAnimated = false; return; }
                numberOfFrames = reader.ReadInt32();
                byteIndex += sizeof(System.Int32);
                //int frameRate = (int)reader.ReadSingle();
                byteIndex += sizeof(System.Single);
                containsRGB = reader.ReadBoolean();
                byteIndex += sizeof(System.Boolean);

                numberOfPointsPerFrame = new int[numberOfFrames];
                totalPoints = 0; // TODO: rename to pointsPerFrame
                for (int i = 0; i < numberOfFrames; i++)
                {
                    numberOfPointsPerFrame[i] = reader.ReadInt32();
                    byteIndex += sizeof(System.Int32);
                    if (numberOfPointsPerFrame[i] > totalPoints) totalPoints = numberOfPointsPerFrame[i]; // largest value will be used as a fixed size for point array
                    totalNumberOfPoints += numberOfPointsPerFrame[i];
                }

                frameBinaryPositionForEachFrame = new System.Int64[numberOfFrames];
                for (int i = 0; i < numberOfFrames; i++)
                {
                    frameBinaryPositionForEachFrame[i] = reader.ReadInt64();
                    byteIndex += sizeof(System.Int64);
                }

                Debug.Log("totalNumberOfPoints: " + totalNumberOfPoints);

                // init playback arrays
                animatedPointArray = new Vector3[totalNumberOfPoints];
                if (containsRGB == true) animatedColorArray = new Vector3[totalNumberOfPoints];

                //Debug.Log("binaryVersion:" + binaryVersion);
                //Debug.Log("numberOfFrames:" + numberOfFrames);
                //Debug.Log("frameRate:"+frameRate);
                //Debug.Log("containsRGB:" + containsRGB);
                //Debug.Log("numberOfPointsPerFrame[0]:" + numberOfPointsPerFrame[0]);
                //Debug.Log("frameBinaryPositionForEachFrame[0]:" + frameBinaryPositionForEachFrame[0]);

                points = new Vector3[totalPoints];
                animatedOffset = new int[numberOfFrames];
                if (containsRGB == true) pointColors = new Vector3[totalPoints];

                // main binary reader loop
                var sizeOfSingle = sizeof(System.Single);
                for (int frame = 0; frame < numberOfFrames; frame++)
                {
                    animatedOffset[frame] = totalCounter;

                    // read all points for frame
                    var bytesToRead = numberOfPointsPerFrame[frame] * 3 * sizeOfSingle;
                    if (containsRGB == true) bytesToRead += numberOfPointsPerFrame[frame] * 3 * sizeOfSingle;
                    var bytes = reader.ReadBytes(bytesToRead);

                    byteIndex = 0;
                    for (int i = 0; i < numberOfPointsPerFrame[frame]; i++)
                    {
                        tempPoint.x = System.BitConverter.ToSingle(bytes, byteIndex) + transformPos.x;
                        byteIndex += sizeOfSingle;
                        tempPoint.y = System.BitConverter.ToSingle(bytes, byteIndex) + transformPos.y;
                        byteIndex += sizeOfSingle;
                        tempPoint.z = System.BitConverter.ToSingle(bytes, byteIndex) + transformPos.z;
                        byteIndex += sizeOfSingle;

                        animatedPointArray[totalCounter] = tempPoint;
                        if (containsRGB == true)
                        {
                            tempColor.x = System.BitConverter.ToSingle(bytes, byteIndex);
                            byteIndex += sizeOfSingle;
                            tempColor.y = System.BitConverter.ToSingle(bytes, byteIndex);
                            byteIndex += sizeOfSingle;
                            tempColor.z = System.BitConverter.ToSingle(bytes, byteIndex);
                            byteIndex += sizeOfSingle;
                            animatedColorArray[totalCounter] = tempColor;
                        }
                        totalCounter++;
                    }
                }

                sourceFile.Close(); //dispose streamer
                reader.Close(); //dispose reader
            }
            else // can read with allbytes
            {
                data = File.ReadAllBytes(fileName);

                if (data.Length < 1)
                {
                    Debug.LogError("ReadAnimatedPointCloud() called, but isAnimated = false");
                    return;
                }

                // parse header data
                System.Int32 byteIndex = 0;
                binaryVersion = data[byteIndex];
                if (binaryVersion != 2) { Debug.LogError("For Animated point cloud, file binaryVersion should have value (2) or bigger. received=" + binaryVersion); isAnimated = false; return; }
                byteIndex += sizeof(System.Byte);
                numberOfFrames = (int)System.BitConverter.ToInt32(data, byteIndex);
                byteIndex += sizeof(System.Int32);
                //			frameRate = System.BitConverter.ToSingle(data,byteIndex); // not used
                byteIndex += sizeof(System.Single);
                containsRGB = System.BitConverter.ToBoolean(data, byteIndex);
                byteIndex += sizeof(System.Boolean);
                numberOfPointsPerFrame = new int[numberOfFrames];
                totalPoints = 0;
                for (int i = 0; i < numberOfFrames; i++)
                {
                    numberOfPointsPerFrame[i] = (int)System.BitConverter.ToInt32(data, byteIndex);
                    //Debug.Log(numberOfPointsPerFrame[i]);
                    byteIndex += sizeof(System.Int32);
                    if (numberOfPointsPerFrame[i] > totalPoints) totalPoints = numberOfPointsPerFrame[i]; // largest value will be used as a fixed size for point array
                    totalNumberOfPoints += numberOfPointsPerFrame[i];
                }
                frameBinaryPositionForEachFrame = new System.Int64[numberOfFrames];
                for (int i = 0; i < numberOfFrames; i++)
                {
                    frameBinaryPositionForEachFrame[i] = (System.Int64)System.BitConverter.ToInt64(data, byteIndex);
                    byteIndex += sizeof(System.Int64);
                }

                // init playback arrays
                animatedPointArray = new Vector3[totalNumberOfPoints];
                if (containsRGB == true) animatedColorArray = new Vector3[totalNumberOfPoints];

                //Debug.Log("binaryVersion:" + binaryVersion);
                //Debug.Log("numberOfFrames:" + numberOfFrames);
                //Debug.Log("frameRate:"+frameRate);
                //Debug.Log("containsRGB:" + containsRGB);
                //Debug.Log("numberOfPointsPerFrame[0]:" + numberOfPointsPerFrame[0]);
                //Debug.Log("frameBinaryPositionForEachFrame[0]:" + frameBinaryPositionForEachFrame[0]);

                points = new Vector3[totalPoints]; // is this needed?
                animatedOffset = new int[numberOfFrames];

                if (containsRGB == true) pointColors = new Vector3[totalPoints];

                var sizeOfSingle = sizeof(System.Single);

                for (int frame = 0; frame < numberOfFrames; frame++)
                {
                    animatedOffset[frame] = totalCounter;
                    for (int i = 0; i < numberOfPointsPerFrame[frame]; i++)
                    {
                        tempPoint.x = System.BitConverter.ToSingle(data, byteIndex) + transformPos.x;
                        byteIndex += sizeOfSingle;
                        tempPoint.y = System.BitConverter.ToSingle(data, byteIndex) + transformPos.y;
                        byteIndex += sizeOfSingle;
                        tempPoint.z = System.BitConverter.ToSingle(data, byteIndex) + transformPos.z;
                        byteIndex += sizeOfSingle;
                        animatedPointArray[totalCounter] = tempPoint;
                        if (containsRGB == true)
                        {
                            tempColor.x = System.BitConverter.ToSingle(data, byteIndex);
                            byteIndex += sizeOfSingle;
                            tempColor.y = System.BitConverter.ToSingle(data, byteIndex);
                            byteIndex += sizeOfSingle;
                            tempColor.z = System.BitConverter.ToSingle(data, byteIndex);
                            byteIndex += sizeOfSingle;
                            animatedColorArray[totalCounter] = tempColor;
                        }
                        totalCounter++;
                    }
                }
            }

            Debug.Log("Finished loading animated point cloud. total points = " + totalCounter);

            totalMaxPoints = totalPoints;

            InitDX11Buffers();
            isLoading = false;
            OnLoadingCompleteCallBack(fileName);
        }



        float SuperPacker(float f1, float f2)
        {
            float truncatedF2 = (float)System.Math.Truncate(f2 * 1024);
            return truncatedF2 + f1;
        }

        bool isInitializingBuffers = false;
        public void InitDX11Buffers()
        {
            isInitializingBuffers = true;
            // cannot init 0 size, so create dummy data if its 0
            if (totalPoints == 0)
            {
                totalPoints = 1;
                points = new Vector3[1];
                if (containsRGB == true)
                {
                    pointColors = new Vector3[1];
                }
            }

            // clear old buffers
            ReleaseDX11Buffers();

            if (bufferPoints != null) bufferPoints.Dispose();

            var packColors2 = false;
            if (packColors2 == true) //  not working
            {
                // broken
            }
            else if (packColors == true) // packer2
            {
                var points2 = new PackedPoint[points.Length];
                for (int i = 0, len = points.Length; i < len; i++)
                {
                    var p = new PackedPoint();
                    // pack red and x
                    var xx = SuperPacker(pointColors[i].x * 0.98f, points[i].x);
                    // pack green and y
                    var yy = SuperPacker(pointColors[i].y * 0.98f, points[i].y);
                    // pack blue and z
                    var zz = SuperPacker(pointColors[i].z * 0.98f, points[i].z);
                    p.x = xx;
                    p.y = yy;
                    p.z = zz;
                    points2[i] = p;
                }
                bufferPoints = new ComputeBuffer(totalPoints, 12);
                bufferPoints.SetData(points2);
                cloudMaterial.SetBuffer("buf_Points", bufferPoints);
            }
            else // original
            {
                bufferPoints = new ComputeBuffer(totalPoints, 12);
                bufferPoints.SetData(points);
                // TODO use mat2int
                cloudMaterial.SetBuffer("buf_Points", bufferPoints);
                if (containsRGB == true)
                {
                    if (bufferColors != null) bufferColors.Dispose();
                    bufferColors = new ComputeBuffer(totalPoints, 12);
                    bufferColors.SetData(pointColors);
                    cloudMaterial.SetBuffer("buf_Colors", bufferColors);
                }
            }

            if (forceDepthBufferPass == true)
            {
                //if (bufferPointsDepth != null) bufferPointsDepth.Dispose();
                //bufferPointsDepth = new ComputeBuffer(totalPoints, 12);
                //bufferPointsDepth.SetData(points);
                depthMaterial.SetBuffer("buf_Points", bufferPoints);
            }

            isInitializingBuffers = false;
        }

        // can try enabling this, if your cloud disappears on alt tab
        //void OnApplicationFocus(bool focused)
        //{
        //    Debug.Log("focus = "+focused);
        //    if (focused) InitDX11Buffers();
        //}


        public void ReleaseDX11Buffers()
        {
            if (bufferPoints != null) bufferPoints.Release();
            bufferPoints = null;
            if (bufferColors != null) bufferColors.Release();
            bufferColors = null;
            //if (bufferPointsDepth != null) bufferPointsDepth.Release();
            //bufferPointsDepth = null;
        }

        // can use this to set new points data
        public void UpdatePointData()
        {
            if (points.Length == bufferPoints.count)
            {
                // same length as earlier
                bufferPoints.SetData(points);
                cloudMaterial.SetBuffer("buf_Points", bufferPoints);
            }
            else
            {
                // new data is different sized array, need to redo it
                totalPoints = points.Length;
                totalMaxPoints = totalPoints;
                bufferPoints.Dispose();
                // NOTE: not for packed data..
                //Debug.Log("new ComputeBuffer");
                bufferPoints = new ComputeBuffer(totalPoints, 12);
                bufferPoints.SetData(points);
                cloudMaterial.SetBuffer("buf_Points", bufferPoints);
            }
        }


        // can use this to set new point colors data
        public void UpdateColorData()
        {
            if (pointColors.Length == bufferColors.count)
            {
                // same length as earlier
                bufferColors.SetData(pointColors);
                cloudMaterial.SetBuffer("buf_Colors", bufferColors);
            }
            else
            {
                // new data is different sized array, need to redo it
                totalPoints = pointColors.Length;
                totalMaxPoints = totalPoints;
                bufferColors.Dispose();
                bufferColors = new ComputeBuffer(totalPoints, 12);
                bufferColors.SetData(pointColors);
                cloudMaterial.SetBuffer("buf_Colors", bufferColors);
            }
        }

        void OnDestroy()
        {
            ReleaseDX11Buffers();
            points = new Vector3[0];
            pointColors = new Vector3[0];

            if (isLoading == true) abortReaderThread = true;
        }


        // drawing mainloop, for drawing the points
        //void OnPostRender() // < works also, BUT must have this script attached to Camera
        public void OnRenderObject()
        {
            // optional: if you only want to render to specific camera, use next line
            if (renderOnlyMainCam == true && Camera.current.CompareTag("MainCamera") == false) return;

            // dont display while loading, it slows down with huge clouds
            if (isLoading == true || displayPoints == false || useCommandBuffer == true) return;
            //Debug.Log("123");

            cloudMaterial.SetPass(0);

#if UNITY_2019_1_OR_NEWER
            Graphics.DrawProceduralNow(MeshTopology.Points, totalPoints);
#else
            Graphics.DrawProcedural(MeshTopology.Points, totalPoints);
#endif

        }


        // called after some file load operation has finished
        void OnLoadingCompleteCallBack(System.Object a)
        {
            //PointCloudTools.DrawBounds(GetBounds(), 99);

            //Debug.Log("OnLoadingCompleteCallBack");
            if (OnLoadingComplete != null) OnLoadingComplete((string)a);

            if (useCommandBuffer == true)
            {
                commandBuffer.DrawProcedural(Matrix4x4identity, cloudMaterial, 0, MeshTopology.Points, totalPoints, 0);
                // transform.localToWorldMatrix
            }

            if (forceDepthBufferPass == true)
            {
                commandBufferDepth.DrawProcedural(Matrix4x4identity, depthMaterial, 0, MeshTopology.Points, totalPoints, 0);
            }
        }

        // -------------------------- POINT CLOUD HELPER METHODS --------------------------------
        // returns current point count, or -1 if points array is null
        public int GetPointCount()
        {
            if (points == null) return -1;
            return points.Length;
        }

        // returns given point position from array
        public Vector3 GetPointPosition(int index)
        {
            if (points == null || index < 0 || index > points.Length - 1) return Vector3.zero;
            return points[index];
        }


        // adjust visible point count
        public void AdjustVisiblePointsAmount(int offsetAmount)
        {
            if (isNewFormat == true)
            {
                // TODO wait for load to finish (probably increased last click already)
                if (isLoadingNewData) return;

                totalPoints += offsetAmount;
                if (totalPoints < 0)
                {
                    totalPoints = 0;
                }
                else if (totalPoints > totalMaxPoints)
                {
                    totalPoints = totalMaxPoints;
                }

                if (totalPoints > initialPointsToRead)
                {
                    //Debug.Log("Need to load..");

                    // load new data, with given point cloud count, then need to set point size here instead of dynamicres script?
                    // TODO later, incrementally load data instead of all data again
                    //CallReadPointCloudThreaded(string fullPath);
                    initialPointsToRead = totalPoints;// + offsetAmount;
                                                      //Debug.Log("initialPointsToRead=" + initialPointsToRead);
                    isLoadingNewData = true;
                    CallReadPointCloudThreaded(fileName);
                }
            }
            else // old format
            {
                totalPoints += offsetAmount;
                if (totalPoints < 0)
                {
                    totalPoints = 0;
                }
                else
                {
                    if (totalPoints > totalMaxPoints) totalPoints = totalMaxPoints;
                }
            }
        }

        // set amount of points to draw
        public void SetVisiblePointCount(int amount)
        {
            if (isNewFormat == true)
            {
                // TODO wait for load to finish (probably increased last click already)
                if (isLoadingNewData) return;

                totalPoints = amount;
                if (totalPoints < 0)
                {
                    totalPoints = 0;
                }
                else if (totalPoints > totalMaxPoints)
                {
                    totalPoints = totalMaxPoints;
                }

                if (totalPoints > initialPointsToRead)
                {
                    //Debug.Log("Need to load..");

                    // load new data, with given point cloud count, then need to set point size here instead of dynamicres script?
                    // TODO later, incrementally load data instead of all data again
                    //CallReadPointCloudThreaded(string fullPath);
                    initialPointsToRead = totalPoints;// + offsetAmount;
                                                      //Debug.Log("initialPointsToRead=" + initialPointsToRead);
                    isLoadingNewData = true;
                    CallReadPointCloudThreaded(fileName);
                }
            }
            else // old format
            {
                totalPoints = amount;
                if (totalPoints < 0)
                {
                    totalPoints = 0;
                }
                else
                {
                    if (totalPoints > totalMaxPoints) totalPoints = totalMaxPoints;
                }
            }
        }


        // set material/shader pointsize
        public void SetPointSize(float newSize)
        {
            cloudMaterial.SetFloat("_Size", newSize);
        }

        // enable/disable drawing
        public void ToggleCloud(bool state)
        {
            displayPoints = state;
        }

        // return current material shader _Size variable
        public float? GetPointSize()
        {
            if (cloudMaterial.HasProperty("_Size") == false) return null;
            return cloudMaterial.GetFloat("_Size");
        }

        public int GetActivePointCount()
        {
            return totalPoints;
        }

        public int GetTotalPointCount()
        {
            return totalMaxPoints;
        }

        public Bounds GetBounds()
        {
            return cloudBounds;
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

    } // class
} // namespace

#endif