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
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace unitycodercom_PointCloudBinaryViewer {
	public class PointCloudViewerTilesDX11 : MonoBehaviour {
		[Header("Load")]
		public string rootFile = "StreamingAssets/.tiles/manu.pcroot";

		[Header("Settings")]
		public bool loadAtStart = true;
		[Tooltip("Use PointCloudColorSizeDX11v2.mat to get started, then experiment with other materials if needed")]
		public Material cloudMaterial;
		[Tooltip("Create copy of the material. Must enable if using multipl viewers with the same material")]
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

		float[] cullingBandDistances = new float[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 150, 200, float.PositiveInfinity };
		CullingGroup cullGroup;

		[Header("Rendering")]
		[Tooltip("1 = send data in big chunk (can cause spike), 32=send within 32 slices (smaller or non-noticeable spikes, but tiles appear bit slower)")]
		[Range(1, 48)]
		public int gpuUploadSteps = 16; // bigger value means, point data upload is spread to more frames (less laggy), good values: 4 - 24
		[Tooltip("Global tile resolution multiplier: 1 = Keep original resolution, 0.5 = half resolution, 0 = 0 points visible. Resolution is updated only during tile update (not instantly)")]
		[Range(0f, 1f)]
		public float tileResolution = 1f;
		[Tooltip("Enable global point size multiplier")]
		public bool useSizeMultiplier = false;
		[Tooltip("Global point size multiplier: 1 = Keep original size, 0.5 = half size. NOTE: Requires shader with SizeMultiplier parameter!")]
		public float pointSizeMultiplier = 1f;

		[Header("Advanced")]
		[Tooltip("Force Garbage Collection after loading")]
		public bool forceGC = false;

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

		int totalPointCount = 0;

		bool rootLoaded = false;

		void Awake() {
			applicationStreamingAssetsPath = Application.streamingAssetsPath;
			FixMainThreadHelper();
		}

		// init
		void Start() {
			cam = Camera.main;

			if (cam == null) { Debug.LogError("Camera Main is missing..", gameObject); }

			// create material clone, so can view multiple clouds
			if (instantiateMaterial == true) {
				cloudMaterial = new Material(cloudMaterial);
			}

			if (loadAtStart == true) {
				abortReaderThread = false;

				// all these are required to do
				rootLoaded = LoadRootFile(rootFile);
				if (rootLoaded == true)
					StartCoroutine(InitCullingSystem());
			}

			// add warnings if bad setttings
#if UNITY_2019_1_OR_NEWER
			if (useNativeArrays == false && releaseTileMemory == true) {
				Debug.LogWarning("useNativeArrays is not enabled, but releaseTileMemory is enabled - Cannot release memory for managed memory tiles");
			}
#endif

		}

		void Update() {
			/*
            // check when all data has been loaded, TODO dont do in update..
            if (initDone == true && loadQueueA.Count == 0 && loadQueueB.Count == 0)
            {
                OnLoadingCompleteCallBack(null);
                initDone = false;
            }*/
		}


		bool isPackedColors = false;

		// TODO this could run in a separate thread
		bool LoadRootFile(string filePath) {
			if (Path.IsPathRooted(filePath) == false) {
				filePath = Path.Combine(applicationStreamingAssetsPath, filePath);
			}

			if (PointCloudTools.CheckIfFileExists(filePath) == false) {
				Debug.LogError("File not found: " + filePath);
				return false;
			}

			if (Path.GetExtension(filePath).ToLower() != ".pcroot") {
				Debug.LogError("File is not V3 root file (.pcroot extension is required) : " + filePath);
				return false;
			}

			StartWorkerThreads();

			Debug.Log("(Tiles Viewer) Loading root file: " + filePath);
			var rootData = File.ReadAllLines(filePath);
			var rootFolder = Path.GetDirectoryName(filePath);

			// get global settings from first row : version | gridsize | totalpointcount | boundsx | boundsy | boundsz | autooffsetx | autooffsety | autooffsetz

			var globalData = rootData[0].Split(sep);

			if (globalData != null && globalData.Length >= 9) {
				v3Version = int.Parse(globalData[0]);

				if (v3Version < 1 || v3Version > 2) {
					Debug.LogError("v3 header version (" + v3Version + ") is not supported in this viewer!");
					return false;
				}

				if (v3Version == 2) {
					isPackedColors = true;
					Debug.Log("(Tiles Viewer) V3 format #2 detected: Packed colors (Make sure you use material that supports PackedColors)");
				}

				gridSize = float.Parse(globalData[1], CultureInfo.InvariantCulture);
				totalPointCount = int.Parse(globalData[2]);
				Debug.Log("(Tiles Viewer) Total point count = " + totalPointCount);
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

				if (v3Version == 2) {
					packMagic = int.Parse(globalData[12], CultureInfo.InvariantCulture);
				}

				cloudOffset = new Vector3(offX, offY, offZ);

				//PointCloudTools.DrawBounds(cloudBounds, 100);
			} else {
				Debug.LogError("Failed to parse global values from " + filePath);
				return false;
			}

			int tileCount = rootData.Length - 1;
			tiles = new PointCloudTile[tileCount];
			tilesCount = tiles.Length;

			Debug.Log("(Tiles Viewer) Found " + tileCount + " tiles");
			if (tileCount <= 0) {
				Debug.LogError("Failed parsing V3 tiles root, no tiles found! Check this file in notepad, does it contain data? Usually this happens if your conversion scaling is wrong (not scaling to smaller), one cell only gets few points.." + filePath);

			}

			// get data, start from next row
			int i = 0;
			for (int rownIndex = 0; rownIndex < tileCount; rownIndex++) {
				var row = rootData[rownIndex + 1].Split('|');

				var t = new PointCloudTile();

				t.filename = Path.Combine(rootFolder, row[0]);
				if (isPackedColors == false)
					t.filenameRGB = Path.Combine(rootFolder, row[0] + ".rgb");

				//Debug.Log(t.filename);

				t.totalPoints = int.Parse(row[1]);
				t.visiblePoints = 0;
				t.loadedPoints = 0;

				// tile bounds
				t.minX = float.Parse(row[2], CultureInfo.InvariantCulture);
				t.minY = float.Parse(row[3], CultureInfo.InvariantCulture);
				t.minZ = float.Parse(row[4], CultureInfo.InvariantCulture);
				t.maxX = float.Parse(row[5], CultureInfo.InvariantCulture);
				t.maxY = float.Parse(row[6], CultureInfo.InvariantCulture);
				t.maxZ = float.Parse(row[7], CultureInfo.InvariantCulture);

				if (isPackedColors == true) {
					t.cellX = int.Parse(row[8], CultureInfo.InvariantCulture);
					t.cellY = int.Parse(row[9], CultureInfo.InvariantCulture);
					t.cellZ = int.Parse(row[10], CultureInfo.InvariantCulture);
				}

				t.center = new Vector3((t.minX + t.maxX) * 0.5f, (t.minY + t.maxY) * 0.5f, (t.minZ + t.maxZ) * 0.5f);
				//Debug.Log("t.minX=" + t.minX + " row[2]=" + row[2]);

				t.isLoading = false;
				t.isReady = false;

				t.material = new Material(cloudMaterial);

				// set offset for packed
				if (isPackedColors == true) {
					// TODO requires original grid coordinate not bounds
					t.material.SetVector("_Offset", new Vector3(t.cellX * gridSize, t.cellY * gridSize, t.cellZ * gridSize));
					t.material.SetFloat("_GridSizeAndPackMagic", gridSize * packMagic);
				}

				tiles[i] = t;

				i++;
			}
			return true;
		}

		void StartWorkerThreads() {
			if (threadRunningA == false) {
				threadRunningA = true;
				ParameterizedThreadStart start = new ParameterizedThreadStart(LoaderWorkerThreadA);
				importerThreadA = new Thread(start);
				importerThreadA.IsBackground = true;
				importerThreadA.Start(null);
			}

			if (threadRunningB == false) {
				threadRunningB = true;
				ParameterizedThreadStart startB = new ParameterizedThreadStart(LoaderWorkerThreadB);
				importerThreadB = new Thread(startB);
				importerThreadB.IsBackground = true;
				importerThreadB.Start(null);
			}
		}

		// keeps loading data, when have something in load queue
		void LoaderWorkerThreadA(System.Object temp) {
			while (abortReaderThread == false) {
				try {
					if (loadQueueA.Count > 0) {
						int loadIndex = loadQueueA.Dequeue();
						//Debug.Log("Loading queue=" + loadIndex);
						ReadPointCloudThreadedNewA(loadIndex);
						//Thread.Sleep(200);
					} else {
						// waiting for work
						Thread.Sleep(16);
					}
				}
				catch (Exception e) {
					Debug.LogException(e);
				}

			}
			//Debug.Log("(Worker A) Thread ended.");
			threadRunningA = false;
		}

		void LoaderWorkerThreadB(System.Object temp) {
			while (abortReaderThread == false) {
				try {
					if (loadQueueB.Count > 0) {
						int loadIndex = loadQueueB.Dequeue();
						//Debug.Log("Loading queue=" + loadIndex);
						ReadPointCloudThreadedNewB(loadIndex);
						//Thread.Sleep(200);
					} else {
						// waiting for work
						Thread.Sleep(16);
					}
				}
				catch (Exception e) {
					Debug.LogException(e);
				}
			}
			//Debug.Log("(Worker B) Thread ended.");
			threadRunningB = false;
		}

		byte[] dataPoints = null;
		// v3 tiles format
		public void ReadPointCloudThreadedNewA(System.Object rawindex) {
			int index = (int)rawindex;
			tiles[index].isLoading = true;
			int pointCount = tiles[index].totalPoints;

			// points
#if UNITY_2019_1_OR_NEWER
			if (useNativeArrays == true) {
				if (tiles[index].pointsNative.IsCreated == true)
					tiles[index].pointsNative.Dispose();
				tiles[index].pointsNative = new NativeArray<byte>(pointCount * 12, Allocator.Persistent);
				dataPoints = File.ReadAllBytes(tiles[index].filename);
				//Debug.Log(index + " pointCount=" + pointCount + " points.len = " + tiles[index].points.Length + "  datapoints.len = " + dataPoints.Length);
				if (abortReaderThread || tiles[index].pointsNative.IsCreated == false)
					return;
				MoveFromByteArray<byte>(ref dataPoints, ref tiles[index].pointsNative);
			} else {
				tiles[index].points = new Vector3[pointCount];
				GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
				IntPtr pV = vectorPointer.AddrOfPinnedObject();
				dataPoints = File.ReadAllBytes(tiles[index].filename);
				Marshal.Copy(dataPoints, 0, pV, pointCount * 12);
				vectorPointer.Free();
			}
#else
            tiles[index].points = new Vector3[pointCount];
            GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
            IntPtr pV = vectorPointer.AddrOfPinnedObject();
            dataPoints = File.ReadAllBytes(tiles[index].filename);
            Marshal.Copy(dataPoints, 0, pV, pointCount * 12);
            vectorPointer.Free();
#endif
			tiles[index].loadedPoints = tiles[index].totalPoints;
			if (forceGC == true)
				GC.Collect();

			// colors
			if (isPackedColors == false) {
#if UNITY_2019_1_OR_NEWER

				if (useNativeArrays == true) {
					if (tiles[index].colorsNative.IsCreated == true)
						tiles[index].colorsNative.Dispose();
					tiles[index].colorsNative = new NativeArray<byte>(pointCount * 12, Allocator.Persistent);
					dataPoints = File.ReadAllBytes(tiles[index].filenameRGB);
					if (abortReaderThread || tiles[index].colorsNative.IsCreated == false)
						return;
					MoveFromByteArray<byte>(ref dataPoints, ref tiles[index].colorsNative);
					if (forceGC == true)
						GC.Collect();
				} else {
					tiles[index].colors = new Vector3[pointCount];
					GCHandle vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
					IntPtr pV = vectorPointer.AddrOfPinnedObject();
					dataPoints = File.ReadAllBytes(tiles[index].filenameRGB);
					Marshal.Copy(dataPoints, 0, pV, pointCount * 12);
					vectorPointer.Free();
				}
#else
                tiles[index].colors = new Vector3[pointCount];
                vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
                pV = vectorPointer.AddrOfPinnedObject();
                dataPoints = File.ReadAllBytes(tiles[index].filenameRGB);
                Marshal.Copy(dataPoints, 0, pV, pointCount * 12);
                vectorPointer.Free();
#endif
				if (forceGC == true)
					GC.Collect();
			}

			// refresh buffers, check if needed?
			isInitializingBuffersA = true;
			tempIndexA = index;
			UnityLibrary.MainThread.Call(CallInitDX11BufferA);

			while (isInitializingBuffersA == true && abortReaderThread == false) {
				Thread.Sleep(1);
			}

			tiles[index].isInQueue = false;
			tiles[index].isLoading = false;
			tiles[index].isReady = true;

		} // ReadPointCloudThreadedA

		byte[] dataPointsB = null;
		public void ReadPointCloudThreadedNewB(System.Object rawindex) {
			int index = (int)rawindex;
			tiles[index].isLoading = true;
			int pointCount = tiles[index].totalPoints;

			// points
#if UNITY_2019_1_OR_NEWER
			if (useNativeArrays == true) {
				if (tiles[index].pointsNative.IsCreated == true)
					tiles[index].pointsNative.Dispose();
				tiles[index].pointsNative = new NativeArray<byte>(pointCount * 12, Allocator.Persistent);
				dataPointsB = File.ReadAllBytes(tiles[index].filename);
				if (abortReaderThread || tiles[index].pointsNative.IsCreated == false)
					return;
				MoveFromByteArray(ref dataPointsB, ref tiles[index].pointsNative);
			} else {
				tiles[index].points = new Vector3[pointCount];
				GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
				IntPtr pV = vectorPointer.AddrOfPinnedObject();
				dataPointsB = File.ReadAllBytes(tiles[index].filename);
				//Debug.Log(index + " pointCount=" + pointCount + " points.len = " + tiles[index].points.Length + "  datapoints.len = " + dataPoints.Length);
				Marshal.Copy(dataPointsB, 0, pV, pointCount * 12);
				vectorPointer.Free();
			}
#else
            tiles[index].points = new Vector3[pointCount];
            GCHandle vectorPointer = GCHandle.Alloc(tiles[index].points, GCHandleType.Pinned);
            IntPtr pV = vectorPointer.AddrOfPinnedObject();
            dataPointsB = File.ReadAllBytes(tiles[index].filename);
            //Debug.Log(index + " pointCount=" + pointCount + " points.len = " + tiles[index].points.Length + "  datapoints.len = " + dataPoints.Length);
            Marshal.Copy(dataPointsB, 0, pV, pointCount * 12);
            vectorPointer.Free();
#endif
			tiles[index].loadedPoints = tiles[index].totalPoints;
			if (forceGC == true)
				GC.Collect();

			// colors
			if (isPackedColors == false) {
#if UNITY_2019_1_OR_NEWER
				if (useNativeArrays == true) {
					if (tiles[index].colorsNative.IsCreated == true)
						tiles[index].colorsNative.Dispose();
					tiles[index].colorsNative = new NativeArray<byte>(pointCount * 12, Allocator.Persistent);
					dataPointsB = File.ReadAllBytes(tiles[index].filenameRGB);
					if (abortReaderThread || tiles[index].colorsNative.IsCreated == false)
						return;
					MoveFromByteArray(ref dataPointsB, ref tiles[index].colorsNative);
				} else {
					tiles[index].colors = new Vector3[pointCount];
					GCHandle vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
					IntPtr pV = vectorPointer.AddrOfPinnedObject();
					dataPointsB = File.ReadAllBytes(tiles[index].filenameRGB);
					Marshal.Copy(dataPointsB, 0, pV, pointCount * 12);
					vectorPointer.Free();
				}
#else
                tiles[index].colors = new Vector3[pointCount];
                vectorPointer = GCHandle.Alloc(tiles[index].colors, GCHandleType.Pinned);
                pV = vectorPointer.AddrOfPinnedObject();
                dataPoints = File.ReadAllBytes(tiles[index].filenameRGB);
                Marshal.Copy(dataPoints, 0, pV, pointCount * 12);
                vectorPointer.Free();
#endif
				if (forceGC == true)
					GC.Collect();
			}

			// refresh buffers, check if needed?
			isInitializingBuffersB = true;
			tempIndexB = index;
			UnityLibrary.MainThread.Call(CallInitDX11BufferB);

			while (isInitializingBuffersB == true && abortReaderThread == false) {
				Thread.Sleep(1);
			}

			tiles[index].isInQueue = false;
			tiles[index].isLoading = false;
			tiles[index].isReady = true;

		} // ReadPointCloudThreadedA

		void CallInitDX11BufferA() {
			StartCoroutine(InitDX11BufferA());
		}

		void CallInitDX11BufferB() {
			StartCoroutine(InitDX11BufferB());
		}

		IEnumerator InitDX11BufferA() {
			int nodeIndex = tempIndexA;

			int pointCount = tiles[nodeIndex].loadedPoints;
			if (pointCount == 0) {
				isInitializingBuffersA = false;
				yield break;
			}
			// init buffers on demand, otherwise grabs full memory
			if (tiles[nodeIndex].bufferPoints != null)
				tiles[nodeIndex].bufferPoints.Release();
			tiles[nodeIndex].bufferPoints = new ComputeBuffer(pointCount, 12);
			if (isPackedColors == false) {
				if (tiles[nodeIndex].bufferColors != null)
					tiles[nodeIndex].bufferColors.Release();
				tiles[nodeIndex].bufferColors = new ComputeBuffer(pointCount, 12);
			}

			int stepSize = pointCount / gpuUploadSteps;
			if (stepSize == 0)
				Debug.LogError(nodeIndex + "  pointCount=" + pointCount + " stepSize=" + stepSize);
			int stepSizeBytes = stepSize;
			int stepCount = pointCount / stepSize;
			int total = 0;
#if UNITY_2019_1_OR_NEWER
			if (useNativeArrays == true) {
				stepSizeBytes *= 12;
			}
#endif
			for (int i = 0; i < stepCount; i++) {
#if UNITY_2019_1_OR_NEWER
				if (useNativeArrays == true) {
					if (tiles[nodeIndex].pointsNative.IsCreated == false)
						continue;
					tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].pointsNative, total, total, stepSizeBytes);
					tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
				} else {
					tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, total, total, stepSizeBytes);
					tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
				}
#else
                tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, total, total, stepSizeBytes);
                tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
#endif
				yield return 0;

				if (isPackedColors == false) {
#if UNITY_2019_1_OR_NEWER
					if (useNativeArrays == true) {
						if (tiles[nodeIndex].colorsNative.IsCreated == false)
							continue;
						tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colorsNative, total, total, stepSizeBytes);
						tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);

					} else {
						tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, total, total, stepSizeBytes);
						tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
					}
#else
                    tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, total, total, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
#endif
					yield return 0;
				}
				total += stepSizeBytes;
			}
			//Debug.Log(nodeIndex+" total=" + total + " / " + pointCount * 12 + " dif=" + (pointCount * 12 - total));
			isInitializingBuffersA = false;
		}

		IEnumerator InitDX11BufferB() {
			int nodeIndex = tempIndexB;

			int pointCount = tiles[nodeIndex].loadedPoints;
			if (pointCount == 0) {
				isInitializingBuffersB = false;
				yield break;
			}

			// init buffers on demand, otherwise grabs full memory
			if (tiles[nodeIndex].bufferPoints != null)
				tiles[nodeIndex].bufferPoints.Release();
			tiles[nodeIndex].bufferPoints = new ComputeBuffer(pointCount, 12);
			if (isPackedColors == false) {
				if (tiles[nodeIndex].bufferColors != null)
					tiles[nodeIndex].bufferColors.Release();
				tiles[nodeIndex].bufferColors = new ComputeBuffer(pointCount, 12);
			}
			int stepSize = pointCount / gpuUploadSteps;
			//if (stepSize == 0) Debug.LogError(nodeIndex + "  pointCount=" + pointCount + " stepSize=" + stepSize);
			int stepSizeBytes = stepSize;
			int stepCount = pointCount / stepSize;
			int total = 0;
#if UNITY_2019_1_OR_NEWER
			if (useNativeArrays == true) {
				stepSizeBytes *= 12;
			}
#endif
			for (int i = 0; i < stepCount; i++) {

#if UNITY_2019_1_OR_NEWER
				if (useNativeArrays == true) {
					if (tiles[nodeIndex].pointsNative.IsCreated == false)
						continue;
					tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].pointsNative, total, total, stepSizeBytes);
					tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
				} else {
					tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, total, total, stepSizeBytes);
					tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
				}
#else
                tiles[nodeIndex].bufferPoints.SetData(tiles[nodeIndex].points, total, total, stepSizeBytes);
                tiles[nodeIndex].material.SetBuffer(bufID, tiles[nodeIndex].bufferPoints);
#endif
				yield return 0;
				if (isPackedColors == false) {

#if UNITY_2019_1_OR_NEWER
					if (useNativeArrays == true) {
						if (tiles[nodeIndex].colorsNative.IsCreated == false)
							continue;
						tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colorsNative, total, total, stepSizeBytes);
						tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);

					} else {
						tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, total, total, stepSizeBytes);
						tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
					}
#else
                    tiles[nodeIndex].bufferColors.SetData(tiles[nodeIndex].colors, total, total, stepSizeBytes);
                    tiles[nodeIndex].material.SetBuffer(bufColorID, tiles[nodeIndex].bufferColors);
#endif
					yield return 0;
				}
				total += stepSizeBytes;
			}
			//Debug.Log(nodeIndex+" total=" + total + " / " + pointCount * 12 + " dif=" + (pointCount * 12 - total));
			isInitializingBuffersB = false;
		}

		public void ReleaseDX11Buffers() {
			if (tiles == null)
				return;
			for (int i = 0; i < tiles.Length; i++) {
				if (tiles[i].bufferPoints != null)
					tiles[i].bufferPoints.Release();
				tiles[i].bufferPoints = null;
				if (tiles[i].bufferColors != null)
					tiles[i].bufferColors.Release();
				tiles[i].bufferColors = null;
#if UNITY_2019_1_OR_NEWER
				if (useNativeArrays == true) {
					if (tiles[i].pointsNative.IsCreated == true)
						tiles[i].pointsNative.Dispose();
					if (tiles[i].colorsNative.IsCreated == true)
						tiles[i].colorsNative.Dispose();
				}
#endif
			}
		}

		void OnDestroy() {
			abortReaderThread = true;

			ReleaseDX11Buffers();
			if (rootLoaded == true) {
				cullGroup.onStateChanged -= OnCullingStateChange;
				cullGroup.Dispose();
				cullGroup = null;
			}
		}

		// drawing mainloop, for drawing the points
		//void OnPostRender() // < works also, BUT must have this script attached to Camera
		PointCloudTile pointCloudTileTemp;
		int visiblePointsTemp = 0;
		int tilesCount = 0;
		public void OnRenderObject() {
			// optional: if you only want to render to specific camera, use next line
			if (rootLoaded == false || (renderOnlyMainCam == true && Camera.current.CompareTag("MainCamera") == false))
				return;

			for (int i = 0, len = tilesCount; i < len; i++) {
				pointCloudTileTemp = tiles[i];
				visiblePointsTemp = pointCloudTileTemp.visiblePoints;
				if (pointCloudTileTemp.isReady == false || pointCloudTileTemp.isLoading == true || visiblePointsTemp == 0)
					continue;

				pointCloudTileTemp.material.SetPass(0);

#if UNITY_2019_1_OR_NEWER
				Graphics.DrawProceduralNow(MeshTopology.Points, visiblePointsTemp);
#else
                Graphics.DrawProcedural(MeshTopology.Points, visiblePointsTemp);
#endif
			}
		}

		// called after all tiles have been loaded
		public void OnLoadingCompleteCallBack(System.Object a) {
			if (OnLoadingComplete != null)
				OnLoadingComplete((string)a);
			Debug.Log("(Tiles Viewer) Finished loading all tiles");

			// NOTE hackfix for cullingdata not refreshing..
			StartCoroutine(RefreshCameraCulling());

			// for now, can stop worker threads
			//abortReaderThread = true;

			//PointCloudTools.DrawBounds(cloudBounds, 100);
		}

		// Temporary fix for culling group not refreshing (unless camera moved)
		IEnumerator RefreshCameraCulling() {
			cullGroup.SetDistanceReferencePoint(new Vector3(0, 999999, 0));
			yield return 0;
			cullGroup.SetDistanceReferencePoint(cam.transform);
		}

		public void FixMainThreadHelper() {
			if (GameObject.Find("#MainThreadHelper") == null || UnityLibrary.MainThread.instanceCount == 0) {
				var go = new GameObject();
				go.name = "#MainThreadHelper";
				go.AddComponent<UnityLibrary.MainThread>();
			}
		}

		BoundingSphere[] boundingSpheres;
		IEnumerator InitCullingSystem() {
			// validate values
			if (startDist < 0) {
				Debug.LogError("startDist must be > 0. You have set it to " + startDist);
				startDist = 0;
			}
			if (endDist < startDist) {
				Debug.LogError("endDist must be > startDist. You have set it to " + endDist);
				endDist = startDist + 1;
			}

			if (lodSteps < 2 || lodSteps > 180) {
				Debug.LogError("lodSteps must be in between 2-180. You have set it to " + lodSteps);
				lodSteps = 100;
			}

			// calculate distances
			cullingBandDistances = new float[lodSteps + 1];
			for (int i = 0; i < lodSteps; i++) {
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
			for (int i = 0; i < objectCount; i++) {
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

		void InitCullingValues() {
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
		void OnCullingStateChange(CullingGroupEvent e) {
			//Debug.Log(e.index + " " + e.isVisible);
			// FIXME this breaks initialization?
			//if (tiles[e.index].isReady == false) return;

			//if (e.hasBecomeInvisible)
			if (e.isVisible == false) // not visible tile TODO check if this breaks dispose?
			{
				tiles[e.index].visiblePoints = 0;

				// if too far, hide, and dispose if supported
				if (e.currentDistance == lodSteps + 1) {
					//PointCloudTools.DrawMinMaxBounds(tiles[e.index].minX, tiles[e.index].minY, tiles[e.index].minZ, tiles[e.index].maxX, tiles[e.index].maxY, tiles[e.index].maxZ, 10);

#if UNITY_2019_1_OR_NEWER
					if (tiles[e.index].isInQueue || tiles[e.index].isLoading)
						return;
					tiles[e.index].loadedPoints = 0;
					tiles[e.index].isReady = false;
					if (useNativeArrays == true) {
						if (releaseTileMemory == true && tiles[e.index].pointsNative.IsCreated) {
							tiles[e.index].pointsNative.Dispose();
							tiles[e.index].bufferPoints.Dispose();
						}
						if (releaseTileMemory == true && isPackedColors == false && tiles[e.index].colorsNative.IsCreated) {
							tiles[e.index].colorsNative.Dispose();
							tiles[e.index].bufferColors.Dispose();
						}
					}
#endif
				}
				return;
			}

			int distanceBand = e.currentDistance;

			float multiplier = (1f - (float)distanceBand / (float)(cullingBandDistances.Length - 1)) * tileResolution;
			//int newpointcount = (int)((float)tiles[e.index].loadedPoints * (useStrongFalloff ? EaseInQuint(0f, 1f, multiplier) : multiplier));
			int newpointcount = (int)((float)tiles[e.index].totalPoints * (useStrongFalloff ? EaseInQuint(0f, 1f, multiplier) : multiplier));

			if (useSizeMultiplier == true) {
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

			if (newpointcount > tiles[e.index].loadedPoints) {
				//Debug.Log("Not enought points loaded, index=" + e.index);
				tiles[e.index].visiblePoints = newpointcount;
				// TODO add to loading queue, to load more points

				if (tiles[e.index].isInQueue == false) {
					tiles[e.index].isInQueue = true;
					if (e.index % 2 == 0) {
						loadQueueA.Enqueue(e.index, distanceBand);
					} else {
						loadQueueB.Enqueue(e.index, distanceBand);
					}
				}

			} else // we have enough or too many points loaded
			  {
				tiles[e.index].visiblePoints = newpointcount;
			}
		}

		// easing methods by https://gist.github.com/cjddmut/d789b9eb78216998e95c Created by C.J. Kimberlin The MIT License (MIT)
		float EaseInQuint(float start, float end, float value) {
			end -= start;
			return end * value * value * value * value * value + start;
		}

#if UNITY_2019_1_OR_NEWER
		public unsafe void MoveFromByteArray<T>(ref byte[] src, ref NativeArray<T> dst) where T : struct {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckReadAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(dst));
			if (src == null)
				throw new ArgumentNullException(nameof(src));
#endif
			//            var size = UnsafeUtility.SizeOf<T>();
			//            if (src.Length != (size * dst.Length))
			//            {
			//                dst.Dispose();
			//                dst = new NativeArray<T>(src.Length / size, Allocator.Persistent);
			//#if ENABLE_UNITY_COLLECTIONS_CHECKS
			//                AtomicSafetyHandle.CheckReadAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(dst));
			//#endif
			//            }

			var dstAddr = (byte*)dst.GetUnsafeReadOnlyPtr();
			fixed (byte* srcAddr = src) {
				UnsafeUtility.MemCpy(&dstAddr[0], &srcAddr[0], src.Length);
			}
		}
#endif

		// PUBLIC API

		// return whole cloud bounds
		public Bounds GetBounds() {
			return cloudBounds;
		}

		// returns cloud (auto/manual) offset that was used in the converter
		public Vector3 GetOffset() {
			return cloudOffset;
		}

		// returns total pointcount
		public int GetTotalPointCount() {
			return totalPointCount;
		}

		// returns visible tiles count
		public int GetVisibleTileCount() {
			return cullGroup.QueryIndices(true, new int[tilesCount], 0);
		}

		// returns total visible point count
		public int GetVisiblePointCount() {
			int[] visibleTiles = new int[tilesCount];
			int visibleTileCount = cullGroup.QueryIndices(true, visibleTiles, 0);
			int counter = 0;
			for (int i = 0; i < visibleTileCount; i++) {
				counter += tiles[i].visiblePoints;
			}
			return counter;
		}

		public Bounds[] GetAllTileBounds() {
			Bounds[] results = new Bounds[tilesCount];

			for (int i = 0; i < tilesCount; i++) {
				results[i] = new Bounds(tiles[i].center, new Vector3(tiles[i].maxX - tiles[i].minX, tiles[i].maxY - tiles[i].minY, tiles[i].maxZ - tiles[i].minZ));
			}

			return results;
		}

		// TODO get node index by position, get node count, get visible nodes, get hidden nodes, ..

	} // class
} // namespace
