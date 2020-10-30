using UnityEngine;
using System.Collections;

namespace unitycoder_extras
{
    public class PointMeshEnable : MonoBehaviour
    {
        public bool usePoints = false;

        void Start()
        {
            usePointMesh();
        }

        void usePointMesh()
        {
            if (usePoints == true)
            {
                Mesh mesh = GetComponent<MeshFilter>().mesh;
                int[] tris = mesh.triangles;
                mesh.SetIndices(tris, MeshTopology.Points, 0);
            }
        }
    }
}
