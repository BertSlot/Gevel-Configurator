using UnityEngine;
using UnityEngine.UI;

namespace HSVPicker.Examples
{
    public class ColorPickerTester : MonoBehaviour 
    {
        public Button changeColor;
        public GameObject colorPickerMenu;
        public Image colorPreview;
        //public new Renderer renderer;
        public ColorPicker picker;

        public Color Color = Color.red;
        public bool SetColorOnStart = false;

	    // Use this for initialization
	    void Start () 
        {
            picker.onValueChanged.AddListener(color =>
            {
                colorPreview.color = color;
                //renderer.material.color = color;
                Color = color;
            });

		    //renderer.material.color = picker.CurrentColor;
            if (SetColorOnStart) 
            {
                picker.CurrentColor = Color;
            }

            changeColor.onClick.AddListener(openColorPickerMenu);
        }
	
	    // Update is called once per frame
	    void Update () {
	
	    }

        void openColorPickerMenu()
        {
            if (colorPickerMenu.active)
            {
                colorPickerMenu.SetActive(false);
            } else
            {
                colorPickerMenu.SetActive(true);
            }
            
        }

        void closeColorPickerMenu()
        {

        }
    }
}