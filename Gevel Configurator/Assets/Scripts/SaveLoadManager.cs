using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class SaveLoadManager : MonoBehaviour {

	/// <summary>
	/// The object which contains all scene objects
	/// </summary>
	[SerializeField]
	private GameObject parentObject;

	/// <summary>
	/// Overcompassing save data class which stores everything  filename, pointcloud, pointcloudPath and object data
	/// </summary>
	[System.Serializable]
	public class SaveData {
		public string SaveFileName;         // project name
		public string PointCloudFilename;   // for use on the webserver
		public string PointcloudPath;       // together with the filename will allow for ease of use in the standalone app
		public List<ObjectData> ObjectDataList;
		// optional other settings below

	}


	/// <summary>
	/// Internal ObjectData Class for storing The data of a placed object. This data is what will be stored in the JSON savefile.
	/// </summary>
	[System.Serializable]
	public class ObjectData {
		public string objectName;
		public string parentName;
		public OriginData originData;
		public Vector3 position;
		public Vector4 rotation;
		public Vector3 scale;
		public ColorRGBA objectColor;
	}

	/// <summary>
	/// Custom object for Storing OriginObject data
	/// </summary>
	[System.Serializable]
	public class OriginData {

		/// <summary>
		/// Name of the Origin Object
		/// </summary>
		public string originObjectName;
		/// <summary>
		/// If the OriginObject has multiple meshes within it it requires different loading since it will be loaded twice otherwise
		/// </summary>
		public bool originObjectHasMultipleMeshes = false;

		/// <summary> 
		/// Name of the child mesh from the origin object with multiple meshes
		/// </summary>
		public string meshName;

		/// <summary>
		/// Origin Data Constructor
		/// </summary>
		/// <param name="obj"></param>
		public OriginData(GameObject obj) {
			if (obj.GetComponent<OriginObject>() != null) { // uses the origin data in the object
				OriginObject oriData = obj.GetComponent<OriginObject>();
				originObjectName = oriData.originObjectName;
				originObjectHasMultipleMeshes = oriData.originObjectHasMultipleMeshes;
				meshName = oriData.meshName;

			} else { // tries to create an origin
				originObjectName = null;
				originObjectHasMultipleMeshes = false;
				if (obj.GetComponent<MeshFilter>() != null)// for the directional light
					meshName = obj.GetComponent<MeshFilter>().name;
				else // this case only happens if it is not meant to be saved or loaded
					meshName = null;

			}
		}

	}
	/// <summary>
	/// Data structure for saving Color that the JSON converter can support. The normal Color() structure causes indefinite loop during conversion.
	/// </summary>
	[System.Serializable]
	public struct ColorRGBA {
		// Color Variables
		public float r;
		public float g;
		public float b;
		public float a;

		/// <summary>
		/// ColorRGBA Constructor
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <param name="a"></param>
		public ColorRGBA(float r, float g, float b, float a) {
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

		/// <summary>
		/// ColorRGBA Constructor using UnityEngine.Color
		/// </summary>
		/// <param name="color"></param>
		public ColorRGBA(Color color) {
			this.r = color.r;
			this.g = color.g;
			this.b = color.b;
			this.a = color.a;
		}

		/// <summary>
		/// This function returns a UnityEngine.Color struct instead for ease of use.
		/// </summary>
		/// <returns>
		/// UnityEngine.Color
		/// </returns>
		public Color GetColor() {
			return new Color() {
				r = this.r,
				g = this.g,
				b = this.b,
				a = this.a
			};
		}

	}

	/// <summary>
	/// Converts a Quaternion struct to a Vector4 so it can be stored in JSON format
	/// </summary>
	/// <param name="rot"></param>
	/// <returns></returns>
	public Vector4 QuaternionToVector4(Quaternion rot) {

		return new Vector4() {
			x = rot.x,
			y = rot.y,
			z = rot.z,
			w = rot.w
		};
	}

	// Start is called before the first frame update
	void Start() {

		// Save to JSON test. not final
		SaveData newProject = new SaveData() {
			SaveFileName = "newProject",
			PointCloudFilename = "pCfilename",
			PointcloudPath = "path",
			ObjectDataList = SaveObjectsToList()
		};


		// Convert list of ObjectData to Json String
		string json = JsonConvert.SerializeObject(newProject, Formatting.Indented);
		Debug.Log(json);
	}


	/// <summary>
	/// This Function Saves all Objects under the parentObject into a List
	/// </summary>
	/// <returns></returns>
	public List<ObjectData> SaveObjectsToList() {

		List<GameObject> ObjList = GetAllChildren(parentObject);
		List<ObjectData> dataList = new List<ObjectData>();

		foreach (GameObject obj in ObjList) {
			dataList.Add(GetObjectData(obj));
		}

		return dataList;
	}

	/// <summary>
	/// This Function returns all Children objects of the given object
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public List<GameObject> GetAllChildren(GameObject gameObject) {
		Transform[] childTransforms = gameObject.GetComponentsInChildren<Transform>();
		var allChildren = new List<GameObject>(childTransforms.Length);

		foreach (var child in childTransforms) {
			if (child.gameObject != gameObject)
				allChildren.Add(child.gameObject);
		}

		return allChildren;
	}

	/// <summary>
	/// Given a GameObject this function returns an ObjectData object
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public ObjectData GetObjectData(GameObject obj) {

		ObjectData data = new ObjectData();
		data.objectName = obj.name;
		data.parentName = obj.transform.parent.name;
		data.originData = new OriginData(obj);
		data.position = obj.transform.localPosition;
		data.rotation = QuaternionToVector4(obj.transform.localRotation);
		data.scale = obj.transform.localScale;

		if (obj.GetComponent<Renderer>() != null) {
			data.objectColor = new ColorRGBA(obj.GetComponent<Renderer>().material.color);
		} else {
			// missing color data. assign default color
			data.objectColor = new ColorRGBA(Color.white);
		}
		return data;
	}

};

