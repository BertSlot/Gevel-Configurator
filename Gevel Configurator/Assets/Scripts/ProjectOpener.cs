using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using SFB;

public class ProjectOpener : MonoBehaviour {
	/// <summary>
	/// String for the selected path
	/// </summary>
	string path;

	/// <summary>
	/// Number for button
	/// </summary>
	int number;

	// Opens file panel to select project
	public void OpenProject() {
		path = StandaloneFileBrowser.OpenFilePanel("Load unity", "", "unity", false).ToString();

		GetText();

	}

	// Check of path isn't empty
	void GetText() {
		if (path != null) {
			CheckButtons();
			UpdateText();
		}
	}

	// Add filename to button
	// Button loads selected script
	void UpdateText() {
		string filename = Path.GetFileName(path);
		string[] x = filename.Split('.');
		Text txtMy = GameObject.Find("StartMenu/LastProject" + number + "Button/Text").GetComponent<Text>();
		txtMy.text = x[0];
	}

	// Check of the button has already a value
	void CheckButtons() {
		Text txtMy1 = GameObject.Find("StartMenu/LastProject1Button/Text").GetComponent<Text>();
		Text txtMy2 = GameObject.Find("StartMenu/LastProject2Button/Text").GetComponent<Text>();
		Text txtMy3 = GameObject.Find("StartMenu/LastProject3Button/Text").GetComponent<Text>();
		Text txtMy4 = GameObject.Find("StartMenu/LastProject4Button/Text").GetComponent<Text>();
		Text txtMy5 = GameObject.Find("StartMenu/LastProject5Button/Text").GetComponent<Text>();
		Text txtMy6 = GameObject.Find("StartMenu/LastProject6Button/Text").GetComponent<Text>();

		if (txtMy1.text == "") {
			number = 1;
		} else if (txtMy2.text == "") {
			number = 2;
		} else if (txtMy3.text == "") {
			number = 3;
		} else if (txtMy4.text == "") {
			number = 4;
		} else if (txtMy5.text == "") {
			number = 5;
		} else if (txtMy6.text == "") {
			number = 6;
		} else {
			number = 1;
		}

	}
}
