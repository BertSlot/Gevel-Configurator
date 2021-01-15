using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MinimalimalizeWindow : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // Makes Unity window disapear to taskbar
    public void OnMinimizeButtonClick()
    {
        ShowWindow(GetActiveWindow(), 2);
    }
}
