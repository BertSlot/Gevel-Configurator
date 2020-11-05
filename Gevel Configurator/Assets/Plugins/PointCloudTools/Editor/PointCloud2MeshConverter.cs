// Point Cloud to Unity Mesh Converter
// Converts pointcloud data into multiple mesh assets
// http://unitycoder.com

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.Rendering;
using System.Threading;
using System.Globalization;

#pragma warning disable 0219 // disable unused var warnings


namespace unitycodercom_PointCloud2MeshConverter
{

    public class PointCloud2MeshConverter : EditorWindow
    {
        private static string appName = "PointCloud2Mesh";
        private Object sourceFile;

        private bool readRGB = false;
        private bool readIntensity = false;
        private bool readNormals = false;
        private bool useScaling = true;
        private float scaleValue = 0.001f;
        private bool flipYZ = true;
        private bool autoOffsetNearZero = true; // takes first point value as offset
        private bool useManualOffset = false;
        private Vector3 manualOffset = Vector3.zero;
        private Vector3 autoOffset = Vector3.zero;


        // advanced settings
        private bool addMeshesToScene = true;
        private bool sortPoints = false;
        private bool splitToGrid = false;
        private float gridSize = 5; // in meters
        private int minPointCount = 1000;
        private bool createLODS = false;
        private int lodLevels = 3; // including full mesh (so 2 are generated)
        private int minLodVertexCount = 1000; // last LOD mesh has this many verts
        private bool decimatePoints = false;
        private int removeEveryNth = 5;
        //		private bool forceRecalculateNormals = false;

        // mesh generation stuff
        // TODO: only use 32bit if enough points
#if UNITY_2017_3_OR_NEWER
        private int MaxVertexCountPerMesh = 1000000;
#else
        private int MaxVertexCountPerMesh = 65000;
#endif
        private Material meshMaterial;
        private List<Mesh> cloudList = new List<Mesh>();
        private int meshCounter = 1;
        private GameObject folder;
        private long masterPointCount = 0;
        private string savePath;
        private int pcCounter = 0;

        readonly static string prefsPrefix = "unitycoder_" + appName + "_";

        // create menu item and window
        [MenuItem("Window/PointCloudTools/Convert Point Cloud To Unity Meshes", false, 2)]
        static void Init()
        {
            PointCloud2MeshConverter window = (PointCloud2MeshConverter)EditorWindow.GetWindow(typeof(PointCloud2MeshConverter));
            window.titleContent = new GUIContent(appName);
            window.minSize = new Vector2(340, 630);
            window.maxSize = new Vector2(340, 634);

            // force dot as decimal separator
            string CultureName = Thread.CurrentThread.CurrentCulture.Name;
            CultureInfo ci = new CultureInfo(CultureName);
            if (ci.NumberFormat.NumberDecimalSeparator != ".")
            {
                ci.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = ci;
            }
        }

        // main loop
        void OnGUI()
        {
            // source file
            GUILayout.Label("Point Cloud source file", EditorStyles.boldLabel);
            sourceFile = EditorGUILayout.ObjectField(sourceFile, typeof(Object), false);

            // TODO: only get fileinfo once, no need to request again
            GUILayout.Label(sourceFile != null ? "file:" + GetSelectedFileInfo() : "", EditorStyles.miniLabel);

            EditorGUILayout.Space();

            // start import settings
            GUILayout.Label("Import settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            readRGB = EditorGUILayout.ToggleLeft(new GUIContent("Read RGB values", null, "Read R G B values"), readRGB, GUILayout.Width(160));
            readIntensity = EditorGUILayout.ToggleLeft(new GUIContent("Read Intensity value", null, "Read intensity value"), readIntensity);
            readRGB = readIntensity ? false : readRGB;
            EditorGUILayout.EndHorizontal();

            readNormals = EditorGUILayout.ToggleLeft(new GUIContent("Read Normal values (PLY)", null, "Only for .PLY files (and most shaders do not support Vertex Normals)"), readNormals);

            // extra options
            EditorGUILayout.Space();
            useScaling = EditorGUILayout.BeginToggleGroup(new GUIContent("Scale values", null, "Enable scaling"), useScaling);
            scaleValue = EditorGUILayout.FloatField(new GUIContent("Scaling multiplier", null, "To scale millimeters to unity meters, use 0.001"), scaleValue);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();
            flipYZ = EditorGUILayout.ToggleLeft(new GUIContent("Flip Y & Z values", null, "Flip YZ values because Unity Y is up"), flipYZ);
            EditorGUILayout.Space();
            autoOffsetNearZero = EditorGUILayout.ToggleLeft(new GUIContent("Auto-offset near 0,0,0", null, "Takes first line from xyz data as offset"), autoOffsetNearZero);
            useManualOffset = EditorGUILayout.BeginToggleGroup(new GUIContent("Add manual offset", null, "Add this offset to XYZ values"), useManualOffset);
            manualOffset = EditorGUILayout.Vector3Field(new GUIContent("Worldspace Offset", null, ""), manualOffset);
            GUILayout.Label("This value wont be scaled, added after YZ values are flipped", EditorStyles.miniLabel);

            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            // advanced settings
            EditorGUILayout.Space();
            GUILayout.Label("Advanced Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            addMeshesToScene = EditorGUILayout.ToggleLeft(new GUIContent("Add Meshes to current scene", null, "Creates mesh objects and adds them to this scene"), addMeshesToScene);
            sortPoints = EditorGUILayout.ToggleLeft(new GUIContent("Sort points in X axis", null, "Sorts points on X axis"), sortPoints);
            splitToGrid = EditorGUILayout.ToggleLeft(new GUIContent("Split to Grid *BETA", null, "NOTE: Requires Sort points to be enabled!"), splitToGrid);
            gridSize = EditorGUILayout.FloatField(new GUIContent("Grid cell size (m)", null, ""), Mathf.Clamp(gridSize, 0.5f, 100f));
            minPointCount = EditorGUILayout.IntField(new GUIContent("min. cell point count", null, "Discards cells with less points"), (int)Mathf.Clamp(minPointCount, 1, Mathf.Infinity));
            //			forceRecalculateNormals = EditorGUILayout.ToggleLeft(new GUIContent("Force RecalculateNormals()",null,"Note: Uses builtin RecalculateNormals(), it wont give correct normals"), forceRecalculateNormals);
            createLODS = EditorGUILayout.ToggleLeft(new GUIContent("Create LODS", null, "Note: Works better with SortPoints or Split2Grid"), createLODS);
            GUI.enabled = createLODS;
            lodLevels = EditorGUILayout.IntSlider(new GUIContent("LOD levels:", null, "Including LOD0 (main) mesh level"), lodLevels, 2, 4);
            //minLodVertexCount = EditorGUILayout.IntSlider(new GUIContent("Minimum LOD point count:",null,"How many points in the last (furthest) LOD mesh"), minLodVertexCount, 1, Mathf.Clamp(vertCount-1,1,65000));
            GUI.enabled = true;

            decimatePoints = EditorGUILayout.ToggleLeft(new GUIContent("Decimate points", null, "Skip rows from data (You should do this with external tools to get better results)"), decimatePoints);
            GUI.enabled = decimatePoints;
            removeEveryNth = EditorGUILayout.IntField(new GUIContent("Remove every Nth point", null, ""), Mathf.Clamp(removeEveryNth, 0, 999999));
            GUI.enabled = true;


            // mesh settings
            EditorGUILayout.Space();
            GUILayout.Label("Mesh Output settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

#if UNITY_2017_3_OR_NEWER
            MaxVertexCountPerMesh = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Vertices per mesh", null, "How many verts per mesh. !Warning: Low values will create millions of files!"), MaxVertexCountPerMesh), 1000, 100000000);
#else
            MaxVertexCountPerMesh = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Vertices per mesh", null, "How many verts per mesh (max 65k). !Warning: Low values will create millions of files!"), MaxVertexCountPerMesh), 1000, 65000);
#endif

            meshMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("Mesh material", null, "Material & Shader for generated meshes *Use MeshPointsDX11QuadOffset.mat if this is for DX11 or later"), meshMaterial, typeof(Material), true);

            // TODO:
            //addToScene = EditorGUILayout.ToggleLeft(new GUIContent("Add meshes to current scene", null, ""), addToScene);

            EditorGUILayout.Space();
            GUI.enabled = sourceFile == null ? false : true; // disabled if no source selected
            if (GUILayout.Button(new GUIContent("Convert to Meshes", "Convert source to meshes"), GUILayout.Height(40)))
            {
                pcCounter = 0;
                savePath = null;

                // check if need to process whole folder
                string fileToRead = AssetDatabase.GetAssetPath(sourceFile);
                if (Directory.Exists(fileToRead))
                {
                    // get all suitable files in this folder
                    string[] filters = new[] { "*.xyz", "*.asc", "*.las", "*.pts", "*.cgo", "*.ply", "*.xyzrgb", ".pcd" };
                    string[] filePaths = filters.SelectMany(f => Directory.GetFiles(fileToRead, f)).ToArray();

                    // convert files one by one
                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        folder = new GameObject();
                        pcCounter++;
                        folder.name = "PointClouds" + pcCounter;
                        Convert2Mesh(filePaths[i]);
                    }

                }
                else
                { // single point cloud
                    var stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    folder = new GameObject("PointClouds-" + sourceFile.name);
                    Convert2Mesh(fileToRead);
                    stopwatch.Stop();
                    Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms");
                    stopwatch.Reset();
                }

            }
            GUI.enabled = true;
        }


        void Convert2Mesh(string fileToRead)
        {
            cloudList.Clear();
            meshCounter = 1;

            if (savePath == null)
            {
                if (addMeshesToScene == true && createLODS == false)
                {
                    savePath = EditorUtility.SaveFilePanelInProject("Mesh assets output folder & basename", "PointChunk", "prefab", "Set base filename");
                }
                else
                {
                    savePath = EditorUtility.SaveFilePanelInProject("Mesh assets output folder & basename", "PointChunk", "asset", "Set base filename");
                }
            }


            // check path
            if (savePath.Length == 0)
            {
                Debug.LogWarning(appName + "> Cancelled..");
                return;
            }

            if (fileToRead.Length != 0)
            {
                //Debug.Log ("readfile: "+path);
            }
            else
            {
                Debug.LogWarning(appName + "> Cannot find file (" + fileToRead + ")");
                return;
            }

            if (File.Exists(fileToRead) == false)
            {
                Debug.LogWarning(appName + "> Cannot find file (" + fileToRead + ")");
                return;
            }

            EditorUtility.DisplayProgressBar(appName, "Checking file..", 0.25f);

            string fileExtension = Path.GetExtension(fileToRead).ToLower();

            if (fileExtension != ".ply")
            {
                if (readNormals == true)
                {
                    Debug.LogWarning(appName + "> Importing normals is only supported for .PLY files");
                    readNormals = false;
                }
            }

            // TEMPORARY: Custom reader for Brekel binary data
            if (fileExtension == ".bin")
            {
                EditorUtility.ClearProgressBar();
                BrekelDataConvert(fileToRead);
                return;
            }

            // TEMPORARY: Custom reader for LAS binary data
            if (fileExtension == ".las")
            {
                EditorUtility.ClearProgressBar();
                LASConverter(fileToRead);
                return;
            }

            masterPointCount = 0;
            long lines = 0;
            int dataCount = 0;

            Vector3[] vertexArray;
            Color[] colorArray = null;
            int[] indexArray;
            Vector3[] normalArray = null;

            double minCornerX = Mathf.Infinity;
            double minCornerY = Mathf.Infinity;
            double minCornerZ = Mathf.Infinity;
            double maxCornerX = Mathf.NegativeInfinity;
            double maxCornerY = Mathf.NegativeInfinity;
            double maxCornerZ = Mathf.NegativeInfinity;

            // reading whole file
            using (StreamReader reader = new StreamReader(File.OpenRead(fileToRead)))
            {
                double x = 0, y = 0, z = 0, nx = 0, ny = 0, nz = 0;
                float r = 0, g = 0, b = 0;
                int indexR = 3, indexG = 4, indexB = 5;
                int indexNX = 3, indexNY = 4, indexNZ = 5;
                int indexI = 3;

                string rawLine = null;
                string origRawLine = null;
                string[] lineSplitted = null;
                int commentsLength = 0;
                int commentLines = 0;

                // formats
                bool replaceCommas = false; // for cgo, catia asc (depends also on pc regional settings, comma doesnt work dot works always?)

                // find first line of point data
                bool comments = true;
                bool headerData = true;
                bool hasNormals = false;
                bool plyHeaderFinished = false;
                bool plyNormalsBeforeRGB = false; // default from MeshLab XYZ NNN RGB

                // parse header
                while (comments == true && reader.EndOfStream == false)
                {
                    origRawLine = reader.ReadLine();

                    // early exit if certain file type
                    if (fileExtension == ".ply" && headerData == true)
                    {
                        // RGB data before normals
                        if (hasNormals == true && (origRawLine.ToLower().Contains("property float red") || origRawLine.ToLower().Contains("property uchar red")))
                        {
                            plyNormalsBeforeRGB = true;
                        }

                        if (origRawLine.ToLower().Contains("property float nx"))
                        {
                            hasNormals = true;
                        }

                        if (origRawLine.ToLower().Contains("format binary"))
                        {
                            Debug.LogError("Only .PLY ASCII format is supported, your file is PLY Binary");
                            EditorUtility.ClearProgressBar();
                            headerData = false;
                            return;
                        }

                        if (origRawLine.ToLower().Contains("element vertex"))
                        {
                            // get point count
                            int tempParse = 0;
                            if (int.TryParse(origRawLine.Split(' ')[2], out tempParse))
                            {
                                masterPointCount = tempParse;
                            }
                            else
                            {
                                Debug.LogError("PLY Header parsing failed, point count not founded");
                                EditorUtility.ClearProgressBar();
                                return;
                            }
                        }

                        if (origRawLine.ToLower().Contains("end_header"))
                        {
                            headerData = false;
                            plyHeaderFinished = true;
                        }

                    }
                    else if ((fileExtension == ".pts" || fileExtension == ".cgo") && masterPointCount == 0)
                    {
                        //Debug.Log(origRawLine);
                        // get point count
                        int tempParse = 0;
                        rawLine = Regex.Replace(origRawLine, "[^.0-9 ]+[^e\\-\\d]", "").Trim(); // cleanup non numeric

                        if (int.TryParse(rawLine, out tempParse))
                        {
                            masterPointCount = tempParse;
                            commentLines = 1;
                            headerData = false;
                        }
                        else
                        {
                            Debug.LogError(fileExtension.ToUpper() + " pts/cgo header parsing failed, point count not founded");
                            EditorUtility.ClearProgressBar();
                            return;

                        }
                    }
                    else if (fileExtension == ".pcd" && headerData == true)
                    {
                        if (origRawLine.Contains("POINTS "))
                        {
                            rawLine = origRawLine.Replace("POINTS ", "").Trim();
                            long.TryParse(rawLine, out masterPointCount);
                        }

                        if (origRawLine.ToLower().Contains("data binary"))
                        {
                            Debug.LogError("Only .pcd ASCII format is supported, your file is PCD Binary");
                            EditorUtility.ClearProgressBar();
                            headerData = false;
                            return;
                        }

                        if (origRawLine.ToLower().Contains("data ascii"))
                        {
                            headerData = false;
                        }

                    }
                    else // other formats
                    {
                        // no custom data
                        headerData = false;
                    }

                    rawLine = origRawLine.Replace(",", "."); // for cgo/catia asc/pts
                    rawLine = Regex.Replace(origRawLine, "[^.0-9 ]+[^e\\-\\d]", ""); // cleanup non numeric
                    rawLine = rawLine.Replace("   ", " ").Replace("  ", " ").Trim();

                    lineSplitted = rawLine.Split(' ');

                    // its still comments or invalid data
                    //Debug.Log(lineSplitted.Length + " , " + ValidateColumns(lineSplitted.Length) + " : " + origRawLine);
                    //Debug.Log("splitlen:" + CheckIfDataIsComments(lineSplitted.Length) + " " + lineSplitted.Length + " hea:" + headerData);
                    //                    Debug.Break();
                    if (headerData == true || rawLine.StartsWith("#") || rawLine.StartsWith("!") || rawLine.StartsWith("*") || rawLine.ToLower().Contains("comment") || CheckIfDataIsComments(lineSplitted.Length) == false)
                    {
                        commentsLength += origRawLine.Length + 1; // +1 is end of line?
                        commentLines++;
                    }
                    else // actual data, get first row
                    {
                        if (replaceCommas == false && rawLine.Contains(",")) replaceCommas = true;
                        if (replaceCommas == true) rawLine = rawLine.Replace(",", "."); // for cgo/catia asc/pts

                        lineSplitted = rawLine.Split(' ');

                        if (readRGB && lineSplitted.Length < 6)
                        {
                            if (fileExtension != ".pcd") Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false;
                        }

                        if (readIntensity == true && (lineSplitted.Length != 4 && lineSplitted.Length != 7)) { Debug.LogError("No Intensity data founded after XYZ, disabling readIntensity"); readIntensity = false; }

                        if (readNormals == true)
                        {
                            //if (lineSplitted.Length!=6 && lineSplitted.Length != 7 && lineSplitted.Length != 9 && lineSplitted.Length != 10) {
                            if (hasNormals == false)
                            {
                                Debug.LogError("No normals data founded, disabling readNormals. [" + lineSplitted.Length + " values founded]");
                                readNormals = false;

                            }
                            else
                            { // we have normals
                                if (plyNormalsBeforeRGB == true)
                                {
                                    indexR += 3;
                                    indexG += 3;
                                    indexB += 3;
                                }
                                else // RGB before normals
                                {

                                }
                            }
                        }
                        else // dont read normals
                        { // check if normals are there, but we dont use them
                            //Debug.Log("hasnormals" + hasNormals);
                            if (hasNormals == true)
                            {
                                if (plyNormalsBeforeRGB == true)
                                {
                                    indexR += 3;
                                    indexG += 3;
                                    indexB += 3;
                                }
                                else // RGB before normals
                                {

                                }
                            }

                            // do we have intensity  for .pts? then skip intensity
                            if (fileExtension == ".pts" && readIntensity == false && lineSplitted.Length == 7)
                            {
                                indexR += 1;
                                indexG += 1;
                                indexB += 1;
                            }

                        }

                        dataCount = lineSplitted.Length;
                        comments = false;
                        lines++;
                    }
                } // while (parsing header)

                //Debug.Log("dc "+ dataCount);

                bool skipRow = false;
                int skippedRows = 0;

                // get first actual data row
                if (!double.TryParse(lineSplitted[0], out x)) skipRow = true;
                if (!double.TryParse(lineSplitted[1], out y)) skipRow = true;
                if (!double.TryParse(lineSplitted[2], out z)) skipRow = true;


                if (skipRow == true)
                {
                    skippedRows++;
                    Debug.LogWarning("First point data row was skipped, conversion will most likely fail (rawline:" + rawLine + ")");
                }


                // get negative autooffset value, needs to be flipped and scaled to match final data
                if (autoOffsetNearZero == true)
                {
                    autoOffset = -new Vector3((float)x, (float)y, (float)z);
                    // scaling enabled, scale auto-offset too
                    //if (useScaling == true) autoOffset *= scaleValue;
                }

                // jump back to start of first line
                EditorUtility.ClearProgressBar();

                // use header count value from ply, cgo, pts..
                if (fileExtension == ".ply" || fileExtension == ".pts" || fileExtension == ".cgo" || fileExtension == ".pcd")
                {
                    lines = masterPointCount;
                }
                else // other formats need to calculate lines
                {
                    // loop all data lines
                    while (reader.EndOfStream == false)
                    {
                        origRawLine = reader.ReadLine();

                        if (lines % 256000 == 1)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar(appName, "Counting lines: " + lines, lines / 20000000.0f))
                            {
                                Debug.Log("Cancelled at: " + lines);
                                EditorUtility.ClearProgressBar();
                                return;
                            }
                        }

                        // check if data is valid
                        // skip comments
                        if (rawLine.IndexOf("!") == 0) continue; // catia asc
                        if (rawLine.IndexOf("*") == 0) continue;
                        if (rawLine.IndexOf("#") == 0) continue;
                        if (replaceCommas == true) rawLine = origRawLine.Replace(",", ".");
                        //rawLine = Regex.Replace(rawLine, "[^.0-9 ]+[^e\\-\\d]", ""); // cleanup non numeric, NOTE does this cause exp value issues?
                        rawLine = rawLine.Replace("   ", " ").Replace("  ", " ").Trim();
                        lineSplitted = rawLine.Split(' ');
                        if (lineSplitted.Length == dataCount) lines++;
                    } // count points

                    EditorUtility.ClearProgressBar();
                }

                // reset back to start
                reader.DiscardBufferedData();
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.BaseStream.Position = 0;

                if (commentLines > 0)
                {
                    for (int i = 0; i < commentLines; i++)
                    {
                        var tmp = reader.ReadLine();
                    }
                }


                masterPointCount = lines;

                vertexArray = new Vector3[masterPointCount];
                indexArray = new int[masterPointCount];

                if (readRGB == true || readIntensity == true) colorArray = new Color[masterPointCount];
                if (readNormals == true) normalArray = new Vector3[masterPointCount];

                long rowCount = 0;
                bool readMore = true;
                double tempVal = 0;
                bool maybeCatiaAsc = fileExtension == ".asc";

                //read all point cloud data here
                for (rowCount = 0; rowCount < masterPointCount - 1; rowCount++)
                {
                    if (rowCount % 256000 == 1)
                    {
                        EditorUtility.DisplayProgressBar(appName, "Processing " + rowCount + " / " + masterPointCount + " points", rowCount / (float)lines);
                        //progressCounter=0;
                    }

                    // process each line
                    rawLine = reader.ReadLine().Trim();

                    // trim duplicate spaces
                    rawLine = rawLine.Replace("   ", " ").Replace("  ", " ").Trim();
                    rawLine = rawLine.Replace(",", "."); // mostly for cgo/catia asc/pts


                    // cleanup non numeric, needed for catia asc
                    if (maybeCatiaAsc == true) rawLine = Regex.Replace(rawLine, "[^.0-9 ]+[^e\\-\\d]", "").Trim();

                    lineSplitted = rawLine.Split(' ');

                    // have same amount of columns in data?
                    if (lineSplitted.Length == dataCount)
                    {
                        if (!double.TryParse(lineSplitted[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out x)) skipRow = true;
                        if (!double.TryParse(lineSplitted[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out y)) skipRow = true;
                        if (!double.TryParse(lineSplitted[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out z)) skipRow = true;
                        //if (!double.TryParse(lineSplitted[0], out x)) skipRow = true;
                        //if (!double.TryParse(lineSplitted[1], out y)) skipRow = true;
                        //if (!double.TryParse(lineSplitted[2], out z)) skipRow = true;

                        //if (rowCount < 10) Debug.Log(lineSplitted[0] + "," + lineSplitted[1] + "," + lineSplitted[2] + "    " + x + "," + y + "," + z);

                        if (readRGB == true)
                        {
                            r = LUT255[System.Convert.ToInt32(lineSplitted[indexR])];
                            g = LUT255[System.Convert.ToInt32(lineSplitted[indexG])];
                            b = LUT255[System.Convert.ToInt32(lineSplitted[indexB])];
                        }

                        if (readIntensity == true)
                        {
                            // TODO: handle different intensity values
                            if (!float.TryParse(lineSplitted[indexI], out r)) skipRow = true;

                            // re-range PTS intensity
                            if (fileExtension == ".pts")
                            {
                                r = Remap(r, -2048, 2047, 0, 1);
                            }

                            // for now, set green and blue to intensity color also
                            g = r;
                            b = r;
                        }

                        if (readNormals == true)
                        {
                            if (!double.TryParse(lineSplitted[indexNX], out nx)) skipRow = true;
                            if (!double.TryParse(lineSplitted[indexNY], out ny)) skipRow = true;
                            if (!double.TryParse(lineSplitted[indexNZ], out nz)) skipRow = true;
                        }

                        if (autoOffsetNearZero == true)
                        {
                            x += autoOffset.x;
                            y += autoOffset.y;
                            z += autoOffset.z;
                        }

                        // if flip
                        if (flipYZ == true)
                        {
                            tempVal = z;
                            z = y;
                            y = tempVal;

                            // flip normals?
                            if (readNormals == true)
                            {
                                tempVal = nz;
                                nz = ny;
                                ny = tempVal;
                            }
                        }

                        // scaling enabled
                        if (useScaling == true)
                        {
                            x *= scaleValue;
                            y *= scaleValue;
                            z *= scaleValue;
                        }

                        if (useManualOffset == true)
                        {
                            x += manualOffset.x;
                            y += manualOffset.y;
                            z += manualOffset.z;
                        }

                        // get cloud corners
                        if (skipRow == false)
                        {
                            minCornerX = System.Math.Min(x, minCornerX);
                            minCornerY = System.Math.Min(y, minCornerY);
                            minCornerZ = System.Math.Min(z, minCornerZ);

                            maxCornerX = System.Math.Max(x, maxCornerX);
                            maxCornerY = System.Math.Max(y, maxCornerY);
                            maxCornerZ = System.Math.Max(z, maxCornerZ);

                            vertexArray[rowCount].Set((float)x, (float)y, (float)z);
                            if (splitToGrid == true)
                            {
                                indexArray[rowCount] = (int)rowCount;
                            }
                            else
                            {
                                indexArray[rowCount] = ((int)rowCount) % MaxVertexCountPerMesh; // some problem if sorted?
                            }
                        }

                        if (readRGB == true || readIntensity == true)
                        {
                            colorArray[rowCount].r = r;
                            colorArray[rowCount].g = g;
                            colorArray[rowCount].b = b;
                            colorArray[rowCount].a = 1f;
                        }

                        if (readNormals == true)
                        {
                            normalArray[rowCount].Set((float)nx, (float)ny, (float)nz);
                        }


                    }
                    else
                    { // if row length is bad
                        skipRow = true;
                    }
                    //					} // line len


                    if (skipRow == true)
                    {
                        skippedRows++;
                        skipRow = false;
                    }
                    else
                    {
                        //						rowCount++;
                    }

                    //					if (reader.EndOfStream)// || rowCount>=masterPointCount)
                    //					{
                    //Debug.Log(reader.EndOfStream);
                    //Debug.Log(rowCount>=masterPointCount);
                    //readMore = false;
                    //						Debug.LogError("Reached end of file too early ("+rowCount+"/"+masterPointCount+")");
                    //						break;
                    //					}

                } // while reading file
                EditorUtility.ClearProgressBar();

                if (skippedRows > 0) Debug.LogWarning("Skipped " + skippedRows.ToString() + " rows (out of " + masterPointCount + " rows) because of parsing errors");

            } // using reader

            // sort points
            if (sortPoints == true)
            {
                EditorUtility.DisplayProgressBar(appName, "Sorting points..", 0.25f);
                ArrayQuickSort(ref vertexArray, ref colorArray, ref indexArray, 0, vertexArray.Length - 1);
                EditorUtility.ClearProgressBar();
            }

            // BETA split to grid
            if (splitToGrid == true)
            {
                Dictionary<string, List<int>> nodes = new Dictionary<string, List<int>>();

                // loop all points
                for (int i = 0, len = vertexArray.Length; i < len; i++)
                {
                    if (decimatePoints == true)
                    {
                        if (i % removeEveryNth == 0) continue;
                    }

                    // get point
                    var p = vertexArray[indexArray[i]];

                    // add to correct cell
                    int cellX = (int)(p.x / gridSize); // TODO use inverted *
                    int cellY = (int)(p.y / gridSize);
                    int cellZ = (int)(p.z / gridSize);
                    //Debug.Log("cellX="+ (int)cellX);

                    string key = cellX + "_" + cellY + "_" + cellZ;
                    //Debug.Log("key="+key);
                    if (nodes.ContainsKey(key))
                    {
                        nodes[key].Add(indexArray[i]);
                    }
                    else
                    { // create new list for this key
                        nodes[key] = new List<int>();
                        nodes[key].Add(indexArray[i]);
                    }
                } // for all points

                // build meshes per cell, leave out below x points

                // loop all cells
                int ii = 0;
                foreach (var indexes in nodes.Values)
                {
                    //Debug.Log("indexes:" + indexes.Count);
                    //if (indexes.Count < minimumPointCount) continue;
                    //var c = Random.ColorHSV();
                    //var count = indexes.Count;

                    // loop points from node
                    var cellverts = new List<Vector3>();
                    var celltris = new List<int>();
                    var cellcolors = new List<Color>();
                    int pointcount = 0;
                    float minX = Mathf.Infinity;
                    float minY = Mathf.Infinity;
                    float minZ = Mathf.Infinity;
                    float maxX = Mathf.NegativeInfinity;
                    float maxY = Mathf.NegativeInfinity;
                    float maxZ = Mathf.NegativeInfinity;

                    // loop all points within cell
                    for (int i = 0, len = indexes.Count; i < len; i++)
                    {
                        // TODO add range
                        var p = vertexArray[indexes[i]];
                        if (p.x < minX) minX = p.x;
                        if (p.x > maxX) maxX = p.x;
                        if (p.y < minY) minY = p.y;
                        if (p.y > maxY) maxY = p.y;
                        if (p.z < minZ) minZ = p.z;
                        if (p.z > maxZ) maxZ = p.z;

                        //cellverts.Add(vertexArray[indexes[i]]);
                        cellverts.Add(p);
                        //celltris.Add(indexArray[indexes[i]]);
                        celltris.Add(pointcount);
                        cellcolors.Add(colorArray[indexes[i]]);
                        //cellcolors.Add(c);
                        pointcount++;

                        // collect until max vert count, then build
                        if (pointcount >= MaxVertexCountPerMesh || i == len - 1)
                        {
                            // skip this mesh if not enough points
                            if (pointcount > minPointCount)
                            {
                                var cv = cellverts.ToArray();
                                var ci = celltris.ToArray();
                                var cc = cellcolors.ToArray();

                                var bounds = new Bounds(new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f), new Vector3((maxX - minX), (maxY - minY), (maxZ - minZ)));

                                var go = BuildMesh(cv, ci, cc, null, bounds);

                                if (addMeshesToScene == true && go != null)
                                {
                                    if (createLODS == true)
                                    {
                                        BuildLODS(go, cv, ci, cc, null, bounds);

                                        var ld = go.transform.parent.GetComponent<LODGroup>();
                                        ld.RecalculateBounds();
                                    }

                                }
                            }

                            pointcount = 0;
                            cellverts.Clear();
                            celltris.Clear();
                            cellcolors.Clear();
                        } // if enough points for one mesh
                    } // loop points in cell
                } // loop cells

                SaveMeshesToProject();

            } // splittogrid
            else // old way
            {
                // build mesh assets
                int indexCount = 0;

                Vector3[] vertices2 = new Vector3[MaxVertexCountPerMesh];
                Vector2[] uvs2 = new Vector2[MaxVertexCountPerMesh];
                int[] triangles2 = new int[MaxVertexCountPerMesh];
                Color[] colors2 = new Color[MaxVertexCountPerMesh];
                Vector3[] normals2 = new Vector3[MaxVertexCountPerMesh];

                EditorUtility.DisplayProgressBar(appName, "Creating " + ((int)(vertexArray.Length / MaxVertexCountPerMesh)) + " mesh arrays", 0.75f);

                float x, y, z;

                float minX = Mathf.Infinity;
                float minY = Mathf.Infinity;
                float minZ = Mathf.Infinity;
                float maxX = Mathf.NegativeInfinity;
                float maxY = Mathf.NegativeInfinity;
                float maxZ = Mathf.NegativeInfinity;

                // process all point data into meshes
                for (int i = 0, totalLen = vertexArray.Length; i < totalLen; i++)
                {
                    x = vertexArray[i].x;
                    y = vertexArray[i].y;
                    z = vertexArray[i].z;

                    vertices2[indexCount].x = x;
                    vertices2[indexCount].y = y;
                    vertices2[indexCount].z = z;

                    // TODO: not used really
                    uvs2[indexCount].x = x;
                    uvs2[indexCount].y = y;

                    triangles2[indexCount] = indexArray[i];
                    if (readRGB == true || readIntensity == true) colors2[indexCount] = colorArray[i];
                    if (readNormals == true) normals2[indexCount] = normalArray[i];

                    // get bounds
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    if (z < minZ) minZ = z;
                    if (z > maxZ) maxZ = z;

                    if (decimatePoints == true)
                    {
                        if (i % removeEveryNth == 0) indexCount++;
                    }
                    else
                    {
                        indexCount++;
                    }

                    if (indexCount >= MaxVertexCountPerMesh || i == vertexArray.Length - 1)
                    {
                        var bounds = new Bounds(new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f), new Vector3((maxX - minX), (maxY - minY), (maxZ - minZ)));
                        //PointCloudHelpers.PointCloudTools.DrawBounds(bounds,30);

                        var go = BuildMesh(vertices2, triangles2, colors2, normals2, bounds);
                        if (addMeshesToScene == true && go != null)
                        {
                            if (createLODS == true) BuildLODS(go, vertices2, triangles2, colors2, normals2, bounds);
                        }

                        // reset bounds for mesh
                        minX = Mathf.Infinity;
                        minY = Mathf.Infinity;
                        minZ = Mathf.Infinity;
                        maxX = Mathf.NegativeInfinity;
                        maxY = Mathf.NegativeInfinity;
                        maxZ = Mathf.NegativeInfinity;

                        indexCount = 0;

                        // need to clear arrays, should use lists otherwise last mesh has too many verts (or slice last array)
                        System.Array.Clear(vertices2, 0, MaxVertexCountPerMesh);
                        System.Array.Clear(uvs2, 0, MaxVertexCountPerMesh);
                        System.Array.Clear(triangles2, 0, MaxVertexCountPerMesh);

                        if (readRGB == true || readIntensity == true) System.Array.Clear(colors2, 0, MaxVertexCountPerMesh);
                        if (readNormals == true) System.Array.Clear(normals2, 0, MaxVertexCountPerMesh);
                    }
                } // all points

                SaveMeshesToProject();

            } // old way not gridsplit

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            Debug.Log("Total amount of points: " + masterPointCount);

        } // convert2meshOptimized2


        void SaveMeshesToProject()
        {
            EditorUtility.ClearProgressBar();
            // save meshes
            EditorUtility.DisplayProgressBar(appName, "Saving " + (cloudList.Count) + " mesh assets", 0.95f);
            string pad = "";
            // if no meshes in scene, or using lods, then save simple meshes for now
            if (addMeshesToScene == false || createLODS == true)
            {
                // TODO instantiate temporary gameobject, save that, destroy
                for (int i = 0; i < cloudList.Count; i++)
                {
                    if (i < 1000) pad = "0";
                    if (i < 100) pad = "00";
                    if (i < 10) pad = "000";
                    AssetDatabase.CreateAsset(cloudList[i], savePath + "_" + pcCounter + "_" + pad + i + ".asset");
                    AssetDatabase.SaveAssets(); // not needed?
                } // save meshes
            }
            else // they are in scene
            {
                for (int i = 0; i < folder.transform.childCount; i++)
                {
                    if (i < 1000) pad = "0";
                    if (i < 100) pad = "00";
                    if (i < 10) pad = "000";
                    string save = savePath + "_" + pcCounter + "_" + pad + i + ".prefab";
                    // save mesh inside prefab http://answers.unity.com/answers/1464552/view.html
#if UNITY_2018_4_OR_NEWER
                    // need to delete old one, otherwise mesh gets appended
                    AssetDatabase.DeleteAsset(save);
                    var rootPrefab = PrefabUtility.SaveAsPrefabAsset(folder.transform.GetChild(i).gameObject, save);
#else
                    var rootPrefab = PrefabUtility.CreatePrefab(save, folder.transform.GetChild(i).gameObject);
#endif
                    var mf = rootPrefab.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        mf.sharedMesh = cloudList[i];
                        AssetDatabase.AddObjectToAsset(cloudList[i], rootPrefab);
                    }
                    AssetDatabase.SaveAssets();
                }
            } // in scene
        }

        void OnDestroy()
        {
            SavePreferences();
        }

        /// <summary>
        /// returns true if good array length
        /// </summary>
        bool CheckIfDataIsComments(int len)
        {
            //      XYZ         XYZI        XYZRGB      XYZIRGB     XYZRGB??    XYZNNNRGB   XYZNNNRGBA?
            return (len == 3 || len == 4 || len == 6 || len == 7 || len == 8 || len == 9 || len == 10);
        }




        void BrekelDataConvert(string fileToRead)
        {
            EditorUtility.DisplayProgressBar(appName, "Converting Brekel frames to meshes", 0.5f);

            var reader = new BinaryReader(File.OpenRead(fileToRead));

            byte binaryVersion = reader.ReadByte();
            int numberOfFrames = reader.ReadInt32();
            //float frameRate=reader.ReadSingle(); // NOT YET USED
            reader.ReadSingle(); // skip framerate field
            bool containsRGB = reader.ReadBoolean();

            // TODO: if its 1, read our own binary
            if (binaryVersion != 2) Debug.LogWarning("BinaryVersion is not 2 - reading file most likely fails..");

            /*
			Debug.Log("binaryVersion:"+binaryVersion);
			Debug.Log("numberOfFrames:"+numberOfFrames);
			Debug.Log("frameRate:"+frameRate);
			Debug.Log("containsRGB:"+containsRGB);
			*/

            int pointCounter = 0; // array index
            Vector3[] vertices = new Vector3[MaxVertexCountPerMesh];
            Vector2[] uvs = new Vector2[MaxVertexCountPerMesh];
            int[] triangles = new int[MaxVertexCountPerMesh];
            Color[] colors = new Color[MaxVertexCountPerMesh];
            Vector3[] normals = new Vector3[MaxVertexCountPerMesh];

            int[] numberOfPointsPerFrame;
            numberOfPointsPerFrame = new int[numberOfFrames];

            for (int i = 0; i < numberOfFrames; i++)
            {
                numberOfPointsPerFrame[i] = reader.ReadInt32();//(int)System.BitConverter.ToInt32(data,byteIndex);
            }

            // Binary positions for each frame, not used
            for (int i = 0; i < numberOfFrames; i++)
            {
                reader.ReadInt64();
            }


            float x, y, z, r, g, b;

            for (int frame = 0; frame < numberOfFrames; frame++)
            {
                for (int i = 0; i < numberOfPointsPerFrame[frame]; i++)
                {
                    x = reader.ReadSingle();
                    y = reader.ReadSingle();
                    z = reader.ReadSingle();

                    if (containsRGB == true)
                    {
                        r = reader.ReadSingle();
                        g = reader.ReadSingle();
                        b = reader.ReadSingle();
                        colors[pointCounter] = new Color(r, g, b, 1f);
                    }

                    // scaling enabled
                    if (useScaling == true)
                    {
                        x *= scaleValue;
                        y *= scaleValue;
                        z *= scaleValue;
                    }


                    // if flip
                    if (flipYZ == true)
                    {
                        vertices[pointCounter] = new Vector3(x, z, y);
                    }
                    else
                    { // noflip
                        vertices[pointCounter] = new Vector3(x, y, z);
                    }

                    if (autoOffsetNearZero == true)
                    {
                        vertices[pointCounter] += autoOffset;
                    }

                    // add manual offset
                    if (useManualOffset == true)
                    {
                        vertices[pointCounter] += manualOffset;
                    }

                    //uvs[pointCounter] = new Vector2(x, y);
                    triangles[pointCounter] = pointCounter;

                    pointCounter++;

                    // do we have enough for this mesh?
                    if (pointCounter >= MaxVertexCountPerMesh || i == numberOfPointsPerFrame[frame] - 1)
                    {
                        BuildMesh(vertices, triangles, colors, normals);
                        pointCounter = 0;
                    }
                } // points on each frame

            } // frames

            EditorUtility.DisplayProgressBar(appName, "Saving created meshes", 0.75f);

            // save meshes
            string pad = "";

            // if no meshes in scene, or using lods, then save simple meshes for now
            if (addMeshesToScene == false || createLODS == true)
            {
                // TODO instantiate temporary gameobject, save that, destroy
                for (int i = 0; i < cloudList.Count; i++)
                {
                    if (i < 1000) pad = "0";
                    if (i < 100) pad = "00";
                    if (i < 10) pad = "000";
                    AssetDatabase.CreateAsset(cloudList[i], savePath + "_" + pcCounter + "_" + pad + i + ".asset");
                    AssetDatabase.SaveAssets(); // not needed?
                } // save meshes
            }
            else // they are in scene
            {
                for (int i = 0; i < folder.transform.childCount; i++)
                {
                    if (i < 1000) pad = "0";
                    if (i < 100) pad = "00";
                    if (i < 10) pad = "000";

                    string save = savePath + "_" + pcCounter + "_" + pad + i + ".prefab";

                    // save mesh inside prefab http://answers.unity.com/answers/1464552/view.html
#if UNITY_2018_4_OR_NEWER
                    var rootPrefab = PrefabUtility.SaveAsPrefabAsset(folder.transform.GetChild(i).gameObject, save);
#else
                    var rootPrefab = PrefabUtility.CreatePrefab(save, folder.transform.GetChild(i).gameObject);
#endif
                    var mf = rootPrefab.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        mf.sharedMesh = cloudList[i];
                        AssetDatabase.AddObjectToAsset(cloudList[i], rootPrefab);
                    }
                    AssetDatabase.SaveAssets();
                }
            }

            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();
            reader.Close();
        }


        // converting las files separately
        void LASConverter(string fileToRead)
        {
            if (splitToGrid == true)
            {
                Debug.LogError("Editor LAS converter doesn't support SplitToGrid - cancelling");
                return;
            }

            if (createLODS == true)
            {
                Debug.LogError("Editor LAS converter doesn't support CreateLODS - cancelling");
                return;
            }

            Vector3[] vertexArray = null;
            Color[] colorArray = null;
            int[] indexArray = null;
            Vector3[] normalArray = null;

            Vector3 minCorner = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 maxCorner = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);

            double x = 0, y = 0, z = 0;
            int r = 0, g = 0, b = 0;

            // open file
            BinaryReader reader = new BinaryReader(File.OpenRead(fileToRead));

            string fileSignature = new string(reader.ReadChars(4));

            if (fileSignature != "LASF") Debug.LogError("LAS> FileSignature error: '" + fileSignature + "'");

            // NOTE: Currently most of this info is not used
            ushort fileSourceID = reader.ReadUInt16();
            ushort globalEncoding = reader.ReadUInt16();

            ulong projectID1 = reader.ReadUInt32(); // optional?
            ushort projectID2 = reader.ReadUInt16(); // optional?
            ushort projectID3 = reader.ReadUInt16(); // optional?
            string projectID4 = new string(reader.ReadChars(8)); // optional?

            byte versionMajor = reader.ReadByte();
            byte versionMinor = reader.ReadByte();

            string systemIdentifier = new string(reader.ReadChars(32));
            string generatingSoftware = new string(reader.ReadChars(32));

            ushort fileCreationDayOfYear = reader.ReadUInt16();
            ushort fileCreationYear = reader.ReadUInt16();
            ushort headerSize = reader.ReadUInt16();

            ulong offsetToPointData = reader.ReadUInt32();

            ulong numberOfVariableLengthRecords = reader.ReadUInt32();

            byte pointDataRecordFormat = reader.ReadByte();

            ushort PointDataRecordLength = reader.ReadUInt16();

            ulong legacyNumberOfPointRecords = reader.ReadUInt32();

            ulong[] legacyNumberOfPointsByReturn = new ulong[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };

            double xScaleFactor = reader.ReadDouble();
            double yScaleFactor = reader.ReadDouble();
            double zScaleFactor = reader.ReadDouble();

            double xOffset = reader.ReadDouble();
            double yOffset = reader.ReadDouble();
            double zOffset = reader.ReadDouble();
            double maxX = reader.ReadDouble();
            double minX = reader.ReadDouble();
            double MaxY = reader.ReadDouble();
            double minY = reader.ReadDouble();
            double maxZ = reader.ReadDouble();
            double minZ = reader.ReadDouble();

            // Only for 1.4
            if (versionMajor == 1 && versionMinor == 4)
            {
                ulong startOfFirstExtentedVariableLengthRecord = reader.ReadUInt64();
                ulong numberOfExtentedVariableLengthRecords = reader.ReadUInt32();

                ulong numberOfPointRecords = reader.ReadUInt64();
                ulong[] numberOfPointsByReturn = new ulong[] {reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),
                    reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),
                    reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64()};
            }

            //ulong numberOfPointRecords = reader.ReadUInt64();
            // VariableLengthRecords
            if (numberOfVariableLengthRecords > 0)
            {
                ushort vlrReserved = reader.ReadUInt16();
                string vlrUserID = new string(reader.ReadChars(16));
                ushort vlrRecordID = reader.ReadUInt16();
                ushort vlrRecordLengthAfterHeader = reader.ReadUInt16();
                string vlrDescription = new string(reader.ReadChars(32));
                /*
				Debug.Log("vlrReserved:"+vlrReserved);
				Debug.Log("vlrUserID:"+vlrUserID);
				Debug.Log("vlrRecordID:"+vlrRecordID);
				Debug.Log("vlrRecordLengthAfterHeader:"+vlrRecordLengthAfterHeader);
				Debug.Log("vlrDescription:"+vlrDescription);*/
            }

            // jump to points start pos
            reader.BaseStream.Seek((long)offsetToPointData, SeekOrigin.Begin);

            // format #2
            if (pointDataRecordFormat != 2 && pointDataRecordFormat != 3) Debug.LogWarning("LAS Import might fail - only pointDataRecordFormat #2 & #3 are supported (Your file is " + pointDataRecordFormat + ")");
            if (versionMinor != 2) Debug.LogWarning("LAS Import might fail - only version LAS 1.2 is supported. (Your file is " + versionMajor + "." + versionMinor + ")");

            masterPointCount = (int)legacyNumberOfPointRecords;

            // scaling enabled, scale manual offset
            //if (useScaling) manualOffset *= scaleValue;

            // progressbar
            float progress = 0;
            long progressCounter = 0;
            EditorUtility.ClearProgressBar();

            int rowCount = 0;
            bool haveMoreToRead = true;
            bool firstPointRead = false;

            //int pointCounter = 0; // array index
            //Vector3[] vertices = new Vector3[MaxVertexCountPerMesh];
            //Vector2[] uvs = new Vector2[vertCount];
            //int[] triangles = new int[MaxVertexCountPerMesh];
            //Color[] colors = new Color[MaxVertexCountPerMesh];
            //Vector3[] normals = new Vector3[MaxVertexCountPerMesh];

            Debug.Log("LAS Reading " + masterPointCount + " points..");

            try
            {
                vertexArray = new Vector3[masterPointCount];
                indexArray = new int[masterPointCount];
                if (readRGB == true) colorArray = new Color[masterPointCount];
                if (readNormals == true) normalArray = new Vector3[masterPointCount];
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("Cannot create point array - probably your file has too much data..(points: " + masterPointCount + ")");
                return;
            }

            // process all points
            while (haveMoreToRead == true)
            {
                if (progressCounter > 256000)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Processing points: " + progress + " / " + masterPointCount, progress / masterPointCount))
                    {
                        Debug.Log("Cancelled at: " + progress);
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    progressCounter = 0;
                }

                progressCounter++;
                progress++;

                long intX = reader.ReadInt32();
                long intY = reader.ReadInt32();
                long intZ = reader.ReadInt32();

                reader.ReadBytes(8); // unknown

                if (pointDataRecordFormat == 3) reader.ReadBytes(8); // GPS Time for format#3

                var colorR = reader.ReadBytes(2); // RED
                var colorG = reader.ReadBytes(2); // GREEN
                var colorB = reader.ReadBytes(2); // BLUE

                x = intX * xScaleFactor + xOffset;
                y = intY * yScaleFactor + yOffset;
                z = intZ * zScaleFactor + zOffset;

                if (autoOffsetNearZero == true)
                {
                    if (firstPointRead == false)
                    {
                        autoOffset = -new Vector3((float)x, (float)y, (float)z);
                        firstPointRead = true;
                    }

                    x += autoOffset.x;
                    y += autoOffset.y;
                    z += autoOffset.z;
                }

                if (flipYZ == true)
                {
                    double yy = y;
                    y = z;
                    z = yy;
                }

                // manual scaling enabled
                if (useScaling == true)
                {
                    x *= scaleValue;
                    y *= scaleValue;
                    z *= scaleValue;
                }

                if (useManualOffset == true)
                {
                    x += manualOffset.x;
                    y += manualOffset.y;
                    z += manualOffset.z;
                }

                //vertices[pointCounter] = new Vector3((float)x, (float)y, (float)z);

                if (readRGB == true)
                {
                    r = System.BitConverter.ToUInt16(colorR, 0);
                    g = System.BitConverter.ToUInt16(colorG, 0);
                    b = System.BitConverter.ToUInt16(colorB, 0);

                    float rr = (float)r / 255f;
                    float gg = (float)g / 255f;
                    float bb = (float)b / 255f;

                    if (rr > 1f) rr /= 255f;
                    if (gg > 1f) gg /= 255f;
                    if (bb > 1f) bb /= 255f;

                    colorArray[rowCount] = new Color(rr, gg, bb, 1);

                    //if (rowCount < 200) Debug.Log("row:" + (rowCount + 1) + " xyz:" + x + "," + y + "," + z + " : " + rr + "," + gg + "," + bb + " pointcounter: " + rowCount);
                }

                // get cloud corners
                minCorner.x = Mathf.Min((float)x, minCorner.x);
                minCorner.y = Mathf.Min((float)y, minCorner.y);
                minCorner.z = Mathf.Min((float)z, minCorner.z);

                maxCorner.x = Mathf.Max((float)x, maxCorner.x);
                maxCorner.y = Mathf.Max((float)y, maxCorner.y);
                maxCorner.z = Mathf.Max((float)z, maxCorner.z);

                vertexArray[rowCount].Set((float)x, (float)y, (float)z);
                indexArray[rowCount] = rowCount % MaxVertexCountPerMesh;

                rowCount++;

                if (reader.BaseStream.Position >= reader.BaseStream.Length || rowCount >= masterPointCount)
                {
                    haveMoreToRead = false;
                }
            } // while loop reading file

            reader.Close();

            // build mesh asset files, arrays per mesh
            int indexCount = 0;
            Vector3[] vertices2 = new Vector3[MaxVertexCountPerMesh];
            //Vector2[] uvs2 = new Vector2[vertCount];
            int[] triangles2 = new int[MaxVertexCountPerMesh];
            Color[] colors2 = new Color[MaxVertexCountPerMesh];
            Vector3[] normals2 = new Vector3[MaxVertexCountPerMesh];
            //			Debug.Log("Total points: "+pointArray.Length);

            EditorUtility.DisplayProgressBar(appName, "Creating " + ((vertexArray.Length / MaxVertexCountPerMesh)) + " mesh arrays", 0.5f);

            for (int i = 0; i < vertexArray.Length; i++)
            {
                vertices2[indexCount] = vertexArray[i];
                triangles2[indexCount] = indexArray[i];
                if (readRGB == true) colors2[indexCount] = colorArray[i];

                if (decimatePoints == true)
                {
                    if (i % removeEveryNth == 0) indexCount++;
                }
                else
                {
                    indexCount++;
                }

                // reach max vertcount
                if (indexCount >= MaxVertexCountPerMesh || i == vertexArray.Length - 1)
                {
                    var go = BuildMesh(vertices2, triangles2, colors2, normals2);

                    if (addMeshesToScene == true && createLODS == true && go != null) BuildLODS(go, vertices2, triangles2, colors2, normals2);

                    // clear old data
                    System.Array.Clear(vertices2, 0, MaxVertexCountPerMesh);
                    System.Array.Clear(triangles2, 0, MaxVertexCountPerMesh);
                    if (readRGB == true || readIntensity == true) System.Array.Clear(colors2, 0, MaxVertexCountPerMesh);
                    if (readNormals == true) System.Array.Clear(normals2, 0, MaxVertexCountPerMesh);

                    indexCount = 0;
                }
            }

            EditorUtility.ClearProgressBar();

            // save meshes

            EditorUtility.DisplayProgressBar(appName, "Saving " + (cloudList.Count) + " mesh assets", 0.75f);


            string pad = "";

            // if no meshes in scene, or using lods, then save simple meshes for now
            if (addMeshesToScene == false || createLODS == true)
            {
                // TODO instantiate temporary gameobject, save that, destroy
                for (int i = 0; i < cloudList.Count; i++)
                {
                    if (i < 1000) pad = "0";
                    if (i < 100) pad = "00";
                    if (i < 10) pad = "000";
                    AssetDatabase.CreateAsset(cloudList[i], savePath + "_" + pcCounter + "_" + pad + i + ".asset");
                    AssetDatabase.SaveAssets(); // not needed?
                } // save meshes
            }
            else // they are in scene
            {
                for (int i = 0; i < folder.transform.childCount; i++)
                {
                    if (i < 1000) pad = "0";
                    if (i < 100) pad = "00";
                    if (i < 10) pad = "000";

                    string save = savePath + "_" + pcCounter + "_" + pad + i + ".prefab";

                    // save mesh inside prefab http://answers.unity.com/answers/1464552/view.html
#if UNITY_2018_4_OR_NEWER
                    var rootPrefab = PrefabUtility.SaveAsPrefabAsset(folder.transform.GetChild(i).gameObject, save);
#else
                    var rootPrefab = PrefabUtility.CreatePrefab(save, folder.transform.GetChild(i).gameObject);
#endif
                    var mf = rootPrefab.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        mf.sharedMesh = cloudList[i];
                        AssetDatabase.AddObjectToAsset(cloudList[i], rootPrefab);
                    }
                    AssetDatabase.SaveAssets();
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }


        // helper functions
        GameObject BuildMesh(Vector3[] verts, int[] tris, Color[] colors, Vector3[] normals, Bounds? bounds = null)
        {
            GameObject targetGO = new GameObject();

            var mf = targetGO.AddComponent<MeshFilter>();
            var mr = targetGO.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
#if UNITY_2017_3_OR_NEWER
            mesh.indexFormat = IndexFormat.UInt32;
#endif
            // main mesh gameobject
            targetGO.isStatic = true;

            mf.mesh = mesh;
            targetGO.transform.name = "PC_" + meshCounter;
            mr.sharedMaterial = meshMaterial;
            mr.receiveShadows = false; // NOTE have to enable shadows in mesh, if needed
            mr.shadowCastingMode = ShadowCastingMode.Off;
#if UNITY_5_6_OR_NEWER
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#else
            mr.lightProbeUsage = LightProbeUsage.Off;
#endif
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            // disable ligtmap static (causes warning spam)
#if UNITY_2019_2_OR_NEWER
            GameObjectUtility.SetStaticEditorFlags(targetGO, ~StaticEditorFlags.ContributeGI);
#else
            GameObjectUtility.SetStaticEditorFlags(targetGO, ~StaticEditorFlags.LightmapStatic & ~StaticEditorFlags.ReflectionProbeStatic);
#endif
            // fix offset for pivot
            if (bounds != null)
            {
                float centerX = ((Bounds)bounds).center.x;
                float centerY = ((Bounds)bounds).center.y;
                float centerZ = ((Bounds)bounds).center.z;

                // move to center of bounds, TODO no need if we move lod root..?
                targetGO.transform.position = ((Bounds)bounds).center;

                // then need to offset vertices

                for (int i = 0, len = verts.Length; i < len; i++)
                {
                    verts[i].x -= centerX;
                    verts[i].y -= centerY;
                    verts[i].z -= centerZ;
                }
                //PointCloudHelpers.PointCloudTools.DrawBounds((Bounds)bounds, 20);
            }

            GameObject lodRoot = null;
            if (createLODS == true)
            {
                lodRoot = new GameObject();

                // fix root position
                lodRoot.transform.position = ((Bounds)bounds).center;

                lodRoot.transform.parent = folder.transform;
                lodRoot.name = "lod_" + meshCounter;
                // move main mesh under lodroot
                //targetGO.transform.parent = lodRoot.transform;
                targetGO.transform.SetParent(lodRoot.transform, true);
            }
            else // no lod
            {
                targetGO.transform.parent = folder.transform;



            }

            mesh.vertices = verts;
            //mesh.uv = uvs;
            if (readRGB == true || readIntensity == true) mesh.colors = colors;
            if (readNormals == true) mesh.normals = normals;

            // TODO: use scanner centerpoint and calculate direction from that..not really accurate
            //if (forceRecalculateNormals) ...

            mesh.SetIndices(tris, MeshTopology.Points, 0);
            mesh.RecalculateBounds();

            if (bounds != null)
            {
                //mesh.bounds = (Bounds)bounds;
                //PointCloudHelpers.PointCloudTools.DrawBounds((Bounds)bounds, 12);

            }

            //PointCloudHelpers.PointCloudTools.DrawBounds(mr.bounds, 12);

            cloudList.Add(mesh);
            meshCounter++;

            // FIXME: temporary workaround to not add objects into scene..
            if (addMeshesToScene == false) DestroyImmediate(targetGO);

            return targetGO;
        }

        void BuildLODS(GameObject mainGO, Vector3[] verts, int[] tris, Color[] colors, Vector3[] normals, Bounds? bounds = null)
        {
            GameObject go;
            LODGroup lodGroup;

            lodGroup = mainGO.transform.parent.gameObject.AddComponent<LODGroup>();
            LOD[] lods = new LOD[lodLevels];

            float lerpStep = 1f / (float)(lodLevels - 1);
            float lerpVal = 1f;

            float centerX = 0;
            float centerY = 0;
            float centerZ = 0;

            // make LODS
            for (int i = 0; i < lodLevels; i++)
            {
                if (i == 0) // main mesh
                {
                    go = mainGO;
                }
                else // create lod meshes
                {
                    go = new GameObject();

                    // fix offset for pivot
                    if (bounds != null)
                    {
                        //go.transform.position = ((Bounds)bounds).center;
                        go.transform.position = mainGO.transform.position;
                    }

                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();

                    // create new mesh
                    Mesh mesh = new Mesh();
                    mesh.Clear();
                    mf.mesh = mesh;
#if UNITY_2017_3_OR_NEWER
                    mesh.indexFormat = verts.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
#endif
                    // main gameobject
                    mainGO.isStatic = true;
                    go.transform.name = "PC_" + meshCounter + "_" + i.ToString();
                    mr.sharedMaterial = meshMaterial;
                    mr.receiveShadows = false;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

#if UNITY_5_6_OR_NEWER
                    mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#else
                    mr.lightProbeUsage = LightProbeUsage.Off;
#endif
                    mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

#if UNITY_2019_2_OR_NEWER
                    GameObjectUtility.SetStaticEditorFlags(mainGO, ~StaticEditorFlags.ContributeGI);
                    GameObjectUtility.SetStaticEditorFlags(go, ~StaticEditorFlags.ContributeGI);
#else
                    GameObjectUtility.SetStaticEditorFlags(mainGO, ~StaticEditorFlags.LightmapStatic & ~StaticEditorFlags.ReflectionProbeStatic);
                    GameObjectUtility.SetStaticEditorFlags(go, ~StaticEditorFlags.LightmapStatic & ~StaticEditorFlags.ReflectionProbeStatic);
#endif

                    lerpVal -= lerpStep;
                    int newVertCount = (int)Mathf.Lerp(minLodVertexCount, decimatePoints ? MaxVertexCountPerMesh / removeEveryNth : MaxVertexCountPerMesh, lerpVal);

                    var newVerts = new Vector3[newVertCount];
                    //var newUvs = new Vector2[newVertCount];
                    var newColors = new Color[newVertCount];
                    var newNormals = new Vector3[newVertCount];
                    var newTris = new int[newVertCount];

                    // get new verts
                    float oldIndex = 0;
                    float stepSize = MaxVertexCountPerMesh / (float)newVertCount;

                    // TODO: if rounds to same index, take next instead of same point?
                    float o = 0;

                    if (bounds != null)
                    {
                        // NOTE bounds are in local space
                        centerX = ((Bounds)bounds).center.x;
                        centerY = ((Bounds)bounds).center.y;
                        centerZ = ((Bounds)bounds).center.z;
                    }

                    for (int newIndex = 0; newIndex < newVertCount; newIndex++)
                    {
                        newVerts[newIndex] = verts[Mathf.FloorToInt(o)];

                        //newVerts[newIndex].x -= centerX;
                        //newVerts[newIndex].y -= centerY;
                        //newVerts[newIndex].z -= centerZ;

                        newTris[newIndex] = newIndex; //tris[newIndex];

                        if (readRGB == true) newColors[newIndex] = colors[Mathf.FloorToInt(o)];

                        /*
						// for debugging LODS, different colors per lod
						switch(i)
						{
						case 1:
							newColors[newIndex] = Color.red;
							break;
						case 2:
							newColors[newIndex] = Color.green;
							break;
						case 3:
							newColors[newIndex] = Color.yellow;
							break;
						case 4:
							newColors[newIndex] = Color.cyan;
							break;
						default:
							newColors[newIndex] = Color.magenta;
							break;
						}*/

                        if (readNormals == true) newNormals[newIndex] = normals[Mathf.FloorToInt(o)];
                        o += stepSize;
                        // exit if used all vertices
                        if (Mathf.FloorToInt(o) >= verts.Length)
                        {
                            break;
                        }
                    }

                    mesh.vertices = newVerts;
                    //mesh.uv = newUvs;
                    if (readRGB == true || readIntensity == true) mesh.colors = newColors;
                    if (readNormals == true) mesh.normals = newNormals;
                    mesh.SetIndices(newTris, MeshTopology.Points, 0);
                    if (bounds != null)
                    {
                        // NOTE bounds is in local space!!
                        //PointCloudHelpers.PointCloudTools.DrawBounds((Bounds)bounds, 20);
                        //mesh.bounds = (Bounds)bounds;
                    }
                    mesh.RecalculateBounds();
                    //PointCloudHelpers.PointCloudTools.DrawBounds(mesh.bounds, 20);

                } // if main mesh or lod meshes

                go.transform.parent = mainGO.transform.parent;
                Renderer[] renderers = new Renderer[1];
                renderers[0] = go.GetComponent<Renderer>();
                float LODVal = Mathf.Lerp(1f, 0.1f, (i + 1) / (float)lodLevels);
                //Debug.Log("lodval="+LODVal+" "+go.transform.name);
                lods[i] = new LOD(LODVal, renderers);

            }// for create lods

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();


        } //BuildLODS



        string GetSelectedFileInfo()
        {
            string tempFilePath = AssetDatabase.GetAssetPath(sourceFile);

            if (Directory.Exists(tempFilePath))
            {
                return "[ Folder ]";
            }
            else
            {
                string tempFileName = Path.GetFileName(tempFilePath);
                return tempFileName + " (" + (new FileInfo(tempFilePath).Length / 1000000) + "MB)";
            }
        }

        bool ValidateSaveAndRead(string path, string fileToRead)
        {
            if (path.Length < 1) { Debug.Log(appName + "> Save cancelled.."); return false; }
            if (fileToRead.Length < 1) { Debug.LogError(appName + "> Cannot find file (" + fileToRead + ")"); return false; }
            if (!File.Exists(fileToRead)) { Debug.LogError(appName + "> Cannot find file (" + fileToRead + ")"); return false; }

            if (Path.GetExtension(fileToRead).ToLower() == ".bin") { Debug.LogError("Source file extension is .bin, binary file conversion is not supported"); return false; }

            return true;
        }

        float Remap(float source, float sourceFrom, float sourceTo, float targetFrom, float targetTo)
        {
            return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
        }


        // http://anh.cs.luc.edu/170/notes/CSharpHtml/sorting.html
        public static void ArrayQuickSort(ref Vector3[] _vertexArray, ref Color[] _colorArray, ref int[] _indexArray, int l, int r)
        {
            int i, j;
            float x;

            i = l;
            j = r;

            x = _vertexArray[(l + r) / 2].x; /* find pivot item */

            while (true)
            {
                while (_vertexArray[i].x < x)
                    i++;
                while (x < _vertexArray[j].x)
                    j--;
                if (i <= j)
                {
                    var tempVector = _vertexArray[i];
                    _vertexArray[i] = _vertexArray[j];
                    _vertexArray[j] = tempVector;

                    /*
                    var tempInt = _indexArray[i]; // old msg: bugged!! its different size?
                    _indexArray[i] = _indexArray[j];
                    _indexArray[j] = tempInt;                   
                    */

                    // sort other arrays also, with same index
                    if (_colorArray != null)
                    {
                        var tempColor = _colorArray[i];
                        _colorArray[i] = _colorArray[j];
                        _colorArray[j] = tempColor;
                    }

                    // TODO: index, UV(not used), normal

                    i++;
                    j--;
                }
                if (i > j)
                    break;
            }
            if (l < j)
                ArrayQuickSort(ref _vertexArray, ref _colorArray, ref _indexArray, l, j);
            if (i < r)
                ArrayQuickSort(ref _vertexArray, ref _colorArray, ref _indexArray, i, r);
        }


        private void OnEnable()
        {
            LoadPreferences();
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        void SavePreferences()
        {
            // save settings on exit
            EditorPrefs.SetBool(prefsPrefix + "readRGB", readRGB);
            EditorPrefs.SetBool(prefsPrefix + "readIntensity", readIntensity);
            EditorPrefs.SetBool(prefsPrefix + "readNormals", readNormals);
            EditorPrefs.SetBool(prefsPrefix + "useScaling", useScaling);
            EditorPrefs.SetFloat(prefsPrefix + "scaleValue", scaleValue);
            EditorPrefs.SetBool(prefsPrefix + "flipYZ", flipYZ);
            EditorPrefs.SetBool(prefsPrefix + "autoOffsetNearZero", autoOffsetNearZero);
            EditorPrefs.SetBool(prefsPrefix + "useManualOffset", useManualOffset);
            EditorPrefs.SetFloat(prefsPrefix + "manualOffset.x", manualOffset.x);
            EditorPrefs.SetFloat(prefsPrefix + "manualOffset.y", manualOffset.y);
            EditorPrefs.SetFloat(prefsPrefix + "manualOffset.z", manualOffset.z);
            EditorPrefs.SetBool(prefsPrefix + "addMeshesToScene", addMeshesToScene);
            EditorPrefs.SetBool(prefsPrefix + "sortPoints", sortPoints);
            EditorPrefs.SetBool(prefsPrefix + "splitToGrid", splitToGrid);
            EditorPrefs.SetBool(prefsPrefix + "createLODS", createLODS);
            EditorPrefs.SetInt(prefsPrefix + "lodLevels", lodLevels);
            EditorPrefs.SetBool(prefsPrefix + "decimatePoints", decimatePoints);
            EditorPrefs.SetInt(prefsPrefix + "removeEveryNth", removeEveryNth);
            EditorPrefs.SetInt(prefsPrefix + "MaxVertexCountPerMesh", MaxVertexCountPerMesh);
            EditorPrefs.SetString(prefsPrefix + "meshMaterial.name", meshMaterial == null ? "" : meshMaterial.name);
        }

        void LoadPreferences()
        {
            // read last used preferences
            readRGB = EditorPrefs.GetBool(prefsPrefix + "readRGB", readRGB);
            readIntensity = EditorPrefs.GetBool(prefsPrefix + "readIntensity", readIntensity);
            readNormals = EditorPrefs.GetBool(prefsPrefix + "readNormals", readNormals);
            useScaling = EditorPrefs.GetBool(prefsPrefix + "useScaling", useScaling);
            scaleValue = EditorPrefs.GetFloat(prefsPrefix + "scaleValue", scaleValue);
            flipYZ = EditorPrefs.GetBool(prefsPrefix + "flipYZ", flipYZ);
            autoOffsetNearZero = EditorPrefs.GetBool(prefsPrefix + "autoOffsetNearZero", autoOffsetNearZero);
            useManualOffset = EditorPrefs.GetBool(prefsPrefix + "useManualOffset", useManualOffset);
            manualOffset.x = EditorPrefs.GetFloat(prefsPrefix + "manualOffset.x", manualOffset.x);
            manualOffset.y = EditorPrefs.GetFloat(prefsPrefix + "manualOffset.y", manualOffset.y);
            manualOffset.z = EditorPrefs.GetFloat(prefsPrefix + "manualOffset.z", manualOffset.z);
            addMeshesToScene = EditorPrefs.GetBool(prefsPrefix + "addMeshesToScene", addMeshesToScene);
            sortPoints = EditorPrefs.GetBool(prefsPrefix + "sortPoints", sortPoints);
            splitToGrid = EditorPrefs.GetBool(prefsPrefix + "splitToGrid", splitToGrid);
            createLODS = EditorPrefs.GetBool(prefsPrefix + "createLODS", createLODS);
            lodLevels = EditorPrefs.GetInt(prefsPrefix + "lodLevels", lodLevels);
            decimatePoints = EditorPrefs.GetBool(prefsPrefix + "decimatePoints", decimatePoints);
            removeEveryNth = EditorPrefs.GetInt(prefsPrefix + "removeEveryNth", removeEveryNth);
            MaxVertexCountPerMesh = EditorPrefs.GetInt(prefsPrefix + "MaxVertexCountPerMesh", MaxVertexCountPerMesh);

            var oldMatName = EditorPrefs.GetString(prefsPrefix + "meshMaterial.name", null);
            if (oldMatName != null)
            {
                var foundedMats = AssetDatabase.FindAssets(oldMatName);
                if (foundedMats != null && foundedMats.Length > 0)
                {
                    var foundedMatPath = AssetDatabase.GUIDToAssetPath(foundedMats[0]);
                    if (string.IsNullOrEmpty(foundedMatPath) == false)
                    {
                        meshMaterial = (Material)AssetDatabase.LoadAssetAtPath(foundedMatPath, typeof(Material));
                    }
                }
            }
        }

        float[] LUT255 = new float[] { 0f, 0.00392156862745098f, 0.00784313725490196f, 0.011764705882352941f, 0.01568627450980392f, 0.0196078431372549f, 0.023529411764705882f, 0.027450980392156862f, 0.03137254901960784f, 0.03529411764705882f, 0.0392156862745098f, 0.043137254901960784f, 0.047058823529411764f, 0.050980392156862744f, 0.054901960784313725f, 0.058823529411764705f, 0.06274509803921569f, 0.06666666666666667f, 0.07058823529411765f, 0.07450980392156863f, 0.0784313725490196f, 0.08235294117647059f, 0.08627450980392157f, 0.09019607843137255f, 0.09411764705882353f, 0.09803921568627451f, 0.10196078431372549f, 0.10588235294117647f, 0.10980392156862745f, 0.11372549019607843f, 0.11764705882352941f, 0.12156862745098039f, 0.12549019607843137f, 0.12941176470588237f, 0.13333333333333333f, 0.13725490196078433f, 0.1411764705882353f, 0.1450980392156863f, 0.14901960784313725f, 0.15294117647058825f, 0.1568627450980392f, 0.1607843137254902f, 0.16470588235294117f, 0.16862745098039217f, 0.17254901960784313f, 0.17647058823529413f, 0.1803921568627451f, 0.1843137254901961f, 0.18823529411764706f, 0.19215686274509805f, 0.19607843137254902f, 0.2f, 0.20392156862745098f, 0.20784313725490197f, 0.21176470588235294f, 0.21568627450980393f, 0.2196078431372549f, 0.2235294117647059f, 0.22745098039215686f, 0.23137254901960785f, 0.23529411764705882f, 0.23921568627450981f, 0.24313725490196078f, 0.24705882352941178f, 0.25098039215686274f, 0.2549019607843137f, 0.25882352941176473f, 0.2627450980392157f, 0.26666666666666666f, 0.27058823529411763f, 0.27450980392156865f, 0.2784313725490196f, 0.2823529411764706f, 0.28627450980392155f, 0.2901960784313726f, 0.29411764705882354f, 0.2980392156862745f, 0.30196078431372547f, 0.3058823529411765f, 0.30980392156862746f, 0.3137254901960784f, 0.3176470588235294f, 0.3215686274509804f, 0.3254901960784314f, 0.32941176470588235f, 0.3333333333333333f, 0.33725490196078434f, 0.3411764705882353f, 0.34509803921568627f, 0.34901960784313724f, 0.35294117647058826f, 0.3568627450980392f, 0.3607843137254902f, 0.36470588235294116f, 0.3686274509803922f, 0.37254901960784315f, 0.3764705882352941f, 0.3803921568627451f, 0.3843137254901961f, 0.38823529411764707f, 0.39215686274509803f, 0.396078431372549f, 0.4f, 0.403921568627451f, 0.40784313725490196f, 0.4117647058823529f, 0.41568627450980394f, 0.4196078431372549f, 0.4235294117647059f, 0.42745098039215684f, 0.43137254901960786f, 0.43529411764705883f, 0.4392156862745098f, 0.44313725490196076f, 0.4470588235294118f, 0.45098039215686275f, 0.4549019607843137f, 0.4588235294117647f, 0.4627450980392157f, 0.4666666666666667f, 0.47058823529411764f, 0.4745098039215686f, 0.47843137254901963f, 0.4823529411764706f, 0.48627450980392156f, 0.49019607843137253f, 0.49411764705882355f, 0.4980392156862745f, 0.5019607843137255f, 0.5058823529411764f, 0.5098039215686274f, 0.5137254901960784f, 0.5176470588235295f, 0.5215686274509804f, 0.5254901960784314f, 0.5294117647058824f, 0.5333333333333333f, 0.5372549019607843f, 0.5411764705882353f, 0.5450980392156862f, 0.5490196078431373f, 0.5529411764705883f, 0.5568627450980392f, 0.5607843137254902f, 0.5647058823529412f, 0.5686274509803921f, 0.5725490196078431f, 0.5764705882352941f, 0.5803921568627451f, 0.5843137254901961f, 0.5882352941176471f, 0.592156862745098f, 0.596078431372549f, 0.6f, 0.6039215686274509f, 0.6078431372549019f, 0.611764705882353f, 0.615686274509804f, 0.6196078431372549f, 0.6235294117647059f, 0.6274509803921569f, 0.6313725490196078f, 0.6352941176470588f, 0.6392156862745098f, 0.6431372549019608f, 0.6470588235294118f, 0.6509803921568628f, 0.6549019607843137f, 0.6588235294117647f, 0.6627450980392157f, 0.6666666666666666f, 0.6705882352941176f, 0.6745098039215687f, 0.6784313725490196f, 0.6823529411764706f, 0.6862745098039216f, 0.6901960784313725f, 0.6941176470588235f, 0.6980392156862745f, 0.7019607843137254f, 0.7058823529411765f, 0.7098039215686275f, 0.7137254901960784f, 0.7176470588235294f, 0.7215686274509804f, 0.7254901960784313f, 0.7294117647058823f, 0.7333333333333333f, 0.7372549019607844f, 0.7411764705882353f, 0.7450980392156863f, 0.7490196078431373f, 0.7529411764705882f, 0.7568627450980392f, 0.7607843137254902f, 0.7647058823529411f, 0.7686274509803922f, 0.7725490196078432f, 0.7764705882352941f, 0.7803921568627451f, 0.7843137254901961f, 0.788235294117647f, 0.792156862745098f, 0.796078431372549f, 0.8f, 0.803921568627451f, 0.807843137254902f, 0.8117647058823529f, 0.8156862745098039f, 0.8196078431372549f, 0.8235294117647058f, 0.8274509803921568f, 0.8313725490196079f, 0.8352941176470589f, 0.8392156862745098f, 0.8431372549019608f, 0.8470588235294118f, 0.8509803921568627f, 0.8549019607843137f, 0.8588235294117647f, 0.8627450980392157f, 0.8666666666666667f, 0.8705882352941177f, 0.8745098039215686f, 0.8784313725490196f, 0.8823529411764706f, 0.8862745098039215f, 0.8901960784313725f, 0.8941176470588236f, 0.8980392156862745f, 0.9019607843137255f, 0.9058823529411765f, 0.9098039215686274f, 0.9137254901960784f, 0.9176470588235294f, 0.9215686274509803f, 0.9254901960784314f, 0.9294117647058824f, 0.9333333333333333f, 0.9372549019607843f, 0.9411764705882353f, 0.9450980392156862f, 0.9490196078431372f, 0.9529411764705882f, 0.9568627450980393f, 0.9607843137254902f, 0.9647058823529412f, 0.9686274509803922f, 0.9725490196078431f, 0.9764705882352941f, 0.9803921568627451f, 0.984313725490196f, 0.9882352941176471f, 0.9921568627450981f, 0.996078431372549f, 1f };

    } // class
} // namespace
