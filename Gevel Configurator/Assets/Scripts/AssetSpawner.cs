using UnityEngine.UI;
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
	private GameObject ObjectListContent;

	/// <summary>
	/// Path to Import_Objects Folder
	/// </summary>
	[SerializeField]
	private string ObjectsFolderPath = "Import_Objects";

	/// <summary>
	/// AssetPanel prefab
	/// </summary>
	[SerializeField]
	private GameObject PanelPrefab;




	/*bool mouseClicksStarted = false;
	int mouseClicks = 0;
	float mouseTimerLimit = .25f;
*/

	// Add Object to a dictionary with Pair<Name,AssetPanel> for cquick searching
	private void Awake() {

		int x = 40;
		int y = -40;
		if (ObjectsFolderPath != null) {
			ObjectsList = Resources.LoadAll(ObjectsFolderPath, typeof(GameObject));
			GameObject AssetPanel;

			foreach (GameObject obj in ObjectsList) {
				// Create Panel
				AssetPanel = Instantiate(PanelPrefab, ObjectListContent.transform);

				// Add Asset to panel and name under panel
				SelectAsset panelData = AssetPanel.GetComponent<SelectAsset>();
				panelData.Asset = obj;
				panelData.AssetNameText = obj.name;

				// Set position of Panel
				RectTransform rt = AssetPanel.GetComponent<RectTransform>();
				rt.anchoredPosition = new Vector2(x, y);

				// Set Preview Sprite of Panel
				SetPanelSprite(AssetPanel, obj, Color.black);

				// Increase Offset
				x += 120;

			}
		}


	}

	private void SetPanelSprite(GameObject obj, GameObject asset, Color BackgroundColor) {
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


	private void Update() {

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