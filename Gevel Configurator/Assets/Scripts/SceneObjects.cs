using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneObjects : MonoBehaviour
{
    /// <summary>
    /// This scroll view object is serialized to set the object height
    /// </summary>
    public GameObject objectList;

    /// <summary>
    /// This object is a child of GameObject 'objectList' containing the list of child objects
    /// </summary>
    public GameObject objectListContent;

    /// <summary>
    /// From this object all child objects are retrieved
    /// </summary>
    public GameObject parentObject;

    // Start is called before the first frame update
    void Start()
    {
        int counter = 0;
        int position = -10;

        foreach (Transform go in parentObject.transform)
        {
            GameObject childObject = new GameObject("Object" + counter, typeof(RectTransform));
            childObject.transform.SetParent(this.objectListContent.transform);

            // Position of object item
            RectTransform rt = childObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 20);
            rt.anchoredPosition = new Vector2(0, position);

            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);

            // Text styling
            Text childText = childObject.AddComponent<Text>();
            childText.text = go.name;
            childText.color = Color.black;
            childText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            
            counter++;
            position += -20;
        }

        // Calculate height for objects list
        int assetsViewHeight = (20 * counter) + 20;

        RectTransform AssetsViewRt = objectListContent.GetComponent<RectTransform>();
        AssetsViewRt.sizeDelta = new Vector2(0, assetsViewHeight);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
