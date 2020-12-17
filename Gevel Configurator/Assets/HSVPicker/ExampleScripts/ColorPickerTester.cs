using UnityEngine;
using UnityEngine.UI;

namespace HSVPicker.Examples
{
    public class ColorPickerTester : MonoBehaviour 
    {
        public Button changeColor;
        public GameObject colorPickerMenu;
        public Image colorPreview;
        public ColorPicker picker;

        public Color Color = Color.red;
        public bool SetColorOnStart = false;

        private new Renderer renderer;

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

            changeColor.onClick.AddListener(toggleColorPickerMenu);
        }

        /// <summary>
        /// Toggle color picker menu
        /// </summary>
        void toggleColorPickerMenu()
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