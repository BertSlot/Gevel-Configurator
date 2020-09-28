using UnityEngine;
using System.Collections;

// USAGE: attach to mesh object, assign lightobject (can be empty gameobject), use shader which has the "_LightPos" vector

namespace unitycoder_pointcloud_extras
{

	public class LightSetter : MonoBehaviour 
	{
		public Transform lightObj;

		Material mat;

		void Start()
		{
			mat = GetComponent<Renderer>().sharedMaterial;
		}

		void LateUpdate () 
		{
			mat.SetVector("_LightPos", new Vector4(lightObj.position.x, lightObj.position.y, lightObj.position.z, 1));
		}
	}

}