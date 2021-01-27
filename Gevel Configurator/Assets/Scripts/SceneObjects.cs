using System.Collections;
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

	/// <summary>
	/// Calculation overzicht object
	/// </summary>
	private GameObject calculationOverview;

	/// <summary>
	/// Property menu object
	/// </summary>
	public GameObject propertyMenu;

	// Start is called before the first frame update
	void Start() {
		gizmo = GameObject.Find("RTGizmoManager");
		objectList = GameObject.Find("ObjectsList");
		objectListContent = GameObject.Find("Content");
		parentObject = GameObject.Find("Objects");
		colorPickerObject = GameObject.Find("BackgroundColor");
		calculationOverview = GameObject.Find("Calculation_Overzicht");

		manager = gizmo.GetComponent<RTG.GizmoManager>();

		propertyMenu.gameObject.SetActive(false);

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
		string characters = "0123456789abcdefghijklmnopqrstuvwxABCDEFGHIJKLMNOPQRSTUVWXYZ";

		string code = "";

		for (int i = 0; i < 5; i++) {
			int a = Random.Range(0, characters.Length);
			code = code + characters[a];
		}

		foreach (GameObject menuObject in listObjectsList)
        {
			if (go.name == menuObject.name)
            {
				go.name = go.name + code;
			}
        }

		Debug.Log(go.name);

		GameObject childObject = CreateListObject(go.transform);
		listObjectsList.Add(childObject);

		RenderListObjects();
		SelectGameObject(childObject, go);
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
			child.SetParent(null, false);
		}

		// Set list height to zero
		RectTransform AssetsViewRt = objectListContent.GetComponent<RectTransform>();
		AssetsViewRt.sizeDelta = new Vector2(0, 0);
	}

	void RenderListObjects() {
		int counter = 0;

		// First clear object list menu
		ClearObjectListMenu();

		foreach (GameObject go in listObjectsList) {
			go.transform.SetParent(this.objectListContent.transform);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(240, 20);

			counter++;
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
		if (this._selectedObjects.Count == 1)
        {
			SetPropertyMenuFields(listGameObject, editorGameObject);
		} else
        {
			propertyMenu.gameObject.SetActive(false);
        }

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

	void SetPropertyMenuFields(GameObject listGameObject, GameObject editorGameObject) {
		// Show property menu
		propertyMenu.gameObject.SetActive(true);

		// Set scale fields
		SetScaleFields(editorGameObject);

		// Set rotation fields
		SetRotationFields(editorGameObject);

		// Set position fields
		SetPositionFields(editorGameObject);

		// Set groups in dropdown
		GameObject dropdownObject = GameObject.Find("GroupDropdown");
		Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();

		AddDropdownItems(dropdown);

		Objects_overzicht overview = calculationOverview.GetComponent<Objects_overzicht>();

		// First remove previous listeners
		dropdown.onValueChanged.RemoveAllListeners();

		dropdown.onValueChanged.AddListener(value =>
		{
			//Debug.Log(dropdown.options[dropdown.value].text);
			overview.AddSurfaceGroup(editorGameObject, dropdown.options[dropdown.value].text);
		});

		foreach (string obj in overview.GetSurfaceGroups(editorGameObject))
		{
			//Debug.Log(obj);
		}

		// Set object name field
		GameObject objectNameObject = GameObject.Find("ObjectNameField");
		InputField objectNameField = objectNameObject.GetComponent<InputField>();

		objectNameField.onValueChanged.RemoveAllListeners();
		objectNameField.text = listGameObject.name;

		// Set object name field listener
		objectNameField.onValueChanged.AddListener(name =>
		{
			bool equal = false;

			// Check if name already exists
			foreach (GameObject menuObject in listObjectsList)
            {
				if (objectNameField.text == menuObject.name)
                {
					equal = true;
				}
			}

			if (!equal)
            {
				listGameObject.name = objectNameField.text;
				Text childText = listGameObject.GetComponent<Text>();
				childText.text = objectNameField.text;
				editorGameObject.name = objectNameField.text;
			}
		});
	}

	public void SetScaleFields(GameObject editorGameObject)
    {
		// Find scale fields
		GameObject xInputObject = GameObject.Find("ScaleXInput");
		GameObject yInputObject = GameObject.Find("ScaleYInput");
		GameObject zInputObject = GameObject.Find("ScaleZInput");

		xInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
		yInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
		zInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();

		// Set scale in properties menu
		xInputObject.GetComponent<InputField>().text = editorGameObject.transform.localScale.x.ToString();
		yInputObject.GetComponent<InputField>().text = editorGameObject.transform.localScale.y.ToString();
		zInputObject.GetComponent<InputField>().text = editorGameObject.transform.localScale.z.ToString();

		xInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(xInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.localScale = new Vector3(input, editorGameObject.transform.localScale.y, editorGameObject.transform.localScale.z);
			}
		});

		yInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(yInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.localScale = new Vector3(editorGameObject.transform.localScale.x, input, editorGameObject.transform.localScale.z);
			}
		});

		zInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(zInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.localScale = new Vector3(editorGameObject.transform.localScale.x, editorGameObject.transform.localScale.y, input);
			}
		});
	}

	public void SetRotationFields(GameObject editorGameObject)
    {
		// Find rotation fields
		GameObject xInputObject = GameObject.Find("RotationXInput");
		GameObject yInputObject = GameObject.Find("RotationYInput");
		GameObject zInputObject = GameObject.Find("RotationZInput");

		xInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
		yInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
		zInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();

		// Set scale in properties menu
		xInputObject.GetComponent<InputField>().text = editorGameObject.transform.rotation.eulerAngles.x.ToString();
		yInputObject.GetComponent<InputField>().text = editorGameObject.transform.rotation.eulerAngles.y.ToString();
		zInputObject.GetComponent<InputField>().text = editorGameObject.transform.rotation.eulerAngles.z.ToString();

		xInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(xInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.eulerAngles = new Vector3(input, editorGameObject.transform.rotation.eulerAngles.y, editorGameObject.transform.rotation.eulerAngles.z);
			}
		});

		yInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(yInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.eulerAngles = new Vector3(editorGameObject.transform.rotation.eulerAngles.x, input, editorGameObject.transform.rotation.eulerAngles.z);
			}
		});

		zInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(zInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.eulerAngles = new Vector3(editorGameObject.transform.rotation.eulerAngles.x, editorGameObject.transform.rotation.eulerAngles.y, input);
			}
		});
	}

	public void SetPositionFields(GameObject editorGameObject)
    {
		// Find rotation fields
		GameObject xInputObject = GameObject.Find("PositionXInput");
		GameObject yInputObject = GameObject.Find("PositionYInput");
		GameObject zInputObject = GameObject.Find("PositionZInput");

		xInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
		yInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();
		zInputObject.GetComponent<InputField>().onValueChanged.RemoveAllListeners();

		// Set scale in properties menu
		xInputObject.GetComponent<InputField>().text = editorGameObject.transform.position.x.ToString();
		yInputObject.GetComponent<InputField>().text = editorGameObject.transform.position.y.ToString();
		zInputObject.GetComponent<InputField>().text = editorGameObject.transform.position.z.ToString();

		xInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(xInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.position = new Vector3(input, editorGameObject.transform.position.y, editorGameObject.transform.position.z);
			}
		});

		yInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(yInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.position = new Vector3(editorGameObject.transform.position.x, input, editorGameObject.transform.position.z);
			}
		});

		zInputObject.GetComponent<InputField>().onValueChanged.AddListener(value =>
		{
			float input;

			if (float.TryParse(zInputObject.GetComponent<InputField>().text, out input))
			{
				editorGameObject.transform.position = new Vector3(editorGameObject.transform.position.x, editorGameObject.transform.position.y, input);
			}
		});
	}

	void AddDropdownItems(Dropdown dropdown) {
		List<string> options = new List<string>();
		Objects_overzicht overview = calculationOverview.GetComponent<Objects_overzicht>();
		foreach (KeyValuePair<string, float> group in overview.SurfaceGroupTotals) {
			options.Add(group.Key);
		}

		dropdown.ClearOptions();
		dropdown.AddOptions(options);
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
