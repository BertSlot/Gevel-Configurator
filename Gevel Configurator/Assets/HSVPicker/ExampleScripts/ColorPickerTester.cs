using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HSVPicker.Examples
{
    public class ColorPickerTester : MonoBehaviour 
    {
        /// <summary>
        /// Change color button object
        /// </summary>
        public Button changeColor;

        /// <summary>
        /// Contains the color picker menu
        /// </summary>
        public GameObject colorPickerMenu;

        /// <summary>
        /// This object displays the selected color
        /// </summary>
        public Image colorPreview;

        /// <summary>
        /// Contains the color picker
        /// </summary>
        public ColorPicker picker;

        public Color Color = Color.red;

        // Use this for initialization
        void Start()
        {
            changeColor.onClick.AddListener(ToggleColorPickerMenu);
        }

        public void ChangeColor(Renderer renderer)
        {
            // First remove previous listeners
            picker.onValueChanged.RemoveAllListeners();

            // Change color
            picker.onValueChanged.AddListener(color =>
            {
                colorPreview.color = color;
                renderer.material.color = color;
                Color = color;
            });
        }

        /// <summary>
        /// Toggle color picker menu
        /// </summary>
        void ToggleColorPickerMenu()
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