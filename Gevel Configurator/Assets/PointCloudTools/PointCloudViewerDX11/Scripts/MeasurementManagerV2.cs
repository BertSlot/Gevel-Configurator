using UnityEngine;
using UnityEngine.UI;
using PointCloudViewer;
using UnityEngine.EventSystems;

namespace PointCloudExtras
{
    public class MeasurementManagerV2 : MonoBehaviour
    {
        [Header("References & Settings")]
        public PointCloudManager pointCloudManager;

        public Text distanceUIText;
        public Color lineColor = Color.yellow;

        protected bool isFirstPoint = true;
        protected bool haveFirstPoint = false;
        protected bool haveSecondPoint = false;
        protected Vector3 startPos;
        protected Vector3 endPos;
        protected Vector3 previousPoint = Vector3.zero;

        public static float closestDistance = 1;

        [Header("Keyboard")]
        [Tooltip("Disable measure picking, if this key is down")] // this is to allow orbitin and clicking, without point picking
        public KeyCode altKey = KeyCode.LeftAlt;

        Camera cam;

        [Header("Point Selection Settings")]
        [Tooltip("Offset from nearplane (meters). Dont select points nearer to this")]
        public float rayOffsetFromNearClip = 0;

        void Start()
        {
            cam = Camera.main;

            closestDistance = 0;

            // subscribe to event listener
            PointCloudManager.PointWasSelected -= PointSelected; // unsubscribe just in case
            PointCloudManager.PointWasSelected += PointSelected;
        }

        // draw line between selected points
        public virtual void Update()
        {
            // dont do pick if over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // left button click to select
            if (Input.GetMouseButtonDown(0) && Input.GetKey(altKey) == false)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                ray.origin = ray.GetPoint(cam.nearClipPlane + rayOffsetFromNearClip);

                //Debug.DrawRay(ray.origin, ray.direction*99, Color.blue, 22);

                // call threaded point picker, you can call this manually from your own scripts, by passing your own ray
                pointCloudManager.RunPointPickingThread(ray);
            }


            if (haveFirstPoint == true && haveSecondPoint == false)
            {
                //            endPos = cam.ScreenToWorldPoint(Input.mousePosition);
                //            GLDebug.DrawLine(startPos, endPos, lineColor, 0, true);
            }
            else if (haveFirstPoint == true && haveSecondPoint == true)
            {
                DrawLine();
            }
        }

        public virtual void DrawLine()
        {
            GLDebug.DrawLine(startPos, endPos, lineColor, 0, true);
        }

        // gets called when PointWasSelected event fires in BinaryViewerDX11
        void PointSelected(Vector3 pos)
        {
            // debug lines only
            PointCloudMath.DebugHighLightPointGreen(pos);

            // was this the first selection
            if (isFirstPoint == true)
            {
                startPos = pos;
                if (distanceUIText != null) distanceUIText.text = "Measure: Select 2nd point";
                haveFirstPoint = true;
                haveSecondPoint = false;
            }
            else
            { // it was 2nd click
                endPos = pos;
                haveSecondPoint = true;

                var distance = Vector3.Distance(previousPoint, pos);
                if (distanceUIText != null) distanceUIText.text = "Distance:" + distance.ToString();
                Debug.Log("Distance:" + distance);
            }

            previousPoint = pos;
            isFirstPoint = !isFirstPoint; // flip boolean
        }

        private void OnDestroy()
        {
            // unsubscribe
            PointCloudManager.PointWasSelected -= PointSelected;
        }
    }
}