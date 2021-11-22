using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using SFB;
using unitycoder_examples;

public class SaveLoadManager : MonoBehaviour {

	/// <summary>
	/// The object which contains all scene objects
	/// </summary>
	[SerializeField]
	private GameObject parentObject;

	[SerializeField]
	private SceneObjects SceneManager;

	[SerializeField]
	private AssetSpawner assetSpawner;

	/// <summary>
	/// Used for storing loaded resources
	/// </summary>
	public UnityEngine.Object[] assets;


	/// <summary>
	/// Overcompassing save data class which stores everything  filename, pointcloud, pointcloudPath and object data
	/// </summary>
	[System.Serializable]
	public class SaveData {
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
	/// Custom object for Storing OriginObject data which is almost the same as OriginObject; but has to be different so that JSON.Serializer has a Serializable class to work with.
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
		/// Empty constructor for json deserialization
		/// </summary>
		public OriginData() { }

		/// <summary>
		/// Origin Data Constructor
		/// </summary>
		/// <param name="obj">UnityEngine.GameObject</param>
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
		/// <param name="r">Red color value</param>
		/// <param name="g">Green color value</param>
		/// <param name="b">Blue color value</param>
		/// <param name="a">Alpha value</param>
		public ColorRGBA(float r, float g, float b, float a) {
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

		/// <summary>
		/// ColorRGBA Constructor using UnityEngine.Color
		/// </summary>
		/// <param name="color">UnityEngine.Color object</param>
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
		/// UnityEngine.Color object
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
	/// <param name="rot">UnityEngine.Quaternion Rotation value object </param>
	/// <returns>UnityEngine.Vector4 object</returns>
	public Vector4 QuaternionToVector4(Quaternion rot) {

		return new Vector4() {
			x = rot.x,
			y = rot.y,
			z = rot.z,
			w = rot.w
		};
	}

	/// <summary>
	/// Converts a UnityEngine.Vector4 into a UnityEngine.Quaternion
	/// </summary>
	/// <param name="rot">Rotation values</param>
	/// <returns>UnityEngine.Quaternion</returns>
	public Quaternion Vector4ToQuaternion(Vector4 rot) {

		return new Quaternion() {
			x = rot.x,
			y = rot.y,
			z = rot.z,
			w = rot.w
		};
	}



	[SerializeField]
	private LoadBinCacheIfAvailable PointCloudLoader;

	private string currentSaveFilePath = "";

	/// <summary>
	/// boolean is used to signal if the save function is already in use
	/// </summary>
	private bool isRunning = false;

	// Start is called before the first frame update
	void Start() {
		// pre-load assets into list
		LoadAssets();
	}

	void Update() {
		// Save Shortcuts
		if (UnityEngine.Application.isEditor) {
			if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.A) &&
				Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)) { // editor save as [Ctrl + Shift + S + A]
				SaveFileAsFunc(); // AS
			} else if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)) { // editor save file [Ctrl + Shift + S]
				if (currentSaveFilePath != "") {
					SaveFileFunc();
				} else {
					SaveFileAsFunc(); // AS
				}
			}
		} else {
			if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift) &&
				Input.GetKey(KeyCode.LeftControl)) { // application save as [Ctrl + Shift + S]
				SaveFileAsFunc(); // AS
			} else if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl)) { // application save file [Ctrl + S]
				if (currentSaveFilePath != "") {
					SaveFileFunc();
				} else {
					SaveFileAsFunc(); // AS
				}
			}

		}
	}


	/// <summary>
	/// Save File As function for use with buttons. It starts a Coroutine for SaveFileAs()
	/// </summary>
	public void SaveFileAsFunc() { // buttons can only use void functions so these wrappers are made to allow that while still providing coroutines.
		if (isRunning == false) {
			StartCoroutine(SaveFileAs(GenerateJsonString()));
		}
	}

	/// <summary>
	/// Save File function for use with buttons. It starts a Coroutine for SaveFile(). If there is no project loaded it will call SaveFileAs()
	/// </summary>
	public void SaveFileFunc() { // buttons can only use void functions so these wrappers are made to allow that while still providing coroutines.
		if (isRunning == false) {
			if (currentSaveFilePath != "") { // if there is no project you are working on save current scene to new project.
				StartCoroutine(SaveFile(GenerateJsonString()));
			} else {
				StartCoroutine(SaveFileAs(GenerateJsonString()));
			}
		}
	}

	/// <summary>
	/// Load File Function for use with Buttons. It starts a coroutine for LoadFile()
	/// </summary>
	public void LoadFileFunc() {
		LoadAssets();
		StartCoroutine(LoadFile());
	}


	/// <summary>
	/// SaveFileAs Coroutine function
	/// </summary>
	/// <param name="json">JSON String</param>
	/// <returns></returns>
	public IEnumerator SaveFileAs(string json) {
		isRunning = true;
		string path = StandaloneFileBrowser.SaveFilePanel("Save Project", "", "ProjectName", "json");
		if (path != "") {
			File.WriteAllText(path, json); // write to new file
			currentSaveFilePath = path;
		}
		yield return true;
		isRunning = false;
	}

	/// <summary>
	/// SaveFile Coroutine function
	/// </summary>
	/// <param name="json">JSON String</param>
	/// <returns></returns>
	public IEnumerator SaveFile(string json) { // save to same file as previous
		isRunning = true;
		if (currentSaveFilePath != "") {
			// TODO
			// Might have to so some open file clear and write mumbo jumbo instead
			File.WriteAllText(currentSaveFilePath, json);
		}
		yield return true;
		isRunning = false;
	}

	/// <summary>
	/// LoadFile Coroutine function
	/// </summary>
	/// <param name="json">JSON String</param>
	/// <returns></returns>
	public IEnumerator LoadFile() {
		string[] temp = StandaloneFileBrowser.OpenFilePanel("Load Project File", "", "json", false);
		if (temp.Length != 0) { // When canceled the length is 0
			currentSaveFilePath = temp[0];
			string json = File.ReadAllText(currentSaveFilePath);
			SaveData loadData = JsonConvert.DeserializeObject<SaveData>(json);
			// here goes Pointcloud initialization 
			// different point cloud initialization for web version
			if (loadData.PointcloudPath != "") {
				PointCloudLoader.fileName = loadData.PointcloudPath;
				PointCloudLoader.StartLoadingPointCloud();
			} else {
				MessageBox.Show("No Point Cloud in save file", "Point Cloud Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				// DialogResult = ^^^^^  (Could do something with the DialogResult)
				// Ask if you want to continue without pointcloud or not?

			}

			// remove current objects
			foreach (Transform child in parentObject.transform) {
				SceneManager.RemoveObjectFromObjectListMenu(child.gameObject);
				Destroy(child.gameObject);
			}


			Dictionary<GameObject, ObjectData> LoadingObjects = new Dictionary<GameObject, ObjectData>();

			//Debug.Log(loadData.ObjectDataList.Count);
			// Add all GameObjects to a Dictionary paired with their ObjectData so they can be referenced
			foreach (var objData in loadData.ObjectDataList) {
				LoadingObjects.Add(GetGameObjectFromObjectData(objData), objData);
				// Also instantiates all objects
				// add all stored objects to the list
			}

			// Assign parent objects to all Objects where this is needed
			foreach (KeyValuePair<GameObject, ObjectData> gameObject in LoadingObjects) {
				if (gameObject.Value.parentName != "Objects") {
					// string tempName = gameObject.Value.parentName;
					// This \/ crazy onliner finds the GameObject with the same name as the saved parentName in the ObjectData
					gameObject.Key.transform.SetParent(LoadingObjects.FirstOrDefault(kvp => kvp.Key.name == gameObject.Value.parentName).Key.transform);
				}
			}

		}
		yield return true;
	}


	/// <summary>
	/// Generates a JSON String from all objects under the parentObject
	/// </summary>
	/// <returns> Json string </returns>
	public string GenerateJsonString() {
		SaveData newProject = new SaveData() {
			PointCloudFilename = Path.GetFileName(PointCloudLoader.fileName), // change to only name of the file instead of full path
			PointcloudPath = PointCloudLoader.fileName,
			ObjectDataList = SaveObjectsToList()
		};

		// Convert list of ObjectData to Json String
		return JsonConvert.SerializeObject(newProject, Formatting.Indented);
	}


	/// <summary>
	/// This Function Saves all Objects under the parentObject into a List
	/// </summary>
	/// <returns> List of ObjectData</returns>
	public List<ObjectData> SaveObjectsToList() {

		List<GameObject> ObjList = GetAllChildren(parentObject);
		List<ObjectData> dataList = new List<ObjectData>();

		foreach (GameObject obj in ObjList) {
			dataList.Add(GetObjectData(obj)); // Add ObjectData object to dataList
		}

		return dataList;
	}

	/// <summary>
	/// Given a GameObject this function returns an ObjectData object
	/// </summary>
	/// <param name="obj">UnityEngine.GameObject</param>
	/// <returns>ObjectData object</returns>
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
		} else {// missing color data. assign default color
			data.objectColor = new ColorRGBA(Color.white);
		}
		return data;
	}

	/// <summary>
	/// Pre-Load Assets into Array for use in loading save-files
	/// </summary>
	public void LoadAssets() {
		assets = Resources.LoadAll(assetSpawner.ObjectsFolderPath, typeof(GameObject));
	}

	/// <summary>
	/// Function for obtaining GameObject from ObjectData
	/// </summary>
	/// <param name="objData">Deserialized ObjectData</param>
	/// <returns>UnityEngine.GameObject</returns>
	public GameObject GetGameObjectFromObjectData(ObjectData objData) {

		// usefull for possible oneliner\______________________________________________________________________________/ could fore go the entire foreach loop
		// GameObject obj = Instantiate(assets.FirstOrDefault(asset => asset.name == objData.originData.originObjectName), parentObject.transform) as GameObject;

		foreach (var asset in assets) {
			if (asset.name == objData.originData.originObjectName) {
				// Checks if the name of the origin object is an asset. 
				// There should always be an asset. Only if its a separate parent object it won't.

				GameObject obj = Instantiate(asset, parentObject.transform) as GameObject;

				if (objData.originData.originObjectHasMultipleMeshes) {
					List<GameObject> children = GetAllChildren(obj);
					foreach (GameObject child in children) {
						// For some reason only Destroy makes it work. when using DeleteObject() it deletes the wrong child object with the same name.
						if (child.name == objData.originData.meshName) {
							// Gives the Child the correct name
							child.name = objData.objectName;
							// UnParent the childobject and set them under the Objects parentObject
							Destroy(obj);
							child.transform.SetParent(parentObject.transform);
							// Add Child to the sceneObjectsList
							obj = child;
							break;
						} else {
							//Debug.Log("annihilate : " + child.name);
							Destroy(child);
						}
					}
				}

				//Debug.Log("Instantiate object");

				if (obj.GetComponent<Renderer>() != null) { // sets color if an asset and render component is found
					obj.GetComponent<Renderer>().material.color = objData.objectColor.GetColor();
					// Other data related to Rendering the object has to be placed here
					// Stuff Like:
					// - Background Image
					// - Smoothness
					// - Metallic
				}

				obj.name = objData.objectName;
				obj.transform.localPosition = objData.position;
				obj.transform.localRotation = Vector4ToQuaternion(objData.rotation);
				obj.transform.localScale = objData.scale;

				OriginObject origin;
				if (obj.GetComponent<OriginObject>() == null) {
					origin = obj.AddComponent<OriginObject>();
				} else {
					origin = obj.GetComponent<OriginObject>();
				}

				origin.originObjectName = objData.originData.originObjectName;
				origin.originObjectHasMultipleMeshes = objData.originData.originObjectHasMultipleMeshes;
				origin.meshName = objData.originData.meshName;

				// And lastly add the object to the scene list
				SceneManager.AddObjectToObjectListMenu(obj);
				return obj;
			}
		}
		// If for some reason there is no asset for the origin object
		// Debug.Log(obj.name);
		Debug.Log("No Asset Found for:" + objData.originData.originObjectName);
		return new GameObject("No Asset Found for:" + objData.objectName);
	}

	/// <summary>
	/// This function returns all Children objects of the given object
	/// </summary>
	/// <param name="gameObject">UnityEngine.GameObject with Child Objects</param>
	/// <returns>List of Child objects</returns>
	public List<GameObject> GetAllChildren(GameObject gameObject) {
		Transform[] childTransforms = gameObject.GetComponentsInChildren<Transform>();
		var allChildren = new List<GameObject>(childTransforms.Length);

		foreach (var child in childTransforms) {
			if (child.gameObject != gameObject)
				allChildren.Add(child.gameObject);
		}

		return allChildren;
	}

};

