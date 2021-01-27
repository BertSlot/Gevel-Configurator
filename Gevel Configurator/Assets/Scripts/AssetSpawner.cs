using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

<<<<<<< Updated upstream
public class AssetSpawner : MonoBehaviour {
=======
public class AssetSpawner : MonoBehaviour
{
>>>>>>> Stashed changes


    /// <summary>
    /// Array for GameObjects loaded from Import_Objects
    /// </summary>
    private Object[] ObjectsList;

    /// <summary>
    /// This object is a child of GameObject 'objectList' containing the list of child objects
    /// </summary>
    [SerializeField]
<<<<<<< Updated upstream
    private GameObject ObjectListContent;
=======
    private GameObject ObjectListContentPanel;
>>>>>>> Stashed changes

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


    // Add Object to a dictionary with Pair<Name,AssetPanel> for quick searching
    private void Awake()
    {
        int x = 40;
        int y = -40;
        if (ObjectsFolderPath != null)
        {
            // Load all objects from folder
            ObjectsList = Resources.LoadAll(ObjectsFolderPath, typeof(GameObject));
            GameObject AssetPanel;

            // Make a panel sprite for every object in the folder
            foreach (GameObject obj in ObjectsList)
            {
                // Create Panel
<<<<<<< Updated upstream
                AssetPanel = Instantiate(PanelPrefab, ObjectListContent.transform);
=======
                AssetPanel = Instantiate(PanelPrefab, ObjectListContentPanel.transform);
>>>>>>> Stashed changes

                // Add Asset to panel and name under panel
                SelectAsset panelData = AssetPanel.GetComponent<SelectAsset>();
                panelData.Asset = obj;
                panelData.AssetNameText = obj.name;

                // Set position of Panel
                RectTransform rt = AssetPanel.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, y);

                // Set Preview Sprite of Panel
                SetPanelSprite(AssetPanel, obj, Color.black);

<<<<<<< Updated upstream
                // Increase Offset
                x += 120;

            }
        }
    }

    // Create sprites for the assests menu
    private void SetPanelSprite(GameObject obj, GameObject asset, Color BackgroundColor)
    {
        Image img = obj.GetComponent<Image>();
        if (img != null)
        {
            RuntimePreviewGenerator.BackgroundColor = BackgroundColor;
            Texture2D texture = RuntimePreviewGenerator.GenerateModelPreview(asset.transform, 100, 100, true);
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 pivot = new Vector2(1, 1);
            Sprite preview = Sprite.Create(texture, rect, pivot);
            img.sprite = preview;
        }
    }


    private void Update()
    {
=======
                //foreach (GameObject obj in ObjectsList) {
                //	// Create Panel
                //	AssetPanel = Instantiate(PanelPrefab, ObjectListContentPanel.transform);

                //}
            }
        }

        // Create sprites for the assests menu
        void SetPanelSprite(GameObject obj, GameObject asset, Color BackgroundColor)
        {
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                RuntimePreviewGenerator.BackgroundColor = BackgroundColor;
                Texture2D texture = RuntimePreviewGenerator.GenerateModelPreview(asset.transform, 100, 100, true);
                Rect rect = new Rect(0, 0, texture.width, texture.height);
                Vector2 pivot = new Vector2(1, 1);
                Sprite preview = Sprite.Create(texture, rect, pivot);
                img.sprite = preview;
            }
        }

>>>>>>> Stashed changes

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