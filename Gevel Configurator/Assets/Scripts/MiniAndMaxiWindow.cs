using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniAndMaxiWindow : MonoBehaviour {

	private void Start() {

		Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
	}

	public void MiniMizeScreen() {
		// standard resolutrion for minimizen
		Screen.SetResolution(1280, 720, false);
		Debug.Log("Minimize screen");
		GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(false);
		GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(true);
	}

	public void MaxiMizeScreen() {
		// resizes to fullscreen and uses the fullscreen resolution, so native resolution
		Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
		Debug.Log("Maximize screen");
		GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(false);
		GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(true);
	}
}
