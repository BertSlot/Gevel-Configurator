using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class print : MonoBehaviour {
	// Start is called before the first frame update


	public GameObject Sphere;
	public Vector3 location;
	public Quaternion rotation;

	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}


	public void PrintButton() {
		Debug.Log("Spwan button Pressed!");
		GameObject.Instantiate(Sphere, location, rotation);
		location.y += 5;
	}
}
