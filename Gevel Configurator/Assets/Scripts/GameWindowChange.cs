using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class GameWindowChange : MonoBehaviour
{

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // Minimize Unity window
    public void MiniMizeScreen()
    {
        Screen.SetResolution(640, 480, false);
        Debug.Log("Minimize screen");
        GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(false);
        GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(true);
    }

    // Set Unity window to full screen
    public void MaxiMizeScreen()
    {
        Screen.SetResolution(1920, 1080, true);
        Debug.Log("Maximize screen");
        GameObject.Find("Navbar/UIButtons/MaximizeButton").SetActive(false);
        GameObject.Find("Navbar/UIButtons/MinimizeButton").SetActive(true);
    }

    // Makes Unity window disapear to taskbar
    public void OnMinimizeButtonClick()
    {
        ShowWindow(GetActiveWindow(), 2);
    }
}
