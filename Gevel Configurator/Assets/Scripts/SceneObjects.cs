using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneObjects : MonoBehaviour
{
    public GameObject assetsView;

    //List<GameObject> objectList = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        int counter = 0;
        int position = 0;

        foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
        {
            GameObject childObject = new GameObject("Object" + counter, typeof(RectTransform));
            childObject.transform.SetParent(this.assetsView.transform);

            RectTransform rt = childObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 50);
            rt.anchoredPosition = new Vector2(0, position);

            Text childText = childObject.AddComponent<Text>();
            childText.text = go.name;
            childText.color = Color.black;
            childText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

            //Debug.Log(go);
            counter++;
            position += 20;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
