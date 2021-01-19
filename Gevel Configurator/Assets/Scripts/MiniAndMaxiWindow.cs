using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniAndMaxiWindow : MonoBehaviour {
	public void MiniMizeScreen() {
		Screen.SetResolution(640, 480, false);
		Debug.Log("Minimize screen");
		GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(false);
		GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(true);
	}

	public void MaxiMizeScreen() {
		Screen.SetResolution(640, 480, true);
		Debug.Log("Maximize screen");
		GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(false);
		GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(true);
	}
}
