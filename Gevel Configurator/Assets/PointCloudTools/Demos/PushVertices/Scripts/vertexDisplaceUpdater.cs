using UnityEngine;
using System.Collections;

// USAGE: attach to gameobject with special vertex displacement shader, assign displacerObj (shader checks distance to this object)

namespace unitycoder_pointcloud_extras
{

	public class vertexDisplaceUpdater : MonoBehaviour {

		public Transform displacerObj;
		private Material mat;
		
		void Start () 
		{
			mat = GetComponent<Renderer>().sharedMaterial;
		}
		
		void Update () 
		{
			mat.SetVector("_Pos", new Vector4(displacerObj.position.x, displacerObj.position.y, displacerObj.position.z, 1));
		}
	}
}