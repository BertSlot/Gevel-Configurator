// single node box
using System.Collections.Generic;
using UnityEngine;

public struct NodeBox
{
    public List<int> points;// = new List<int>();
    // TODO replace bounds
    // TODO: remove nodes with 0 or few points!
    public Bounds bounds;
    // node box bounds
    /*
    public float minX;
    public float minY;
    public float minZ;
    public float maxX;
    public float maxY;
    public float maxZ;*/
}