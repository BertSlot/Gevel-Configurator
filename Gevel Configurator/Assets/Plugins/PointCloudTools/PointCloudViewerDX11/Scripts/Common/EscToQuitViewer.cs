using UnityEngine;
using System.Collections;

namespace unitycodercom_pointcloud_extras
{

	public class EscToQuitViewer : MonoBehaviour {


		
		// Update is called once per frame
		void Update () {
		
			if (Input.GetKeyDown("escape"))
			{
				Application.Quit();
			}

		}
	}
}