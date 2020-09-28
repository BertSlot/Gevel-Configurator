using UnityEngine;

namespace UnityLibrary
{
    public class CullingGroupExample : MonoBehaviour
    {
        // distance to search objects from
        public float searchDistance = 10;

        // collection of objects
        Renderer[] objects;
        CullingGroup cullGroup;
        BoundingSphere[] bounds;

        public Transform cloudRoot;

        public float[] distances = new float[] { 10, float.PositiveInfinity };

        //public Gradient distanceColor;
        public Color[] distanceColors = new Color[] { Color.green, Color.red };
        public Color culledColor = Color.gray;

        bool colorInvisibleObjects = true;

        void Start()
        {
            // create culling group
            cullGroup = new CullingGroup();
            cullGroup.targetCamera = Camera.main;

            // measure distance to our transform
            cullGroup.SetDistanceReferencePoint(transform);

            // search distance "bands" starts from 0, so index=0 is from 0 to searchDistance
            cullGroup.SetBoundingDistances(distances);

            // get cloud pieces
            int objectCount = cloudRoot.childCount;
            bounds = new BoundingSphere[objectCount];

            // collect meshes (TODO getallchild renderers)
            objects = new Renderer[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                var go = cloudRoot.GetChild(i).gameObject;
                objects[i] = go.GetComponent<Renderer>();
                // collect bounds for objects
                var b = new BoundingSphere();
                b.position = go.transform.position;

                // get mesh bounds radius, as spherical
                var mb = go.GetComponent<MeshFilter>().mesh.bounds;
                var bx = mb.extents.x;
                var by = mb.extents.y;
                var bz = mb.extents.z;

                b.radius = Mathf.Max(bx, Mathf.Max(by, bz));
                bounds[i] = b;
            }

            // set bounds that we track
            cullGroup.SetBoundingSpheres(bounds);
            cullGroup.SetBoundingSphereCount(objects.Length);

            // subscribe to event
            cullGroup.onStateChanged += StateChanged;
        }

        // object state has changed in culling group
        void StateChanged(CullingGroupEvent e)
        {
            if (colorInvisibleObjects == true && e.isVisible == false)
            {
                objects[e.index].material.color = culledColor;
                return;
            }
            /*
            // if we are in distance band index 0, that is between 0 to searchDistance
            if (e.currentDistance == 0)
            {
                objects[e.index].material.color = Color.green;
            }
            else // too far, set color to red
            {
                objects[e.index].material.color = Color.red;
            }*/

            int distanceBand = e.currentDistance;
            if (distanceBand > distanceColors.Length - 1) distanceBand = distanceColors.Length - 1;

            objects[e.index].material.color = distanceColors[distanceBand];



        }

        // cleanup
        private void OnDestroy()
        {
            cullGroup.onStateChanged -= StateChanged;
            cullGroup.Dispose();
            cullGroup = null;
        }

    }
}