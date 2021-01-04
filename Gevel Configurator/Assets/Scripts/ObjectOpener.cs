using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using SFB;

public class ObjectOpener : MonoBehaviour {
	string path;
	public void OpenObject() {
		path = StandaloneFileBrowser.OpenFilePanel("Overwrite with unity", "", "unity", false).ToString();
		CheckObject();

	}
	void CheckObject() {
		if (path != null) {
			//UpdateText();
			Debug.Log("Object geopend!");
		}
	}
}
