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
                // Set rendering mode to transparent if color alpha is < 1
                if (color.a < 1)
                {
                    renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    renderer.material.SetInt("_ZWrite", 0);
                    renderer.material.DisableKeyword("_ALPHATEST_ON");
                    renderer.material.DisableKeyword("_ALPHABLEND_ON");
                    renderer.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    renderer.material.renderQueue = 3000;
                } else
                {
                    renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    renderer.material.SetInt("_ZWrite", 1);
                    renderer.material.DisableKeyword("_ALPHATEST_ON");
                    renderer.material.DisableKeyword("_ALPHABLEND_ON");
                    renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    renderer.material.renderQueue = -1;
                }

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