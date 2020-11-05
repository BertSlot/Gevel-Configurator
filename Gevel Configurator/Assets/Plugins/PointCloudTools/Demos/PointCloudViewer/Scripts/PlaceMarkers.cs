// example script for placing markers on selected points

using UnityEngine;
using UnityEngine.UI;
using PointCloudViewer;
using UnityEngine.EventSystems;

namespace unitycoder_examples
{
    public class PlaceMarkers : MonoBehaviour
    {
        public PointCloudManager pointCloudManager;

        public GameObject prefab;
        public Vector3 offset = new Vector3(0, 0.5f, 0);
        Camera cam;

        void Start()
        {
            cam = Camera.main;

            // subscribe to event listener
            PointCloudManager.PointWasSelected -= PointSelected; // unsubscribe just in case
            PointCloudManager.PointWasSelected += PointSelected;
        }

        void PointSelected(Vector3 pos)
        {
            // we got point selected event, lets instantiate object there
            Debug.Log("Clicked " + pos);
            var go = Instantiate(prefab, pos + offset, Quaternion.identity) as GameObject;

            // fix for 2017.x
            go.transform.position = pos + offset;
            
            // find text component from our prefab
            var txt = go.GetComponentInChildren<Text>();
            if (txt != null)
            {
                // 2 decimals
                txt.text = (pos).ToString("0.0#");
            }
        }

        public virtual void Update()
        {
            // dont do pick if over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // left button click to select
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                ray.origin = ray.GetPoint(cam.nearClipPlane + 0.05f);

                // call threaded point picker, you can call this manually from your own scripts, by passing your own ray
                pointCloudManager.RunPointPickingThread(ray);
            }
        }

        void OnDestroy()
        {
            // unsubscribe
            PointCloudManager.PointWasSelected -= PointSelected;
        }
    }
}