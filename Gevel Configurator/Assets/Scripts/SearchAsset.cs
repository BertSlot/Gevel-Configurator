using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchAsset : MonoBehaviour
{

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
    /// AssetPanel prefab
    /// </summary>
    [SerializeField]
    private GameObject PanelPrefab;

    /// <summary>
    /// Text field for the input
    /// </summary>
    public Text SeachInput;

    /// <summary>
    /// Parent object for removed children objects
    /// </summary>
    public GameObject parent;

    /// <summary>
    /// String for input of objects
    /// </summary>
    private string ObjectsFolderPath;


    // checks input field in AssetsMenu
    public void SearchAssets()
    {
        string assetName = SeachInput.text.ToString();

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            parent.transform.GetChild(i).gameObject.SetActive(false);
        }

        ObjectsFolderPath = "Import_Objects/" + assetName;
        SpawnAsset(ObjectsFolderPath);
    }

    // Spawns assets with name from function above
    private void SpawnAsset(string AssetName)
    {
        int x = 40;
        int y = -40;
        if (AssetName != null)
        {
            // Load all objects from folder
            ObjectsList = Resources.LoadAll(AssetName, typeof(GameObject));
            GameObject AssetPanel;

            // Make a panel sprite for every object in the folder
            foreach (GameObject obj in ObjectsList)
            {
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
    }
}
