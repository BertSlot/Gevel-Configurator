using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class print : MonoBehaviour {
	// Start is called before the first frame update


	public Camera Cam;
	public GameObject Object;
	public GameObject ParentObject;


	private Vector3 location;
	private Quaternion rotation = new Quaternion(0, 0, 0, 0);
	private bool buttonState = false;


	// Update is called once per frame
	void Update() {

		if (Input.GetMouseButtonUp(0) && buttonState) {

			RaycastHit hit;
			Vector2 mousePos = new Vector2();
			mousePos.x = Input.mousePosition.x;
			mousePos.y = Input.mousePosition.y;

			Ray ray = Cam.ScreenPointToRay(mousePos);
			if (Physics.Raycast(ray, out hit)) {
				var Dist = Vector3.Distance(hit.transform.position, Cam.transform.position);
				Dist /= 2;
				location = Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Dist));
				GameObject.Instantiate(Object, location, rotation, ParentObject.transform);
			} else {
				location = Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 500));
				GameObject.Instantiate(Object, location, rotation, ParentObject.transform);
			}
		}
	}


	public void PrintButton() {
		Debug.Log("Spawn button Pressed!");
		if (buttonState) {
			buttonState = false;
		} else {
			buttonState = true;
		}


	}
}
