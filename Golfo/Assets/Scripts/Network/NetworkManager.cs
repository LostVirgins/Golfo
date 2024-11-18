using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace lv.network
{
    public class LobbyNameEvent : UnityEvent<string> { }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private IPAddress m_serverIP = IPAddress.Parse("127.0.0.1");
        private int m_serverPort = 9050;

        private UdpClient m_udpClient;
        private IPEndPoint m_serverEndPoint;
        private Dictionary<IPEndPoint, Player> m_connectedPlayers = new Dictionary<IPEndPoint, Player>();

        public Queue<PacketData> m_packetQueue { get; private set; } = new Queue<PacketData>();
        private float m_sendInterval = 0.05f;
        private float m_lastSendTime = 0f;

        public bool isHost;
        public string m_lobbyName;

        public LobbyNameEvent OnLobbyNameReceived = new LobbyNameEvent();

        private void Awake()
        {
            Instance = this;
            m_serverEndPoint = null;
            DontDestroyOnLoad(gameObject);
        }

        public void Start() { }

        public void Update()
        {
            while (m_packetQueue.Count != 0)
            {
                if (m_packetQueue.TryDequeue(out PacketData packetData))
                    ProcessPacket(packetData.m_packet, packetData.m_senderEP);
            }
        }

        public void StartHost(string lobbyName)
        {
            Debug.Log("Hosting game...");
            isHost = true;
            m_lobbyName = lobbyName;

            m_serverEndPoint = new IPEndPoint(m_serverIP, m_serverPort);

            m_udpClient = new UdpClient(m_serverPort);
            m_udpClient.BeginReceive(OnReceiveData, null);

            Player newPlayer = new Player($"{System.Guid.NewGuid()}");
            m_connectedPlayers[m_serverEndPoint] = newPlayer;

            Debug.Log("Server up and running. Waiting for new Players...");
        }

        public void JoinServer(string ipAddress, string username)
        {
            Debug.Log("Joining server...");
            isHost = false;
            m_serverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), m_serverPort);

            m_udpClient = new UdpClient(0);
            m_udpClient.BeginReceive(OnReceiveData, null);

            Packet authReqPacket = new Packet();
            authReqPacket.WriteByte((byte)PacketType.connection_request);
            authReqPacket.WriteString(username);

            SendPacket(authReqPacket, m_serverEndPoint);

            Debug.Log("Authentication request sent to server.");
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint senderEP = new IPEndPoint(0, 0);
            byte[] data = m_udpClient.EndReceive(result, ref senderEP);
            Packet packet = new Packet(data);
            m_packetQueue.Enqueue(new PacketData(packet, senderEP));

            m_udpClient.BeginReceive(OnReceiveData, null);
        }

        private void ProcessPacket(Packet packet, IPEndPoint senderEndPoint)
        {
            packet.SetStreamPos(0);
            PacketType packetType = (PacketType)packet.ReadByte();

            if (packetType == PacketType.connection_request)
            {
                ConnectionRequest(packet, senderEndPoint);
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
        }

        private void ProcessGamePacket(Packet packet, PacketType packetType, IPEndPoint clientEndPoint)
        {
            Debug.Log("Processing game packet...");

            switch (packetType)
            {
                case PacketType.lobby_name:     LobbyName(packet);  break;
                case PacketType.game_start:     GameStart();        break;
                case PacketType.game_end:       GameEnd();          break;
                case PacketType.ball_strike:    BallStrike();       break;
                case PacketType.player_turn:    PlayerTurn();       break;
                default: break;
            }
        }

        private void SendPacket(PacketData packetData)
        {
            byte[] data = packetData.m_packet.GetData();
            m_udpClient.Send(data, data.Length, packetData.m_senderEP);
        }

        private void SendPacket(Packet packet, IPEndPoint endPoint = null)
        {
            endPoint = endPoint ?? m_serverEndPoint;
            byte[] data = packet.GetData();
            m_udpClient.Send(data, data.Length, endPoint);
        }

        private void BroadcastPacket(Packet packet)
        {
            foreach (var client in m_connectedPlayers.Keys)
                SendPacket(packet, client);
        }

        private void AddPlayer(string sessionToken, IPEndPoint senderEndPoint)
        {
            m_connectedPlayers[senderEndPoint] = new Player(sessionToken);
            Debug.Log($"New Player {senderEndPoint} authenticated with session {sessionToken}");
        }

        private void OnApplicationQuit()
        {
            m_udpClient.Close();
        }


        // Packet Processing -------------------------------
        private void ConnectionRequest(Packet packet, IPEndPoint senderEndPoint)
        {
            string username = packet.ReadString();
            string sessionToken = "";
            PacketType status = AuthenticationManager.Instance.AuthenticateClient(username, m_serverEndPoint, ref sessionToken);

            if (status == PacketType.auth_success)
            {
                AddPlayer(sessionToken, senderEndPoint);

                Packet lobbyNamePacket = new Packet();
                lobbyNamePacket.WriteByte((byte)PacketType.lobby_name);
                lobbyNamePacket.WriteString(sessionToken);
                lobbyNamePacket.WriteString(m_lobbyName);
                m_packetQueue.Enqueue(new PacketData(lobbyNamePacket, m_serverEndPoint));
            }
            else
            {
                Debug.Log("Authentication failed.");
            }
        }

        private void LobbyName(Packet packet)
        {
            if (isHost)
                SendPacket(packet);
            else
                OnLobbyNameReceived.Invoke(packet.ReadString());
        }

        private void GameStart()
        {
            SceneManager.LoadScene(sceneName: "2_game_test");
        }

        private void GameEnd()
        {

        }

        private void BallStrike()
        {

        }

        private void PlayerTurn()
        {

        }


        // Get / Set --------------------------------------------
        public List<Player> GetConnectedPlayers()
        {
            List<Player> players = new List<Player>();

            foreach (var player in m_connectedPlayers.Values)
                players.Add(player);

            return players;
        }
    }
}
