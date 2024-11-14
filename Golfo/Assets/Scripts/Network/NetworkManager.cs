using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor.Sprites;

namespace lv.network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private IPAddress m_serverIP = IPAddress.Parse("127.0.0.1");
        private int m_serverPort = 9050;

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

        public void Start()
        {
            StartHost();
            JoinServer(m_serverIP.ToString());
        }

        public void StartHost()
        {
            m_serverEndPoint = null;
            Debug.Log("Hosting game...");

            if (m_udpClient == null)
            {
                m_udpClient = new UdpClient(m_serverPort);
                m_udpClient.BeginReceive(OnReceiveData, null);
            }

            Debug.Log("Server up and running. Waiting for new Players...");
        }

        public void JoinServer(string ipAddress)
        {
            m_serverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), m_serverPort);
            Debug.Log("Joining server...");

            Packet authReqPacket = new Packet();
            authReqPacket.WriteByte((byte)PacketType.connection_request);
            authReqPacket.WriteString("hekbas");

            SendPacket(authReqPacket, m_serverEndPoint);

            Debug.Log("Authentication request sent to server.");
        }

        private void OnReceiveData(IAsyncResult result)
        {
            byte[] data = m_udpClient.EndReceive(result, ref m_serverEndPoint);
            Packet packet = new Packet(data);

            PacketType packetType = (PacketType)packet.ReadByte();

            if (packetType == PacketType.connection_request)
            {
                string username = packet.ReadString();
                ConnectionRequest(username);
            }
            else
            {
                string clientSessionToken = packet.ReadString();
                PacketType authStatus = AuthenticationManager.Instance.IsAuthenticated(m_serverEndPoint, clientSessionToken);

                switch (authStatus)
                {
                    case PacketType.auth_success:
                        ProcessGamePacket(packet, packetType, m_serverEndPoint);
                        break;

                    case PacketType.invalid_session:
                        Debug.Log("Received packet with an invalid session token.");
                        break;

                    case PacketType.expired_session:
                        Debug.Log("Received packet from a client with an expired session.");
                        break;

                    default:
                        Debug.Log("Unknown authentication status.");
                        break;
                }
            }

            m_udpClient.BeginReceive(OnReceiveData, null);
        }

        private void ProcessGamePacket(Packet packet, PacketType packetType, IPEndPoint clientEndPoint)
        {
            Debug.Log("Processing game packet...");

            switch (packetType)
            {
                case PacketType.player_movement:    ProccessPlayerMovement();  break;
                case PacketType.player_turn:        ProccessPlayerTurn();      break;
                default: break;
            }
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



        // Packet Managing -------------------------------

        private void ConnectionRequest(string username)
        {
            string sessionToken = "";
            PacketType status = AuthenticationManager.Instance.AuthenticateClient(username, m_serverEndPoint, ref sessionToken);

            if (status == PacketType.auth_success)
            {
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

        private void ProccessPlayerMovement()
        {
            
        }

        private void ProccessPlayerTurn()
        {

        }
    }
}
