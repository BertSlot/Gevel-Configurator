using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

namespace RTG {
	/// <summary>
	/// This class is for managing the various gizmos and the selecting of objects in the scene.
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	public class GizmoManager : MonoBehaviour {


		/// <summary>
		/// A private enum which is used by the class to differentiate between different 
		/// gizmo types. Where this enum will come in handy is when we use the 
		/// W,E,R,T keys to switch between different types of gizmos. When the W key is 
		/// pressed for example, we will call the 'SetWorkGizmoId' function passing 
		/// GizmoId.Move as the parameter.
		/// </summary>
		public enum GizmoId {
			Move = 1,
			Rotate = 2,
			Scale = 3,
			Universal = 4
		}

		/// <summary>
		/// The following 4 variables are references to the ObjectTransformGizmo behaviours
		/// that will be used to move, rotate and scale our objects.
		/// </summary>
		private ObjectTransformGizmo _objectMoveGizmo;
		private ObjectTransformGizmo _objectRotationGizmo;
		private ObjectTransformGizmo _objectScaleGizmo;
		private ObjectTransformGizmo _objectUniversalGizmo;


		/// <summary>
		/// Camera object
		/// </summary>
		[SerializeField]
		private Camera Cam;

		/// <summary>
		/// ParentObject for spawning new objects
		/// </summary>
		[SerializeField]
		private GameObject ParentObject;

		/// <summary>
		/// The current work gizmo id. The work gizmo is the gizmo which is currently used
		/// to transform objects. The W,E,R,T keys can be used to change the work gizmo as
		/// needed.
		/// </summary>
		private GizmoId _workGizmoId;
		/// <summary>
		/// A reference to the current work gizmo. If the work gizmo id is GizmoId.Move, then
		/// this will point to '_objectMoveGizmo'. For GizmoId.Rotate, it will point to 
		/// '_objectRotationGizmo' and so on.
		/// </summary>
		private ObjectTransformGizmo _workGizmo;

		/// <summary>
		/// A list of objects which are currently selected. This is also the list that holds
		/// the gizmo target objects. 
		/// </summary>
		public List<GameObject> _selectedObjects = new List<GameObject>();

		/// <summary>
		/// A Dictionary of objects and transform values wich are copied from the selected objects. This list also contains 
		/// the objects that will be instantiated on the paste command
		/// </summary>
		private List<GameObject> _clipboard = new List<GameObject>();

		/// <summary>
		/// Color of the outline for the selected objects
		/// </summary>
		[SerializeField]
		public Color highlight_color = Color.green;

		/// <summary>
		/// Width of the outline for the selected objects
		/// </summary>
		[SerializeField, Range(0f, 10f)]
		public float highlight_width = 4f;

		/// <summary>
		/// Side menu objects
		/// </summary>
		private GameObject sideMenu;

		/// <summary>
		/// Contains SceneObjects script
		/// </summary>
		private SceneObjects scene;

		/// <summary>
		/// SelectObject for the scripts
		/// </summary>
		//private GameObject 

		public bool AllowKeyboardInputs = true;

		/// <summary>
		/// Performs all necessary initializations.
		/// </summary>
		private void Start() {
			// Create the 4 gizmos
			_objectMoveGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
			_objectRotationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
			_objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
			_objectUniversalGizmo = RTGizmosEngine.Get.CreateObjectUniversalGizmo();

			// Call the 'SetEnabled' function on the parent gizmo to make sure
			// the gizmos are initially hidden in the scene. We want the gizmo
			// to show only when we have a target object available.
			_objectMoveGizmo.Gizmo.SetEnabled(false);
			_objectRotationGizmo.Gizmo.SetEnabled(false);
			_objectScaleGizmo.Gizmo.SetEnabled(false);
			_objectUniversalGizmo.Gizmo.SetEnabled(false);

			// Link the selected objects list to the gizmos
			_objectMoveGizmo.SetTargetObjects(_selectedObjects);
			_objectRotationGizmo.SetTargetObjects(_selectedObjects);
			_objectScaleGizmo.SetTargetObjects(_selectedObjects);
			_objectUniversalGizmo.SetTargetObjects(_selectedObjects);

			// We initialize the work gizmo to the move gizmo by default.
			_workGizmo = _objectMoveGizmo;
			_workGizmoId = GizmoId.Move;

			// Find side menu
			sideMenu = GameObject.Find("SideMenu");
			scene = sideMenu.GetComponent<SceneObjects>();
		}

		/// <summary>
		/// Called every frame to perform all necessary updates.
		/// </summary>
		private void Update() {
			// Check if the left mouse button was pressed in the current frame.
			if (Input.GetMouseButtonDown(0) &&
				RTGizmosEngine.Get.HoveredGizmo == null) {

				// Pick a game object
				GameObject pickedObject = PickGameObject();

				if (pickedObject != null) {
					// Get name of picked object
					string objectName = pickedObject.name;
					GameObject objectList = scene.GetObjectListContent();

					// Find sidemenu object by name in objectList
					GameObject listGameObject = null;
					if (objectList.transform.Find(objectName) != null) {
						listGameObject = objectList.transform.Find(objectName).gameObject;
					}

					if (listGameObject != null) {
						// Set last selected object in SceneObject script & Higlight gameobject in editor
						scene.SelectGameObject(listGameObject, pickedObject);
					}
				} else {
					if (!EventSystem.current.IsPointerOverGameObject()) {
						// If we reach this point, it means no object was picked. This means that we clicked
						// in thin air, so we just clear the selected objects list.
						RemoveHighlights(_selectedObjects);
						_selectedObjects.Clear();

						// The selection has changed
						OnSelectionChanged();
					}
				}
			}

			// TODO add giant if to check if you are in an input field as to not make mistakes by deleting or copying objects
			if (AllowKeyboardInputs) {

				// If the Ctrl + C key was pressed we add the currently selected objects to the clipboard list
				// If the Ctrl + V key was pressed we add the objects currently in the clipboard list to the scene
				if (Application.isEditor) { // this works when you are using the editor
					if (Input.GetKeyDown(KeyCode.C)) {
						if (_selectedObjects.Count != 0) {
							CopyToClipboard(_selectedObjects);
						} else {
							_clipboard.Clear();
						}
					} else if (Input.GetKeyDown(KeyCode.V)) {
						if (_clipboard.Count != 0) {
							RemoveHighlights(_selectedObjects);
							_selectedObjects.Clear();
							foreach (var newObj in Paste()) {
								if (!_selectedObjects.Contains(newObj)) {
									_selectedObjects.Add(newObj);
								}
							}
							OnSelectionChanged();
						}
					}
				} else { // This works when you are not using the editor
					if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftControl)) {
						if (_selectedObjects.Count != 0) {
							CopyToClipboard(_selectedObjects);
						} else {
							_clipboard.Clear();
						}
					} else if (Input.GetKeyDown(KeyCode.V) && Input.GetKey(KeyCode.LeftControl)) {
						if (_clipboard.Count != 0) {
							RemoveHighlights(_selectedObjects);
							_selectedObjects.Clear();
							foreach (var newObj in Paste()) {
								if (!_selectedObjects.Contains(newObj)) {
									_selectedObjects.Add(newObj);
								}
							}
							OnSelectionChanged();
						}
					}
				}

				if (Input.GetKey(KeyCode.Delete)) {
					if (_selectedObjects.Count != 0) {
						RemoveHighlights(_selectedObjects);
						DeleteObjectList(_selectedObjects);
						_selectedObjects.Clear();
						OnSelectionChanged();
					}
				}
				// If the G key was pressed, we change the transform space to Global. Otherwise,
				// if the L key was pressed, we change it to Local.
				if (Input.GetKeyDown(KeyCode.G))
					SetTransformSpace(GizmoSpace.Global);
				else if (Input.GetKeyDown(KeyCode.L))
					SetTransformSpace(GizmoSpace.Local);

				// We will change the pivot type when the P key is pressed
				if (Input.GetKeyDown(KeyCode.P)) {
					// Retrieve the current transform pivot and activate the other one instead.
					GizmoObjectTransformPivot currentPivot = _objectMoveGizmo.TransformPivot;
					if (currentPivot == GizmoObjectTransformPivot.ObjectGroupCenter)
						SetTransformPivot(GizmoObjectTransformPivot.ObjectMeshPivot);
					else
						SetTransformPivot(GizmoObjectTransformPivot.ObjectGroupCenter);
				}

				// Switch between different gizmo types using the W,E,R,T keys.
				if (Input.GetKeyDown(KeyCode.W))
					SetWorkGizmoId(GizmoId.Move);
				else if (Input.GetKeyDown(KeyCode.E))
					SetWorkGizmoId(GizmoId.Rotate);
				else if (Input.GetKeyDown(KeyCode.R))
					SetWorkGizmoId(GizmoId.Scale);
				else if (Input.GetKeyDown(KeyCode.T))
					SetWorkGizmoId(GizmoId.Universal);
			}
			OnSelectionChanged();
		}


		/// <summary>
		/// Spawns object using the middle of the screen as position of the raycast
		/// </summary>
		/// <param name="obj"></param>
		public void SpawnObjectMiddle(GameObject obj) {

			Vector3 location;
			Quaternion rotation = new Quaternion(0, 0, 0, 0);

			Vector2 mousePos = new Vector2 {
				x = Screen.width / 2,
				y = Screen.height / 2
			};

			Ray ray = Cam.ScreenPointToRay(mousePos);

			GameObject spawnedObj;
			//Debug.Log(obj.name);
			if (Physics.Raycast(ray, out RaycastHit hit)) {
				var Dist = Vector3.Distance(hit.transform.position, Cam.transform.position);
				Dist /= 2;
				location = Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Dist));
				spawnedObj = Instantiate(obj, location, rotation, ParentObject.transform);
			} else {
				location = Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 500));
				spawnedObj = Instantiate(obj, location, rotation, ParentObject.transform);
			}

			RemoveHighlights(_selectedObjects);
			_selectedObjects.Clear();

			// for .obj files that have child objects with meshes
			List<GameObject> children = spawnedObj.GetAllChildren();
			if (children.Count > 0) { // if the gameobject has no children it doesn't need to loop over anything
				foreach (GameObject child in children) {

					// For some reason only Destroy makes it work. when using DeleteObject() it deletes the wrong child object with the same name.
					if (child.GetComponent<MeshRenderer>() == null) {
						//Debug.Log("annihilate : " + child.name);
						Destroy(child);
					} else {
						// This is making sure the all the objects spawned have the same name as the parent so that the can be selected
						// The names dont have to be different because the addObjectToObjectListMenu() function takes care of duplicate names.
						child.name = spawnedObj.name;
						// UnParent the childobjects and set them under the Objects Parent object
						child.transform.SetParent(ParentObject.transform);

						// Add children to the sceneObjectsList
						_selectedObjects.Add(child);
						scene.AddObjectToObjectListMenu(child);

						AddOrigin(child, obj.name, true);
					}
				}
			}

			if (spawnedObj.GetComponent<Collider>() == null && spawnedObj.GetComponent<MeshCollider>() == null) {
				//Debug.Log("Annihilate : " + spawnedObj.name);
				Destroy(spawnedObj);
			} else {
				AddOrigin(spawnedObj, obj.name, false);
				_selectedObjects.Add(spawnedObj);
				scene.AddObjectToObjectListMenu(spawnedObj);
			}
			OnSelectionChanged();

		}

		/// <summary>
		/// Spawns object using mouse position as raycast
		/// </summary>
		/// <param name="obj"></param>
		public void SpawnObjectMouse(GameObject obj) {

			Vector3 location;
			Quaternion rotation = new Quaternion(0, 0, 0, 0);

			Vector2 mousePos = new Vector2 {
				x = Input.mousePosition.x,
				y = Input.mousePosition.y
			};

			Ray ray = Cam.ScreenPointToRay(mousePos);
			GameObject spawnedObj;
			if (Physics.Raycast(ray, out RaycastHit hit)) {
				var Dist = Vector3.Distance(hit.transform.position, Cam.transform.position);
				Dist /= 2;
				location = Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Dist));
				spawnedObj = Instantiate(obj, location, rotation, ParentObject.transform);
			} else {
				location = Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 500));
				spawnedObj = Instantiate(obj, location, rotation, ParentObject.transform);
			}

			RemoveHighlights(_selectedObjects);
			_selectedObjects.Clear();


			// for .obj files that have child objects with meshes
			List<GameObject> children = spawnedObj.GetAllChildren();
			if (children.Count > 0) {
				foreach (GameObject child in children) {
					// For some reason only Destroy makes it work. when using DeleteObject() it deletes the wrong child object.
					if (child.GetComponent<MeshRenderer>() == null) {
						//Debug.Log("annihilate : " + child.name);
						Destroy(child);
					} else {
						// This is making sure the all the objects spawned have the same name as the parent so that the can be selected
						// The names dont have to be different because the addObjectToObjectListMenu() function takes care of duplicate names.
						child.name = spawnedObj.name;
						// UnParent the childobjects and set them under the Objects Parent object
						child.transform.SetParent(ParentObject.transform);

						// Add children to the sceneObjectsList
						_selectedObjects.Add(child);
						scene.AddObjectToObjectListMenu(child);

						AddOrigin(child, obj.name, true);
					}
				}
			}

			if (spawnedObj.GetComponent<Collider>() == null && spawnedObj.GetComponent<MeshCollider>() == null) {
				//Debug.Log("Annihilate : " + spawnedObj.name);
				Destroy(spawnedObj);

			} else {
				AddOrigin(spawnedObj, obj.name, false);
				_selectedObjects.Add(spawnedObj);
				scene.AddObjectToObjectListMenu(spawnedObj);
			}
			OnSelectionChanged();

		}

		/// <summary>
		/// This function adds a component of type OriginObject to obj containing information about the object from which it is derived. 
		/// This info is used for replicating this object while loading from a save file.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="originName"></param>
		/// <param name="childOfMultiMesh"></param>
		private void AddOrigin(GameObject obj, string originName, bool childOfMultiMesh) {
			OriginObject OriObj = obj.AddComponent<OriginObject>();
			OriObj.originObjectName = originName;
			OriObj.originObjectHasMultipleMeshes = childOfMultiMesh;
			OriObj.meshName = obj.GetMesh().name;

		}

		/*
		/// <summary>
		/// An implementation of the OnGUI function which shows the current transform
		/// space and transform pivot in the top left corner of the screen.
		/// </summary>
		private void OnGUI() {
			// We will use a style with a green text color
			var guiStyle = new GUIStyle();
			guiStyle.normal.textColor = Color.green;


			guiStyle.margin.top = Screen.height - 105; // dit blijft net boven de spawn knop

			GUILayout.Label(" ", guiStyle);
			// Draw the transform space label.

			string label = "Transform Space: " + _objectMoveGizmo.TransformSpace.ToString() + "\n" + "Transform Pivot: " + _objectMoveGizmo.TransformPivot.ToString();
			GUILayout.Label(label, guiStyle);

			// Same for transform pivot
			//GUILayout.Label("Transform Pivot: " + _objectMoveGizmo.TransformPivot.ToString(), guiStyle);
		}
		*/

		/// <summary>
		/// This function instantiates the objects stored in the _clipboard <GameObject,Transform> Dictionary.
		/// </summary>
		private List<GameObject> Paste() {
			List<GameObject> instantiatedObjects = new List<GameObject>();

			foreach (var obj in _clipboard) {
				GameObject duplicateObject = Instantiate(obj, obj.transform.parent);
				// To make sure no duplicates make it through
				if (!instantiatedObjects.Contains(duplicateObject)) {
					instantiatedObjects.Add(duplicateObject);
					scene.AddObjectToObjectListMenu(duplicateObject);
				}
			}
			return instantiatedObjects;
		}

		/// <summary>
		/// This function copies the given list to the _clipboard Dictionary
		/// </summary>
		private void CopyToClipboard(List<GameObject> gameObjects) {
			_clipboard.Clear();
			foreach (var obj in gameObjects) {
				_clipboard.Add(obj);
			}
		}


		/// <summary>
		/// Deletes single GameObject
		/// </summary>
		/// <param name="obj"></param>
		private void DeleteObject(GameObject obj) {
			scene.RemoveObjectFromObjectListMenu(obj);
			Destroy(obj);
		}

		/// <summary>
		/// Deletes List of Gameobjects one by one
		/// </summary>
		/// <param name="objList"></param>
		private void DeleteObjectList(List<GameObject> objList) {
			foreach (GameObject obj in objList) {
				DeleteObject(obj);
			}
		}


		/// <summary>
		/// Uses the mouse position to pick a game object in the scene. Returns
		/// the picked game object or null if no object is picked.
		/// </summary>
		/// <remarks>
		/// Objects must have colliders attached.
		/// </remarks>
		private GameObject PickGameObject() {
			if (!EventSystem.current.IsPointerOverGameObject()) {
				// Build a ray using the current mouse cursor position
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

				// Check if the ray intersects a game object. If it does, return it
				RaycastHit rayHit;
				if (Physics.Raycast(ray, out rayHit, float.MaxValue))
					return rayHit.collider.gameObject;

				// No object is intersected by the ray. Return null.
				return null;
			}
			return null;
		}

		/// <summary>
		/// Highlight a given gameobject
		/// </summary>
		public void HighlightGameObject(GameObject pickedObject) {
			// Is the CTRL key pressed?
			if (Input.GetKey(KeyCode.LeftControl)) {
				// The CTRL key is pressed; it means we find ourselves in 2 possible situations:
				// a) the picked object is already selected, in which case we deselect it;
				// b) the picked object is not selected, in which case we append it to the selection.
				if (_selectedObjects.Contains(pickedObject)) {
					_selectedObjects.Remove(pickedObject);
					RemoveHighlight(pickedObject);

				} else {
					_selectedObjects.Add(pickedObject);
				}
				// The selection has changed
				OnSelectionChanged();
			} else {
				// The CTRL key is not pressed; in this case we just clear the selection and
				// select only the object that we clicked on.
				RemoveHighlights(_selectedObjects);
				_selectedObjects.Clear();
				_selectedObjects.Add(pickedObject);

				// The selection has changed
				OnSelectionChanged();
			}
		}

		/// <summary>
		/// This function is called to change the type of work gizmo. This is
		/// used in the 'Update' function in response to the user pressing the
		/// W,E,R,T keys to switch between different gizmo types.
		/// This specific function allows for it to be called using an interger as ID
		/// </summary>
		public void SetWorkGizmoIdInt(int gizmoId) {
			// If the specified gizmo id is the same as the current id, there is nothing left to do
			if ((GizmoId)gizmoId == _workGizmoId)
				return;

			// Start with a clean slate and disable all gizmos
			_objectMoveGizmo.Gizmo.SetEnabled(false);
			_objectRotationGizmo.Gizmo.SetEnabled(false);
			_objectScaleGizmo.Gizmo.SetEnabled(false);
			_objectUniversalGizmo.Gizmo.SetEnabled(false);

			// At this point all gizmos are disabled. Now we need to check the gizmo id
			// and adjust the '_workGizmo' variable.
			_workGizmoId = (GizmoId)gizmoId;
			if ((GizmoId)gizmoId == GizmoId.Move)
				_workGizmo = _objectMoveGizmo;
			else if ((GizmoId)gizmoId == GizmoId.Rotate)
				_workGizmo = _objectRotationGizmo;
			else if ((GizmoId)gizmoId == GizmoId.Scale)
				_workGizmo = _objectScaleGizmo;
			else if ((GizmoId)gizmoId == GizmoId.Universal)
				_workGizmo = _objectUniversalGizmo;

			// If we have any selected objects, we need to make sure the work gizmo is enabled
			if (_selectedObjects.Count != 0) {
				// Make sure the work gizmo is enabled.
				_workGizmo.Gizmo.SetEnabled(true);

				// When working with transform spaces and pivots, the gizmos need to know about the pivot object. 
				// This piece of information is necessary when the transform space is set to local because in that 
				// case the gizmo will have its rotation synchronized with the target objects rotation. But because 
				// there is more than one target object, we need to tell the gizmo which object to use. This is the 
				// role if the pivot object in this case. This pivot object is also useful when the transform pivot 
				// is set to 'ObjectMeshPivot' because it will be used to adjust the position of the gizmo. 
				_workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);
			}
		}


		/// <summary>
		/// This function is called to change the type of work gizmo. This is
		/// used in the 'Update' function in response to the user pressing the
		/// W,E,R,T keys to switch between different gizmo types.
		/// </summary>
		public void SetWorkGizmoId(GizmoId gizmoId) {
			// If the specified gizmo id is the same as the current id, there is nothing left to do
			if (gizmoId == _workGizmoId)
				return;

			// Start with a clean slate and disable all gizmos
			_objectMoveGizmo.Gizmo.SetEnabled(false);
			_objectRotationGizmo.Gizmo.SetEnabled(false);
			_objectScaleGizmo.Gizmo.SetEnabled(false);
			_objectUniversalGizmo.Gizmo.SetEnabled(false);

			// At this point all gizmos are disabled. Now we need to check the gizmo id
			// and adjust the '_workGizmo' variable.
			_workGizmoId = gizmoId;
			if (gizmoId == GizmoId.Move)
				_workGizmo = _objectMoveGizmo;
			else if (gizmoId == GizmoId.Rotate)
				_workGizmo = _objectRotationGizmo;
			else if (gizmoId == GizmoId.Scale)
				_workGizmo = _objectScaleGizmo;
			else if (gizmoId == GizmoId.Universal)
				_workGizmo = _objectUniversalGizmo;

			// If we have any selected objects, we need to make sure the work gizmo is enabled
			if (_selectedObjects.Count != 0) {
				// Make sure the work gizmo is enabled.
				_workGizmo.Gizmo.SetEnabled(true);

				// When working with transform spaces and pivots, the gizmos need to know about the pivot object. 
				// This piece of information is necessary when the transform space is set to local because in that 
				// case the gizmo will have its rotation synchronized with the target objects rotation. But because 
				// there is more than one target object, we need to tell the gizmo which object to use. This is the 
				// role if the pivot object in this case. This pivot object is also useful when the transform pivot 
				// is set to 'ObjectMeshPivot' because it will be used to adjust the position of the gizmo. 
				_workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);
			}
		}


		/// <summary>
		/// Called from the 'Update' function whenever the '_selectedObjects' list
		/// changes. It is responsible for updating the gizmos.
		/// </summary>
		private void OnSelectionChanged() {
			// If we have any selected objects, we need to make sure the work gizmo is enabled
			if (_selectedObjects.Count != 0) {

				SetHighlights(_selectedObjects);

				// Make sure the work gizmo is enabled. There is no need to check if the gizmo is already
				// enabled. The 'SetEnabled' call will simply be ignored if that is the case.
				_workGizmo.Gizmo.SetEnabled(true);

				// Last object that is selected will be used as the pivot object
				_workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);

				// In order to ensure that the correct position and rotation
				// are used, we need to call 'RefreshPositionAndRotation'.
				_workGizmo.RefreshPositionAndRotation();
			} else {
				// The target object is null. In this case, we don't want any gizmos to be visible
				// in the scene, so we disable all of them.
				_objectMoveGizmo.Gizmo.SetEnabled(false);
				_objectRotationGizmo.Gizmo.SetEnabled(false);
				_objectScaleGizmo.Gizmo.SetEnabled(false);
				_objectUniversalGizmo.Gizmo.SetEnabled(false);
			}
		}

		/// <summary>
		/// This function removes highlights from all objects in a list
		/// </summary>
		/// <param name="objects"></param>
		public void RemoveHighlights(List<GameObject> objects) {
			foreach (var obj in objects) {
				RemoveHighlight(obj);
			}
		}

		/// <summary>
		/// This function removes the highlight of a single object
		/// </summary>
		/// <param name="obj"></param>
		public void RemoveHighlight(GameObject obj) {
			if (obj.GetComponent<Outline>() != null) {
				Outline outline = obj.GetComponent<Outline>();
				outline.enabled = false;
			}
		}

		/// <summary>
		/// This function sets highlights for a list of objects
		/// </summary>
		/// <param name="objects"></param>
		private void SetHighlights(List<GameObject> objects) {

			foreach (var obj in objects) {
				SetHighlight(obj);
			}
		}

		/// <summary>
		/// This function sets the highlight for a single object. 
		/// If the object already has the outline script attached it only enables it.
		/// </summary>
		/// <param name="obj"></param>
		private void SetHighlight(GameObject obj) {
			if (obj.GetComponent<Outline>() == null) {
				Outline outline = obj.AddComponent<Outline>();
				outline.OutlineMode = Outline.Mode.OutlineAll;
				outline.OutlineColor = highlight_color;
				outline.OutlineWidth = highlight_width;
				outline.enabled = true;
			} else {
				Outline outline = obj.GetComponent<Outline>();
				outline.enabled = true;
			}
		}


		/// <summary>
		/// Called from the 'Update' function in response to user input in order
		/// to change the transform space for all gizmos. The parameter represents
		/// the desired transform space and is of type 'GizmoSpace'. This is an enum
		/// with 2 mamebers: Global and Local.
		/// </summary>
		private void SetTransformSpace(GizmoSpace transformSpace) {
			// In order to change the transform space for a gizmo, we need to call the 
			// gizmo's 'SetTransformSpace' function. We do this for all gizmos and pass
			// the specified transform space as parameter.
			_objectMoveGizmo.SetTransformSpace(transformSpace);
			_objectRotationGizmo.SetTransformSpace(transformSpace);
			_objectScaleGizmo.SetTransformSpace(transformSpace);
			_objectUniversalGizmo.SetTransformSpace(transformSpace);
		}

		/// <summary>
		/// Called from the 'Update' function in response to user input in order
		/// to change the transform pivot for all gizmos. The parameter represents
		/// the desired transform pivot and is of type 'GizmoObjectTransformPivot'.
		/// </summary>
		private void SetTransformPivot(GizmoObjectTransformPivot transformPivot) {
			_objectMoveGizmo.SetTransformPivot(transformPivot);
			_objectRotationGizmo.SetTransformPivot(transformPivot);
			_objectScaleGizmo.SetTransformPivot(transformPivot);
			_objectUniversalGizmo.SetTransformPivot(transformPivot);
		}
	}
}