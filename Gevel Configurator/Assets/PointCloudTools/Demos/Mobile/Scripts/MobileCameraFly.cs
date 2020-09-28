using UnityEngine;
using System.Collections;

// Simple camerafly script for mobile (has issues in some rotation directions like directly up/down)
// usage: Attach to main camera, test in mobile

namespace unitycodercom_pointcloud_extras
{

    public class MobileCameraFly : MonoBehaviour
    {

        public float flySpeed = 10;
        public float rotateSpeed = 3;

        Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
        }

        void Update()
        {

            // touch screen to fly
            if (Input.touchCount > 0)
            {
                if (Input.touchCount == 1) // forward
                {
                    transform.Translate(transform.forward * flySpeed * Time.deltaTime, Space.World);
                }
                if (Input.touchCount == 2) // backwards with 2 touch (or with W and S key both in editor)
                {
                    transform.Translate(-transform.forward * flySpeed * Time.deltaTime, Space.World);
                }

                // rotate towards touch position
                var touchPos = cam.ScreenToViewportPoint(Input.GetTouch(0).position);
                transform.eulerAngles += new Vector3(Mathf.Lerp(rotateSpeed, -rotateSpeed, touchPos.y), Mathf.Lerp(-rotateSpeed, rotateSpeed, touchPos.x), 0);
            }
        }
    }
}
