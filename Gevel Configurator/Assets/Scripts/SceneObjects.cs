﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HSVPicker.Examples;

public class SceneObjects : MonoBehaviour {
	/// <summary>
	/// This scroll view object is serialized to set the object height
	/// </summary>
	private GameObject objectList;

	/// <summary>
	/// This object is a child of GameObject 'objectList' containing the list of child objects
	/// </summary>
	private GameObject objectListContent;

	/// <summary>
	/// From this object all child objects are retrieved
	/// </summary>
	private GameObject parentObject;

	/// <summary>
	/// Contains RTGizmoManager gameobject for the GizmoManager script
	/// </summary>
	private GameObject gizmo;

	/// <summary>
	/// Contains GizmoManager from gizmo
	/// </summary>
	private RTG.GizmoManager manager;

	/// <summary>
	/// Contains colorPickerMenu
	/// </summary>
	private GameObject colorPickerObject;

	/// <summary>
	/// Last selected object
	/// </summary>
	public List<GameObject> _selectedObjects = new List<GameObject>();

	/// <summary>
	/// List with list objects
	/// </summary>
	private List<GameObject> listObjectsList = new List<GameObject>();

	// Start is called before the first frame update
	void Start() {
		gizmo = GameObject.Find("RTGizmoManager");
		objectList = GameObject.Find("ObjectsList");
		objectListContent = GameObject.Find("Content");
		parentObject = GameObject.Find("Objects");
		colorPickerObject = GameObject.Find("BackgroundColor");

		manager = gizmo.GetComponent<RTG.GizmoManager>();

		SetObjectListMenu();
	}

	void SetObjectListMenu() {
		foreach (Transform go in parentObject.transform) {
			GameObject childObject = CreateListObject(go);
			listObjectsList.Add(childObject);
		}

		RenderListObjects();
	}

	public void AddObjectToObjectListMenu(GameObject go) {
		GameObject childObject = CreateListObject(go.transform);
		listObjectsList.Add(childObject);

		RenderListObjects();
	}

	public void RemoveObjectFromObjectListMenu(GameObject go) {
		for (int i = 0; i < listObjectsList.Count; i++) {
			if (listObjectsList[i].name == go.name) {
				listObjectsList.Remove(listObjectsList[i]);
				break;
			}
		}

		RenderListObjects();
	}

	void ClearObjectListMenu() {
		foreach (Transform child in objectListContent.transform) {
			// child.parent = null;
			child.SetParent(null, false);
			// Destroy(child.gameObject);
		}

		// Set list height to zero
		RectTransform AssetsViewRt = objectListContent.GetComponent<RectTransform>();
		AssetsViewRt.sizeDelta = new Vector2(0, 0);
	}

	void RenderListObjects() {
		int counter = 0;
		//int position = -10;*/

		// First clear object list menu
		ClearObjectListMenu();

		foreach (GameObject go in listObjectsList) {
			go.transform.SetParent(this.objectListContent.transform);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(240, 20);
			/*
			rt.anchoredPosition = new Vector2(0, position);
			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.pivot = new Vector2(0.5f, 1);
			*/
			counter++;
			//position += -20;*/
		}

		// Calculate height for objects list
		int assetsViewHeight = (20 * counter) + 20;

		RectTransform AssetsViewRt = this.objectListContent.GetComponent<RectTransform>();
		AssetsViewRt.sizeDelta = new Vector2(0, assetsViewHeight);

	}

	GameObject CreateListObject(Transform go) {
		GameObject childObject = new GameObject(go.name, typeof(RectTransform));

		// Text styling
		Text childText = childObject.AddComponent<Text>();
		childText.text = go.name;
		childText.color = Color.black;
		childText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

		// Add script
		childObject.AddComponent(typeof(SelectObject));

		return childObject;
	}

	public GameObject GetObjectListContent() {
		return this.objectListContent;
	}

	public void SelectGameObject(GameObject listGameObject, GameObject editorGameObject) {
		if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) {
			DeselectAllGameObjects();
		}

		if (this._selectedObjects.Contains(listGameObject)) {
			DeselectGameObject(listGameObject, editorGameObject);
		} else {
			AddGameObject(listGameObject, editorGameObject);
		}
	}

	void AddGameObject(GameObject listGameObject, GameObject editorGameObject) {
		// Add object to list
		this._selectedObjects.Add(listGameObject);

		// Highlight editor gameobject
		manager.HighlightGameObject(editorGameObject);

		// Highlight object in sidemenu
		Text childText = listGameObject.GetComponent<Text>();
		childText.color = Color.white;

		// Set property menu fields
		SetPropertyMenuFields(editorGameObject);

		// Set color picker object
		ChangeColor(editorGameObject);
	}

	void DeselectGameObject(GameObject listGameObject, GameObject editorGameObject) {
		// Remove object from list
		this._selectedObjects.Remove(listGameObject);

		// Remove editor gameobject hightlight
		manager.RemoveHighlight(editorGameObject);

		// Remove sidemenu object highlight
		Text childText = listGameObject.GetComponent<Text>();
		childText.color = Color.black;
	}

	void DeselectAllGameObjects() {
		// Remove sidemenu objects highlight
		foreach (GameObject selected in this._selectedObjects) {
			Text childText = selected.GetComponent<Text>();
			childText.color = Color.black;
		}

		// Clear object list
		this._selectedObjects.Clear();

		// Remove all editor hightlights
		manager.RemoveHighlights(manager._selectedObjects);
	}

	void SetPropertyMenuFields(GameObject selectedObject) {
		// Find dimensions fields
		GameObject xInputObject = GameObject.Find("XInput");
		GameObject yInputObject = GameObject.Find("YInput");
		GameObject zInputObject = GameObject.Find("ZInput");

		// Set dimensions in properties menu
		//xInputObject.GetComponent<InputField>().text = "wow";
		Debug.Log(selectedObject.transform.localPosition.ToString());
		xInputObject.GetComponent<InputField>().text = selectedObject.transform.localScale.x.ToString();
		yInputObject.GetComponent<InputField>().text = selectedObject.transform.localScale.y.ToString();
		zInputObject.GetComponent<InputField>().text = selectedObject.transform.localScale.z.ToString();
	}
	void ChangeColor(GameObject editorGameObject) {
		// Get renderer of selectedObject
		Renderer objectRenderer = editorGameObject.GetComponent<Renderer>();

		// Get colorPickerTester script
		ColorPickerTester colorPicker = colorPickerObject.GetComponent<ColorPickerTester>();
		colorPicker.ChangeColor(objectRenderer);
	}

	void ChangeBackgroundImage(GameObject editorGameObject) {
		editorGameObject.AddComponent<Image>();
	}
}
