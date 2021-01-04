using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using SFB;

public class ProjectOpener : MonoBehaviour {
	string path;
	public void OpenProject() {
		path = StandaloneFileBrowser.OpenFilePanel("Overwrite with unity", "", "unity", false).ToString();
		GetText();

	}
	void GetText() {
		if (path != null) {
			UpdateText();
		}
	}
	void UpdateText() {
		string filename = Path.GetFileName(path);
		string[] x = filename.Split('.');
		Text txtMy = GameObject.Find("StartMenu/LastProject2Button/Text").GetComponent<Text>();
		txtMy.text = x[0];
	}
}
