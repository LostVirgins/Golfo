using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace lv.network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private UdpClient m_udpClient;
        private IPEndPoint m_serverEndPoint;
        private Dictionary<IPEndPoint, Player> m_connectedPlayers = new Dictionary<IPEndPoint, Player>();
        private Dictionary<IPEndPoint, string> m_clientSessions = new Dictionary<IPEndPoint, string>();


        private void Awake()
        {
            Instance = this;
            m_udpClient = new UdpClient(9050);
            m_udpClient.BeginReceive(OnReceiveData, null);
        }

        public void StartHost()
        {
            m_serverEndPoint = null;
            Debug.Log("Hosting game...");
        }

        public void JoinServer(string ipAddress)
        {
            m_serverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 9050);
            Debug.Log("Joining server...");
        }

        private void OnReceiveData(IAsyncResult result)
        {
            byte[] data = m_udpClient.EndReceive(result, ref m_serverEndPoint);
            Packet packet = new Packet(data);

            int packetType = packet.ReadInt();

            if (packetType == 0) // 0 = connection request (authentication)
            {
                AuthenticationStatus status = AuthenticationManager.Instance.AuthenticateClient(packet, m_serverEndPoint);

                if (status == AuthenticationStatus.Success)
                {
                    string sessionToken = packet.ReadString();
                    m_clientSessions[m_serverEndPoint] = sessionToken;

                    Player newPlayer = new Player(sessionToken);
                    m_connectedPlayers[m_serverEndPoint] = newPlayer;

                    Debug.Log($"Player authenticated with session {sessionToken}");
                }
                else
                {
                    Debug.Log("Authentication failed.");
                }
            }
            else
            {
                string clientSessionToken = packet.ReadString();

                AuthenticationStatus authStatus = AuthenticationManager.Instance.IsAuthenticated(m_serverEndPoint, clientSessionToken);

                switch (authStatus)
                {
                    case AuthenticationStatus.Success:
                        ProcessGamePacket(packet, m_serverEndPoint);
                        break;

                    case AuthenticationStatus.InvalidSession:
                        Debug.Log("Received packet with an invalid session token.");
                        break;

                    case AuthenticationStatus.SessionExpired:
                        Debug.Log("Received packet from a client with an expired session.");
                        break;

                    default:
                        Debug.Log("Unknown authentication status.");
                        break;
                }
            }

            m_udpClient.BeginReceive(OnReceiveData, null);
        }

        private void ProcessGamePacket(Packet packet, IPEndPoint clientEndPoint)
        {
            Debug.Log("Processing game packet...");
        }

        public void SendPacket(Packet packet, IPEndPoint endPoint = null)
        {
            endPoint = endPoint ?? m_serverEndPoint;
            byte[] data = packet.GetData();
            m_udpClient.Send(data, data.Length, endPoint);
        }

        private void OnApplicationQuit()
        {
            m_udpClient.Close();
        }
    }
}
