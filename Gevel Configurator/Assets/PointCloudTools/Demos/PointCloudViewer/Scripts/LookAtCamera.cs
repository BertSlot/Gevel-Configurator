// example script for making object always look towards camera

using UnityEngine;

namespace unitycoder_examples
{
    public class LookAtCamera : MonoBehaviour
    {
        Transform cam;

        void Start()
        {
            cam = Camera.main.transform;
        }

        void LateUpdate()
        {
            transform.LookAt(cam);
        }
    }
}