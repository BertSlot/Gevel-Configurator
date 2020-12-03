﻿using System.Collections;
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

    // Dimension input field objects in properties menu
    private GameObject xInputObject;
    private GameObject yInputObject;
    private GameObject zInputObject;

    // Start is called before the first frame update
    void Start()
    {
        selectedObject = gameObject;
        sideMenu = GameObject.Find("SideMenu");
        gizmo = GameObject.Find("RTGizmoManager");
        objectsList = GameObject.Find("Objects");

        // Find dimensions fields
        xInputObject = GameObject.Find("XInput");
        yInputObject = GameObject.Find("YInput");
        zInputObject = GameObject.Find("ZInput");
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

        // Higlight gameobject in editor
        manager.HighlightGameObject(childObject);

        if (scene.lastSelected)
        {
            scene.DeselectObject();
        }

        // Set last selected object in SceneObject script
        scene.lastSelected = selectedObject;

        // Hightlight gameobject in side menu
        Text childText = selectedObject.GetComponent<Text>();
        childText.color = Color.white;

        Renderer objectRenderer = childObject.GetComponent<Renderer>();

        // Set dimensions in properties menu
        xInputObject.GetComponent<InputField>().text = childObject.GetComponent<Renderer>().bounds.size.x.ToString();
        yInputObject.GetComponent<InputField>().text = childObject.GetComponent<Renderer>().bounds.size.y.ToString();
        zInputObject.GetComponent<InputField>().text = childObject.GetComponent<Renderer>().bounds.size.z.ToString();
    }
}
