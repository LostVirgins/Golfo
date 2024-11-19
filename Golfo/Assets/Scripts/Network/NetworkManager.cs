using lv.gameplay;
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
    public class ChatMessageEvent : UnityEvent<string> { }
    public class BallStrikeEvent : UnityEvent<PacketData> { }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private IPAddress m_serverIP = IPAddress.Parse("127.0.0.1");
        private int m_serverPort = 9050;
        public IPEndPoint m_serverEndPoint { get; private set; }
        private UdpClient m_udpClient;
        public Dictionary<IPEndPoint, Player> m_connectedPlayers { get; private set; } = new Dictionary<IPEndPoint, Player>();

        public Queue<PacketData> m_sendQueue { get; private set; } = new Queue<PacketData>();
        public Queue<PacketData> m_receiveQueue { get; private set; } = new Queue<PacketData>();
        private float m_sendInterval = 0.05f;
        private float m_lastSendTime = 0f;

        public bool isHost;
        public string m_lobbyName;

        public LobbyNameEvent OnReceiveLobbyName = new LobbyNameEvent();
        public ChatMessageEvent OnReceiveChatMessage = new ChatMessageEvent();
        public BallStrikeEvent OnBallStrike = new BallStrikeEvent();

        private void Awake()
        {
            Instance = this;
            m_serverEndPoint = null;
            DontDestroyOnLoad(gameObject);
        }

        public void Start() { }

        public void Update()
        {
            while (m_sendQueue.Count != 0)
            {
                if (m_sendQueue.TryDequeue(out PacketData packetData))
                {
                    if (packetData.m_isBroadCast)
                        BroadcastPacket(packetData);
                    else
                        SendPacket(packetData);
                }
            }

            while (m_receiveQueue.Count != 0)
            {
                if (m_receiveQueue.TryDequeue(out PacketData packetData))
                    ProcessPacket(packetData);
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

            Packet authReq = new Packet();
            authReq.WriteByte((byte)PacketType.connection_request);
            authReq.WriteString(username);

            m_sendQueue.Enqueue(new PacketData(authReq, m_serverEndPoint));

            Debug.Log("Authentication request sent to server.");
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint senderEP = new IPEndPoint(0, 0);
            byte[] data = m_udpClient.EndReceive(result, ref senderEP);
            Packet packet = new Packet(data);
            m_receiveQueue.Enqueue(new PacketData(packet, senderEP));

            m_udpClient.BeginReceive(OnReceiveData, null);
        }

        private void ProcessPacket(PacketData packetData)
        {
            Debug.Log($"Processing packet from {packetData.m_remoteEP}");

            packetData.m_packet.SetStreamPos(0);
            PacketType packetType = (PacketType)packetData.m_packet.ReadByte();

            if (packetType == PacketType.connection_request)
            {
                ConnectionRequest(packetData.m_packet, packetData.m_remoteEP);
            }
            else
            {
                string clientSessionToken = packetData.m_packet.ReadString();
                //hekbas: manage authentication correctly in the future
                //PacketType authStatus = AuthenticationManager.Instance.IsAuthenticated(senderEndPoint, clientSessionToken);
                PacketType authStatus = PacketType.auth_success;

                switch (authStatus)
                {
                    case PacketType.auth_success:
                        ProcessGamePacket(packetType, packetData);
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

        private void ProcessGamePacket(PacketType packetType, PacketData packetData)
        {
            Debug.Log("Processing game packet...");

            switch (packetType)
            {
                case PacketType.lobby_name:     LobbyName(packetData);      break;
                case PacketType.chat_message:   ChatMessage(packetData);    break;
                case PacketType.game_start:     GameStart(packetData);      break;
                case PacketType.game_end:       GameEnd();                  break;
                case PacketType.ball_strike:    BallStrike(packetData);     break;
                case PacketType.player_turn:    PlayerTurn();               break;
                default: Debug.Log("Packet Type not found.");               break;
            }
        }

        private void SendPacket(PacketData packetData)
        {
            byte[] data = packetData.m_packet.GetData();
            m_udpClient.Send(data, data.Length, packetData.m_remoteEP);
        }

        private void SendPacket(Packet packet, IPEndPoint endPoint = null)
        {
            endPoint = endPoint ?? m_serverEndPoint;
            byte[] data = packet.GetData();
            m_udpClient.Send(data, data.Length, endPoint);
        }

        private void BroadcastPacket(PacketData packetData)
        {
            foreach (var client in m_connectedPlayers.Keys)
            {
                if (client.Equals(m_serverEndPoint)) continue;
                SendPacket(packetData.m_packet, client);
            }
        }

        private void BroadcastPacket(Packet packet)
        {
            //hekbas: this will also resend to origin!
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
                m_sendQueue.Enqueue(new PacketData(lobbyNamePacket, senderEndPoint));
            }
            else
            {
                Debug.Log("Authentication failed.");
            }
        }

        private void LobbyName(PacketData packetData)
        {
            if (isHost)
                SendPacket(packetData);
            else
                OnReceiveLobbyName.Invoke(packetData.m_packet.ReadString());
        }
        
        private void ChatMessage(PacketData packetData)
        {
            if (isHost)
                BroadcastPacket(packetData);

            OnReceiveChatMessage.Invoke(packetData.m_packet.ReadString());
        }

        private void GameStart(PacketData packetData)
        {
            byte length = packetData.m_packet.ReadByte();

            for (byte i = 0; i < length; i++)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());
                string sessionToken = packetData.m_packet.ReadString();
                Player player = new Player(sessionToken);
                m_connectedPlayers[ipEndPoint] = player;
            }

            SceneManager.LoadScene(sceneName: "2_game_test");
        }

        private void GameEnd()
        {

        }

        private void BallStrike(PacketData packetData)
        {
            if (isHost)
                BroadcastPacket(packetData);

            OnBallStrike.Invoke(packetData);
            //GameManager.Instance.OnBallStrike(packetData);
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


        // Helpers ----------------------------------------------
        private IPEndPoint ParseIPEndPoint(string endPointString)
        {
            string[] parts = endPointString.Split(':');
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ip))
            {
                if (int.TryParse(parts[1], out var port))
                    return new IPEndPoint(ip, port);
            }

            throw new FormatException($"Invalid IPEndPoint string: {endPointString}");
        }
    }
}
