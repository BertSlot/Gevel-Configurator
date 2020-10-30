// sample code for checking bound collisions with cloud points
// NOTE: this is quite slow and uses mainthread currently

using UnityEngine;

namespace unitycoder_examples
{
    public class BoxCloudCollision : MonoBehaviour
    {
        PointCloudViewer.PointCloudManager pointCloudManager;
        Vector3 previousPos;

        Rigidbody rb;
        Renderer r;

        bool didHit = false;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            r = GetComponent<Renderer>();

            // get reference to point manager, to check collisions with
            pointCloudManager = PointCloudViewer.PointCloudManager.instance;
        }

        void FixedUpdate()
        {
            // if we hit once, we dont check collisions anymore (since we are stopped)
            if (didHit == true) return;

            // should call this check on separate thread
            if (pointCloudManager.BoundsIntersectsCloud(r.bounds) == true)
            {
                didHit = true;
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }
}