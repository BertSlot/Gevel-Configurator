// point picking example for v3 tiles viewer
// this script would change in future, so for now i'd suggest duplicate this script and make your own logic to manage first and 2nd pick

using unitycodercom_PointCloudBinaryViewer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PointPickerV3 : MonoBehaviour
{
    public PointCloudViewerTilesDX11 viewer;

    [Header("Settings")]
    [Tooltip("Offset from nearplane (meters). Don't select points nearer to this")]
    public float rayOffsetFromNearClip = 0;

    public Color lineColor = Color.yellow;

    [Tooltip("UI Text element to show measurement info")]
    public Text distanceUIText;

    [Tooltip("Disable point picking while this key is pressed (sometimes required, if you use left mousebutton for camera movement with alt keys)")]
    public KeyCode disabledWithKey = KeyCode.LeftAlt;

    protected bool isFirstPoint = true;
    protected bool haveFirstPoint = false;
    protected bool haveSecondPoint = false;
    protected Vector3 startPos;
    protected Vector3 endPos;
    protected Vector3 previousPoint = Vector3.zero;

    Camera cam;

    void Start()
    {
        cam = Camera.main;

        viewer.PointWasSelected -= PointSelected;
        viewer.PointWasSelected += PointSelected;
    }

    void Update()
    {
        // dont pick if mouse is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // left button click to select
        if (Input.GetMouseButtonDown(0) && Input.GetKey(disabledWithKey) == false)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            ray.origin = ray.GetPoint(cam.nearClipPlane + rayOffsetFromNearClip);

            //Debug.DrawRay(ray.origin, ray.direction*999, Color.blue, 33);

            // call threaded point picker, you can call this manually from your own scripts, by passing your own ray
            viewer.RunPointPickingThread(ray);
        }


        // drawline, if have 2 points selected
        if (haveFirstPoint == true && haveSecondPoint == true)
        {
            DrawLine(startPos, endPos);
        }
    }

    public virtual void DrawLine(Vector3 startPos, Vector3 endPos)
    {
        GLDebug.DrawLine(startPos, endPos, lineColor, 0, true);
    }

    // gets called when PointWasSelected event fires in viewer
    void PointSelected(Vector3 pos)
    {
        // editor debug line only
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
        }

        Debug.Log("(v3) Picked point at :" + pos);

        previousPoint = pos;
        isFirstPoint = !isFirstPoint; // flip boolean
    }
}
