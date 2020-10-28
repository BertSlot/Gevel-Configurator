using UnityEngine;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace unitycodercom_PointCloudHelpers
{
    public class PeekHeader
    {
        public static PeekHeaderData PeekHeaderASC(StreamReader reader, bool readRGB)
        {
            PeekHeaderData ph = new PeekHeaderData();
            string line = "";
            bool comments = true;

            while (comments == true && !reader.EndOfStream)
            {
                line = reader.ReadLine().Replace("   ", " ").Replace("  ", " ").Trim();
                ph.linesRead++;
                if (line.StartsWith("#") || line.StartsWith("!")) // temporary fix for Geomagic asc
                {
                    // still comments
                }
                else
                {
                    comments = false;
                }
            }

            string[] row = line.Split(' ');

            if (readRGB) { if (row.Length < 4) { Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false; } }

            // check for CatiaASC
            if (line.ToLower().StartsWith("x")) { Debug.LogError("This looks like CATIA Asc data, but you have selected 'ASC' input file format instead"); ph.readSuccess = false; return ph; }

            ph.x = double.Parse(row[0]);
            ph.y = double.Parse(row[1]);
            ph.z = double.Parse(row[2]);
            ph.readSuccess = true;

            return ph;
        }

        public static PeekHeaderData PeekHeaderCGO(StreamReader reader, bool readRGB)
        {
            PeekHeaderData ph = new PeekHeaderData();

            string line = reader.ReadLine(); // cgo first line should have point count
            line = line.Replace("   ", " ").Replace("  ", " ").Trim();
            line = reader.ReadLine(); // get sample data from first line
            ph.linesRead++;
            if (IsNullOrEmptyLine(line)) { ph.readSuccess = false; return ph; }
            string[] row = line.Split(' ');
            if (readRGB) { if (row.Length < 4) { Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false; } }
            ph.x = double.Parse(row[0].Replace(",", "."));
            ph.y = double.Parse(row[1].Replace(",", "."));
            ph.z = double.Parse(row[2].Replace(",", "."));
            ph.readSuccess = true;

            return ph;
        }

        public static PeekHeaderData PeekHeaderPCD(StreamReader reader, ref bool readRGB, ref long masterPointCount)
        {
            PeekHeaderData ph = new PeekHeaderData();

            //# .PCD v.7 - Point Cloud Data file format
            //VERSION .7
            //FIELDS x y z rgb
            //SIZE 4 4 4 4
            //TYPE F F F F
            //COUNT 1 1 1 1
            //WIDTH 213
            //HEIGHT 1
            //VIEWPOINT 0 0 0 1 0 0 0
            //POINTS 213
            //DATA ascii

            string line = "";
            line = reader.ReadLine();
            if (line.StartsWith("#") == true)
            {
                ph.linesRead++;
            }

            // version
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.Contains(".7") == false)
            {
                Debug.LogWarning("Only version v0.7 is tested.. your file version is: " + line);
            }
            // fields
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.Contains("rgb") == false && line.Contains("r g b") == false) readRGB = false;
            // size
            line = reader.ReadLine();
            ph.linesRead++;
            // type
            line = reader.ReadLine();
            ph.linesRead++;
            // count
            line = reader.ReadLine();
            ph.linesRead++;
            // width
            line = reader.ReadLine();
            ph.linesRead++;
            // heigth
            line = reader.ReadLine();
            ph.linesRead++;
            // viewpoint
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.ToLower().Contains("viewpoint"))
            {
                // points
                line = reader.ReadLine();
                ph.linesRead++;
            }
            else // no viewpoint line, probably v0.5
            {
                // then this line was points
            }
            line = line.Replace("POINTS ", "").Trim();
            if (!long.TryParse(line, out masterPointCount))
            {
                Debug.LogError("Failed to read point count from PCD file");
                ph.readSuccess = false; return ph;
            }
            // datatype
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.Contains("ascii") == false)
            {
                Debug.LogError("Only ascii PCD files are currently supported..");
            }
            // first data row
            line = reader.ReadLine();
            ph.linesRead++;
            string[] row = line.Split(' ');
            if (readRGB == true) { if (row.Length < 4) { Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false; } }
            //if (readIntensity) { if (row.Length < 4) { Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false; } }
            ph.x = double.Parse(row[0].Replace(",", "."));
            ph.y = double.Parse(row[1].Replace(",", "."));
            ph.z = double.Parse(row[2].Replace(",", "."));
            ph.readSuccess = true;
            return ph;
        }

        public static PeekHeaderData PeekHeaderCGO(StreamReader reader, ref bool readRGB, bool readIntensity, ref long masterPointCount)
        {
            PeekHeaderData ph = new PeekHeaderData();
            string line = reader.ReadLine(); // first line is point count
            ph.linesRead++;

            line = line.Replace(",", ".").Replace("   ", " ").Replace("  ", " ").Trim();
            line = Regex.Replace(line, "[^0-9]", ""); // remove non-numeric chars

            if (IsNullOrEmptyLine(line)) { ph.readSuccess = false; return ph; }

            // try parse first line
            if (!long.TryParse(line, out masterPointCount))
            {
                Debug.LogError("Failed to read point count from PTS file");
                ph.readSuccess = false; return ph;
            }

            line = reader.ReadLine(); // first actual line
            ph.linesRead++;

            line = line.Replace(",", ".").Replace("   ", " ").Replace("  ", " ").Trim();

            string[] row = line.Split(' ');
            //			Debug.Log(row.Length);

            if (readRGB == true)
            {
                if (row.Length < 6)
                {
                    Debug.LogError("No RGB data founded, disabling readRGB");
                    readRGB = false;
                }
            }
            if (readIntensity == true) { if (row.Length != 4 && row.Length != 7) { Debug.LogError("No Intensity data founded, disabling readIntensity"); readIntensity = false; } }

            // take first point pos
            ph.x = double.Parse(row[0]);
            ph.y = double.Parse(row[1]);
            ph.z = double.Parse(row[2]);

            ph.readSuccess = true;

            return ph;
        }


        public static PeekHeaderData PeekHeaderCATIA_ASC(StreamReader reader, ref bool readRGB)
        {
            PeekHeaderData ph = new PeekHeaderData();
            string line = reader.ReadLine(); // first lines are not used
            line = line.Replace("   ", " ").Replace("  ", " ").Trim();
            line = reader.ReadLine(); // 2
            line = reader.ReadLine(); // 3
            line = reader.ReadLine(); // 4
            line = reader.ReadLine(); // 5
            line = reader.ReadLine(); // 6
            line = reader.ReadLine(); // 7
            line = reader.ReadLine(); // 8

            line = reader.ReadLine(); // first actual line
            ph.linesRead++;

            if (IsNullOrEmptyLine(line)) { ph.readSuccess = false; return ph; }
            string[] row = line.Split(' ');
            if (readRGB) { if (row.Length < 11) { Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false; } }
            ph.x = double.Parse(row[1]);
            ph.y = double.Parse(row[3]);
            ph.z = double.Parse(row[5]);
            ph.readSuccess = true;

            return ph;
        }

        public static PeekHeaderData PeekHeaderXYZ(StreamReader reader, ref bool readRGB)
        {
            PeekHeaderData ph = new PeekHeaderData();

            string line = reader.ReadLine(); // first actual line
            line = line.Replace("   ", " ").Replace("  ", " ").Trim();
            ph.linesRead++;

            // check if first line is NOT empty
            if (IsNullOrEmptyLine(line)) { ph.readSuccess = false; return ph; }

            string[] row = line.Split(' ');
            if (readRGB) { if (row.Length < 6) { Debug.LogError("No RGB data founded after XYZ, disabling readRGB"); readRGB = false; } }

            ph.readSuccess = true;

            if (row.Length < 3)
            {
                Debug.LogError("No XYZ data founded on first line, maybe you have selected wrong input file format");
                ph.readSuccess = false;
            }
            else
            {
                if (double.TryParse(row[0], out ph.x) == false) ph.readSuccess = false;
                if (double.TryParse(row[1], out ph.y) == false) ph.readSuccess = false;
                if (double.TryParse(row[2], out ph.z) == false) ph.readSuccess = false;
            }

            return ph;
        }

        public static PeekHeaderData PeekHeaderPTS(StreamReader reader, bool readRGB, bool readIntensity, ref long masterPointCount)
        {
            PeekHeaderData ph = new PeekHeaderData();
            string line = reader.ReadLine(); // first line is point count
            ph.linesRead++;

            line = line.Replace("   ", " ").Replace("  ", " ").Trim();
            line = line.Replace(',', '.');
            line = Regex.Replace(line, "[^0-9]", ""); // remove non-numeric chars

            if (IsNullOrEmptyLine(line)) { ph.readSuccess = false; return ph; }

            // try parse first line
            if (!long.TryParse(line, out masterPointCount))
            {
                Debug.LogError("Failed to read point count from PTS file");
                ph.readSuccess = false; return ph;
            }

            line = reader.ReadLine(); // first actual line
            ph.linesRead++;

            line = line.Replace("   ", " ").Replace("  ", " ").Trim();
            line = line.Replace(',', '.');

            string[] row = line.Split(' ');

            if (readRGB) { if (row.Length < 6) { Debug.LogError("No RGB data founded, disabling readRGB"); readRGB = false; } }
            if (readIntensity) { if (row.Length != 4 && row.Length != 7) { Debug.LogError("No Intensity data founded, disabling readIntensity"); readIntensity = false; } }

            // take first point pos
            ph.x = double.Parse(row[0], CultureInfo.InvariantCulture);
            ph.y = double.Parse(row[1], CultureInfo.InvariantCulture);
            ph.z = double.Parse(row[2], CultureInfo.InvariantCulture);

            ph.readSuccess = true;

            return ph;
        }

        public static PeekHeaderData PeekHeaderPLY(StreamReader reader, bool readRGB, ref long masterPointCount, ref bool plyHasNormals, ref bool plyHasDensity)
        {
            PeekHeaderData ph = new PeekHeaderData();
            plyHasDensity = false;
            string line = reader.ReadLine();
            ph.linesRead++;
            line = line.Replace("   ", " ").Replace("  ", " ").Trim();
            // is this ply
            if (line.ToLower() != "ply")
            {
                Debug.LogWarning("Header error #1: not 'ply'");
                ph.readSuccess = false;
                return ph;
            }

            line = reader.ReadLine();
            ph.linesRead++;
            // is this ascii ply
            if (line.Contains("format ascii") == false)
            {
                Debug.LogWarning("(PLY) Header error #2: Binary format is not supported.");
                ph.readSuccess = false;
                return ph;
            }

            // look for element vertex count, try max 100 rows, or until end header
            bool foundVertexCount = false;
            for (int i = 0, len = 100; i < len; i++)
            {
                line = reader.ReadLine();
                ph.linesRead++;
                if (string.IsNullOrEmpty(line) == false)
                {
                    if (line.ToLower().Contains("element vertex"))
                    {
                        // this has vertex count, lets exit
                        foundVertexCount = true;
                        break;
                    }

                    if (reader.EndOfStream == true)
                    {
                        Debug.LogError("PLY Header: Unexpected end of header (1).. Failed!");
                        ph.readSuccess = false;
                        return ph;
                    }
                }
            }

            if (foundVertexCount == false)
            {
                Debug.LogError("PLY Header: Cannot find row 'element vertex .....' - Import failed!");
                ph.readSuccess = false;
                return ph;
            }

            string[] row = line.Split(' ');
            masterPointCount = long.Parse(row[2], NumberStyles.Integer);
            Debug.Log("(Converter) Reading " + masterPointCount + " points..");

            // loop until x,y,z data
            bool foundXYZ = false;
            for (int i = 0, len = 100; i < len; i++)
            {
                line = reader.ReadLine();
                ph.linesRead++;
                if (string.IsNullOrEmpty(line) == false)
                {
                    if (line.ToLower().Contains("property float x") || line.ToLower().Contains("property double x"))
                    {
                        foundXYZ = true;
                        break;
                    }

                    if (reader.EndOfStream == true)
                    {
                        Debug.LogError("PLY Header: Unexpected end of header (2).. Failed!");
                        ph.readSuccess = false;
                        return ph;
                    }
                }
            }

            if (foundXYZ == false)
            {
                Debug.LogError("PLY Header: Cannot find XYZ properties in header - Import failed!");
                ph.readSuccess = false;
                return ph;
            }

            if (masterPointCount < 1) { Debug.LogError("Header error #3: ply vertex count < 1"); ph.readSuccess = false; return ph; }

            // check properties
            if (line.ToLower() != "property float x" && line.ToLower() != "property double x") { Debug.LogError("Header error #4a: property x error"); ph.readSuccess = false; return ph; }
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.ToLower() != "property float y" && line.ToLower() != "property double y") { Debug.LogError("Header error #4b: property y error"); ph.readSuccess = false; return ph; }
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.ToLower() != "property float z" && line.ToLower() != "property double z") { Debug.LogWarning("Header error #4c: property z error"); ph.readSuccess = false; return ph; }

            // check if density row
            line = reader.ReadLine();
            ph.linesRead++;
            if (line.ToLower() == "property float density")
            {
                // skip density row
                plyHasDensity = true;
                line = reader.ReadLine();
                ph.linesRead++;
            }

            if (line.ToLower() == "property float nx")
            {
                plyHasNormals = true;

                line = reader.ReadLine(); // ny
                line = reader.ReadLine(); // nz
                ph.linesRead++;
                ph.linesRead++;

                // rgb
                line = reader.ReadLine();
                ph.linesRead++;
                if (line.ToLower() == "property uchar red")
                {
                    // yes, take other lines out also
                    line = reader.ReadLine(); // g
                    line = reader.ReadLine(); // b
                    line = reader.ReadLine(); // a
                    ph.linesRead++;
                    ph.linesRead++;
                    ph.linesRead++;
                }
                else
                { // no color vals
                    readRGB = false;
                }

                // face elements (not used)
                line = reader.ReadLine();
                ph.linesRead++;
            }
            else
            { // no normals

                if (line.ToLower() == "property uchar red")
                {
                    // yes, take other lines out also
                    line = reader.ReadLine(); // g
                    line = reader.ReadLine(); // b
                    line = reader.ReadLine(); // a
                    ph.linesRead++;
                    ph.linesRead++;
                    ph.linesRead++;
                    // face elements (not used)
                }
                else
                { // no color vals or normals either
                    Debug.LogWarning("PLY Header: No RGB data found in header, disabling RGB import");
                    readRGB = false;
                }
            }

            // property list title (not used)

            // look for end header
            bool foundedEndHeader = false;
            var maxRowsToCheck = ph.linesRead + 50;

            while (ph.linesRead < maxRowsToCheck && reader.EndOfStream == false)
            {
                if (line.ToLower().IndexOf("end_header") > -1)
                {
                    foundedEndHeader = true;
                    break;
                }
                line = reader.ReadLine().Replace("   ", " ").Replace("  ", " ").Trim();
                ph.linesRead++;
            }

            if (foundedEndHeader == false)
            {
                Debug.LogWarning("Header error #5: 'end_header' not found in correct place");
                ph.readSuccess = false;
                return ph;
            }

            // read first line to get data
            line = reader.ReadLine(); // x y z r g b a
            ph.linesRead++;

            if (IsNullOrEmptyLine(line)) { ph.readSuccess = false; return ph; }
            row = line.Split(' ');
            if (readRGB) { if (row.Length < 5) { Debug.LogError("No RGB data founded, disabling readRGB!"); readRGB = false; } }

            ph.x = double.Parse(row[0], CultureInfo.InvariantCulture);
            ph.y = double.Parse(row[1], CultureInfo.InvariantCulture);
            ph.z = double.Parse(row[2], CultureInfo.InvariantCulture);
            ph.readSuccess = true;
            ph.hasRGB = readRGB;

            return ph;
        }

        public static bool IsNullOrEmptyLine(string line)
        {
            if (line.Length < 1 || line == null || line == string.Empty) { Debug.LogError("First line of the file is empty..quitting!"); return true; }
            return false;
        }

    }
}