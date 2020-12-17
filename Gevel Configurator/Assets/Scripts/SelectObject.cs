using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelectObject : MonoBehaviour, IPointerClickHandler {
	/// <summary>
    /// Contains sideMenu gameobject for the SceneObjects script
    /// </summary>
	private GameObject sideMenu;

	/// <summary>
    /// Contains the 3d-editor objects
    /// </summary>
	private GameObject objectsList;

	// Start is called before the first frame update
	void Start() {
		sideMenu = GameObject.Find("SideMenu");
		objectsList = GameObject.Find("Objects");
	}

	public void OnPointerClick(PointerEventData pointerEventData) {
		// Get sceneObjects script
		SceneObjects scene = sideMenu.GetComponent<SceneObjects>();

		string objectName = gameObject.GetComponent<Text>().text;
		GameObject editorGameObject = objectsList.transform.Find(objectName).gameObject;

		// Set last selected object in SceneObject script & Higlight gameobject in editor
		scene.SelectGameObject(gameObject, editorGameObject);
	}
}
