﻿using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetSpawner : MonoBehaviour {


	/// <summary>
	/// Array for GameObjects loaded from Import_Objects
	/// </summary>
	private Object[] ObjectsList;

	/// <summary>
	/// This object is a child of GameObject 'objectList' containing the list of child objects
	/// </summary>
	[SerializeField]
	private GameObject ObjectListContentPanel;

	/// <summary>
	/// Path to Import_Objects Folder
	/// </summary>
	[SerializeField]
	public string ObjectsFolderPath = "Import_Objects";

	/// <summary>
	/// AssetPanel prefab
	/// </summary>
	[SerializeField]
	private GameObject PanelPrefab;

	private void OnDisable() {

		for (int i = 0; i < ObjectListContentPanel.transform.childCount; i++) {
			ObjectListContentPanel.transform.GetChild(i).gameObject.SetActive(false);
			Destroy(ObjectListContentPanel.transform.GetChild(i).gameObject);
		}


	}

	private void OnEnable() {
		SpawnAssetPanels();
	}

	private void SpawnAssetPanels() {
		int x = 40;
		int y = -40;
		if (ObjectsFolderPath != null) {
			// Load all objects from folder
			ObjectsList = Resources.LoadAll(ObjectsFolderPath, typeof(GameObject));
			GameObject AssetPanel;

			// Make a panel sprite for every object in the folder
			foreach (GameObject obj in ObjectsList) {
				// Create Panel
				AssetPanel = Instantiate(PanelPrefab, ObjectListContentPanel.transform);

				// Add Asset to panel and name under panel
				SelectAsset panelData = AssetPanel.GetComponent<SelectAsset>();
				panelData.Asset = obj;
				panelData.AssetNameText = obj.name;

				// Set position of Panel
				RectTransform rt = AssetPanel.GetComponent<RectTransform>();
				rt.anchoredPosition = new Vector2(x, y);

				// Set Preview Sprite of Panel
				SetPanelSprite(AssetPanel, obj, Color.black);

				//foreach (GameObject obj in ObjectsList) {
				//	// Create Panel
				//	AssetPanel = Instantiate(PanelPrefab, ObjectListContentPanel.transform);

				//}
			}
		}
	}



	// Create sprites for the assests menu
	void SetPanelSprite(GameObject obj, GameObject asset, Color BackgroundColor) {
		Image img = obj.GetComponent<Image>();
		if (img != null) {
			RuntimePreviewGenerator.BackgroundColor = BackgroundColor;
			Texture2D texture = RuntimePreviewGenerator.GenerateModelPreview(asset.transform, 100, 100, true);
			Rect rect = new Rect(0, 0, texture.width, texture.height);
			Vector2 pivot = new Vector2(1, 1);
			Sprite preview = Sprite.Create(texture, rect, pivot);
			img.sprite = preview;
		}
	}


}


/*
	public void OnClick() {
		mouseClicks++;
		if (mouseClicksStarted) {
			return;
		}
		mouseClicksStarted = true;
		Invoke("checkMouseDoubleClick", mouseTimerLimit);
	}


	private void checkMouseDoubleClick() {
		if (mouseClicks > 1) {
			Debug.Log("Double Clickedd");
			GameObject.Find("AssetsMenu").SetActive(false);

		} else {
			Debug.Log("Error: Double click to spawn object!");
		}
		mouseClicksStarted = false;
		mouseClicks = 0;
	}
}
*/