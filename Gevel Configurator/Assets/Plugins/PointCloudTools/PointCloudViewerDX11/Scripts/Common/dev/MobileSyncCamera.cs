using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace PointCloudRemote
{
    public class MobileSyncCamera : MonoBehaviour
    {
        public string serverAddress = "127.0.0.1";
        public int serverPort = 15000;

        public bool sendPosition = true;


        Socket sock;
        IPAddress serverAddr;
        IPEndPoint endPoint;
        bool isConnected = false;

        IEnumerator Start()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverAddr = IPAddress.Parse(serverAddress);
            endPoint = new IPEndPoint(serverAddr, serverPort);

            while (SocketConnected(sock) == false)
            {
                Debug.Log("Trying to connect..");
                yield return new WaitForSeconds(1);
            }

            isConnected = true;
            Debug.Log("Connected!");
        }

        void LateUpdate()
        {
            if (isConnected == false) return;
            // convert data to byte array
            float[] floatArray;

            if (sendPosition == false)
            {
                floatArray = new float[] { transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z };
            }
            else
            {
                floatArray = new float[] { transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z, transform.position.x, transform.position.y, transform.position.z };
            }
            var byteArray = new byte[floatArray.Length * 4];
            Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
            sock.SendTo(byteArray, endPoint);
        }

        private void OnDestroy()
        {
            sock.Close();
        }

        // https://stackoverflow.com/a/2661876/5452781
        bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
    }
}