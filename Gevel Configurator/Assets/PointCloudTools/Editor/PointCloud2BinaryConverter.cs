// Point Cloud to Binary Converter
// Saves pointcloud data into custom binary file, for faster viewing
// http://unitycoder.com

#pragma warning disable 0219 // disable unused var warnings (mostly in LAS converter)

using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
using unitycodercom_PointCloudHelpers;
using System.Text;
using PointCloudHelpers;
using System.Collections.Generic;

namespace unitycodercom_PointCloud2Binary
{

    public class PointCloud2BinaryConverter : EditorWindow
    {
        private static string appName = "PointCloud2Binary";
        private Object sourceFile;
        //                                             0      1         2      3      4            5              6      7      8
        private string[] fileFormats = new string[] { "XYZ", "XYZRGB", "CGO", "ASC", "CATIA ASC", "PLY (ASCII)", "LAS", "PTS", "PCD (ASCII)" };
        private string[] fileFormatInfo = new string[]{"sample: \"32.956900 5.632800 5.673400\"", // XYZ
			"sample: \"32.956900 5.632800 5.670000 128 190 232\"", // XYZRGB
			"sample: \"683,099976 880,200012 5544,700195\"", // CGO
			"sample: \" -1192.9 2643.6 5481.2\"", // ASC
			"sample: \"X 31022.1919 Y -3314.1098 Z 6152.5000\"", //  CATIA ASC
			"sample: \"-0.680891 -90.6809 0 204 204 204 255\"", // PLY (ASCII)
			"info: LAS 1.4", // LAS
			"info: 42.72464 -16.1426 -32.16625 10 88 23 98", // PTS
            "info: 42.72464 -16.1426 -32.16625 4.2108e+06" // PCD ASCII
		};
        private int fileFormat = 0;
        private bool readRGB = false;
        private bool readIntensity = false; // only for PTS currently
        private bool intensityRange255 = false;
        private bool useScaling = true;
        private float scaleValue = 0.001f;
        private bool flipYZ = true;
        private bool autoOffsetNearZero = true; // takes first point value as offset
        private bool useManualOffset = false;
        private Vector3 manualOffset = Vector3.zero;
        private Vector3 autoOffset = Vector3.zero;
        private bool plyHasNormals = false;

        bool useBinFormatV2 = false;
        bool useBinFormatV3 = false;
        bool randomizePoints = false;
        static float gridSize = 5;
        int minimumPointCount = 1000;
        //static bool gridPreview = false;

        static bool limitPointCount = false;
        int maxPointCount = 100000;

        const string sep = "|";

        /*
        bool randomizeArray = false;
        bool useOptimizedBinFormat = false;
        bool packColors = false;
        */

        private long masterPointCount = 0;
        //		private bool compressed=false;

        private byte binaryVersion = 1;

        readonly static string prefsPrefix = "unitycoder_" + appName + "_";

        // create menu item and window
        [MenuItem("Window/PointCloudTools/Convert Point Cloud To Binary (DX11)", false, 1)]
        static void Init()
        {
            PointCloud2BinaryConverter window = (PointCloud2BinaryConverter)EditorWindow.GetWindow(typeof(PointCloud2BinaryConverter));
            window.titleContent = new GUIContent(appName);
            window.minSize = new Vector2(380, 544);
            window.maxSize = new Vector2(380, 548);
            //SceneView.onSceneGUIDelegate += OnSceneUpdate;
        }

        /*
        private static void OnSceneUpdate(SceneView sceneview)
        {
            if (gridPreview)
            {
                int grids = 3;
                for (int z = 0; z < grids; z++)
                {
                    for (int y = 0; y < grids; y++)
                    {
                        for (int x = 0; x < grids; x++)
                        {
                            Debug.DrawRay(new Vector3(gridSize * x, gridSize * y, gridSize * z), Vector3.forward * gridSize, Color.red, 0);
                            Debug.DrawRay(new Vector3(gridSize * x, gridSize * y, gridSize * z), Vector3.right * gridSize, Color.green, 0);
                            Debug.DrawRay(new Vector3(gridSize * x, gridSize * y, gridSize * z), Vector3.up * gridSize, Color.blue, 0);
                        }
                    }
                }
            }
        }*/

        public void OnDestroy()
        {
            //SceneView.onSceneGUIDelegate -= OnSceneUpdate;
        }

        // main loop
        void OnGUI()
        {
            // source field
            GUILayout.Label("Point Cloud source file", EditorStyles.boldLabel);
            sourceFile = EditorGUILayout.ObjectField(sourceFile, typeof(Object), true);
            EditorGUILayout.BeginHorizontal();
            if (sourceFile != null)
            {
                GUILayout.Label("File:" + GetSelectedFileInfo(), EditorStyles.miniLabel);
                if (GUILayout.Button(new GUIContent("Show Header", "Prints few rows from the file into console (to check values)"), GUILayout.Height(16)))
                {
                    unitycodercom_ShowHeaderDataHelper.ShowHeaderDataHelper.PrintHeaderData(sourceFile);
                }
            }
            else
            {
                GUILayout.Label("", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // file format dropdown
            GUILayout.Label(new GUIContent("Input file format", "File extension can be anything, this selection decides the parsing method"));
            fileFormat = EditorGUILayout.Popup(fileFormat, fileFormats);
            GUILayout.Label(fileFormatInfo[fileFormat], EditorStyles.miniLabel);
            EditorGUILayout.Space();

            // import RGB
            GUILayout.Label("Import settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 85;
            if (fileFormat == 0) // XYZ format
            {
                GUI.enabled = false;
                readRGB = false;
                readRGB = EditorGUILayout.ToggleLeft(new GUIContent("Read RGB values", null, "Read R G B values"), readRGB);
                GUI.enabled = true;
            }
            else
            {
                readRGB = EditorGUILayout.ToggleLeft(new GUIContent("Read RGB values", null, "Read R G B values"), readRGB);
            }
            readIntensity = readRGB ? false : readIntensity;


            if (fileFormat == 7) // PTS
            {
                readIntensity = EditorGUILayout.BeginToggleGroup(new GUIContent("Read INT/REF value", null, "Read Intensity/Reflectivity value (instead of RGB)"), readIntensity);
                readRGB = readIntensity ? false : readRGB;

                intensityRange255 = EditorGUILayout.ToggleLeft(new GUIContent("0-255", null, "Is INT/REFL value in 0 to 255 or -2048 to 2047 range"), intensityRange255);
                EditorGUILayout.EndToggleGroup();
            }


            /*
            if (fileFormat == 8) // PTS, PCD
            {
                readIntensity = EditorGUILayout.ToggleLeft(new GUIContent("Read INT/REFL value", null, "Read Intensity/Reflectivity value (instead of RGB)"), readIntensity);
                readRGB = readIntensity ? false : readRGB;
            }*/
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // scaling
            EditorGUIUtility.labelWidth = 1;
            EditorGUILayout.BeginHorizontal();
            useScaling = EditorGUILayout.BeginToggleGroup(new GUIContent("Scale values", null, "If you data is in millimeters, scale with 0.001 to convert into (Unity) meters"), useScaling);
            scaleValue = EditorGUILayout.FloatField(new GUIContent("Scaling multiplier", null, "Multiply XYZ values with this multiplier"), scaleValue);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 0;

            // flip y/z
            flipYZ = EditorGUILayout.ToggleLeft(new GUIContent("Flip Y & Z values", null, "Flip YZ values because Unity Y is up"), flipYZ);
            EditorGUILayout.Space();

            // offset
            autoOffsetNearZero = EditorGUILayout.ToggleLeft(new GUIContent("Auto-offset near 0,0,0", null, "Takes first line from xyz data as an offset"), autoOffsetNearZero);
            useManualOffset = EditorGUILayout.BeginToggleGroup(new GUIContent("Add Manual Offset", null, "Add this offset to XYZ values"), useManualOffset);
            manualOffset = EditorGUILayout.Vector3Field(new GUIContent("Manual Offset", null, ""), manualOffset);
            GUILayout.Label("This value is added AFTER auto-offset and BEFORE scaling and flipping", EditorStyles.miniLabel);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            randomizePoints = EditorGUILayout.ToggleLeft(new GUIContent("Randomize points*", null, "Required for V3, optional for V1 and V2"), randomizePoints);
            //            packColors = EditorGUILayout.ToggleLeft(new GUIContent("Pack colors to position data (not working yet!)", null, "Not yet supported"), packColors);
            limitPointCount = EditorGUILayout.ToggleLeft(new GUIContent("Load Limited Amount", null, "Load only small amount of points first, to test your import settings"), limitPointCount);
            maxPointCount = (int)Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("maxPointCount", null, "Load only this many points"), maxPointCount), 1, 10000000);

            EditorGUILayout.Space();
            // new formats
            useBinFormatV2 = EditorGUILayout.ToggleLeft(new GUIContent("Use V2 .ucpc Format", null, "Save into new V2 format (loads faster than V1)"), useBinFormatV2);
            useBinFormatV3 = useBinFormatV2 ? false : useBinFormatV3;

            useBinFormatV3 = EditorGUILayout.ToggleLeft(new GUIContent("Use V3 .pcroot Format", null, "Optimized tiles format for large clouds (BETA) *For more features, use commandline LAS Converter tool"), useBinFormatV3);
            useBinFormatV2 = useBinFormatV3 ? false : useBinFormatV2;
            randomizePoints = useBinFormatV3 ? true : randomizePoints;

            GUI.enabled = useBinFormatV3;
            gridSize = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Grid Cell Size", null, "Split cloud into tiles (size x size x size)"), gridSize), 1, 1000);
            minimumPointCount = (int)Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Cell minimum point count", null, "Skip tiles with points less than this amount"), minimumPointCount), 1, 1000000);
            GUI.enabled = true;

            GUI.enabled = sourceFile == null ? false : true;
            // extras
            //compressed = EditorGUILayout.ToggleLeft(new GUIContent("Compress colors",null,"Compresses RGB into single float"), compressed);
            EditorGUILayout.Space();

            // convert button
            if (GUILayout.Button(new GUIContent("Convert to Binary", "Convert source to custom binary"), GUILayout.Height(40)))
            {
                if (useBinFormatV2)
                {
                    if (readRGB == false)
                    {
                        Debug.LogError(appName + "> V2 format requires Read RGB enabled");
                        return;
                    }
                    ConvertBinaryV2();
                }
                else
                {
                    if (useBinFormatV3)
                    {
                        if (readRGB == false)
                        {
                            Debug.LogError(appName + "> V3 format requires Read RGB enabled");
                            return;
                        }
                        ConvertBinaryV2();
                    }
                    else
                    {
                        ConvertBinaryV1();
                    }

                }
            }
            GUI.enabled = true;
        } // OnGUI()


        bool IsNullOrEmptyLine(string line)
        {
            if (line.Length < 3 || line == null || line == string.Empty) { Debug.LogError("First line of the file is empty..quitting!"); return true; }
            return false;
        }

        // conversion function
        void ConvertBinaryV1()
        {
            // TEMPORARY: Custom reader for LAS binary
            if (fileFormats[fileFormat] == "LAS")
            {
                LASConverter();
                return;
            }

            string saveFilePath = null;

            saveFilePath = EditorUtility.SaveFilePanelInProject("Output binary file (v1)", sourceFile.name + ".bin", "bin", "");
            string fileToRead = AssetDatabase.GetAssetPath(sourceFile);

            if (!ValidateSaveAndRead(saveFilePath, fileToRead)) return;

            long lines = 0;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // TODO read several lines at a time
            // get initial data (so can check if data is ok)
            using (StreamReader streamReader = new StreamReader(File.OpenRead(fileToRead)))
            //using (FileStream fs = File.Open(fileToRead, FileMode.Open, FileAccess.Read, FileShare.None))
            //using (BufferedStream streamReader = new BufferedStream(fs))
            {
                double x = 0, y = 0, z = 0;
                float r = 0, g = 0, b = 0; //,nx=0,ny=0,nz=0;
                string line = null;
                string[] row = null;

                PeekHeaderData headerCheck;
                headerCheck.x = 0; headerCheck.y = 0; headerCheck.z = 0;
                headerCheck.linesRead = 0;

                switch (fileFormats[fileFormat])
                {
                    case "ASC": // ASC (space at front)
                        {
                            headerCheck = PeekHeader.PeekHeaderASC(streamReader, readRGB);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "CGO": // CGO	(counter at first line and uses comma)
                        {
                            //                            headerCheck = PeekHeader.PeekHeaderCGO(txtReader, readRGB);
                            headerCheck = PeekHeader.PeekHeaderCGO(streamReader, ref readRGB, readIntensity, ref masterPointCount);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "CATIA ASC": // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
                        {
                            headerCheck = PeekHeader.PeekHeaderCATIA_ASC(streamReader, ref readRGB);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "XYZRGB":
                    case "XYZ": // XYZ RGB(INT)
                        {
                            headerCheck = PeekHeader.PeekHeaderXYZ(streamReader, ref readRGB);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "PTS": // PTS (INT) (RGB)
                        {
                            headerCheck = PeekHeader.PeekHeaderPTS(streamReader, readRGB, readIntensity, ref masterPointCount);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "PLY (ASCII)": // PLY (ASCII)
                        {
                            headerCheck = PeekHeader.PeekHeaderPLY(streamReader, readRGB, ref masterPointCount, ref plyHasNormals);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            readRGB = headerCheck.hasRGB;
                            //lines = headerCheck.linesRead;
                        }
                        break;

                    case "PCD (ASCII)": // PCD (ASCII)
                        {
                            headerCheck = PeekHeader.PeekHeaderPCD(streamReader, ref readRGB, ref masterPointCount);
                            if (headerCheck.readSuccess == false) { streamReader.Close(); return; }
                        }
                        break;

                    default:
                        Debug.LogError(appName + "> Unknown fileformat error (1) " + fileFormats[fileFormat]);
                        break;

                } // switch format


                if (autoOffsetNearZero == true)
                {
                    autoOffset = -new Vector3((float)headerCheck.x, (float)headerCheck.y, (float)headerCheck.z);
                    // scaling enabled, scale offset too
                    //                    if (useScaling == true) autoOffset *= scaleValue;
                }

                // progressbar
                float progress = 0;
                long progressCounter = 0;

                // get total amount of points from formats that have it
                if (fileFormats[fileFormat] == "PLY (ASCII)" || fileFormats[fileFormat] == "PTS" || fileFormats[fileFormat] == "CGO" || fileFormats[fileFormat] == "PCD (ASCII)")
                {
                    lines = masterPointCount;

                    // reset back to start of file
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    streamReader.BaseStream.Position = 0;

                    // get back to before first actual data line
                    for (int i = 0; i < headerCheck.linesRead - 1; i++)
                    {
                        streamReader.ReadLine();
                    }

                }
                else
                { // other formats need to be read completely

                    // reset back to start of file
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    streamReader.BaseStream.Position = 0;

                    // get back to first actual data line
                    for (int i = 0; i < headerCheck.linesRead; i++)
                    {
                        streamReader.ReadLine();
                    }
                    lines = 0;

                    // calculate actual point data lines
                    int splitCount = 0;
                    while (!streamReader.EndOfStream)
                    {
                        line = streamReader.ReadLine();

                        if (progressCounter > 256000)
                        {
                            EditorUtility.DisplayProgressBar(appName, "Counting lines: " + lines, lines / 50000000.0f);
                            progressCounter = 0;
                        }

                        progressCounter++;

                        if (line.Length > 9)
                        {
                            splitCount = CharCount(line, ' ');
                            if (splitCount > 2 && splitCount < 16)
                            {
                                lines++;
                            }
                        }
                    }

                    Debug.Log("Found rows: " + lines);

                    EditorUtility.ClearProgressBar();

                    // reset back to start of data
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    streamReader.BaseStream.Position = 0;

                    // now skip header lines
                    for (int i = 0; i < headerCheck.linesRead; i++)
                    {
                        streamReader.ReadLine();
                    }

                    masterPointCount = lines;
                }


                // if limited amount
                if (limitPointCount == true)
                {
                    Debug.Log("Loading only limited amount of points: " + maxPointCount + " out of " + masterPointCount);
                    masterPointCount = (long)maxPointCount;
                }

                // start saving into binary file
                var bs = new BufferedStream(new FileStream(saveFilePath, FileMode.Create));
                var writer = new BinaryWriter(bs);

                // write header
                long dataIndex = 0;
                long dataIndexRGB = 0;
                // old header
                binaryVersion = 1;
                writer.Write(binaryVersion);
                writer.Write((System.Int32)masterPointCount);
                writer.Write(readRGB | readIntensity);

                progressCounter = 0;
                int skippedRows = 0;
                long rowCount = 0;
                bool haveMoreToRead = true;

                // for testing loading times
                //                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                //                stopwatch.Start();

                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex("[ ]{2,}", options);

                //List<Vector3> tempPointList = new List<Vector3>();
                //List<Vector3> tempColorList = new List<Vector3>();
                //Bounds bounds = new Bounds();

                // process all points, one line at a time
                while (haveMoreToRead == true)
                {
                    if (progressCounter > 256000)
                    {
                        //EditorUtility.DisplayProgressBaxr(appName, "Converting pointcloud to V1 .bin: " + rowCount + " / " + lines, rowCount / (float)lines);
                        if (EditorUtility.DisplayCancelableProgressBar(appName, "Converting pointcloud to V1 .bin: " + rowCount + " / " + lines, rowCount / (float)lines))
                        {
                            Debug.LogWarning("Operation cancelled!");
                            EditorUtility.ClearProgressBar();
                            return;
                        }
                        progressCounter = 0;
                    }

                    progressCounter++;

                    line = streamReader.ReadLine();

                    if (line != null && line.Length > 9)
                    {
                        // trim duplicate spaces
                        line = line.Replace("   ", " ").Replace("  ", " ").Trim();
                        row = line.Split(' ');

                        if (row.Length > 2)
                        {
                            switch (fileFormats[fileFormat])
                            {
                                case "ASC": // ASC
                                    if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                    {
                                        skippedRows++;
                                        continue;
                                    }
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);
                                    if (readRGB == true)
                                    {
                                        r = LUT255[int.Parse(row[3])];
                                        g = LUT255[int.Parse(row[4])];
                                        b = LUT255[int.Parse(row[5])];
                                    }
                                    break;

                                case "CGO": // CGO	(counter at first line and uses comma)
                                    if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                    {
                                        skippedRows++;
                                        continue;
                                    }
                                    x = double.Parse(row[0].Replace(",", "."));
                                    y = double.Parse(row[1].Replace(",", "."));
                                    z = double.Parse(row[2].Replace(",", "."));
                                    break;

                                case "CATIA ASC": // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
                                    if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                    {
                                        skippedRows++;
                                        continue;
                                    }
                                    x = double.Parse(row[1]);
                                    y = double.Parse(row[3]);
                                    z = double.Parse(row[5]);
                                    break;

                                case "XYZRGB":
                                case "XYZ": // XYZ RGB(INT)
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);
                                    if (readRGB == true)
                                    {
                                        r = LUT255[int.Parse(row[3])];
                                        g = LUT255[int.Parse(row[4])];
                                        b = LUT255[int.Parse(row[5])];
                                    }
                                    break;

                                case "PTS": // PTS (INT) (RGB)
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);

                                    if (readRGB == true)
                                    {
                                        if (row.Length == 7) // XYZIRGB (skip i or reflectance)
                                        {
                                            r = LUT255[int.Parse(row[4])];
                                            g = LUT255[int.Parse(row[5])];
                                            b = LUT255[int.Parse(row[6])];
                                        }
                                        else if (row.Length == 6) // XYZRGB
                                        {
                                            r = LUT255[int.Parse(row[3])];
                                            g = LUT255[int.Parse(row[4])];
                                            b = LUT255[int.Parse(row[5])];
                                        }
                                    }
                                    else if (readIntensity == true)
                                    {
                                        if (row.Length == 4 || row.Length == 7) // XYZI or XYZIRGB *FIXME: this is wrong, recap pts has XYZ REFLECTANCE RGB
                                        {
                                            // pts intensity -2048 to 2047 ?
                                            if (intensityRange255 == true)
                                            {
                                                r = LUT255[int.Parse(row[3])];
                                            }
                                            else
                                            {
                                                r = Remap(float.Parse(row[3]), -2048, 2047, 0, 1);
                                            }
                                            g = r;
                                            b = r;
                                        }
                                    }
                                    break;

                                case "PLY (ASCII)": // PLY (ASCII)
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
                                    if (readRGB == true)
                                    {
                                        if (plyHasNormals == true)
                                        {
                                            r = float.Parse(row[6]) / 255f;
                                            g = float.Parse(row[7]) / 255f;
                                            b = float.Parse(row[8]) / 255f;
                                        }
                                        else
                                        { // no normals
                                            r = float.Parse(row[3]) / 255f;
                                            g = float.Parse(row[4]) / 255f;
                                            b = float.Parse(row[5]) / 255f;
                                        }
                                        //a = float.Parse(row[6])/255; // TODO: alpha not supported yet
                                    }
                                    /*
                                    }*/
                                    break;

                                case "PCD (ASCII)": // PCD (ASCII)
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);

                                    if (readRGB == true)
                                    {
                                        // TODO: would need to check both rgb formats, this is for single value only
                                        if (row.Length == 4)
                                        {
                                            var rgb = (int)decimal.Parse(row[3], System.Globalization.NumberStyles.Float);

                                            r = (rgb >> 16) & 0x0000ff;
                                            g = (rgb >> 8) & 0x0000ff;
                                            b = (rgb) & 0x0000ff;

                                            r = LUT255[(int)r];
                                            g = LUT255[(int)g];
                                            b = LUT255[(int)b];
                                        }
                                        else if (row.Length == 6)
                                        {
                                            r = LUT255[int.Parse(row[3])];
                                            g = LUT255[int.Parse(row[4])];
                                            b = LUT255[int.Parse(row[5])];
                                        }
                                    }
                                    else if (readIntensity == true)
                                    {
                                        if (row.Length == 4) // XYZI only
                                        {
                                            r = float.Parse(row[3]);
                                            g = r;
                                            b = r;
                                        }
                                    }
                                    break;

                                default:
                                    Debug.LogError(appName + "> Error : Unknown format");
                                    break;

                            } // switch

                            if (autoOffsetNearZero == true)
                            {
                                x += autoOffset.x;
                                y += autoOffset.y;
                                z += autoOffset.z;
                            }

                            if (useManualOffset == true)
                            {
                                x += manualOffset.x;
                                y += manualOffset.y;
                                z += manualOffset.z;
                            }

                            // scaling enabled
                            if (useScaling == true)
                            {
                                x *= scaleValue;
                                y *= scaleValue;
                                z *= scaleValue;
                            }

                            if (flipYZ == true)
                            {
                                var tempVal = z;
                                z = y;
                                y = tempVal;
                            }

                            // old version
                            writer.Write((float)x);
                            writer.Write((float)y);
                            writer.Write((float)z);

                            // if have color data
                            if (readRGB == true || readIntensity == true)
                            {
                                writer.Write(r);
                                writer.Write(g);
                                writer.Write(b);
                            }
                            rowCount++;
                        }
                        else
                        { // if row length
                          //Debug.Log(line);
                            skippedRows++;
                        }
                    }

                    // reached end or enough points
                    if (rowCount >= masterPointCount || streamReader.EndOfStream)
                    {

                        if (skippedRows > 0) Debug.LogWarning("Parser skipped " + skippedRows + " rows (probably bad data or comment lines in point cloud file)");
                        //Debug.Log(masterVertexCount);

                        if (rowCount < masterPointCount) // error, file ended too early, not enough points
                        {
                            Debug.LogWarning("File does not contain enough points, fixing point count to " + rowCount + " (expected : " + masterPointCount + ") - Fixing header point count.");

                            // fix header point count, not needed for v2
                            writer.BaseStream.Seek(0, SeekOrigin.Begin);
                            writer.Write(binaryVersion);
                            writer.Write((System.Int32)rowCount);

                        }
                        haveMoreToRead = false;
                    }
                } // while loop reading file

                writer.Close();

                // for testing load timer
                //                stopwatch.Stop();
                //                Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms"); // this one gives you the time in ms
                //                stopwatch.Reset();


                Debug.Log(appName + "> Binary file saved: " + saveFilePath + " (" + masterPointCount + " points)");
                EditorUtility.ClearProgressBar();
            } // using reader

            stopwatch.Stop();
            Debug.LogFormat("Timer: {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
        } // Convert2Binary

        string v3extension = ".pct";

        void ConvertBinaryV2()
        {
            // TEMPORARY: Custom reader for LAS binary
            if (fileFormats[fileFormat] == "LAS")
            {
                if (useBinFormatV2 == true || useBinFormatV3 == true)
                {
                    Debug.LogError("V2 or V3 format is not supported for LAS. Check standalone converter: https://github.com/unitycoder/UnityPointCloudViewer/wiki/Commandline-Tools#laslaz-converter");
                }
                else
                {
                    LASConverter();
                }
                return;
            }

            string saveFilePath;
            string fileOnly = null;
            string baseFolder = null;

            if (useBinFormatV2 == false)
            {
                if (useBinFormatV3 == true)
                {
                    saveFilePath = EditorUtility.SaveFilePanelInProject("Output folder for binary v3 tiles", sourceFile.name, "pcroot", "");
                    if (string.IsNullOrEmpty(saveFilePath)) return;
                    fileOnly = Path.GetFileNameWithoutExtension(saveFilePath);
                    baseFolder = Path.GetDirectoryName(saveFilePath);
                }
                else // orig format
                {
                    saveFilePath = EditorUtility.SaveFilePanelInProject("Output binary v1 file", sourceFile.name + ".bin", "bin", "");
                }
            }
            else
            {
                saveFilePath = EditorUtility.SaveFilePanelInProject("Output binary v2 file", sourceFile.name + ".ucpc", "ucpc", "");
            }
            string fileToRead = AssetDatabase.GetAssetPath(sourceFile);

            if (!ValidateSaveAndRead(saveFilePath, fileToRead)) return;
            long lines;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // get initial data (so can check if data is ok)
            using (StreamReader streamReader = new StreamReader(File.OpenRead(fileToRead)))
            //using (FileStream fs = File.Open(fileToRead, FileMode.Open, FileAccess.Read, FileShare.None))
            //using (BufferedStream streamReader = new BufferedStream(fs))
            {
                double x = 0, y = 0, z = 0;
                float r = 0, g = 0, b = 0; //,nx=0,ny=0,nz=0;
                string line = null;
                string[] row = null;

                PeekHeaderData headerCheck;
                headerCheck.x = 0; headerCheck.y = 0; headerCheck.z = 0;
                headerCheck.linesRead = 0;

                switch (fileFormats[fileFormat])
                {
                    case "ASC": // ASC (space at front)
                        {
                            headerCheck = PeekHeader.PeekHeaderASC(streamReader, readRGB);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "CGO": // CGO	(counter at first line and uses comma)
                        {
                            //                            headerCheck = PeekHeader.PeekHeaderCGO(txtReader, readRGB);
                            headerCheck = PeekHeader.PeekHeaderCGO(streamReader, ref readRGB, readIntensity, ref masterPointCount);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "CATIA ASC": // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
                        {
                            headerCheck = PeekHeader.PeekHeaderCATIA_ASC(streamReader, ref readRGB);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "XYZRGB":
                    case "XYZ": // XYZ RGB(INT)
                        {
                            headerCheck = PeekHeader.PeekHeaderXYZ(streamReader, ref readRGB);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "PTS": // PTS (INT) (RGB)
                        {
                            headerCheck = PeekHeader.PeekHeaderPTS(streamReader, readRGB, readIntensity, ref masterPointCount);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            lines = headerCheck.linesRead;
                        }
                        break;

                    case "PLY (ASCII)": // PLY (ASCII)
                        {
                            headerCheck = PeekHeader.PeekHeaderPLY(streamReader, readRGB, ref masterPointCount, ref plyHasNormals);
                            if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                            readRGB = headerCheck.hasRGB;
                        }
                        break;

                    case "PCD (ASCII)": // PCD (ASCII)
                        {
                            headerCheck = PeekHeader.PeekHeaderPCD(streamReader, ref readRGB, ref masterPointCount);
                            if (headerCheck.readSuccess == false) { streamReader.Close(); return; }
                        }
                        break;

                    default:
                        Debug.LogError(appName + "> Unknown fileformat error (1) " + fileFormats[fileFormat]);
                        break;

                } // switch format


                if (autoOffsetNearZero == true)
                {
                    autoOffset = -new Vector3((float)headerCheck.x, (float)headerCheck.y, (float)headerCheck.z);
                    // scaling enabled, scale offset too
                    //                    if (useScaling == true) autoOffset *= scaleValue;
                    Debug.Log("(converter) autoOffset=" + autoOffset);

                }

                // progressbar
                float progress = 0;
                long progressCounter = 0;

                // get total amount of points from formats that have it
                if (fileFormats[fileFormat] == "PLY (ASCII)" || fileFormats[fileFormat] == "PTS" || fileFormats[fileFormat] == "CGO" || fileFormats[fileFormat] == "PCD (ASCII)")
                {
                    lines = masterPointCount;

                    // reset back to start of file
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    streamReader.BaseStream.Position = 0;

                    // get back to before first actual data line
                    for (int i = 0; i < headerCheck.linesRead - 1; i++)
                    {
                        streamReader.ReadLine();
                    }

                }
                else
                { // other formats need to be read completely

                    // reset back to start of file
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    streamReader.BaseStream.Position = 0;

                    // get back to first actual data line
                    for (int i = 0; i < headerCheck.linesRead; i++)
                    {
                        streamReader.ReadLine();
                    }
                    lines = 0;

                    // calculate actual point data lines
                    int splitCount = 0;
                    while (!streamReader.EndOfStream)
                    {
                        line = streamReader.ReadLine();

                        if (progressCounter > 256000)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar(appName, "(1/3) Counting rows: " + lines, lines / 50000000.0f))
                            {
                                Debug.LogWarning("Operation cancelled!");
                                EditorUtility.ClearProgressBar();
                                return;
                            }
                            progressCounter = 0;
                        }

                        progressCounter++;

                        if (line.Length > 9)
                        {
                            // TODO could skip these, expect only good data..
                            splitCount = CharCount(line, ' ');
                            if (splitCount > 2 && splitCount < 16)
                            {
                                lines++;
                            }
                        }
                    }

                    Debug.Log("(converter) Found rows: " + lines);

                    EditorUtility.ClearProgressBar();

                    // reset back to start of data
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    streamReader.BaseStream.Position = 0;

                    // now skip header lines
                    for (int i = 0; i < headerCheck.linesRead; i++)
                    {
                        streamReader.ReadLine();
                    }
                    masterPointCount = lines;
                } // get point counts


                // if limited amount
                if (limitPointCount == true)
                {
                    Debug.Log("Loading only limited amount of points: " + maxPointCount + " out of " + masterPointCount);
                    masterPointCount = (long)maxPointCount;
                }


                // create arrays already, so can randomize later.. could make separate float buffers it that allows bigger array
                // TODO this fails on large arrays..need to split, use separate x,y,z or try in commandline?
                //Vector3[] points = new Vector3[masterPointCount];
                float[] pointsX = new float[masterPointCount];
                float[] pointsY = new float[masterPointCount];
                float[] pointsZ = new float[masterPointCount];

                //Vector3[] colors = new Vector3[masterPointCount];
                float[] colorsR = new float[masterPointCount];
                float[] colorsG = new float[masterPointCount];
                float[] colorsB = new float[masterPointCount];

                // start saving into binary file
                // 27120ms manuascii_xyzrgba v2, v1 = 14800ms 

                long dataIndex = 0;
                long dataIndexRGB = 0;

                BufferedStream bsPoints = null;
                BinaryWriter writerPoints = null;

                if (useBinFormatV2 == true)
                {
                    bsPoints = new BufferedStream(new FileStream(saveFilePath, FileMode.Create));
                    writerPoints = new BinaryWriter(bsPoints);

                    // write header v2 : 34 bytes
                    var magic = new byte[] { 0x75, 0x63, 0x70, 0x63 };
                    writerPoints.Write(magic); // 4b
                    dataIndex += 4;
                    binaryVersion = 2;
                    writerPoints.Write(binaryVersion); // 1b
                    dataIndex += 1;
                    writerPoints.Write(readRGB || readIntensity); // 1b
                    dataIndex += 1;
                    writerPoints.Write((System.Int32)masterPointCount); // 4b
                    dataIndex += 4;
                }

                // calculate RGB startpoint
                dataIndexRGB = dataIndex + masterPointCount * (4 + 4 + 4);

                progressCounter = 0;
                int skippedRows = 0;
                long rowCount = 0;
                bool haveMoreToRead = true;

                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex("[ ]{2,}", options);

                float minX = Mathf.Infinity;
                float minY = Mathf.Infinity;
                float minZ = Mathf.Infinity;
                float maxX = Mathf.NegativeInfinity;
                float maxY = Mathf.NegativeInfinity;
                float maxZ = Mathf.NegativeInfinity;
                float xf, yf, zf;

                bool forceCancelled = false;

                // process all points, one line at a time
                while (haveMoreToRead == true)
                {
                    if (progressCounter > 256000)
                    {
                        //EditorUtility.DisplayProgressBar(appName, "(2/3) Reading points: " + rowCount + " / " + masterPointCount, rowCount / (float)lines);
                        if (EditorUtility.DisplayCancelableProgressBar(appName, "(2/3) Reading points: " + rowCount + " / " + masterPointCount, rowCount / (float)lines))
                        {
                            Debug.LogWarning("Operation cancelled!");
                            rowCount = masterPointCount;
                            forceCancelled = true;
                            writerPoints.Close();
                            bsPoints.Dispose();
                            EditorUtility.ClearProgressBar();
                            return;
                        }
                        progressCounter = 0;
                    }

                    progressCounter++;

                    line = streamReader.ReadLine();

                    if (line != null && line.Length > 9)
                    {
                        // trim duplicate spaces
                        line = line.Replace("   ", " ").Replace("  ", " ").Trim();
                        row = line.Split(' ');

                        if (row.Length > 2)
                        {
                            switch (fileFormats[fileFormat])
                            {
                                case "ASC": // ASC
                                    if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                    {
                                        skippedRows++;
                                        continue;
                                    }
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);
                                    if (readRGB == true)
                                    {
                                        // TODO skip this, use bytes
                                        r = LUT255[int.Parse(row[3])];
                                        g = LUT255[int.Parse(row[4])];
                                        b = LUT255[int.Parse(row[5])];
                                    }
                                    break;

                                case "CGO": // CGO	(counter at first line and uses comma)
                                    if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*') || IsFirstCharacter(line, '#'))
                                    {
                                        skippedRows++;
                                        continue;
                                    }
                                    x = double.Parse(row[0].Replace(",", "."));
                                    y = double.Parse(row[1].Replace(",", "."));
                                    z = double.Parse(row[2].Replace(",", "."));
                                    break;

                                case "CATIA ASC": // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
                                    if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*') || IsFirstCharacter(line, '#'))
                                    {
                                        skippedRows++;
                                        continue;
                                    }
                                    x = double.Parse(row[1]);
                                    y = double.Parse(row[3]);
                                    z = double.Parse(row[5]);
                                    break;

                                case "XYZRGB":
                                case "XYZ": // XYZ RGB(INT)
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);
                                    if (readRGB == true)
                                    {
                                        r = LUT255[int.Parse(row[3])];
                                        g = LUT255[int.Parse(row[4])];
                                        b = LUT255[int.Parse(row[5])];
                                    }
                                    break;

                                case "PTS": // PTS (INT) (RGB)
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);

                                    if (readRGB == true)
                                    {
                                        if (row.Length == 7) // XYZIRGB
                                        {
                                            r = LUT255[int.Parse(row[4])];
                                            g = LUT255[int.Parse(row[5])];
                                            b = LUT255[int.Parse(row[6])];
                                        }
                                        else if (row.Length == 6) // XYZRGB
                                        {
                                            r = LUT255[int.Parse(row[3])];
                                            g = LUT255[int.Parse(row[4])];
                                            b = LUT255[int.Parse(row[5])];
                                        }
                                    }
                                    else if (readIntensity == true)
                                    {
                                        if (row.Length == 4 || row.Length == 7) // XYZI or XYZIRGB
                                        {
                                            // pts intensity -2048 to 2047 ?
                                            if (intensityRange255 == true)
                                            {
                                                r = LUT255[int.Parse(row[3])];
                                            }
                                            else
                                            {
                                                r = Remap(float.Parse(row[3]), -2048, 2047, 0, 1);
                                            }
                                            g = r;
                                            b = r;
                                        }
                                    }
                                    break;

                                case "PLY (ASCII)": // PLY (ASCII)
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
                                    if (readRGB == true)
                                    {
                                        if (plyHasNormals == true)
                                        {
                                            r = float.Parse(row[6]) / 255f;
                                            g = float.Parse(row[7]) / 255f;
                                            b = float.Parse(row[8]) / 255f;
                                        }
                                        else
                                        { // no normals
                                            r = LUT255[int.Parse(row[3])];
                                            g = LUT255[int.Parse(row[4])];
                                            b = LUT255[int.Parse(row[5])];
                                        }
                                        //a = float.Parse(row[6])/255; // TODO: alpha not supported yet
                                    }
                                    /*
                                    }*/
                                    break;

                                case "PCD (ASCII)": // PCD (ASCII)
                                    x = double.Parse(row[0]);
                                    y = double.Parse(row[1]);
                                    z = double.Parse(row[2]);

                                    if (readRGB == true)
                                    {
                                        // TODO: need to check both rgb formats, this is for single value only
                                        if (row.Length == 4)
                                        {
                                            var rgb = (int)decimal.Parse(row[3], System.Globalization.NumberStyles.Float);

                                            r = (rgb >> 16) & 0x0000ff;
                                            g = (rgb >> 8) & 0x0000ff;
                                            b = (rgb) & 0x0000ff;

                                            r = LUT255[(int)r];
                                            g = LUT255[(int)g];
                                            b = LUT255[(int)b];
                                        }
                                        else if (row.Length == 6)
                                        {
                                            r = LUT255[int.Parse(row[3])];
                                            g = LUT255[int.Parse(row[4])];
                                            b = LUT255[int.Parse(row[5])];
                                        }
                                    }
                                    else if (readIntensity)
                                    {
                                        if (row.Length == 4) // XYZI only
                                        {
                                            r = float.Parse(row[3]);
                                            g = r;
                                            b = r;
                                        }
                                    }
                                    break;

                                default:
                                    Debug.LogError(appName + "> Error : Unknown format");
                                    break;

                            } // switch

                            if (autoOffsetNearZero == true)
                            {
                                x += autoOffset.x;
                                y += autoOffset.y;
                                z += autoOffset.z;
                            }

                            if (useManualOffset == true)
                            {
                                x += manualOffset.x;
                                y += manualOffset.y;
                                z += manualOffset.z;
                            }

                            // scaling enabled
                            if (useScaling == true)
                            {
                                x *= scaleValue;
                                y *= scaleValue;
                                z *= scaleValue;
                            }

                            if (flipYZ == true)
                            {
                                var tempVal = z;
                                z = y;
                                y = tempVal;
                            }

                            xf = (float)x;
                            yf = (float)y;
                            zf = (float)z;

                            pointsX[rowCount] = xf;
                            pointsY[rowCount] = yf;
                            pointsZ[rowCount] = zf;

                            // get bounds for whole cloud
                            if (xf < minX) minX = xf;
                            if (x > maxX) maxX = xf;
                            if (yf < minY) minY = yf;
                            if (y > maxY) maxY = yf;
                            if (zf < minZ) minZ = zf;
                            if (z > maxZ) maxZ = zf;

                            if (readRGB == true || readIntensity == true)
                            {
                                colorsR[rowCount] = r;
                                colorsG[rowCount] = g;
                                colorsB[rowCount] = b;
                            }
                            rowCount++;
                        }
                        else
                        { // if row length
                          //Debug.Log(line);
                            skippedRows++;
                        }
                    }

                    // reached end or enough points
                    if (rowCount >= masterPointCount || streamReader.EndOfStream)
                    {

                        if (skippedRows > 0) Debug.LogWarning("Parser skipped " + skippedRows + " rows (probably bad data or comment lines in point cloud file)");
                        //Debug.Log(masterVertexCount);

                        if (rowCount < masterPointCount) // error, file ended too early, not enough points
                        {
                            Debug.LogWarning("File does not contain enough points, fixing point count to " + rowCount + " (expected : " + masterPointCount + ") - Fixing header point count.");
                        }

                        haveMoreToRead = false;
                    }
                } // while loop reading whole file

                if (useBinFormatV2 == true)
                {
                    if (randomizePoints == true)
                    {
                        Debug.Log(appName + "> Randomizing array..");
                        //PointCloudTools.Shuffle(new System.Random(), ref points, ref colors);
                        PointCloudTools.Shuffle(new System.Random(), ref pointsX, ref pointsY, ref pointsZ, ref colorsR, ref colorsG, ref colorsB);
                    }

                    EditorUtility.DisplayProgressBar(appName, "(3/3) Saving points..", 0.99f);

                    // output bounds
                    writerPoints.Write(minX);
                    writerPoints.Write(minY);
                    writerPoints.Write(minZ);
                    writerPoints.Write(maxX);
                    writerPoints.Write(maxY);
                    writerPoints.Write(maxZ);
                    dataIndex += 4 + 4 + 4 + 4 + 4 + 4; // bounds
                    /*
                    Debug.Log("minX=" + minX);
                    Debug.Log("maxX=" + maxX);
                    Debug.Log("minY=" + minY);
                    Debug.Log("maxY=" + maxY);
                    Debug.Log("minZ=" + minZ);
                    Debug.Log("maxZ=" + maxZ);*/

                    // save to file v2
                    for (int i = 0, len = pointsX.Length; i < len; i++)
                    {
                        writerPoints.Write(pointsX[i]);
                        writerPoints.Write(pointsY[i]);
                        writerPoints.Write(pointsZ[i]);
                    }
                    for (int i = 0, len = colorsR.Length; i < len; i++)
                    {
                        writerPoints.Write(colorsR[i]);
                        writerPoints.Write(colorsG[i]);
                        writerPoints.Write(colorsB[i]);
                    }

                    writerPoints.Close();
                    bsPoints.Dispose();

                    // for testing load timer
                    //                stopwatch.Stop();
                    //                Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms"); // this one gives you the time in ms
                    //                stopwatch.Reset();


                    Debug.Log(appName + "> Binary file saved: " + saveFilePath + " (" + masterPointCount + " points)");
                }
                else // v3 BETA
                {
                    int v3Version = 1; // initial basic version

                    // get whole cloud bounds
                    float cloudBoundsMinX = minX;
                    float cloudBoundsMinY = minY;
                    float cloudBoundsMinZ = minZ;
                    float cloudBoundsMaxX = maxX;
                    float cloudBoundsMaxY = maxY;
                    float cloudBoundsMaxZ = maxZ;

                    int MAXHEIGHT = (int)(maxY + 1);
                    int MAXWIDTH = (int)(maxX + 1);

                    EditorUtility.DisplayProgressBar(appName, "(3/7) Randomizing points: " + masterPointCount, 0.5f);

                    // our nodes (containing indexes into the points within that node)
                    var nodes = new Dictionary<string, List<int>>();

                    // no need to collect, if each node is fixed grid?
                    List<PointCloudTile> nodeBounds = new List<PointCloudTile>();

                    Debug.Log("(v3 converter) Randomizing array..");
                    //PointCloudTools.Shuffle(new System.Random(), ref points, ref colors);
                    PointCloudTools.Shuffle(new System.Random(), ref pointsX, ref pointsY, ref pointsZ, ref colorsR, ref colorsG, ref colorsB);

                    float gridSizeInverted = 1f / gridSize;
                    progressCounter = 0;

                    // loop all points to collect their point index into correct cells
                    for (int i = 0, len = (int)masterPointCount; i < len; i++)
                    {

                        if (progressCounter > 256000)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar(appName, "(4/7) Collecting points to tiles : " + i + " / " + len, i / (float)len))
                            {
                                Debug.Log("Cancelled at " + i + " / " + len);
                                EditorUtility.ClearProgressBar();
                                return;
                            }
                            progressCounter = 0;
                        }
                        progressCounter++;

                        // get point
                        var px = pointsX[i];
                        var py = pointsY[i];
                        var pz = pointsZ[i];

                        // add to correct cell
                        int cellX = (int)(px * gridSizeInverted);
                        int cellY = (int)(py * gridSizeInverted);
                        int cellZ = (int)(pz * gridSizeInverted);

                        string key = cellX + "_" + cellY + "_" + cellZ;

                        if (nodes.ContainsKey(key) == true)
                        {
                            nodes[key].Add(i);
                        }
                        else
                        { // create new list for this key

                            nodes[key] = new List<int>();
                            nodes[key].Add(i);
                        }
                    } // for all points

                    // build meshes per cell, leave out below x points
                    // loop all nodes (their point indexes), TODO should warn if millions of cells..?
                    int fileCounter = 0;
                    int skippedCounter = 0;
                    progressCounter = 0;

                    foreach (List<int> indexes in nodes.Values)
                    {
                        if (indexes.Count < minimumPointCount)
                        {
                            skippedCounter++;
                            continue;
                        }

                        // get this node bounds
                        minX = Mathf.Infinity;
                        minY = Mathf.Infinity;
                        minZ = Mathf.Infinity;
                        maxX = Mathf.NegativeInfinity;
                        maxY = Mathf.NegativeInfinity;
                        maxZ = Mathf.NegativeInfinity;

                        // build tilefile for these points
                        //Debug.Log("save file " + saveFilePath + fileCounter + v3extension);
                        string fullpath = Path.Combine(baseFolder, fileOnly) + fileCounter + v3extension;
                        // save filename only, since root file is in same folder
                        string fullpathFileOnly = fileOnly + fileCounter + v3extension;

                        //Debug.Log("fullpathOLD=" + fullpathOLD);
                        //Debug.Log("fullpath=" + fullpath);

                        fileCounter++;

                        bsPoints = new BufferedStream(new FileStream(fullpath, FileMode.Create));
                        writerPoints = new BinaryWriter(bsPoints);


                        // output all points within that node cell
                        for (int i = 0, len = indexes.Count; i < len; i++)
                        {
                            if (progressCounter > 256000)
                            {
                                if (EditorUtility.DisplayCancelableProgressBar(appName, "(5/7) Output points to tiles (" + fileCounter + " / " + nodes.Count + ") : " + i + " / " + len, i / (float)len))
                                {
                                    Debug.Log("Cancelled at " + i + " / " + len);
                                    EditorUtility.ClearProgressBar();
                                    writerPoints.Close();
                                    bsPoints.Dispose();
                                    return;
                                }
                                progressCounter = 0;
                            }
                            progressCounter++;

                            var px = pointsX[indexes[i]];
                            var py = pointsY[indexes[i]];
                            var pz = pointsZ[indexes[i]];

                            if (px < minX) minX = px;
                            if (px > maxX) maxX = px;
                            if (py < minY) minY = py;
                            if (py > maxY) maxY = py;
                            if (pz < minZ) minZ = pz;
                            if (pz > maxZ) maxZ = pz;

                            writerPoints.Write(px);
                            writerPoints.Write(py);
                            writerPoints.Write(pz);
                        } // loop all point in cell cells

                        // close tile/node
                        writerPoints.Close();
                        bsPoints.Dispose();

                        // save RGB
                        var bsColors = new BufferedStream(new FileStream(fullpath + ".rgb", FileMode.Create));
                        var writerColors = new BinaryWriter(bsColors);
                        // output all points within that node cell
                        for (int i = 0, len = indexes.Count; i < len; i++)
                        {
                            //var c = colors[indexes[i]];
                            writerColors.Write(colorsR[indexes[i]]);
                            writerColors.Write(colorsG[indexes[i]]);
                            writerColors.Write(colorsB[indexes[i]]);
                        } // loop all point in cell cells
                        // close tile/node
                        writerColors.Close();
                        bsColors.Dispose();

                        // collect node bounds, name and pointcount
                        var cb = new PointCloudTile();
                        cb.filename = fullpathFileOnly;//.Replace("Assets/StreamingAssets/", "");
                        cb.totalPoints = indexes.Count;

                        cb.minX = minX;
                        cb.minY = minY;
                        cb.minZ = minZ;

                        cb.maxX = maxX;
                        cb.maxY = maxY;
                        cb.maxZ = maxZ;

                        cb.center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
                        nodeBounds.Add(cb);
                    } // loop all cells

                    // save rootfile

                    // first row is gridsize
                    var tilerootdata = new List<string>();

                    EditorUtility.DisplayProgressBar(appName, "(6/7) Sorting tiles: " + nodeBounds.Count, 0.98f);

                    // sort nodes to 0,0,0, not needed anymore
                    //nodeBounds.Sort(delegate (PointCloudTile aa, PointCloudTile bb) { return Vector3.Distance(Vector3.zero, aa.center).CompareTo(Vector3.Distance(Vector3.zero, bb.center)); });

                    progressCounter = 0;
                    for (int i = 0, len = nodeBounds.Count; i < len; i++)
                    {
                        if (progressCounter > 256)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar(appName, "(7/7) Creating pcroot file : " + i + " / " + len, i / (float)len))
                            {
                                Debug.Log("Cancelled at " + i + " / " + len);
                                EditorUtility.ClearProgressBar();
                                return;
                            }
                            progressCounter = 0;
                        }
                        progressCounter++;

                        var tilerow = nodeBounds[i].filename + sep + nodeBounds[i].totalPoints + sep + nodeBounds[i].minX + sep + nodeBounds[i].minY + sep + nodeBounds[i].minZ + sep + nodeBounds[i].maxX + sep + nodeBounds[i].maxY + sep + nodeBounds[i].maxZ;
                        tilerootdata.Add(tilerow);
                    }

                    // add global settings to first row
                    //               version,          gridsize,                   pointcount,              boundsMinX,             boundsMinY,             boundsMinZ,             boundsMaxX,             boundsMaxY,             boundsMaxZ
                    var globalData = v3Version + sep + gridSize.ToString() + sep + masterPointCount + sep + cloudBoundsMinX + sep + cloudBoundsMinY + sep + cloudBoundsMinZ + sep + cloudBoundsMaxX + sep + cloudBoundsMaxY + sep + cloudBoundsMaxZ;
                    if (useManualOffset == true)
                    {
                        autoOffset += manualOffset;
                    }
                    globalData += sep + autoOffset.x + sep + autoOffset.y + sep + autoOffset.z;
                    tilerootdata.Insert(0, globalData);

                    //File.WriteAllLines(Path.Combine(Application.streamingAssetsPath, saveBaseName + ".pcroot"), tilerootdata);
                    var outputFile = Path.Combine(baseFolder, fileOnly) + ".pcroot";
                    File.WriteAllLines(outputFile, tilerootdata.ToArray());

                    Debug.Log("(v3 converter) Done : " + outputFile + " (skipped " + skippedCounter + " nodes with less than " + minimumPointCount + " points)");

                    if ((tilerootdata.Count - 1) <= 0)
                    {
                        Debug.LogError("Actually, no tiles found! You should probably enable scale values to make your cloud to smaller size? Or make gridsize bigger, or set minimum point count smaller.");
                    }
                } // v3

                EditorUtility.ClearProgressBar();
            } // using reader

            stopwatch.Stop();
            Debug.LogFormat("(Converter) Timer: {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();

        } // Convert3Binary


        void LASConverter()
        {
            var saveFilePath = EditorUtility.SaveFilePanel("Save binary file", "Assets/", sourceFile.name + ".bin", "bin");
            string fileToRead = AssetDatabase.GetAssetPath(sourceFile);
            if (!ValidateSaveAndRead(saveFilePath, fileToRead)) return;

            double x = 0f, y = 0f, z = 0f;
            float r = 0f, g = 0f, b = 0f;

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

                /*
				Debug.Log ("startOfFirstExtentedVariableLengthRecord:"+startOfFirstExtentedVariableLengthRecord); // FIXME
				Debug.Log ("numberOfExtentedVariableLengthRecords:"+numberOfExtentedVariableLengthRecords);
				Debug.Log ("numberOfPointRecords:"+numberOfPointRecords);
				Debug.Log ("numberOfPointsByReturn:"+numberOfPointsByReturn[0]); // *** // FIXME
				*/

            }


            /*
			Debug.Log ("fileSignature:"+fileSignature);
			Debug.Log ("fileSourceID:"+fileSourceID);
			Debug.Log ("globalEncoding:"+globalEncoding);
			Debug.Log ("ProjectID1:"+projectID1);
			Debug.Log ("ProjectID2:"+projectID2);
			Debug.Log ("ProjectID3:"+projectID3);
			Debug.Log ("ProjectID4:"+projectID4);
			
			Debug.Log ("versionMajor:"+versionMajor);
			Debug.Log ("versionMinor:"+versionMinor);
			Debug.Log ("systemIdentifier:"+systemIdentifier);
			Debug.Log ("generatingSoftware:"+generatingSoftware);
			Debug.Log ("fileCreationDayOfYear:"+fileCreationDayOfYear);
			Debug.Log ("fileCreationYear:"+fileCreationYear);
			Debug.Log ("headerSize:"+headerSize);
			Debug.Log ("offsetToPointData:"+offsetToPointData);
			Debug.Log ("numberOfVariableLengthRecords:"+numberOfVariableLengthRecords);
			Debug.Log ("pointDataRecordFormat:"+pointDataRecordFormat);
			Debug.Log ("PointDataRecordLength:"+PointDataRecordLength);
			Debug.Log ("legacyNumberOfPointRecords:"+legacyNumberOfPointRecords);
			Debug.Log ("legacyNumberOfPointsByReturn:"+legacyNumberOfPointsByReturn[0]); // ***
			Debug.Log ("xScaleFactor:"+xScaleFactor);
			Debug.Log ("yScaleFactor:"+yScaleFactor);
			Debug.Log ("zScaleFactor:"+zScaleFactor);
			Debug.Log ("xOffset:"+xOffset);
			Debug.Log ("yOffset:"+yOffset);
			Debug.Log ("zOffset:"+zOffset);
			Debug.Log ("maxX:"+maxX);
			Debug.Log ("minX:"+minX);
			Debug.Log ("MaxY:"+MaxY);
			Debug.Log ("minY:"+minY);
			Debug.Log ("maxZ:"+maxZ);
			Debug.Log ("minZ:"+minZ);
			*/


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
				Debug.Log("vlrDescription:"+vlrDescription);
				*/

            }

            // jump to points start pos
            reader.BaseStream.Seek((long)offsetToPointData, SeekOrigin.Begin);

            Debug.Log("LAS file, PointDataRecordFormat: " + pointDataRecordFormat + ", Version:" + versionMajor + "." + versionMinor);

            // format #2
            if (pointDataRecordFormat != 2 && pointDataRecordFormat != 3) Debug.LogWarning("LAS Import might fail - only pointDataRecordFormat #2 & #3 are supported (Your file is " + pointDataRecordFormat + ")");
            if (versionMinor != 2) Debug.LogWarning("LAS Import might fail - only version LAS 1.2 is supported. (Your file is " + versionMajor + "." + versionMinor + ")");

            masterPointCount = (int)legacyNumberOfPointRecords;

            // scaling enabled, scale manual offset
            if (useScaling == true) manualOffset *= scaleValue;

            // progressbar
            long progress = 0;
            long progressCounter = 0;
            EditorUtility.ClearProgressBar();

            // saving, write header
            //var writer = new BinaryWriter(File.Open(saveFilePath, FileMode.Create));
            using (BinaryWriter writer = new BinaryWriter(File.Open(saveFilePath, FileMode.Create)))
            {
                binaryVersion = 1;
                writer.Write(binaryVersion);
                writer.Write((System.Int32)masterPointCount);
                writer.Write(readRGB);

                long rowCount = 0;
                bool haveMoreToRead = true;
                bool firstPointRead = false;

                // process all points
                while (haveMoreToRead == true)
                {
                    if (progressCounter > 256000)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(appName, "Converting pointcloud to V1 .bin : " + progress + " / " + masterPointCount, progress / (float)masterPointCount))
                        {
                            Debug.Log("Cancelled at " + progress + " / " + masterPointCount);
                            haveMoreToRead = false;
                            break;
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

                    if (useManualOffset == true)
                    {
                        x += manualOffset.x;
                        y += manualOffset.y;
                        z += manualOffset.z;
                    }

                    if (flipYZ == true)
                    {
                        double yy = y;
                        y = z;
                        z = yy;
                    }

                    if (useScaling == true)
                    {
                        x *= scaleValue;
                        y *= scaleValue;
                        z *= scaleValue;
                    }

                    writer.Write((float)x);
                    writer.Write((float)y);
                    writer.Write((float)z);

                    if (readRGB == true)
                    {
                        r = (float)System.BitConverter.ToUInt16(colorR, 0);
                        g = (float)System.BitConverter.ToUInt16(colorG, 0);
                        b = (float)System.BitConverter.ToUInt16(colorB, 0);

                        //if (rowCount<100)	Debug.Log("row:"+(rowCount+1)+" xyz:"+x+","+y+","+z+" : "+r+","+g+","+b);

                        //LUT255[int.Parse(row[3])];

                        r = ((float)r) / 255f;
                        g = ((float)g) / 255f;
                        b = ((float)b) / 255f;

                        // fix for high values
                        if (r > 1) r /= 255f;
                        if (g > 1) g /= 255f;
                        if (b > 1) b /= 255f;

                        writer.Write(r);
                        writer.Write(g);
                        writer.Write(b);
                    }

                    rowCount++;

                    if (reader.BaseStream.Position >= reader.BaseStream.Length || rowCount >= masterPointCount)
                    {

                        if (rowCount < masterPointCount) // error, file ended too early, not enough points
                        {
                            Debug.LogWarning("LAS file does not contain enough points, fixing point count to " + rowCount);

                            // fix header point count
                            writer.BaseStream.Seek(0, SeekOrigin.Begin);
                            writer.Write(binaryVersion);
                            writer.Write((System.Int32)rowCount);
                        }
                        haveMoreToRead = false;
                    }

                } // while loop reading file

                //writer.Close();
            }


            Debug.Log(appName + "> Binary file saved: " + saveFilePath + " (" + masterPointCount + " points)");
            EditorUtility.ClearProgressBar();
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

        string GetSelectedFileInfo()
        {
            string tempFilePath = AssetDatabase.GetAssetPath(sourceFile);

            if (Directory.Exists(tempFilePath))
            {
                return "[ Folders are NOT yet supported for binary conversion ]";
            }
            else
            {
                string tempFileName = Path.GetFileName(tempFilePath);
                return tempFileName + " (" + (new FileInfo(tempFilePath).Length / 1000000) + "MB)";
            }
        }



        int CharCount(string source, char separator)
        {
            int count = 0;
            for (int i = 0, length = source.Length; i < length; i++)
            {
                if (source[i] == separator) count++;
            }
            return count;
        }

        bool IsFirstCharacter(string source, char toFind)
        {
            if (source == null || source.Length == 0) return false;
            return source[0] == toFind;
        }

        // http://stackoverflow.com/a/16776096/5452781
        string FilterWhiteSpaces(string input)
        {
            if (input == null)
                return string.Empty;

            StringBuilder stringBuilder = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (i == 0 || c != ' ' || (c == ' ' && input[i - 1] != ' '))
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }

        float[] LUT255 = new float[] { 0f, 0.00392156862745098f, 0.00784313725490196f, 0.011764705882352941f, 0.01568627450980392f, 0.0196078431372549f, 0.023529411764705882f, 0.027450980392156862f, 0.03137254901960784f, 0.03529411764705882f, 0.0392156862745098f, 0.043137254901960784f, 0.047058823529411764f, 0.050980392156862744f, 0.054901960784313725f, 0.058823529411764705f, 0.06274509803921569f, 0.06666666666666667f, 0.07058823529411765f, 0.07450980392156863f, 0.0784313725490196f, 0.08235294117647059f, 0.08627450980392157f, 0.09019607843137255f, 0.09411764705882353f, 0.09803921568627451f, 0.10196078431372549f, 0.10588235294117647f, 0.10980392156862745f, 0.11372549019607843f, 0.11764705882352941f, 0.12156862745098039f, 0.12549019607843137f, 0.12941176470588237f, 0.13333333333333333f, 0.13725490196078433f, 0.1411764705882353f, 0.1450980392156863f, 0.14901960784313725f, 0.15294117647058825f, 0.1568627450980392f, 0.1607843137254902f, 0.16470588235294117f, 0.16862745098039217f, 0.17254901960784313f, 0.17647058823529413f, 0.1803921568627451f, 0.1843137254901961f, 0.18823529411764706f, 0.19215686274509805f, 0.19607843137254902f, 0.2f, 0.20392156862745098f, 0.20784313725490197f, 0.21176470588235294f, 0.21568627450980393f, 0.2196078431372549f, 0.2235294117647059f, 0.22745098039215686f, 0.23137254901960785f, 0.23529411764705882f, 0.23921568627450981f, 0.24313725490196078f, 0.24705882352941178f, 0.25098039215686274f, 0.2549019607843137f, 0.25882352941176473f, 0.2627450980392157f, 0.26666666666666666f, 0.27058823529411763f, 0.27450980392156865f, 0.2784313725490196f, 0.2823529411764706f, 0.28627450980392155f, 0.2901960784313726f, 0.29411764705882354f, 0.2980392156862745f, 0.30196078431372547f, 0.3058823529411765f, 0.30980392156862746f, 0.3137254901960784f, 0.3176470588235294f, 0.3215686274509804f, 0.3254901960784314f, 0.32941176470588235f, 0.3333333333333333f, 0.33725490196078434f, 0.3411764705882353f, 0.34509803921568627f, 0.34901960784313724f, 0.35294117647058826f, 0.3568627450980392f, 0.3607843137254902f, 0.36470588235294116f, 0.3686274509803922f, 0.37254901960784315f, 0.3764705882352941f, 0.3803921568627451f, 0.3843137254901961f, 0.38823529411764707f, 0.39215686274509803f, 0.396078431372549f, 0.4f, 0.403921568627451f, 0.40784313725490196f, 0.4117647058823529f, 0.41568627450980394f, 0.4196078431372549f, 0.4235294117647059f, 0.42745098039215684f, 0.43137254901960786f, 0.43529411764705883f, 0.4392156862745098f, 0.44313725490196076f, 0.4470588235294118f, 0.45098039215686275f, 0.4549019607843137f, 0.4588235294117647f, 0.4627450980392157f, 0.4666666666666667f, 0.47058823529411764f, 0.4745098039215686f, 0.47843137254901963f, 0.4823529411764706f, 0.48627450980392156f, 0.49019607843137253f, 0.49411764705882355f, 0.4980392156862745f, 0.5019607843137255f, 0.5058823529411764f, 0.5098039215686274f, 0.5137254901960784f, 0.5176470588235295f, 0.5215686274509804f, 0.5254901960784314f, 0.5294117647058824f, 0.5333333333333333f, 0.5372549019607843f, 0.5411764705882353f, 0.5450980392156862f, 0.5490196078431373f, 0.5529411764705883f, 0.5568627450980392f, 0.5607843137254902f, 0.5647058823529412f, 0.5686274509803921f, 0.5725490196078431f, 0.5764705882352941f, 0.5803921568627451f, 0.5843137254901961f, 0.5882352941176471f, 0.592156862745098f, 0.596078431372549f, 0.6f, 0.6039215686274509f, 0.6078431372549019f, 0.611764705882353f, 0.615686274509804f, 0.6196078431372549f, 0.6235294117647059f, 0.6274509803921569f, 0.6313725490196078f, 0.6352941176470588f, 0.6392156862745098f, 0.6431372549019608f, 0.6470588235294118f, 0.6509803921568628f, 0.6549019607843137f, 0.6588235294117647f, 0.6627450980392157f, 0.6666666666666666f, 0.6705882352941176f, 0.6745098039215687f, 0.6784313725490196f, 0.6823529411764706f, 0.6862745098039216f, 0.6901960784313725f, 0.6941176470588235f, 0.6980392156862745f, 0.7019607843137254f, 0.7058823529411765f, 0.7098039215686275f, 0.7137254901960784f, 0.7176470588235294f, 0.7215686274509804f, 0.7254901960784313f, 0.7294117647058823f, 0.7333333333333333f, 0.7372549019607844f, 0.7411764705882353f, 0.7450980392156863f, 0.7490196078431373f, 0.7529411764705882f, 0.7568627450980392f, 0.7607843137254902f, 0.7647058823529411f, 0.7686274509803922f, 0.7725490196078432f, 0.7764705882352941f, 0.7803921568627451f, 0.7843137254901961f, 0.788235294117647f, 0.792156862745098f, 0.796078431372549f, 0.8f, 0.803921568627451f, 0.807843137254902f, 0.8117647058823529f, 0.8156862745098039f, 0.8196078431372549f, 0.8235294117647058f, 0.8274509803921568f, 0.8313725490196079f, 0.8352941176470589f, 0.8392156862745098f, 0.8431372549019608f, 0.8470588235294118f, 0.8509803921568627f, 0.8549019607843137f, 0.8588235294117647f, 0.8627450980392157f, 0.8666666666666667f, 0.8705882352941177f, 0.8745098039215686f, 0.8784313725490196f, 0.8823529411764706f, 0.8862745098039215f, 0.8901960784313725f, 0.8941176470588236f, 0.8980392156862745f, 0.9019607843137255f, 0.9058823529411765f, 0.9098039215686274f, 0.9137254901960784f, 0.9176470588235294f, 0.9215686274509803f, 0.9254901960784314f, 0.9294117647058824f, 0.9333333333333333f, 0.9372549019607843f, 0.9411764705882353f, 0.9450980392156862f, 0.9490196078431372f, 0.9529411764705882f, 0.9568627450980393f, 0.9607843137254902f, 0.9647058823529412f, 0.9686274509803922f, 0.9725490196078431f, 0.9764705882352941f, 0.9803921568627451f, 0.984313725490196f, 0.9882352941176471f, 0.9921568627450981f, 0.996078431372549f, 1f };

        private void OnEnable()
        {
            LoadPreferences();
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        void LoadPreferences()
        {
            // read last used preferences
            fileFormat = EditorPrefs.GetInt(prefsPrefix + "fileFormat", fileFormat);
            readRGB = EditorPrefs.GetBool(prefsPrefix + "readRGB", readRGB);
            readIntensity = EditorPrefs.GetBool(prefsPrefix + "readIntensity", readIntensity);
            useScaling = EditorPrefs.GetBool(prefsPrefix + "useScaling", useScaling);
            scaleValue = EditorPrefs.GetFloat(prefsPrefix + "scaleValue", scaleValue);
            flipYZ = EditorPrefs.GetBool(prefsPrefix + "flipYZ", flipYZ);
            autoOffsetNearZero = EditorPrefs.GetBool(prefsPrefix + "autoOffsetNearZero", autoOffsetNearZero);
            useManualOffset = EditorPrefs.GetBool(prefsPrefix + "useManualOffset", useManualOffset);
            manualOffset.x = EditorPrefs.GetFloat(prefsPrefix + "manualOffset.x", manualOffset.x);
            manualOffset.y = EditorPrefs.GetFloat(prefsPrefix + "manualOffset.y", manualOffset.y);
            manualOffset.z = EditorPrefs.GetFloat(prefsPrefix + "manualOffset.z", manualOffset.z);

            useBinFormatV2 = EditorPrefs.GetBool(prefsPrefix + "useBinFormatV2", useBinFormatV2);
            randomizePoints = EditorPrefs.GetBool(prefsPrefix + "randomizePoints", randomizePoints);
            useBinFormatV3 = EditorPrefs.GetBool(prefsPrefix + "useBinFormatV3", useBinFormatV3);
            minimumPointCount = EditorPrefs.GetInt(prefsPrefix + "minimumPointCount", minimumPointCount);
            gridSize = EditorPrefs.GetFloat(prefsPrefix + "gridSize", gridSize);
            maxPointCount = EditorPrefs.GetInt(prefsPrefix + "maxPointCount", maxPointCount);
            limitPointCount = EditorPrefs.GetBool(prefsPrefix + "limitPointCount", limitPointCount);
            intensityRange255 = EditorPrefs.GetBool(prefsPrefix + "intensityRange255", intensityRange255);

        }

        void SavePreferences()
        {
            // save settings on exit
            EditorPrefs.SetInt(prefsPrefix + "fileFormat", fileFormat);
            EditorPrefs.SetBool(prefsPrefix + "readRGB", readRGB);
            EditorPrefs.SetBool(prefsPrefix + "readIntensity", readIntensity);
            EditorPrefs.SetBool(prefsPrefix + "useScaling", useScaling);
            EditorPrefs.SetFloat(prefsPrefix + "scaleValue", scaleValue);
            EditorPrefs.SetBool(prefsPrefix + "flipYZ", flipYZ);
            EditorPrefs.SetBool(prefsPrefix + "autoOffsetNearZero", autoOffsetNearZero);
            EditorPrefs.SetBool(prefsPrefix + "useManualOffset", useManualOffset);
            EditorPrefs.SetFloat(prefsPrefix + "manualOffset.x", manualOffset.x);
            EditorPrefs.SetFloat(prefsPrefix + "manualOffset.y", manualOffset.y);
            EditorPrefs.SetFloat(prefsPrefix + "manualOffset.z", manualOffset.z);

            EditorPrefs.SetBool(prefsPrefix + "useBinFormatV2", useBinFormatV2);
            EditorPrefs.SetBool(prefsPrefix + "randomizePoints", randomizePoints);
            EditorPrefs.SetBool(prefsPrefix + "useBinFormatV3", useBinFormatV3);
            EditorPrefs.SetInt(prefsPrefix + "minimumPointCount", minimumPointCount);
            EditorPrefs.SetFloat(prefsPrefix + "gridSize", gridSize);
            EditorPrefs.SetInt(prefsPrefix + "maxPointCount", maxPointCount);
            EditorPrefs.SetBool(prefsPrefix + "limitPointCount", limitPointCount);
            EditorPrefs.SetBool(prefsPrefix + "intensityRange255", intensityRange255);
        }


    } // class
} // namespace
