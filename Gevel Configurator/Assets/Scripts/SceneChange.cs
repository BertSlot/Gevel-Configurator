using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class SceneChange : MonoBehaviour
{

    //Load scene of project on button clicks
    public void Project1()
    {
        Text txtMy = GameObject.Find("StartMenu/LastProject1Button/Text").GetComponent<Text>();
        SceneManager.LoadScene(txtMy.text);
    }

    public void Project2()
    {
        Text txtMy = GameObject.Find("StartMenu/LastProject2Button/Text").GetComponent<Text>();
        SceneManager.LoadScene(txtMy.text);
    }

    public void Project3()
    {
        Text txtMy = GameObject.Find("StartMenu/LastProject3Button/Text").GetComponent<Text>();
        SceneManager.LoadScene(txtMy.text);
    }

    public void Project4()
    {
        Text txtMy = GameObject.Find("StartMenu/LastProject4Button/Text").GetComponent<Text>();
        SceneManager.LoadScene(txtMy.text);
    }

    public void Project5()
    {
        Text txtMy = GameObject.Find("StartMenu/LastProject5Button/Text").GetComponent<Text>();
        SceneManager.LoadScene(txtMy.text);
    }

    public void Project6()
    {
        Text txtMy = GameObject.Find("StartMenu/LastProject6Button/Text").GetComponent<Text>();
        SceneManager.LoadScene(txtMy.text);
    }

    public void StartMenuOpener()
    {
        SceneManager.LoadScene("Start Menu");
    }
}
