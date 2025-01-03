using lv.gameplay;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

namespace lv.network
{
    public class LobbyNameEvent : UnityEvent<string> { }
    public class ChatMessageEvent : UnityEvent<string> { }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;

        private IPAddress m_serverIP = IPAddress.Parse("127.0.0.1");
        private int m_serverPort = 9050;
        public IPEndPoint m_hostEndPoint { get; private set; }

        public IPEndPoint m_localEndPoint { get; private set; }
        public UdpClient m_udpClient { get; private set; }
        public Dictionary<IPEndPoint, Player> m_players = new Dictionary<IPEndPoint, Player>();

        private Queue<PacketData> m_sendQueue = new Queue<PacketData>();
        private Queue<PacketData> m_receiveQueue = new Queue<PacketData>();
        private float m_tickRate = 0.1f; // 100ms - 10 ticks/s
        private float m_lastTickTime = 0f;

        public bool m_isHost;
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
                ServerTick();

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

        private void OnApplicationQuit()
        {
            ShutdownHost();
        }

        private void ServerTick()
        {
            m_lastTickTime += Time.deltaTime;

            if (m_lastTickTime >= m_tickRate)
            {
                m_lastTickTime = 0;
                GameManager.Instance.SendGameData();
            }
        }


        // Send Operations -------------------------------------------------------------------
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
                if (packetData.m_omitSender && client.Equals(m_hostEndPoint)) continue;
                SendPacket(packetData.m_packet, client);
            }
        }

        private void BroadcastPacket(Packet packet)
        {
            //hekbas: this will also resend to origin!
            foreach (var client in m_players.Keys)
                SendPacket(packet, client);
        }


        // Receive Operations -----------------------------------------------------------------
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
            //Debug.Log("Processing game packet...");

            switch (packetType)
            {
                case PacketType.lobby_info:         LobbyInfo(packetData);          break;
                case PacketType.chat_join:          ChatJoin(packetData);           break;
                case PacketType.chat_message:       ChatMessage(packetData);        break;
                case PacketType.game_start:         GameStart(packetData);          break;
                case PacketType.game_end:           GameEnd();                      break;
                case PacketType.ball_strike:        BallStrike(packetData);         break;
                case PacketType.player_position:    PlayerPosition(packetData);     break;
                case PacketType.player_score:       PlayerScore(packetData);        break;
                case PacketType.player_in_hole:     PlayerInHole(packetData);       break;
                case PacketType.next_hole:          NextHole(packetData);           break;
                case PacketType.obstacle_data_A:    ObstacleData_A(packetData);     break;
                case PacketType.obstacle_data_B:    ObstacleData_B(packetData);     break;
                default: Debug.Log("Packet Type not found.");                       break;
            }
        }


        // Received Packet Processing ---------------------------------------------------------
        private void ConnectionRequest(Packet packet, IPEndPoint senderEndPoint)
        {
            string username = packet.ReadString();
            string sessionToken = "";
            PacketType status = AuthenticationManager.Instance.AuthenticateClient(username, m_hostEndPoint, ref sessionToken);

            if (status == PacketType.auth_success)
            {
                AddPlayer(username, sessionToken, senderEndPoint);

                Packet lobbyNamePacket = new Packet();
                lobbyNamePacket.WriteByte((byte)PacketType.lobby_info);
                lobbyNamePacket.WriteString(sessionToken);
                lobbyNamePacket.WriteString(senderEndPoint.ToString());
                lobbyNamePacket.WriteString(m_lobbyName);
                m_sendQueue.Enqueue(new PacketData(lobbyNamePacket, senderEndPoint));

                Packet chatJoin = new Packet();
                chatJoin.WriteByte((byte)PacketType.chat_join);
                chatJoin.WriteString(sessionToken);
                chatJoin.WriteString(username);
                m_sendQueue.Enqueue(new PacketData(chatJoin, senderEndPoint, true, false));
            }
            else
            {
                Debug.Log("Authentication failed.");
            }
        }

        private void AddPlayer(string username, string sessionToken, IPEndPoint senderEndPoint)
        {
            m_players[senderEndPoint] = new Player(username, sessionToken);
            Debug.Log($"New Player {senderEndPoint} authenticated with session {sessionToken}");
        }

        private void LobbyInfo(PacketData packetData)
        {
            if (m_isHost)
                SendPacket(packetData);
            else
            {
                m_localEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());
                OnReceiveLobbyName.Invoke(packetData.m_packet.ReadString());
            }
        }
        
        private void ChatJoin(PacketData packetData)
        {
            OnReceiveChatMessage.Invoke(packetData.m_packet.ReadString() + " joined");
        }

        private void ChatMessage(PacketData packetData) 
        {
            if (m_isHost)
                BroadcastPacket(packetData);

            OnReceiveChatMessage.Invoke(packetData.m_packet.ReadString());
        }

        private void GameStart(PacketData packetData)
        {
            byte length = packetData.m_packet.ReadByte();

            for (byte i = 0; i < length; i++)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());
                string username = packetData.m_packet.ReadString();
                string sessionToken = packetData.m_packet.ReadString();
                Player player = new Player(username, sessionToken);
                m_players[ipEndPoint] = player;
            }

            SceneManager.LoadScene(sceneName: "2_map_A");
        }

        private void GameEnd()
        {
            GameManager.Instance.OnGameEnd();
        }

        private void BallStrike(PacketData packetData)
        {
            // Input preditcion OFF
            //if (m_isHost) BroadcastPacket(packetData);
            //GameManager.Instance.OnBallStrike(packetData);

            // Score
            if (m_isHost)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());
                m_players[ipEndPoint].m_score[GameManager.Instance.m_currentHole] += 1;
            }
        }

        private void PlayerPosition(PacketData packetData)
        {
            if (m_isHost)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());

                if (!m_players.ContainsKey(ipEndPoint)) return;
                m_players[ipEndPoint].m_netEndPos = packetData.m_packet.ReadVector3();
                m_players[ipEndPoint].m_netEndVel = packetData.m_packet.ReadVector3();
            }
            else
            {
                GameManager.Instance.OnNetworkPlayerPosition(packetData);
            }
        }

        private void PlayerScore(PacketData packetData)
        {
            Packet packet = packetData.m_packet;
            int playerCount = packet.ReadInt();

            for (int i = 0; i < playerCount; i++)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packet.ReadString());
                int scoreCount = packet.ReadInt();

                for (int j = 0; j < scoreCount; j++)
                    m_players[ipEndPoint].m_score[j] = packet.ReadInt();
            }
        }

        private void PlayerInHole(PacketData packetData)
        {
            if (m_isHost)
            {
                IPEndPoint ipEndPoint = ParseIPEndPoint(packetData.m_packet.ReadString());
                Debug.Log("Enter Hole" + ipEndPoint.ToSafeString());
                m_players[ipEndPoint].m_inHole = true;

                bool isHoleFinished = true;
                foreach (var player in m_players.Values)
                {
                    if (player.m_inHole == false)
                    {
                        isHoleFinished = false;
                        break;
                    }
                }

                if (isHoleFinished == true)
                {
                    Debug.Log("ALL IN HOLE");
                    GameManager.Instance.OnAllPlayersInHole(packetData);
                }
            }
        }

        private void NextHole(PacketData packetData)
        {
            if (!m_isHost)
                GameManager.Instance.OnNextHole(packetData);
        }

        private void ObstacleData_A(PacketData packetData)
        {
            if (!m_isHost)
                GameManager.Instance.OnNetworkObstacleData_A(packetData);
        }

        private void ObstacleData_B(PacketData packetData)
        {
            if (!m_isHost)
                GameManager.Instance.OnNetworkObstacleData_B(packetData);
        }


        // Public Methods --------------------------------------------------------------------

        /// <summary>
        /// Starts hosting a game, initializes the server, and sets up the host's networking configuration.
        /// </summary>
        /// <param name="lobbyName">The name of the lobby to be hosted.</param>
        public void StartHost(string lobbyName, string userName)
        {
            Debug.Log("Hosting game...");
            m_isHost = true;
            m_lobbyName = lobbyName;

            m_hostEndPoint = new IPEndPoint(m_serverIP, m_serverPort);
            m_localEndPoint = m_hostEndPoint;

            m_udpClient = new UdpClient(m_serverPort);
            m_udpClient.BeginReceive(OnReceiveData, null);

            Player newPlayer = new Player(userName, $"{System.Guid.NewGuid()}");
            m_players[m_hostEndPoint] = newPlayer;

            Debug.Log("Server up and running. Waiting for new Players...");
        }

        /// <summary>
        /// Shuts down the hosted server, releases networking resources, and resets the server state.
        /// </summary>
        public void ShutdownHost()
        {
            Debug.Log("Shutting down server...");

            if (m_isHost)
            {
                if (m_udpClient != null)
                {
                    m_udpClient.Close();
                    m_udpClient = null;
                }

                m_players.Clear();

                m_isHost = false;
                m_lobbyName = string.Empty;
                m_hostEndPoint = null;
                m_localEndPoint = null;

                Debug.Log("Server has been shut down successfully.");
            }
            else
            {
                Debug.LogWarning("ShutdownHost called, but the server is not currently hosting.");
            }
        }

        /// <summary>
        /// Joins an existing server as a client and sends an authentication request.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to connect to.</param>
        /// <param name="username">The username to identify the player joining the server.</param>
        public void JoinServer(string ipAddress, string username)
        {
            Debug.Log("Joining server...");
            m_isHost = false;
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
        /// Leaves the current server, loads Main Menu.
        /// </summary>
        public void ExitServer()
        {
            if (m_udpClient != null)
            {
                m_udpClient.Close();
                m_udpClient = null;
            }

            m_players.Clear();

            m_isHost = false;
            m_lobbyName = string.Empty;
            m_hostEndPoint = null;
            m_localEndPoint = null;
        }

        /// <summary>
        /// Moves the current game object to the active scene so it gets destroyed during scene loading.
        /// </summary>
        public void RemoveOnLoad()
        {
            SceneManager.MoveGameObjectToScene(this.GameObject(), SceneManager.GetActiveScene());
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

        /// <summary>
        /// Fetches local player from players list.
        /// </summary>
        public Player MyPlayer()
        {
            return m_players[m_localEndPoint];
        }
    }
}
