using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PointCloudHelpers
{
    public static class PointCloudTools
    {

        /// <summary>
        /// checks if file exists on given path
        /// </summary>
        /// <param name="fileToRead">full path to file</param>
        /// <returns></returns>
        public static bool CheckIfFileExists(string fileToRead)
        {
            return File.Exists(fileToRead);
        }

        // randomize array https://stackoverflow.com/a/110570/5452781
        public static void Shuffle<T>(System.Random rng, ref T[] array, ref T[] array2)
        {
            int index = array.Length;
            while (index > 1)
            {
                int rnd = rng.Next(index--);

                T temp = array[index];
                array[index] = array[rnd];
                array[rnd] = temp;

                T temp2 = array2[index];
                array2[index] = array2[rnd];
                array2[rnd] = temp2;
            }
        }

        public static void Shuffle<T>(System.Random rng, ref T[] array1, ref T[] array2, ref T[] array3, ref T[] arrayR, ref T[] arrayG, ref T[] arrayB)
        {
            int index = array1.Length;
            while (index > 1)
            {
                int rnd = rng.Next(index--);

                T temp = array1[index];
                array1[index] = array1[rnd];
                array1[rnd] = temp;

                T temp2 = array2[index];
                array2[index] = array2[rnd];
                array2[rnd] = temp2;

                T temp3 = array3[index];
                array3[index] = array3[rnd];
                array3[rnd] = temp3;

                T tempR = arrayR[index];
                arrayR[index] = arrayR[rnd];
                arrayR[rnd] = tempR;

                T tempG = arrayG[index];
                arrayG[index] = arrayG[rnd];
                arrayG[rnd] = tempG;

                T tempB = arrayB[index];
                arrayB[index] = arrayB[rnd];
                arrayB[rnd] = tempB;
            }
        }

        // shuffle list
        public static void Shuffle(System.Random rng, ref List<Vector3> array, ref List<Vector3> array2)
        {
            for (int index = 0, length = array.Count; index < length; index++)
            {
                int rnd = rng.Next(index);

                var temp = array[index];
                array[index] = array[rnd];
                array[rnd] = temp;

                var temp2 = array2[index];
                array2[index] = array2[rnd];
                array2[rnd] = temp2;
            }
        }


        public static bool IsFirstCharacter(string source, char toFind)
        {
            if (source == null || source.Length == 0) return false;
            return source[0] == toFind;
        }


        // returns currently assigned filepath (not necessarily same as currently loaded cloud, if you have modified the path variables)
        public static string GetCurrentFilePath(string baseFolder, string fileName)
        {
            return Path.Combine(Application.dataPath, baseFolder + fileName);
        }

        // draw bounds with Debug.DrawLines
        public static void DrawBounds(Bounds b, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }

        public static void DrawMinMaxBounds(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(minX, minY, minZ);
            var p2 = new Vector3(maxX, minY, minZ);
            var p3 = new Vector3(maxX, minY, maxZ);
            var p4 = new Vector3(minX, minY, maxZ);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(minX, maxY, minZ);
            var p6 = new Vector3(maxX, maxY, minZ);
            var p7 = new Vector3(maxX, maxY, maxZ);
            var p8 = new Vector3(minX, maxY, maxZ);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }

        // https://stackoverflow.com/a/48000498/5452781
        public static string HumanReadableCount(this long num)
        {
            if (num > 999999999 || num < -999999999)
            {
                return num.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999999 || num < -999999)
            {
                return num.ToString("0,,.##M", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999 || num < -999)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }

    }
}
