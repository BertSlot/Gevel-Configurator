// sample code for spawning cubes

using UnityEngine;
namespace unitycoder_examples
{
    public class BoxSpawner : MonoBehaviour
    {
        public Rigidbody prefab;

        Camera cam;

        void Start()
        {
            cam = Camera.main;
        }

        void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var rb = Instantiate(prefab, cam.transform.position, Quaternion.identity) as Rigidbody;
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                rb.AddForce(ray.direction * 20, ForceMode.Impulse);
                Destroy(rb.gameObject, 3);
            }
        }
    }
}