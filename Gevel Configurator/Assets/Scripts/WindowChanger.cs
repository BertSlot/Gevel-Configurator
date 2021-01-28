using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowChanger : MonoBehaviour {

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	// Makes Unity window disapear to taskbar
	public void OnMinimizeButtonClick() {
		ShowWindow(GetActiveWindow(), 2);
	}

	private void Awake() {
		Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, true);
		if (Application.isEditor) {
			Debug.Log("FullScreen native resolution: " + Display.main.systemWidth.ToString() + " , " + Display.main.systemHeight.ToString());
		}
	}

	// Minimize screen with borders
	public void MiniMizeScreen() {
		Screen.SetResolution(1280, 720, false);
		Debug.Log("Minimize screen");
		GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(false);
		GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(true);
	}

	// Maximize Screen borderless
	public void MaxiMizeScreen() {
		Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, true);
		Debug.Log("Maximize screen");
		GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(false);
		GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(true);
	}

}
