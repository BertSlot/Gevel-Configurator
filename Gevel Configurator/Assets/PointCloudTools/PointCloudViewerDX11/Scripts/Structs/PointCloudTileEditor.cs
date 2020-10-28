using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
#endif

public struct PointCloudTileEditor
{
    // bounds
    public float minX;
    public float minY;
    public float minZ;
    public float maxX;
    public float maxY;
    public float maxZ;

    // for packed data
    public int cellX;
    public int cellY;
    public int cellZ;

    public Vector3 center;

    public int totalPoints;
    public int loadedPoints;
    public int visiblePoints;

    // TODO no need both
    public bool isLoading;
    public bool isReady;
    public bool isInQueue;

    // viewing
    public Material material;
    public ComputeBuffer bufferPoints;
    public ComputeBuffer bufferColors;
#if UNITY_2019_1_OR_NEWER
    public NativeArray<byte> pointsNative;
    public NativeArray<byte> colorsNative;
#endif
    public Vector3[] points;
    public Vector3[] colors;

    // only used in editor
    public string filename;
    public string filenameRGB;
}