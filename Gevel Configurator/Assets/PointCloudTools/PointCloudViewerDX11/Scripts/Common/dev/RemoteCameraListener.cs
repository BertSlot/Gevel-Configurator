using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace PointCloudRemote
{
    public class RemoteCameraListener : MonoBehaviour
    {
        // TODO display my ip as info somewhere
        // TODO send lower resolution when moving
        // TODO vr needs 2 eyes


        public int serverPort = 15000;
        public bool receivePosition = true;
        public Transform cameraTransform;

        // networking
        string ReceivedMsg;
        IPEndPoint ipEndPoint;
        private object obj = null;
        private AsyncCallback AC;
        byte[] receivedBytes;
        Thread receiveThread;
        UdpClient client;

        bool gotData = false;
        float[] floatArray;
        Vector3 eulerAngle;
        Vector3 newPosition;

        // Use this for initialization
        void Start()
        {
            InitializeUDPListener();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (gotData == true)
            {
                cameraTransform.eulerAngles = eulerAngle;
                if (receivePosition == true)
                {
                    cameraTransform.position = newPosition;
                }
                gotData = false;
            }
        }

        public void InitializeUDPListener()
        {
            if (receivePosition == true)
            {
                floatArray = new float[6];
            }
            else
            {
                floatArray = new float[3];
            }

            ipEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
            client.ExclusiveAddressUse = false;
            client.EnableBroadcast = true;
            client.Client.Bind(ipEndPoint);
            AC = new AsyncCallback(ParseUDPData);
            client.BeginReceive(AC, obj);
            Debug.Log("RemoteCamera: UDP - Start Receiving..");
        }

        void ParseUDPData(IAsyncResult result)
        {
            receivedBytes = (client.EndReceive(result, ref ipEndPoint));
            // TODO could validate len

            // 12 bytes for now 4*3 floats
//             Debug.Log("len=" + receivedBytes.Length);

            // skip parsing if still moving cam
            if (gotData == false)
            {
                // parse to floats
                //float[] floatArray = new float[receivedBytes.Length / 4];
                Buffer.BlockCopy(receivedBytes, 0, floatArray, 0, receivedBytes.Length);
                //Debug.Log("received:" + floatArray[0] + "," + floatArray[1] + "," + floatArray[2]);
                eulerAngle.x = floatArray[0];
                eulerAngle.y = floatArray[1];
                eulerAngle.z = floatArray[2];
                if (receivePosition == true)
                {
                    newPosition.x = floatArray[3];
                    newPosition.y = floatArray[4];
                    newPosition.z = floatArray[5];
                }
                gotData = true;
            }
            else
            {
                //receiving too fast..
            }


            client.BeginReceive(AC, obj);
        }


        private void OnDestroy()
        {
            if (client != null) client.Close();
        }

    }
}
