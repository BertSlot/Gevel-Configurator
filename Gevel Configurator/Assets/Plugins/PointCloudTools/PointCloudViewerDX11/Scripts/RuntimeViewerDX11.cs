// Point Cloud Binary Viewer DX11 for runtime parsing
// http://unitycoder.com

using UnityEngine;
using System.Collections;
using unitycodercom_PointCloudHelpers;
using Debug = UnityEngine.Debug;
using PointCloudHelpers;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using System;
using UnityLibrary;
#if !UNITY_SAMSUNGTV && !UNITY_WEBGL
using System.Threading;
using System.IO;
#endif

namespace PointCloudRuntimeViewer {
	// supported formats
	public enum PointCloudFormat {
		USE_FILE_EXTENSION,
		XYZ,
		XYZRGB,
		CGO,
		ASC,
		CATIA_ASC,
		PLY_ASCII,
		LAS,
		PTS,
		PCD_ASCII,
	}

	public class RuntimeViewerDX11 : MonoBehaviour {
#if !UNITY_SAMSUNGTV && !UNITY_WEBGL

		[Header("Settings")]
		public string fullPath = "raw.xyz";
		public bool loadAtStart = false;
		public Material cloudMaterial;
		[Tooltip("If disabled, uses mesh rendering")]
		public bool useDX11 = true;
		[Tooltip("Mesh rendering requires different material, those in Material/Mesh folder")]
		public Material meshMaterial;
		[Tooltip("Create copy of the material. Must enable if viewing multiple clouds with same materials")]
		public bool instantiateMaterial = false; // set True if using multiple viewers
		private int totalPoints = 0;
		[HideInInspector]
		public int totalMaxPoints = 0; // new variable to keep total maximum point count
		private ComputeBuffer bufferPoints;
		private ComputeBuffer bufferColors;
		private Vector3[] points;
		private Vector3[] pointColors;
		private Vector3 dataColor;
		private float r, g, b;

		private bool isLoading = true;
		private bool drawFirstFrameForced = false; // used with showFirstFrameWhenAvailable
		private bool haveError = false;

		// runtime reader
		[Tooltip("0=XYZ, 1=XYZRGB, 2=CGO, 3=ASC, 4=CATIA ASC, 5=PLY (ASCII), 6=LAS, 7=PTS, 8=PCD (ASCII)")]
		public PointCloudFormat pointCloudFormat = PointCloudFormat.USE_FILE_EXTENSION;
		public bool readRGB = false;
		public bool readIntensity = false; // only for PTS currently

		[Header("Visibility")]
		public bool displayPoints = true;
		[Tooltip("Enable this if you have multiple cameras and only want to draw in MainCamera")]
		public bool renderOnlyMainCam = false;

		[Header("Rendering")]
		[Tooltip("Draw using CommandBuffer instead of OnRenderObject")]
		public bool useCommandBuffer = false;
		[Tooltip("Default value: AfterForwardOpaque")]
		public CameraEvent camDrawPass = CameraEvent.AfterForwardOpaque;
		CommandBuffer commandBuffer;
		public bool forceDepthBufferPass = false;
		//        Material depthMaterial;
		[Tooltip("Changing CameraEvent takes effect only at Start(). Default value: AfterDepthTexture")]
		public CameraEvent camDepthPass = CameraEvent.AfterDepthTexture;
		CommandBuffer commandBufferDepth;

		[Header("Optional")]
		[Tooltip("Old brute-force method (to be decprecated)")]
		public bool enablePicking = true;
		public delegate void PointSelected(Vector3 pointPos);
		public event PointSelected PointWasSelected;
		// how many points are checked for measurement per frame (larger values will hang mainthread longer, too low values cause measuring to take very long time)
		int maxIterationsPerFrame = 256000;
		bool isSearchingPoint = false;

		public delegate void OnLoadComplete(string filename);
		public event OnLoadComplete OnLoadingComplete;

		//		private bool readNormals = false;
		public bool useUnitScale = false;
		public float unitScale = 0.001f;
		public bool flipYZ = true;
		public bool autoOffsetNearZero = true; // takes first point value as offset
		public bool useManualOffset = false;
		public Vector3 manualOffset = Vector3.zero;
		public bool plyHasNormals = false;
		private bool plyHasDensity = false;

		bool hasLoadedPointCloud = false;
		private long masterPointCount = 0;

		public bool showDebug = false;

		float[] LUT255 = new float[] { 0f, 0.00392156862745098f, 0.00784313725490196f, 0.011764705882352941f, 0.01568627450980392f, 0.0196078431372549f, 0.023529411764705882f, 0.027450980392156862f, 0.03137254901960784f, 0.03529411764705882f, 0.0392156862745098f, 0.043137254901960784f, 0.047058823529411764f, 0.050980392156862744f, 0.054901960784313725f, 0.058823529411764705f, 0.06274509803921569f, 0.06666666666666667f, 0.07058823529411765f, 0.07450980392156863f, 0.0784313725490196f, 0.08235294117647059f, 0.08627450980392157f, 0.09019607843137255f, 0.09411764705882353f, 0.09803921568627451f, 0.10196078431372549f, 0.10588235294117647f, 0.10980392156862745f, 0.11372549019607843f, 0.11764705882352941f, 0.12156862745098039f, 0.12549019607843137f, 0.12941176470588237f, 0.13333333333333333f, 0.13725490196078433f, 0.1411764705882353f, 0.1450980392156863f, 0.14901960784313725f, 0.15294117647058825f, 0.1568627450980392f, 0.1607843137254902f, 0.16470588235294117f, 0.16862745098039217f, 0.17254901960784313f, 0.17647058823529413f, 0.1803921568627451f, 0.1843137254901961f, 0.18823529411764706f, 0.19215686274509805f, 0.19607843137254902f, 0.2f, 0.20392156862745098f, 0.20784313725490197f, 0.21176470588235294f, 0.21568627450980393f, 0.2196078431372549f, 0.2235294117647059f, 0.22745098039215686f, 0.23137254901960785f, 0.23529411764705882f, 0.23921568627450981f, 0.24313725490196078f, 0.24705882352941178f, 0.25098039215686274f, 0.2549019607843137f, 0.25882352941176473f, 0.2627450980392157f, 0.26666666666666666f, 0.27058823529411763f, 0.27450980392156865f, 0.2784313725490196f, 0.2823529411764706f, 0.28627450980392155f, 0.2901960784313726f, 0.29411764705882354f, 0.2980392156862745f, 0.30196078431372547f, 0.3058823529411765f, 0.30980392156862746f, 0.3137254901960784f, 0.3176470588235294f, 0.3215686274509804f, 0.3254901960784314f, 0.32941176470588235f, 0.3333333333333333f, 0.33725490196078434f, 0.3411764705882353f, 0.34509803921568627f, 0.34901960784313724f, 0.35294117647058826f, 0.3568627450980392f, 0.3607843137254902f, 0.36470588235294116f, 0.3686274509803922f, 0.37254901960784315f, 0.3764705882352941f, 0.3803921568627451f, 0.3843137254901961f, 0.38823529411764707f, 0.39215686274509803f, 0.396078431372549f, 0.4f, 0.403921568627451f, 0.40784313725490196f, 0.4117647058823529f, 0.41568627450980394f, 0.4196078431372549f, 0.4235294117647059f, 0.42745098039215684f, 0.43137254901960786f, 0.43529411764705883f, 0.4392156862745098f, 0.44313725490196076f, 0.4470588235294118f, 0.45098039215686275f, 0.4549019607843137f, 0.4588235294117647f, 0.4627450980392157f, 0.4666666666666667f, 0.47058823529411764f, 0.4745098039215686f, 0.47843137254901963f, 0.4823529411764706f, 0.48627450980392156f, 0.49019607843137253f, 0.49411764705882355f, 0.4980392156862745f, 0.5019607843137255f, 0.5058823529411764f, 0.5098039215686274f, 0.5137254901960784f, 0.5176470588235295f, 0.5215686274509804f, 0.5254901960784314f, 0.5294117647058824f, 0.5333333333333333f, 0.5372549019607843f, 0.5411764705882353f, 0.5450980392156862f, 0.5490196078431373f, 0.5529411764705883f, 0.5568627450980392f, 0.5607843137254902f, 0.5647058823529412f, 0.5686274509803921f, 0.5725490196078431f, 0.5764705882352941f, 0.5803921568627451f, 0.5843137254901961f, 0.5882352941176471f, 0.592156862745098f, 0.596078431372549f, 0.6f, 0.6039215686274509f, 0.6078431372549019f, 0.611764705882353f, 0.615686274509804f, 0.6196078431372549f, 0.6235294117647059f, 0.6274509803921569f, 0.6313725490196078f, 0.6352941176470588f, 0.6392156862745098f, 0.6431372549019608f, 0.6470588235294118f, 0.6509803921568628f, 0.6549019607843137f, 0.6588235294117647f, 0.6627450980392157f, 0.6666666666666666f, 0.6705882352941176f, 0.6745098039215687f, 0.6784313725490196f, 0.6823529411764706f, 0.6862745098039216f, 0.6901960784313725f, 0.6941176470588235f, 0.6980392156862745f, 0.7019607843137254f, 0.7058823529411765f, 0.7098039215686275f, 0.7137254901960784f, 0.7176470588235294f, 0.7215686274509804f, 0.7254901960784313f, 0.7294117647058823f, 0.7333333333333333f, 0.7372549019607844f, 0.7411764705882353f, 0.7450980392156863f, 0.7490196078431373f, 0.7529411764705882f, 0.7568627450980392f, 0.7607843137254902f, 0.7647058823529411f, 0.7686274509803922f, 0.7725490196078432f, 0.7764705882352941f, 0.7803921568627451f, 0.7843137254901961f, 0.788235294117647f, 0.792156862745098f, 0.796078431372549f, 0.8f, 0.803921568627451f, 0.807843137254902f, 0.8117647058823529f, 0.8156862745098039f, 0.8196078431372549f, 0.8235294117647058f, 0.8274509803921568f, 0.8313725490196079f, 0.8352941176470589f, 0.8392156862745098f, 0.8431372549019608f, 0.8470588235294118f, 0.8509803921568627f, 0.8549019607843137f, 0.8588235294117647f, 0.8627450980392157f, 0.8666666666666667f, 0.8705882352941177f, 0.8745098039215686f, 0.8784313725490196f, 0.8823529411764706f, 0.8862745098039215f, 0.8901960784313725f, 0.8941176470588236f, 0.8980392156862745f, 0.9019607843137255f, 0.9058823529411765f, 0.9098039215686274f, 0.9137254901960784f, 0.9176470588235294f, 0.9215686274509803f, 0.9254901960784314f, 0.9294117647058824f, 0.9333333333333333f, 0.9372549019607843f, 0.9411764705882353f, 0.9450980392156862f, 0.9490196078431372f, 0.9529411764705882f, 0.9568627450980393f, 0.9607843137254902f, 0.9647058823529412f, 0.9686274509803922f, 0.9725490196078431f, 0.9764705882352941f, 0.9803921568627451f, 0.984313725490196f, 0.9882352941176471f, 0.9921568627450981f, 0.996078431372549f, 1f };

		string applicationStreamingAssetsPath;

		Vector3 tempPoint;
		Vector3 tempColor;
		private Camera cam;

		[Header("Caching")]
		[Tooltip("Output .bin file, so can load it with PointCloudViewerDX11, instead of parsing raw pointcloud again")]
		public bool cacheBinFile = false;
		[Tooltip("If cache file exists, don't save again")]
		public bool overrideExistingCacheFile = false;

		private void Awake() {
			applicationStreamingAssetsPath = Application.streamingAssetsPath;
		}

		// init
		private IEnumerator Start() {
			cam = Camera.main;

			if (instantiateMaterial == true) {
				cloudMaterial = new Material(cloudMaterial);
			}

			// check if MainThread script exists in scene, its required only for threading though
			if (GameObject.Find("#MainThreadHelper") == null) {
				var go = new GameObject("#MainThreadHelper");
				go.AddComponent<UnityLibrary.MainThread>();
			}


			if (useCommandBuffer == true) {
				commandBuffer = new CommandBuffer();
				cam.AddCommandBuffer(camDrawPass, commandBuffer);
			}

			if (forceDepthBufferPass == true) {
				//depthMaterial = cloudMaterial;
				commandBufferDepth = new CommandBuffer();
				cam.AddCommandBuffer(camDepthPass, commandBufferDepth);
			}

			if (loadAtStart == true) {
				// allow app to start first
				yield return new WaitForSecondsRealtime(1);

				try {
					CallImporterThreaded(fullPath);
				}
				catch (Exception e) {
					Debug.LogException(e);
				}

			}

			yield return null;
		}

		void Update() {
			if (isLoading == true || haveError == true)
				return;
			// experimentel point picking, to be removed
			if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
				return;
			if (enablePicking)
				SelectClosestPoint();
		}

		bool abortReaderThread = false;
		Thread importerThread;

		public void CallImporterThreaded(string fullPath) {
			// if not full path, try streaming assets
			if (Path.IsPathRooted(fullPath) == false) {
				fullPath = Path.Combine(Application.streamingAssetsPath, fullPath);
			}
			if (File.Exists(fullPath) == false) {
				Debug.LogError("File not found: " + fullPath);
				return;
			}

			Debug.Log("Reading threaded pointcloud file: " + fullPath, gameObject);

			ParameterizedThreadStart start = new ParameterizedThreadStart(LoadRawPointCloud);
			importerThread = new Thread(start);
			importerThread.IsBackground = true;
			importerThread.Start(fullPath); // TODO use normal thread, not params
		}

		// raw point cloud reader
		public void LoadRawPointCloud(System.Object a) {
			// cleanup old buffers
			if (useDX11 == true)
				ReleaseDX11Buffers();

			fullPath = (string)a;

			// if not full path, try streaming assets
			if (Path.IsPathRooted(fullPath) == false) {
				fullPath = Path.Combine(applicationStreamingAssetsPath, fullPath);
			}

			if (PointCloudTools.CheckIfFileExists(fullPath) == false) {
				Debug.LogError("File not found:" + fullPath);
				return;
			}

			// check if automatic fileformat, get extension
			if (pointCloudFormat == PointCloudFormat.USE_FILE_EXTENSION) {
				var extension = Path.GetExtension(fullPath).ToUpper();
				switch (extension) {
					case ".ASC":
						pointCloudFormat = PointCloudFormat.ASC;
						break;
					case ".CATIA_ASC":
						pointCloudFormat = PointCloudFormat.CATIA_ASC;
						break;
					case ".CGO":
						pointCloudFormat = PointCloudFormat.CGO;
						break;
					case ".LAS":
						pointCloudFormat = PointCloudFormat.LAS;
						break;
					case ".XYZ":
						pointCloudFormat = PointCloudFormat.XYZ;
						break;
					case ".PCD":
						pointCloudFormat = PointCloudFormat.PCD_ASCII;
						break;
					case ".PLY":
						pointCloudFormat = PointCloudFormat.PLY_ASCII;
						break;
					case ".PTS":
						pointCloudFormat = PointCloudFormat.PTS;
						break;
					case ".XYZRGB":
						pointCloudFormat = PointCloudFormat.XYZRGB;
						break;
					default:
						LogMessage("Unknown file extension: " + extension + ", trying to import as XYZRGB..");
						pointCloudFormat = PointCloudFormat.XYZRGB;
						break;
				}
			}

			// Custom reader for LAS binary
			if (pointCloudFormat == PointCloudFormat.LAS) {
				//LASDataConvert();
				LogMessage("LAS format is not yet supported in runtime importer:" + fullPath);
				return;
			}

			isLoading = true;
			hasLoadedPointCloud = false;

			LogMessage("Loading " + pointCloudFormat + " file: " + fullPath);

			long lines = 0;

			// get initial data (so can check if data is ok)
			using (StreamReader streamReader = new StreamReader(File.OpenRead(fullPath))) {
				double x = 0, y = 0, z = 0;
				float r = 0, g = 0, b = 0; //,nx=0,ny=0,nz=0;; // init vals
				string line = null;
				string[] row = null;

				PeekHeaderData headerCheck;
				headerCheck.x = 0;
				headerCheck.y = 0;
				headerCheck.z = 0;
				headerCheck.linesRead = 0;

				switch (pointCloudFormat) {
					case PointCloudFormat.ASC: // ASC (space at front)
						{
						headerCheck = PeekHeader.PeekHeaderASC(streamReader, readRGB);
						if (!headerCheck.readSuccess) { streamReader.Close(); return; }
						lines = headerCheck.linesRead;
					}
					break;

					case PointCloudFormat.CGO: // CGO	(counter at first line and uses comma)
						{
						headerCheck = PeekHeader.PeekHeaderCGO(streamReader, readRGB);
						if (!headerCheck.readSuccess) { streamReader.Close(); return; }
						lines = headerCheck.linesRead;
					}
					break;

					case PointCloudFormat.CATIA_ASC: // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
						{
						headerCheck = PeekHeader.PeekHeaderCATIA_ASC(streamReader, ref readRGB);
						if (!headerCheck.readSuccess) { streamReader.Close(); return; }
						lines = headerCheck.linesRead;
					}
					break;

					case PointCloudFormat.XYZRGB:
					case PointCloudFormat.XYZ: // XYZ RGB(INT)
						{
						headerCheck = PeekHeader.PeekHeaderXYZ(streamReader, ref readRGB);
						if (!headerCheck.readSuccess) { streamReader.Close(); return; }
						lines = headerCheck.linesRead;
					}
					break;

					case PointCloudFormat.PTS: // PTS (INT) (RGB)
						{
						headerCheck = PeekHeader.PeekHeaderPTS(streamReader, readRGB, readIntensity, ref masterPointCount);
						if (!headerCheck.readSuccess) { streamReader.Close(); return; }
						lines = headerCheck.linesRead;
					}
					break;

					case PointCloudFormat.PLY_ASCII: // PLY (ASCII)
						{
						headerCheck = PeekHeader.PeekHeaderPLY(streamReader, readRGB, ref masterPointCount, ref plyHasNormals, ref plyHasDensity);
						if (!headerCheck.readSuccess) { streamReader.Close(); return; }
					}
					break;

					case PointCloudFormat.PCD_ASCII: // PCD (ASCII)
						{
						headerCheck = PeekHeader.PeekHeaderPCD(streamReader, ref readRGB, ref masterPointCount);
						if (headerCheck.readSuccess == false) { streamReader.Close(); return; }
					}
					break;
					default:
						Debug.LogError("> Unknown fileformat error (1) " + pointCloudFormat);
						break;

				} // switch format


				if (autoOffsetNearZero == true) {
					manualOffset = new Vector3((float)headerCheck.x, (float)headerCheck.y, (float)headerCheck.z);
				}

				// scaling enabled, scale offset too
				if (useUnitScale == true)
					manualOffset *= unitScale;

				// progressbar
				long progressCounter = 0;

				// get total amount of points
				if (pointCloudFormat == PointCloudFormat.PLY_ASCII || pointCloudFormat == PointCloudFormat.PTS || pointCloudFormat == PointCloudFormat.CGO || pointCloudFormat == PointCloudFormat.PCD_ASCII) {
					lines = masterPointCount;

					// reset back to start of file
					streamReader.DiscardBufferedData();
					streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
					streamReader.BaseStream.Position = 0;

					// get back to before first actual data line
					for (int i = 0; i < headerCheck.linesRead - 1; i++) {
						streamReader.ReadLine();
					}

				} else { // other formats need to be read completely

					// reset back to start of file
					streamReader.DiscardBufferedData();
					streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
					streamReader.BaseStream.Position = 0;

					// get back to first actual data line
					for (int i = 0; i < headerCheck.linesRead; i++) {
						streamReader.ReadLine();
					}
					lines = 0;

					// calculate actual point data lines
					int splitCount = 0;
					LogMessage("calculating actual point data lines");
					while (streamReader.EndOfStream == false && abortReaderThread == false) {
						line = streamReader.ReadLine();

						if (progressCounter > 256000) {
							progressCounter = 0;
						}

						progressCounter++;


						if (line.Length > 9) {
							splitCount = CharCount(line, ' ');
							if (splitCount > 2 && splitCount < 16) {
								lines++;
							}
						}
					}


					// reset back to start of data
					streamReader.DiscardBufferedData();
					streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
					streamReader.BaseStream.Position = 0;

					// now skip header lines
					for (int i = 0; i < headerCheck.linesRead; i++) {
						streamReader.ReadLine();
					}

					masterPointCount = lines;
				}

				// create buffers
				points = new Vector3[masterPointCount];

				if (readRGB == true || readIntensity == true) {
					pointColors = new Vector3[masterPointCount];
				}

				totalPoints = (int)masterPointCount;

				progressCounter = 0;

				int skippedRows = 0;
				long rowCount = 0;
				bool haveMoreToRead = true;

				// process all points
				LogMessage("processing all points");
				while (haveMoreToRead == true && abortReaderThread == false) {
					if (progressCounter > 256000) {
						// TODO: add runtime progressbar
						//EditorUtility.DisplayProgressBar(appName, "Converting point cloud to binary file", rowCount / (float)lines);
						progressCounter = 0;
					}

					progressCounter++;

					line = streamReader.ReadLine();

					if (line != null)// && line.Length > 9)
					{
						// trim duplicate spaces
						line = line.Replace("   ", " ").Replace("  ", " ").Trim();
						row = line.Split(' ');

						if (row.Length > 2) {
							switch (pointCloudFormat) {
								case PointCloudFormat.ASC: // ASC
									if (line.IndexOf('!') == 0 || line.IndexOf('*') == 0) {
										skippedRows++;
										continue;
									}
									x = double.Parse(row[0]);
									y = double.Parse(row[1]);
									z = double.Parse(row[2]);
									break;

								case PointCloudFormat.CGO: // CGO	(counter at first line and uses comma)
									if (line.IndexOf('!') == 0 || line.IndexOf('*') == 0) {
										skippedRows++;
										continue;
									}
									x = double.Parse(row[0].Replace(",", "."));
									y = double.Parse(row[1].Replace(",", "."));
									z = double.Parse(row[2].Replace(",", "."));
									break;

								case PointCloudFormat.CATIA_ASC: // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
									if (line.IndexOf('!') == 0 || line.IndexOf('*') == 0) {
										skippedRows++;
										continue;
									}
									x = double.Parse(row[1]);
									y = double.Parse(row[3]);
									z = double.Parse(row[5]);
									break;

								case PointCloudFormat.XYZRGB:
								case PointCloudFormat.XYZ: // XYZ RGB(INT)
									x = double.Parse(row[0]);
									y = double.Parse(row[1]);
									z = double.Parse(row[2]);

									if (readRGB == true) {
										r = LUT255[int.Parse(row[3])];
										g = LUT255[int.Parse(row[4])];
										b = LUT255[int.Parse(row[5])];
									}
									break;

								case PointCloudFormat.PTS: // PTS (INT) (RGB)
									x = double.Parse(row[0]);
									y = double.Parse(row[1]);
									z = double.Parse(row[2]);

									if (readRGB == true) {
										if (row.Length == 7) // XYZIRGB
										{
											r = LUT255[int.Parse(row[4])];
											g = LUT255[int.Parse(row[5])];
											b = LUT255[int.Parse(row[6])];
										} else if (row.Length == 6) // XYZRGB
										  {
											r = LUT255[int.Parse(row[3])];
											g = LUT255[int.Parse(row[4])];
											b = LUT255[int.Parse(row[5])];
										}
									} else if (readIntensity == true) {
										if (row.Length == 4 || row.Length == 7) // XYZI or XYZIRGB
										{
											r = Remap(float.Parse(row[3]), -2048, 2047, 0, 1);
											g = r;
											b = r;
										}
									}
									break;

								case PointCloudFormat.PLY_ASCII: // PLY (ASCII)
									x = double.Parse(row[0]);
									y = double.Parse(row[1]);
									z = double.Parse(row[2]);

									/*
									// normals
									if (readNormals)
									{
										// Vertex normals are the normalized average of the normals of the faces that contain that vertex
										// TODO: need to fix normal values?
										nx = float.Parse(row[3]);
										ny = float.Parse(row[4]);
										nz = float.Parse(row[5]);

										// and rgb
										if (readRGB)
										{
											r = float.Parse(row[6])/255;
											g = float.Parse(row[7])/255;
											b = float.Parse(row[8])/255;
											//a = float.Parse(row[6])/255; // TODO: alpha not supported yet
										}

									}else{ // no normals, but maybe rgb
										*/
									if (readRGB == true) {
										// TODO: need to fix PLY CloudCompare normals, they are before RGB
										if (plyHasNormals == true) {
											r = LUT255[int.Parse(row[6])];
											g = LUT255[int.Parse(row[7])];
											b = LUT255[int.Parse(row[8])];
										} else if (plyHasDensity == false) // no normals or density
										  {
											r = LUT255[int.Parse(row[3])];
											g = LUT255[int.Parse(row[4])];
											b = LUT255[int.Parse(row[5])];
										} else // no normals, but have density
										  {
											r = LUT255[int.Parse(row[4])];
											g = LUT255[int.Parse(row[5])];
											b = LUT255[int.Parse(row[6])];
										}
										//a = float.Parse(row[6])/255; // TODO: alpha not supported yet
									}
									/*
									}*/
									break;

								case PointCloudFormat.PCD_ASCII: // pcd ascii
									x = double.Parse(row[0]);
									y = double.Parse(row[1]);
									z = double.Parse(row[2]);

									if (readRGB == true) {
										// TODO: need to check both rgb formats
										if (row.Length == 4) {
											var rgb = (int)decimal.Parse(row[3], System.Globalization.NumberStyles.Float);
											r = (rgb >> 16) & 0x0000ff;
											g = (rgb >> 8) & 0x0000ff;
											b = (rgb) & 0x0000ff;
											r = LUT255[(int)r];
											g = LUT255[(int)g];
											b = LUT255[(int)b];
										} else if (row.Length == 6) {
											r = LUT255[int.Parse(row[3])];
											g = LUT255[int.Parse(row[4])];
											b = LUT255[int.Parse(row[5])];
										}
									}
									break;


								default:
									Debug.LogError("> Error Unknown format:" + pointCloudFormat);
									break;

							} // switch

							// scaling enabled
							if (useUnitScale == true) {
								x *= unitScale;
								y *= unitScale;
								z *= unitScale;
							}

							// manual offset enabled
							if (autoOffsetNearZero == true || useManualOffset == true) // NOTE: can use only one at a time
							{
								x -= manualOffset.x;
								y -= manualOffset.y;
								z -= manualOffset.z;
							}

							// if flip
							if (flipYZ == true) {
								points[rowCount].Set((float)x, (float)z, (float)y);
							} else {
								points[rowCount].Set((float)x, (float)y, (float)z);
							}

							// if have color data
							if (readRGB == true || readIntensity == true) {
								pointColors[rowCount].Set(r, g, b);
							}
							/*
							// if have normals data, TODO: not possible yet
							if (readNormals)
							{
								writer.Write(nx);
								writer.Write(ny);
								writer.Write(nz);
							}
							*/

							rowCount++;

						} else { // if row length
							skippedRows++;
						}

					} else { // if linelen
						skippedRows++;
					}


					// reached end or enough points
					if (streamReader.EndOfStream == true || rowCount >= masterPointCount) {

						if (skippedRows > 0)
							Debug.LogWarning("Parser skipped " + skippedRows + " rows (wrong length or bad data)");
						//Debug.Log(masterVertexCount);

						if (rowCount < masterPointCount) // error, file ended too early, not enough points
						{
							Debug.LogWarning("File does not contain enough points, fixing point count to " + rowCount + " (expected : " + masterPointCount + ")");
							// fix header point count
							//                            writer.BaseStream.Seek(0, SeekOrigin.Begin);
							//                            writer.Write(binaryVersion);
							//                            writer.Write((System.Int32)rowCount);
						}
						haveMoreToRead = false;
					}
				} // while loop reading file


				// done reading, display it now
				isLoading = false;
				if (useDX11 == true) {
					MainThread.Call(InitDX11Buffers);
					Thread.Sleep(10); // wait for buffers to be ready_
				}
				OnLoadingCompleteCallBack(fullPath);

				hasLoadedPointCloud = true;
			} // using reader

			Debug.Log("Finished loading.");

			// if mesh version, build meshes
			if (useDX11 == false) {
				// build mesh assets
				int indexCount = 0;

#if UNITY_2017_3_OR_NEWER
				int MaxVertexCountPerMesh = 1000000;
#else
				int MaxVertexCountPerMesh = 65000;
#endif

				Vector3[] verts = new Vector3[MaxVertexCountPerMesh];
				Vector2[] uvs2 = new Vector2[MaxVertexCountPerMesh];
				int[] tris = new int[MaxVertexCountPerMesh];
				Color[] cols = new Color[MaxVertexCountPerMesh];
				Vector3[] norms = new Vector3[MaxVertexCountPerMesh];

				// process all point data into meshes
				for (int i = 0, len = points.Length; i < len; i++) {
					verts[indexCount] = points[i];
					uvs2[indexCount].Set(points[i].x, points[i].y);
					tris[indexCount] = i % MaxVertexCountPerMesh;

					if (readRGB || readIntensity) {
						cols[indexCount] = new Color(pointColors[i].x, pointColors[i].y, pointColors[i].z, 1);
					}
					//if (readNormals) normals2[indexCount] = normalArray[i];

					indexCount++;

					if (indexCount >= MaxVertexCountPerMesh || i == MaxVertexCountPerMesh - 1) {
						var m = new TempMesh();
						m.verts = verts;
						m.tris = tris;
						m.cols = cols;
						m.norms = norms;
						//Debug.Log(m.verts.Length);
						isBuildingMesh = true;
						MainThread.Call(BuildMesh, m);
						while (isBuildingMesh == true) {
							Thread.Sleep(50);
						}
						//if (addMeshesToScene && go != null) if (createLODS) BuildLODS(go, vertices2, triangles2, colors2, normals2);

						indexCount = 0;

						// need to clear arrays, should use lists otherwise last mesh has too many verts (or slice last array)
						System.Array.Clear(verts, 0, MaxVertexCountPerMesh);
						System.Array.Clear(uvs2, 0, MaxVertexCountPerMesh);
						System.Array.Clear(tris, 0, MaxVertexCountPerMesh);
						if (readRGB || readIntensity)
							System.Array.Clear(cols, 0, MaxVertexCountPerMesh);
						//if (readNormals) System.Array.Clear(norms, 0, MaxVertexCountPerMesh);
					}
				} // all points
			} // use dx11

			// if caching, save as bin
			if (cacheBinFile == true) {
				var outputFile = fullPath + ".bin";

				if (File.Exists(outputFile) == true && overrideExistingCacheFile == false) {
					Debug.Log("Cache file already exists, not saving new cached file.." + outputFile);
					return;
				}

				var writer = new BinaryWriter(File.Open(outputFile, FileMode.Create));
				if (writer == null) {
					Debug.LogError("Cannot output file: " + outputFile);
					return;
				}

				byte binaryVersion = 1;
				writer.Write(binaryVersion);
				writer.Write((System.Int32)masterPointCount);
				writer.Write(readRGB | readIntensity);

				for (int i = 0, length = points.Length; i < length; i++) {
					writer.Write(points[i].x);
					writer.Write(points[i].y);
					writer.Write(points[i].z);
					if (readRGB == true || readIntensity == true) {
						writer.Write(pointColors[i].x);
						writer.Write(pointColors[i].y);
						writer.Write(pointColors[i].z);
					}
				}
				writer.Close();
				Debug.Log("Finished saving cached file: " + outputFile);
			} // cache
		} // LoadRawPointCloud()


		public struct TempMesh {
			public Vector3[] verts;
			public int[] tris;
			public Color[] cols;
			public Vector3[] norms;
		}

		public void InitDX11Buffers() {
			// cannot init 0 size, so create dummy data if its 0
			if (totalPoints == 0) {
				totalPoints = 1;
				points = new Vector3[1];
				if (readRGB == true) {
					pointColors = new Vector3[1];
				}
			}

			// clear old buffers
			if (useDX11 == true)
				ReleaseDX11Buffers();

			if (bufferPoints != null)
				bufferPoints.Dispose();

			bufferPoints = new ComputeBuffer(totalPoints, 12);
			bufferPoints.SetData(points);
			cloudMaterial.SetBuffer("buf_Points", bufferPoints);

			if (readRGB == true) {
				if (bufferColors != null)
					bufferColors.Dispose();
				bufferColors = new ComputeBuffer(totalPoints, 12);
				bufferColors.SetData(pointColors);
				cloudMaterial.SetBuffer("buf_Colors", bufferColors);
			}

			if (forceDepthBufferPass == true) {
				// not needed now, since using same material
				//cloudMaterial.SetBuffer("buf_Points", bufferPoints);
			}
		}


		void ReleaseDX11Buffers() {
			if (bufferPoints != null)
				bufferPoints.Release();
			bufferPoints = null;
			if (bufferColors != null)
				bufferColors.Release();
			bufferColors = null;
		}

		void OnDestroy() {
			abortReaderThread = true;

			if (importerThread != null)
				importerThread.Abort();

			if (useDX11 == true) {
				ReleaseDX11Buffers();

				// cleanup
				points = new Vector3[0];
				pointColors = new Vector3[0];
			}

		}


		// mainloop, for displaying the points
		//	void OnPostRender () // < works also if attached to camera
		void OnRenderObject() {
			// optional: if you only want to render to specific camera, use next line
			if (renderOnlyMainCam == true && Camera.current.CompareTag("MainCamera") == false)
				return;

			if (displayPoints == false || useCommandBuffer == true)
				return;
			if (drawFirstFrameForced == false && isLoading == true)
				return;

			cloudMaterial.SetPass(0);
#if UNITY_2019_1_OR_NEWER
			Graphics.DrawProceduralNow(MeshTopology.Points, totalPoints);
#else
			Graphics.DrawProcedural(MeshTopology.Points, totalPoints);
#endif

		}


		// called after some file load operation has finished
		void OnLoadingCompleteCallBack(System.Object a) {
			if (OnLoadingComplete != null)
				OnLoadingComplete((string)a);

			if (useCommandBuffer == true) {
				commandBuffer.DrawProcedural(Matrix4x4.identity, cloudMaterial, 0, MeshTopology.Points, totalPoints, 1);
			}

			if (forceDepthBufferPass == true) {
				commandBufferDepth.DrawProcedural(Matrix4x4.identity, cloudMaterial, 0, MeshTopology.Points, totalPoints, 1);
			}
		}

		// bruteforce point picker
		void SelectClosestPoint() {
			// left click for measuring
			if (Input.GetMouseButtonDown(0)) {
				if (!isSearchingPoint && hasLoadedPointCloud)
					StartCoroutine(FindClosestPointBrute(Input.mousePosition));
			}
		}

		// TODO replace with new measuring system
		IEnumerator FindClosestPointBrute(Vector2 mousePos) // in screen pixel coordinates
		{
			isSearchingPoint = true;

			int? closestIndex = null;
			float closestDistance = Mathf.Infinity;
			Camera cam = Camera.main;

			var offsetPixels = new Vector2(0, 32); // search area in pixels
			var farPointUp = cam.ScreenPointToRay(mousePos + offsetPixels).GetPoint(999);
			var farPointDown = cam.ScreenPointToRay(mousePos - offsetPixels).GetPoint(999);

			offsetPixels = new Vector2(32, 0);  // search area in pixels
			var farPointLeft = cam.ScreenPointToRay(mousePos - offsetPixels).GetPoint(999);
			var farPointRight = cam.ScreenPointToRay(mousePos + offsetPixels).GetPoint(999);

			var screenPos = Vector2.zero;
			float distance = Mathf.Infinity;

			// build filtering planes
			Plane forwardPlane = new Plane(cam.transform.forward, cam.transform.position);
			Plane bottomLeft = new Plane(cam.transform.position, farPointDown, farPointLeft);
			Plane topLeft = new Plane(cam.transform.position, farPointLeft, farPointUp);
			Plane topRight = new Plane(cam.transform.position, farPointUp, farPointRight);
			Plane bottomRight = new Plane(cam.transform.position, farPointRight, farPointDown);

			/*
			// display search area
			Debug.DrawLine(farPointDown,farPointLeft, Color.magenta,20);
			Debug.DrawLine(farPointLeft,farPointUp, Color.magenta,20);
			Debug.DrawLine(farPointUp,farPointRight,Color.magenta,20);
			Debug.DrawLine(farPointRight,farPointDown,Color.magenta,20);
			*/

			// check all points, until find close enough hit
			var pixelThreshold = 3; // if distance is this or less, just select it

			for (int i = 0, len = points.Length; i < len; i++) {
				if (i % maxIterationsPerFrame == 0) {
					// Pause our work here, and continue finding on the next frame
					yield return null;
				}

				if (!forwardPlane.GetSide(points[i]))
					continue;
				if (topRight.GetSide(points[i]))
					continue;
				if (bottomRight.GetSide(points[i]))
					continue;
				if (bottomLeft.GetSide(points[i]))
					continue;
				if (topLeft.GetSide(points[i]))
					continue;

				screenPos = cam.WorldToScreenPoint(points[i]);

				distance = Vector2.Distance(mousePos, screenPos);
				//distance = DistanceApprox(mousePos, screenPos);

				if (distance < closestDistance) {
					closestDistance = distance;
					closestIndex = i;
					if (distance <= pixelThreshold)
						break; // early exit on close enough hit
				}
			}

			if (closestIndex != null) {
				if (PointWasSelected != null)
					PointWasSelected(points[(int)closestIndex]); // fire event if have listeners
				Debug.Log("PointIndex:" + ((int)closestIndex) + " pos:" + points[(int)closestIndex]);
			} else {
				Debug.Log("No point selected..");
			}
			isSearchingPoint = false;
		}

		void LogMessage(string msg) {
			Debug.Log(msg);
		}

		bool ValidateSaveAndRead(string path, string fileToRead) {
			if (path.Length < 1) { Debug.Log("> Save cancelled.."); return false; }
			if (fileToRead.Length < 1) { Debug.LogError("> Cannot find file (" + fileToRead + ")"); return false; }
			if (!File.Exists(fileToRead)) { Debug.LogError("> Cannot find file (" + fileToRead + ")"); return false; }
			if (Path.GetExtension(fileToRead).ToLower() == ".bin") { Debug.LogError("Source file extension is .bin, binary file conversion is not supported"); return false; }
			return true;
		}

		float Remap(float source, float sourceFrom, float sourceTo, float targetFrom, float targetTo) {
			return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
		}

		int CharCount(string source, char separator) {
			int count = 0;
			for (int i = 0, length = source.Length; i < length; i++) {
				if (source[i] == separator)
					count++;
			}
			return count;
		}


		public bool IsLoading() {
			return isLoading;
		}

		bool isBuildingMesh = false;
		int meshCounter = 0;
		void BuildMesh(System.Object a) {
			var m = (TempMesh)a;
			var verts = m.verts;
			var tris = m.tris;
			var colors = m.cols;
			//var normals = m.norms;

			GameObject target = new GameObject();

			var mf = target.AddComponent<MeshFilter>();
			var mr = target.AddComponent<MeshRenderer>();

			Mesh mesh = new Mesh();
#if UNITY_2017_3_OR_NEWER
			mesh.indexFormat = IndexFormat.UInt32;
#endif
			target.isStatic = false;
			mf.mesh = mesh;
			target.transform.name = "PC_" + meshCounter;
			mr.sharedMaterial = meshMaterial;
			mr.receiveShadows = false;
			mr.shadowCastingMode = ShadowCastingMode.Off;
#if UNITY_5_6_OR_NEWER
			mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#else
			mr.lightProbeUsage = LightProbeUsage.Off;
#endif
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

			// disable ligtmap static 
			//GameObjectUtility.SetStaticEditorFlags(target, ~StaticEditorFlags.LightmapStatic);

			//GameObject lodRoot = null;

			//target.transform.parent = folder.transform;

			mesh.vertices = verts;
			//mesh.uv = uvs;
			if (readRGB == true || readIntensity == true) {
				mesh.colors = colors;
			}
			//if (readNormals == true) mesh.normals = normals;

			// TODO: use scanner centerpoint and calculate direction from that..not really accurate
			//if (forceRecalculateNormals) ...

			mesh.SetIndices(tris, MeshTopology.Points, 0);
			//mesh.RecalculateBounds();

			//cloudList.Add(mesh);
			meshCounter++;

			// FIXME: temporary workaround to not add objects into scene..
			//if (addMeshesToScene == false) DestroyImmediate(target);

			//return target;
			isBuildingMesh = false;
		}

#endif

	} // class
} // namespace

