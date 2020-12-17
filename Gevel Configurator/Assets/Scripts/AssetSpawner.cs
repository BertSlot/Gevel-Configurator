using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetSpawner : MonoBehaviour
{

    bool mouseClicksStarted = false;
    int mouseClicks = 0;
    float mouseTimerLimit = .25f;
    
    // Start is called before the first frame update
    public void OnClick()
    {
        mouseClicks++;
        if (mouseClicksStarted)
        {
            return;
        }
        mouseClicksStarted = true;
        Invoke("checkMouseDoubleClick", mouseTimerLimit);
    }


    private void checkMouseDoubleClick()
    {
        if (mouseClicks > 1)
        {
            Debug.Log("Double Clickedd");
            GameObject.Find("AssetsMenu").SetActive(false);

        }
        else
        {
            Debug.Log("Error: Double click to spawn object!");
        }
        mouseClicksStarted = false;
        mouseClicks = 0;
    }
}
