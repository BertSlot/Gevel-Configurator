using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class ObjectOpener : MonoBehaviour
{
    string path;
    public void OpenObject()
    {
        path = EditorUtility.OpenFilePanel("Overwrite with unity", "", "unity");
        CheckObject();

    }
    void CheckObject()
    {
        if (path != null)
        {
            //UpdateText();
            Debug.Log("Object geopend!");
        }
    }
}
