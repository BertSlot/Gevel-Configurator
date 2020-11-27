using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelectObject : MonoBehaviour, IPointerClickHandler
{
    private GameObject selectedObject;
    private GameObject sideMenu;
    private GameObject gizmo;
    private GameObject objectsList;

    // Start is called before the first frame update
    void Start()
    {
        selectedObject = gameObject;
        sideMenu = GameObject.Find("SideMenu");
        gizmo = GameObject.Find("RTGizmoManager");
        objectsList = GameObject.Find("Objects");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        SceneObjects scene = sideMenu.GetComponent<SceneObjects>();
        RTG.GizmoManager manager = gizmo.GetComponent<RTG.GizmoManager>();

        string objectName = selectedObject.GetComponent<Text>().text;
        GameObject childObject = objectsList.transform.Find(objectName).gameObject;

        manager.HighlightGameObject(childObject);

        if (scene.lastSelected)
        {
            scene.DeselectObject();
        }

        // Set last selected object in SceneObject script
        scene.lastSelected = selectedObject;

        //Debug.Log(selectedObject);
        Text childText = selectedObject.GetComponent<Text>();
        childText.color = Color.white;
    }
}
