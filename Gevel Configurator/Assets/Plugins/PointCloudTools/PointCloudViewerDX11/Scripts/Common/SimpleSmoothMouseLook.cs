// Very simple smooth mouselook for the MainCamera in Unity
// original code, by Francis R. Griffiths-Keam - www.runningdimensions.com
// http://forum.unity3d.com/threads/73117-A-Free-Simple-Smooth-Mouselook
// this is heavily modified version

using UnityEngine;

namespace unitycodercom_pointcloud_extras
{
    public class SimpleSmoothMouseLook : MonoBehaviour
    {
        [Header("References")]
        public unitycodercom_PointCloudBinaryViewer.PointCloudViewerDX11 activeCloudViewer;

        [Header("Mouse Look")]
        [Tooltip("Hold right mouse button down to rotate")]
        public bool rotateWithRightMouse = true;
        public Vector2 clampInDegrees = new Vector2(360, 180);
        public bool lockCursor = false;
        public Vector2 sensitivity = new Vector2(2, 2);
        public Vector2 smoothing = new Vector2(3, 3);
        Vector2 targetDirection;
        Vector2 _mouseAbsolute;
        Vector2 _smoothMouse;

        [Header("Camera Pan")]
        public bool enablePanning = true;
        public float panSpeed = 40;

        [Header("Camera Orbit")]
        public bool enableOrbit = false;
        public KeyCode orbitKey = KeyCode.LeftAlt;
        [Tooltip("Set this to -1, if you dont require any mousebutton pressed for orbit")]
        public int orbitMouseKey = 2;
        public float orbitXSpeed = 400;
        public float orbitYSpeed = 400;
        Vector3 targetposition;
        float orbitDistance = 10.0f;
        private float xDeg = 0.0f;
        private float yDeg = 0.0f;

        [Header("Camera Zoom")]
        public bool scrollZoom = false;
        public int zoomRate = 40;
        public float zoomDampening = 5.0f;
        public KeyCode zoomExtends = KeyCode.F; // whole cloud

        [Header("Camera Fly")]
        public float flySpeed = 20; // default speed
        public float accelerationRatio = 5; // when shift is pressed
        public float slowDownRatio = 0.5f; // when ctrl is pressed
        public KeyCode increaseSpeedKey = KeyCode.LeftShift;
        public KeyCode decreaseSpeedKey = KeyCode.LeftControl;
        public KeyCode upKey = KeyCode.E;
        public KeyCode downKey = KeyCode.Q;
        public string horizontalAxisKey = "Horizontal";
        public string verticalAxisKey = "Vertical";

        Camera cam;

        void Awake()
        {
            cam = Camera.main;

            // Set target direction to the camera's initial orientation
            targetDirection = transform.rotation.eulerAngles;
        }

        void Update()
        {
            Framing();
            if (MouseOrbit() == true) return;
            MouseLook();
            CameraFly();
        }

        void Framing()
        {
            if (Input.GetKeyUp(zoomExtends))
            {
                // FIXME this only accepts single viewer as target..wont work with multiple ones..
                if (activeCloudViewer != null) FocusCameraOnGameObject(activeCloudViewer.GetBounds());
            }
        }

        public void GetOrbitTarget()
        {
            targetposition = transform.forward * orbitDistance;
            //orbitDistance = Vector3.Distance(transform.position, targetposition);
        }


        bool MouseOrbit()
        {
            // early exit, if not enabled, or no key is pressed, or if mousekey is set, but not pressed
            if (enableOrbit == false || Input.GetKey(orbitKey) == false || (orbitMouseKey > -1 == true && Input.GetMouseButton(orbitMouseKey) == false))
            {
                return false;
            }

            orbitDistance += Input.GetAxis("Mouse ScrollWheel") * zoomRate * Time.deltaTime;

            xDeg = (Input.GetAxis("Mouse X") * orbitXSpeed);
            yDeg = -(Input.GetAxis("Mouse Y") * orbitYSpeed);
            transform.RotateAround(targetposition, Vector3.up, xDeg * Time.deltaTime);
            transform.RotateAround(targetposition, transform.right, yDeg * Time.deltaTime);

            targetDirection = transform.rotation.eulerAngles;

            return true;
        }

        static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }

        void MouseLook()
        {
            // Get raw mouse input for a cleaner reading on more sensitive mice.
            var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            // panning with middle moouse button
            if (enablePanning == true)
            {
                if (Input.GetMouseButton(2))
                {
                    transform.Translate(-mouseDelta * panSpeed * Time.deltaTime, Space.Self);
                    GetOrbitTarget(); // update orbit target
                }
            }

            // Ensure the cursor is always locked when set
            if (lockCursor) Cursor.lockState = CursorLockMode.Locked;

            if (rotateWithRightMouse == true && !Input.GetMouseButton(1)) return;

            // Allow the script to clamp based on a desired target value.
            Quaternion targetOrientation = Quaternion.Euler(targetDirection);

            // Scale input against the sensitivity setting and multiply that against the smoothing value.
            mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));
            // Interpolate mouse movement over time to apply smoothing delta.
            _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
            _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);
            // Find the absolute mouse movement value from point zero.
            _mouseAbsolute += _smoothMouse;

            // Clamp and apply the local x value first, so as not to be affected by world transforms.
            if (clampInDegrees.x < 360) _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

            var xRotation = Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right);
            transform.localRotation = xRotation;

            // Then clamp and apply the global y value.
            if (clampInDegrees.y < 360) _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, transform.InverseTransformDirection(Vector3.up));
            transform.localRotation *= yRotation;
            transform.rotation *= targetOrientation;
        }

        void CameraFly()
        {
            bool moved = false;
            // shift was pressed down
            if (Input.GetKeyDown(increaseSpeedKey))
            {
                flySpeed *= accelerationRatio; // increase flyspeed
            }

            if (Input.GetKeyUp(increaseSpeedKey))
            {
                flySpeed /= accelerationRatio; // flyspeed back to normal
            }

            if (Input.GetKeyDown(decreaseSpeedKey))
            {
                flySpeed *= slowDownRatio; // decrease flyspeed
            }
            if (Input.GetKeyUp(decreaseSpeedKey))
            {
                flySpeed /= slowDownRatio; // flyspeed back to normal
            }

            if (Input.GetAxis(verticalAxisKey) != 0)
            {
                // TODO combine all moves to one vector
                transform.Translate(transform.forward * flySpeed * Input.GetAxis(verticalAxisKey) * Time.deltaTime, Space.World);
                moved = true;
            }
            if (Input.GetAxis(horizontalAxisKey) != 0)
            {
                transform.Translate(transform.right * flySpeed * Input.GetAxis(horizontalAxisKey) * Time.deltaTime, Space.World);
                moved = true;
            }

            if (scrollZoom == true)
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0)
                {
                    transform.Translate(transform.forward * zoomRate * Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime, Space.World);
                    moved = true;
                }
            }

            if (Input.GetKey(upKey))
            {
                transform.Translate(transform.up * flySpeed * 0.5f * Time.deltaTime, Space.World);
                moved = true;
            }
            else if (Input.GetKey(downKey))
            {
                transform.Translate(-transform.up * flySpeed * 0.5f * Time.deltaTime, Space.World);
                moved = true;
            }
            if (moved == true) GetOrbitTarget();
        } // CameraFly()

        // https://stackoverflow.com/questions/25368259/making-an-object-fit-exactly-inside-the-camera-frustum-in-three-js
        public void FocusCameraOnGameObject(Bounds b)
        {
            var height = b.max.y;
            var width = b.max.x - b.min.x;
            var vertical_FOV = cam.fieldOfView * (Mathf.PI / 180f);
            var aspectRatio = Screen.width / Screen.height;
            var horizontal_FOV = 2 * Mathf.Atan(Mathf.Tan(vertical_FOV / 2) * aspectRatio);
            var distance_vertical = height / (2 * Mathf.Tan(vertical_FOV / 2));
            var distance_horizontal = width / (2 * Mathf.Tan(horizontal_FOV / 2));
            var z_distance = distance_vertical >= distance_horizontal ? distance_vertical : distance_horizontal;
            // current viewdir
            var viewDir = transform.forward;
            // move camera so that object is in front
            cam.transform.position = b.center - viewDir * z_distance * 2;
        }

    } // class 
} // namespace