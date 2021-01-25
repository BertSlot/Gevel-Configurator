using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseApplication : MonoBehaviour
{
    public void QuitApplication()
    {
        if (Application.isEditor){
            Debug.Log("Application will not close atm");
        }else {
            Application.Quit();
        }
        
    }
}
