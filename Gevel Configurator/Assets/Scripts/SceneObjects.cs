using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneObjects : MonoBehaviour
{
    public GameObject assetsView;

    // Start is called before the first frame update
    void Start()
    {
        int counter = 0;
        int position = -10;

        foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
        {
            GameObject childObject = new GameObject("Object" + counter, typeof(RectTransform));
            childObject.transform.SetParent(this.assetsView.transform);

            // Position of object item
            RectTransform rt = childObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 20);
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

        RectTransform AssetsViewRt = assetsView.GetComponent<RectTransform>();
        AssetsViewRt.sizeDelta = new Vector2(0, assetsViewHeight);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
