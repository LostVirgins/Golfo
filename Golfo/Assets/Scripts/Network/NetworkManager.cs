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

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private IPAddress m_serverIP = IPAddress.Parse("127.0.0.1");
        private int m_serverPort = 9050;
        public IPEndPoint m_hostEndPoint { get; private set; }

        public IPEndPoint m_localEndPoint { get; private set; }
        public UdpClient m_udpClient { get; private set; }
        public Dictionary<IPEndPoint, Player> m_players { get; private set; } = new Dictionary<IPEndPoint, Player>();

        private Queue<PacketData> m_sendQueue = new Queue<PacketData>();
        private Queue<PacketData> m_receiveQueue = new Queue<PacketData>();
        private float m_tickRate = 0.10f; // 100ms - 10 ticks/s
        private float m_lastTickTime = 0f;

        public bool isHost;
        public string m_lobbyName;

        public LobbyNameEvent OnReceiveLobbyName = new LobbyNameEvent();
        public ChatMessageEvent OnReceiveChatMessage = new ChatMessageEvent();

        private void Awake()
        {
            Instance = this;
            m_hostEndPoint = null;
            DontDestroyOnLoad(gameObject);
        }

        public void Start() { }

        public void Update()
        {
            if (GameManager.Instance != null)
                PlayersInfo();

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
                case PacketType.lobby_name:         LobbyName(packetData);          break;
                case PacketType.chat_message:       ChatMessage(packetData);        break;
                case PacketType.game_start:         GameStart(packetData);          break;
                case PacketType.game_end:           GameEnd();                      break;
                case PacketType.ball_strike:        BallStrike(packetData);         break;
                case PacketType.player_position:    PlayerPosition(packetData);     break;
                case PacketType.player_turn:        PlayerTurn();                   break;
                default: Debug.Log("Packet Type not found.");                       break;
            }
        }

        private void SendPacket(PacketData packetData)
        {
            byte[] data = packetData.m_packet.GetData();
            m_udpClient.Send(data, data.Length, packetData.m_remoteEP);
        }

        private void SendPacket(Packet packet, IPEndPoint endPoint = null)
        {
            endPoint = endPoint ?? m_hostEndPoint;
            byte[] data = packet.GetData();
            m_udpClient.Send(data, data.Length, endPoint);
        }

        private void BroadcastPacket(PacketData packetData)
        {
            foreach (var client in m_players.Keys)
            {
                if (client.Equals(m_hostEndPoint)) continue;
                SendPacket(packetData.m_packet, client);
            }
        }

        private void BroadcastPacket(Packet packet)
        {
            //hekbas: this will also resend to origin!
            foreach (var client in m_players.Keys)
                SendPacket(packet, client);
        }

        private void AddPlayer(string sessionToken, IPEndPoint senderEndPoint)
        {
            m_players[senderEndPoint] = new Player(sessionToken);
            Debug.Log($"New Player {senderEndPoint} authenticated with session {sessionToken}");
        }

        private void PlayersInfo()
        {
            m_lastTickTime += Time.deltaTime;
            if (isHost)
            {
                if (m_lastTickTime >= m_tickRate)
                {
                    m_lastTickTime = 0;

                    Packet playersPos = new Packet();
                    playersPos.WriteByte((byte)PacketType.player_position);
                    playersPos.WriteInt(m_players.Count);

                    foreach (var player in m_players)
                    {
                        playersPos.WriteString(player.Key.ToString());
                        playersPos.WriteVector3(player.Value.m_golfBall.transform.position);
                    }

                    EnqueueSend(new PacketData(playersPos, m_hostEndPoint, true));
                }
            }
            else
            {
                if (m_lastTickTime >= m_tickRate)
                {
                    m_lastTickTime = 0;

                    Packet playerPos = new Packet();
                    playerPos.WriteByte((byte)PacketType.player_position);
                    playerPos.WriteString(m_localEndPoint.ToString());
                    playerPos.WriteVector3(GameManager.Instance.m_player.transform.position);
                    EnqueueSend(new PacketData(playerPos, m_hostEndPoint, false));
                }
            }
        }

        private void OnApplicationQuit()
        {
            m_udpClient.Close();
        }


        // Packet Processing ----------------------------------------------------------
        private void ConnectionRequest(Packet packet, IPEndPoint senderEndPoint)
        {
            string username = packet.ReadString();
            string sessionToken = "";
            PacketType status = AuthenticationManager.Instance.AuthenticateClient(username, m_hostEndPoint, ref sessionToken);

            if (status == PacketType.auth_success)
            {
                AddPlayer(sessionToken, senderEndPoint);

                Packet lobbyNamePacket = new Packet();
                lobbyNamePacket.WriteByte((byte)PacketType.lobby_name);
                lobbyNamePacket.WriteString(sessionToken);
                lobbyNamePacket.WriteString(senderEndPoint.ToString());
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
            {
                m_localEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());
                OnReceiveLobbyName.Invoke(packetData.m_packet.ReadString());
            }
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
                m_players[ipEndPoint] = player;
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

            GameManager.Instance.OnBallStrike(packetData);
        }

        private void PlayerPosition(PacketData packetData)
        {
            if (isHost)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());

                if (m_players.ContainsKey(ipEndPoint))
                    m_players[ipEndPoint].m_golfBall.transform.position = packetData.m_packet.ReadVector3();
            }
            else
            {
                GameManager.Instance.OnNetworkPlayerPosition(packetData);
            }
        }

        private void PlayerTurn()
        {

        }


        // Public Methods ---------------------------------------------------------------

        /// <summary>
        /// Starts hosting a game, initializes the server, and sets up the host's networking configuration.
        /// </summary>
        /// <param name="lobbyName">The name of the lobby to be hosted.</param>
        public void StartHost(string lobbyName)
        {
            Debug.Log("Hosting game...");
            isHost = true;
            m_lobbyName = lobbyName;

            m_hostEndPoint = new IPEndPoint(m_serverIP, m_serverPort);
            m_localEndPoint = m_hostEndPoint;

            m_udpClient = new UdpClient(m_serverPort);
            m_udpClient.BeginReceive(OnReceiveData, null);

            Player newPlayer = new Player($"{System.Guid.NewGuid()}");
            m_players[m_hostEndPoint] = newPlayer;

            Debug.Log("Server up and running. Waiting for new Players...");
        }

        /// <summary>
        /// Joins an existing server as a client and sends an authentication request.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to connect to.</param>
        /// <param name="username">The username to identify the player joining the server.</param>
        public void JoinServer(string ipAddress, string username)
        {
            Debug.Log("Joining server...");
            isHost = false;
            m_hostEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), m_serverPort);
            m_udpClient = new UdpClient(0);

            m_udpClient.BeginReceive(OnReceiveData, null);

            Packet authReq = new Packet();
            authReq.WriteByte((byte)PacketType.connection_request);
            authReq.WriteString(username);

            m_sendQueue.Enqueue(new PacketData(authReq, m_hostEndPoint));

            Debug.Log("Authentication request sent to server.");
        }

        /// <summary>
        /// Retrieves the list of players currently connected to the server.
        /// </summary>
        /// <returns>A list of Player objects representing connected players.</returns>
        public List<Player> GetConnectedPlayers()
        {
            List<Player> players = new List<Player>();

            foreach (var player in m_players.Values)
                players.Add(player);

            return players;
        }

        /// <summary>
        /// Adds a packet to the send queue.
        /// </summary>
        /// <param name="packet">The packet to add to the send queue.</param>
        public void EnqueueSend(PacketData packet)
        {
            lock (m_sendQueue)
            {
                m_sendQueue.Enqueue(packet);
            }
        }

        /// <summary>
        /// Adds a packet to the receive queue.
        /// </summary>
        /// <param name="packet">The packet to add to the receive queue.</param>
        public void EnqueueReceive(PacketData packet)
        {
            lock (m_receiveQueue)
            {
                m_receiveQueue.Enqueue(packet);
            }
        }

        /// <summary>
        /// Parses a string into an IPEndPoint object.
        /// </summary>
        /// <param name="endPointString">The string representation of the IPEndPoint (e.g., "127.0.0.1:12345").</param>
        /// <returns>The parsed IPEndPoint object.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not in the correct format.</exception>
        public IPEndPoint ParseIPEndPoint(string endPointString)
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
