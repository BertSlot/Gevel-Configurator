using UnityEngine;

public struct Cloud
{
    public string id; // for now its file path as an identifier, FIXME TODO?
    public int viewerIndex; // which viewer has this cloud belongs to, would this work?
    public Bounds bounds; // root whole cloud bounds, or reference to viewer? (to get bounds from that, but later want meshes also..)
    public NodeBox[] nodes; // cloud child box nodes
}