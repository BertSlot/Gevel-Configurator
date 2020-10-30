// math helpers

using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

public static class PointCloudMath
{

    public static readonly int seed = System.Guid.NewGuid().GetHashCode();
    public static System.Random rnd = new System.Random(seed);
    public static void ResetRandom()
    {
        rnd = new System.Random(seed);
    }

    // https://stackoverflow.com/a/110570/5452781
    public static void ShuffleXYZ<T>(System.Random rng, ref T[] array1)
    {
        int n = array1.Length;
        int maxVal = array1.Length / 3; // xyz key

        while (n > 3)
        {
            int k = rng.Next(maxVal) * 3; // multiples of 3 only
            n -= 3;

            T tempX = array1[n];
            array1[n] = array1[k];
            array1[k] = tempX;

            T tempY = array1[n + 1];
            array1[n + 1] = array1[k + 1];
            array1[k + 1] = tempY;

            T tempZ = array1[n + 2];
            array1[n + 2] = array1[k + 2];
            array1[k + 2] = tempZ;
        }
    }

    // https://gamedev.stackexchange.com/a/103714/73429
    public static float RayBoxIntersect2(Vector3 rpos, Vector3 irdir, Vector3 vmin, Vector3 vmax)
    {
        float t1 = (vmin.x - rpos.x) * irdir.x;
        float t2 = (vmax.x - rpos.x) * irdir.x;
        float t3 = (vmin.y - rpos.y) * irdir.y;
        float t4 = (vmax.y - rpos.y) * irdir.y;
        float t5 = (vmin.z - rpos.z) * irdir.z;
        float t6 = (vmax.z - rpos.z) * irdir.z;
        float aMin = t1 < t2 ? t1 : t2;
        float aMax = t1 > t2 ? t1 : t2;
        float bMin = t3 < t4 ? t3 : t4;
        float bMax = t3 > t4 ? t3 : t4;
        float cMin = t5 < t6 ? t5 : t6;
        float cMax = t5 > t6 ? t5 : t6;
        float fMax = aMin > bMin ? aMin : bMin;
        float fMin = aMax < bMax ? aMax : bMax;
        float t7 = fMax > cMin ? fMax : cMin;
        float t8 = fMin < cMax ? fMin : cMax;
        float t9 = (t8 < 0 || t7 > t8) ? -1 : t7;
        return t9;
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        float vecx = a.x - b.x;
        float vecy = a.y - b.y;
        float vecz = a.z - b.z;
        return vecx * vecx + vecy * vecy + vecz * vecz;
    }

    const float lineLen = 0.25f;

    public static void DebugHighLightPointGray(System.Object op)
    {
        var p = (Vector3)op;
        var c = Color.gray;
        Debug.DrawRay(p, Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.forward * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.forward * lineLen, c, 33);
    }

    public static void DebugHighLightPointGreen(System.Object op)
    {
        var p = (Vector3)op;
        var c = Color.green;
        Debug.DrawRay(p, Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.forward * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.forward * lineLen, c, 33);
    }

    public static void DebugHighLightPointRed(System.Object op)
    {
        var p = (Vector3)op;
        var c = Color.red;
        Debug.DrawRay(p, Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.forward * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.forward * lineLen, c, 33);
    }


    public static void DebugHighLightPointYellow(System.Object op)
    {
        var p = (Vector3)op;
        var c = Color.yellow;
        Debug.DrawRay(p, Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.up * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.right * lineLen, c, 33);
        Debug.DrawRay(p, Vector3.forward * lineLen, c, 33);
        Debug.DrawRay(p, -Vector3.forward * lineLen, c, 33);
    }

    //unsafe public static float BytesToFloat(byte[] value, int startIndex)
    //{
    //    int val = value[startIndex] | value[startIndex + 1] << 8 | value[startIndex + 2] << 16 | value[startIndex + 3] << 24;
    //    return *(float*)&val;
    //}

    //unsafe public static int BytesToInt(byte[] value, int startIndex)
    //{
    //    return value[startIndex] | value[startIndex + 1] << 8 | value[startIndex + 2] << 16 | value[startIndex + 3] << 24;
    //}

    public static Vector2 SuperUnpacker(float f, float _GridSizeAndPackMagic)
    {
        return new Vector2(f - Mathf.Floor(f), Mathf.Floor(f) / _GridSizeAndPackMagic);
    }

#if UNITY_2019_1_OR_NEWER
    public unsafe static void MoveFromByteArray<T>(ref byte[] src, ref NativeArray<T> dst) where T : struct
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(dst));
        if (src == null)
            throw new System.ArgumentNullException(nameof(src));
#endif
        //            var size = UnsafeUtility.SizeOf<T>();
        //            if (src.Length != (size * dst.Length))
        //            {
        //                dst.Dispose();
        //                dst = new NativeArray<T>(src.Length / size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        //#if ENABLE_UNITY_COLLECTIONS_CHECKS
        //                AtomicSafetyHandle.CheckReadAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(dst));
        //#endif
        //            }

        var dstAddr = (byte*)dst.GetUnsafeReadOnlyPtr();
        fixed (byte* srcAddr = src)
        {
            UnsafeUtility.MemCpy(&dstAddr[0], &srcAddr[0], src.Length);
        }
    }
#endif

}
