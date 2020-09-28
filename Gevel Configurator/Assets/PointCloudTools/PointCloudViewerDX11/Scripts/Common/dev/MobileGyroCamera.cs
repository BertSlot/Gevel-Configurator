using UnityEngine;

namespace PointCloudRemote
{
    public class MobileGyroCamera : MonoBehaviour
    {
        // https://github.com/Wave-Unity-Library/Motion-Mapping#gyro-cam
        public float slerpValue = 0.2f;
        public int verticalOffsetAngle = -90;
        public int horizontalOffsetAngle = 0;
        private Quaternion phoneOrientation;
        private Quaternion correctedPhoneOrientation;
        private Quaternion horizontalRotationCorrection;
        private Quaternion verticalRotationCorrection;
        private Quaternion inGameOrientation;

        void Start()
        {
            Input.gyro.enabled = true;
        }

        void Update()
        {
            // TODO: gyro stuff not required in VR mode

            // camera orientation with gyro
            phoneOrientation = Input.gyro.attitude;
            correctedPhoneOrientation = new Quaternion(phoneOrientation.x, phoneOrientation.y, -phoneOrientation.z, -phoneOrientation.w);
            verticalRotationCorrection = Quaternion.AngleAxis(verticalOffsetAngle, Vector3.left);
            horizontalRotationCorrection = Quaternion.AngleAxis(horizontalOffsetAngle, Vector3.up);
            inGameOrientation = horizontalRotationCorrection * verticalRotationCorrection * correctedPhoneOrientation;
            transform.rotation = Quaternion.Slerp(transform.rotation, inGameOrientation, slerpValue);
        }
    }
}