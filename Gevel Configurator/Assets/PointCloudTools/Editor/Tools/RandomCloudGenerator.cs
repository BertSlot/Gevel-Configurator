// Random Point Cloud Binary Generator
// Creates point cloud with given amount of points for testing

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Globalization;

namespace unitycodercom_RandomCloudGenerator
{

    public class RandomCloudGenerator : EditorWindow
    {
        private static string appName = "RandomCloudGenerator";
        private bool createBinaryCloud = true;
        private bool useRGB = true;
        private int plyVertexCount = 1000000;
        private byte binaryVersion = 1;
        private Vector3 from = Vector3.zero;
        private Vector3 to = Vector3.one * 10;
        private bool createPTSCloud = false;
        private bool createXYZCloud = false;
        private bool useNewFormatV2 = false;

        // create menu item and window
        [MenuItem("Window/PointCloudTools/Create test binary cloud", false, 200)]
        static void Init()
        {
            RandomCloudGenerator window = (RandomCloudGenerator)EditorWindow.GetWindow(typeof(RandomCloudGenerator));
            window.titleContent = new GUIContent(appName);
            window.minSize = new Vector2(340, 280);
            window.maxSize = new Vector2(340, 284);

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
            plyVertexCount = EditorGUILayout.IntField("Amount of points", plyVertexCount);

            from = EditorGUILayout.Vector3Field("From XYZ", from);
            to = EditorGUILayout.Vector3Field("Until XYZ", to);

            EditorGUILayout.Space();

            useRGB = EditorGUILayout.ToggleLeft(new GUIContent("Enable RGB colors", null, "Should be almost always enabled (to save RGB data)"), useRGB);

            EditorGUILayout.Space();

            createBinaryCloud = EditorGUILayout.ToggleLeft(new GUIContent("Create Binary Cloud", null, "Creates .bin (v1) or .ucpc (v2) file"), createBinaryCloud);
            GUI.enabled = createBinaryCloud;
            useNewFormatV2 = EditorGUILayout.ToggleLeft(new GUIContent("use New Format V2 (.ucpc)", null, ""), useNewFormatV2);
            GUI.enabled = true;

            createPTSCloud = EditorGUILayout.ToggleLeft(new GUIContent("Create Ascii .pts Cloud", null, "Creates .pts file"), createPTSCloud);
            createXYZCloud = createPTSCloud ? false : createXYZCloud;

            createXYZCloud = EditorGUILayout.ToggleLeft(new GUIContent("Create Ascii .xyz Cloud", null, "Creates .xyz file"), createXYZCloud);
            createPTSCloud = createXYZCloud ? false : createPTSCloud;

            EditorGUILayout.Space();

            // convert button
            if (GUILayout.Button(new GUIContent("Create Random Cloud", "Creates random binary cloud"), GUILayout.Height(40)))
            {
                if (useNewFormatV2 == true)
                {
                    CreateRandomCloudV2();
                }
                else
                {
                    if (createBinaryCloud == true) CreateRandomCloud();
                    if (createPTSCloud == true) CreateAsciiCloud(usePts: true);
                    if (createXYZCloud == true) CreateAsciiCloud(usePts: false);
                }
            }
            GUI.enabled = true;
        } //ongui

        // v1 .bin
        void CreateRandomCloud()
        {
            if (plyVertexCount < 1 && plyVertexCount > 99999999) return;

            var saveFilePath = EditorUtility.SaveFilePanel("Save binary file", "Assets/", "random.bin", "bin");

            if (string.IsNullOrEmpty(saveFilePath)) return;

            float x = 0, y = 0, z = 0;//,r=0,g=0,b=0; //,nx=0,ny=0,nz=0;; // init vals

            long progressCounter = 0;

            // prepare to start saving binary file

            // write header
            var writer = new BinaryWriter(File.Open(saveFilePath, FileMode.Create));
            writer.Write(binaryVersion);
            writer.Write((System.Int32)plyVertexCount);
            writer.Write(useRGB);

            progressCounter = 0;

            long rowCount = 0;
            bool haveMoreToRead = true;

            // process all points
            while (haveMoreToRead)
            {
                if (progressCounter > 256000)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Creating random point cloud " + rowCount + " / " + plyVertexCount, rowCount / (float)plyVertexCount))
                    {
                        Debug.LogWarning("Operation cancelled!");
                        writer.Close();
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    progressCounter = 0;
                }


                progressCounter++;
                x = Random.Range(from.x, to.x);
                y = Random.Range(from.y, to.y);
                z = Random.Range(from.z, to.z);
                writer.Write(x);
                writer.Write(y);
                writer.Write(z);
                if (useRGB == true)
                {
                    writer.Write(Random.value);
                    writer.Write(Random.value);
                    writer.Write(Random.value);
                }
                rowCount++;
                if (rowCount >= plyVertexCount) haveMoreToRead = false;
            }

            writer.Close();

            Debug.Log(appName + "> Binary file saved: " + saveFilePath + " (" + plyVertexCount + " points)");
            EditorUtility.ClearProgressBar();
        }

        void CreateRandomCloudV2()
        {
            if (plyVertexCount < 1 && plyVertexCount > 999999999) return;

            if (useRGB == false)
            {
                Debug.LogWarning("V2 version requires rgb data for now");
                useRGB = true;
            }

            var saveFilePath = EditorUtility.SaveFilePanel("Save binary file", "Assets/", "random.ucpc", "ucpc");

            if (string.IsNullOrEmpty(saveFilePath)) return;

            float x = 0, y = 0, z = 0;//,r=0,g=0,b=0; //,nx=0,ny=0,nz=0;; // init vals
            long progressCounter = 0;

            // write header v2
            var writer = new BinaryWriter(File.Open(saveFilePath, FileMode.Create));

            var magic = new byte[] { 0x75, 0x63, 0x70, 0x63 }; // ucpc
            writer.Write(magic); // 4b
            binaryVersion = 2;
            writer.Write(binaryVersion); // 1b
            writer.Write(useRGB); // 1b
            // TODO add intensity only, or normal or color size, or packed or others
            writer.Write((System.Int32)plyVertexCount); // 4b

            writer.Write(from.x); // 4b
            writer.Write(from.y); // 4b
            writer.Write(from.z); // 4b
            writer.Write(to.x); // 4b
            writer.Write(to.y); // 4b
            writer.Write(to.z); // 4b

            progressCounter = 0;
            long rowCount = 0;

            // process all points
            while (rowCount < plyVertexCount)
            {
                if (progressCounter > 256000)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Creating random points " + rowCount + " / " + plyVertexCount, rowCount / (float)plyVertexCount))
                    {
                        Debug.LogWarning("Operation cancelled!");
                        writer.Close();
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    progressCounter = 0;

                }
                progressCounter++;
                x = Random.Range(from.x, to.x);
                y = Random.Range(from.y, to.y);
                z = Random.Range(from.z, to.z);
                writer.Write(x);
                writer.Write(y);
                writer.Write(z);
                rowCount++;
            }

            rowCount = 0; progressCounter = 0;
            while (rowCount < plyVertexCount)
            {
                if (progressCounter > 256000)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Creating random colors " + rowCount + " / " + plyVertexCount, rowCount / (float)plyVertexCount))
                    {
                        Debug.LogWarning("Operation cancelled!");
                        writer.Close();
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    progressCounter = 0;
                }
                progressCounter++;
                if (useRGB == true)
                {
                    writer.Write(Random.value);
                    writer.Write(Random.value);
                    writer.Write(Random.value);

                }
                rowCount++;
            }

            writer.Close();

            Debug.Log(appName + "> Binary file saved: " + saveFilePath + " (" + plyVertexCount + " points)");
            EditorUtility.ClearProgressBar();

            //if (createAsciiXYZ) CreateXYZ();
        }

        void CreateAsciiCloud(bool usePts)
        {
            string saveFilePath = "";
            if (usePts == true)
            {
                saveFilePath = EditorUtility.SaveFilePanel("Save PTS file", "Assets/", "random.pts", "pts");
            }
            else
            {
                saveFilePath = EditorUtility.SaveFilePanel("Save XYZ file", "Assets/", "random.xyz", "xyz");
            }

            if (saveFilePath == null) return;

            float x = 0, y = 0, z = 0;//,r=0,g=0,b=0; //,nx=0,ny=0,nz=0;; // init vals
            float progress = 0;
            long progressCounter = 0;

            // prepare to start saving binary file
            var writer = new StreamWriter(File.Open(saveFilePath, FileMode.Create));

            string sep = " ";

            progress = 0;
            progressCounter = 0;

            long rowCount = 0;
            bool haveMoreToRead = true;

            if (usePts == true)
            {
                // .pts header (point count)
                writer.WriteLine(plyVertexCount);
            }

            // process all points
            while (haveMoreToRead)
            {

                if (progressCounter > 256000)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Creating random point cloud " + rowCount + " / " + plyVertexCount, rowCount / (float)plyVertexCount))
                    {
                        Debug.LogWarning("Operation cancelled!");
                        writer.Close();
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                    progressCounter = 0;
                }

                progressCounter++;
                progress++;

                x = Random.Range(from.x, to.x);
                y = Random.Range(from.y, to.y);
                z = Random.Range(from.z, to.z);

                // if have color data
                if (useRGB == true)
                {
                    writer.WriteLine(x + sep + y + sep + z + sep + RandomColorValue() + sep + RandomColorValue() + sep + RandomColorValue());
                }
                else
                {
                    writer.WriteLine(x + sep + y + sep + z);
                }

                rowCount++;
                if (rowCount >= plyVertexCount) haveMoreToRead = false;
            }

            writer.Close();

            Debug.Log(appName + "> File saved: " + saveFilePath + " (" + plyVertexCount + " points)");
            EditorUtility.ClearProgressBar();
        }

        int RandomColorValue()
        {
            return Random.Range(0, 255);
        }

    } // class
} // namespace
