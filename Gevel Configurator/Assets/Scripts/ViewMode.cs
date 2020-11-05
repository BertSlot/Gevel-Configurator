using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewMode : MonoBehaviour
{
    public GameObject EditMode;
    public GameObject PresentMode;

    // Start is called before the first frame update
    void Start()
    {
        PresentMode.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowEditMode()
    {
        EditMode.gameObject.SetActive(true);
        PresentMode.gameObject.SetActive(false);
    }

    public void ShowPresentMode()
    {
        EditMode.gameObject.SetActive(false);
        PresentMode.gameObject.SetActive(true);
    }
}
