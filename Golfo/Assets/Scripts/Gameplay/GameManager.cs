using lv.network;
using lv.ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace lv.gameplay
{
    public enum GameState : byte
    {
        playing,
        out_of_bounds,
        changing_hole,
        game_end
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        NetworkManager netManager = NetworkManager.Instance;

        public GameState m_gameState = GameState.playing;

        public GameObject m_player;
        public Rigidbody m_rb;

        [SerializeField] private GameObject m_camera;
        [SerializeField] private GameObject m_golfBallPrefab;
        [SerializeField] private GameObject m_spawner;
        [SerializeField] private GameObject m_obstacleParent_A;
        [SerializeField] private GameObject m_obstacleParent_B;
        [SerializeField] private GameObject m_mapData;

        List<GameObject> m_obstacles_A = new List<GameObject>();
        List<GameObject> m_obstacles_B = new List<GameObject>();

        [SerializeField] private float m_lerpSpeed = 0.01f;
        private float m_lastSnapTime = 0f;
        private float m_maxLerpTime = 0f;
        private float m_curentLerpTime = 0f;
        private bool lerpingPos = false;

        public int currentHole = 0;
        private Vector3 newPosition;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            InstantiatePlayers();
            m_obstacles_A = GetAllChildren(m_obstacleParent_A);
            m_obstacles_B = GetAllChildren(m_obstacleParent_B);
            m_rb = m_player.GetComponent<Rigidbody>();
        }

        void Update()
        {
            m_lastSnapTime += Time.deltaTime;
        }

        void FixedUpdate()
        {
            InterpolateBalls();

            if (m_gameState == GameState.out_of_bounds)
            {
                m_rb.velocity = Vector3.zero;
                m_rb.angularVelocity = Vector3.zero;
                m_rb.position = m_player.GetComponent<LineForce>().lastShotPosition;
                m_gameState = GameState.playing;
            }

            if (m_gameState == GameState.changing_hole)
            {
                m_mapData.GetComponent<MapData>().m_Holes[currentHole - 1].bound.SetActive(false);
                m_mapData.GetComponent<MapData>().m_Holes[currentHole].bound.SetActive(true);

                m_rb.velocity = Vector3.zero;
                m_rb.angularVelocity = Vector3.zero;
                m_rb.position = newPosition;

                if (netManager.m_isHost)
                {
                    foreach (var player in netManager.m_players)
                        player.Value.m_inHole = false;
                }

                m_gameState = GameState.playing;
            }
        }

        void InstantiatePlayers()
        {
            foreach (var player in netManager.m_players.Values)
            {
                player.m_golfBall = Instantiate(m_golfBallPrefab, m_spawner.transform.position, Quaternion.identity);
                player.m_netInitPos = m_spawner.transform.position;
                player.m_netEndPos = m_spawner.transform.position;
            }

            m_player = netManager.MyPlayer().m_golfBall;

            m_camera.AddComponent<CameraFollow>();
            m_camera.GetComponent<CameraFollow>().m_target = m_player.transform;
            m_player.AddComponent<EnterHole>();

            int index = 0;
            foreach (var player in netManager.m_players.Values)
            {
                Color ballColor = Color.white;
                switch (index)
                {
                    case 0: ballColor = new Color(1.0f, 0.0f, 0.0f, 0.4f); break;  // Red
                    case 1: ballColor = new Color(0.0f, 1.0f, 0.0f, 0.4f); break;  // Green
                    case 2: ballColor = new Color(0.0f, 0.0f, 1.0f, 0.4f); break;  // Blue
                    case 3: ballColor = new Color(1.0f, 1.0f, 0.0f, 0.4f); break;  // Yellow
                    case 4: ballColor = new Color(1.0f, 0.0f, 1.0f, 0.4f); break;  // Magenta
                    case 5: ballColor = new Color(0.0f, 1.0f, 1.0f, 0.4f); break;  // Cyan
                    case 6: ballColor = new Color(1.0f, 0.5f, 0.0f, 0.4f); break;  // Orange
                    case 7: ballColor = new Color(0.5f, 0.0f, 0.5f, 0.4f); break;  // Purple
                    default: ballColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f); break; //Rand for extra people
                }

                if (player.m_golfBall == m_player)
                {
                    player.m_golfBall.GetComponent<LineForce>().enabled = true;
                    player.m_golfBall.layer = LayerMask.NameToLayer("PlayerBall");
                    player.m_golfBall.tag = "MyPlayer";
                    ballColor.a = 1.0f;
                }

                player.m_golfBall.GetComponent<Renderer>().material.color = ballColor;
                index++;
            }
        }

        public List<GameObject> GetAllChildren(GameObject parent)
        {
            List<GameObject> children = new List<GameObject>();

            foreach (Transform child in parent.transform)
                children.Add(child.gameObject);

            return children;
        }

        private void InterpolateBalls()
        {
            if (m_curentLerpTime >= m_maxLerpTime) return;

            m_curentLerpTime += Time.deltaTime;
            float t = m_curentLerpTime / m_maxLerpTime;

            foreach (var player in netManager.m_players)
            {
                if (player.Key.Equals(netManager.m_localEndPoint)) continue;
                player.Value.m_golfBall.transform.position = Vector3.Lerp(player.Value.m_netInitPos, player.Value.m_netEndPos, t);
                player.Value.m_golfBall.GetComponent<Rigidbody>().velocity = Vector3.Lerp(player.Value.m_netInitVel, player.Value.m_netEndVel, t);
            }
        }

        private void InterpolateObstacles()
        {
            foreach (var obstacle in m_obstacles_A)
            {
                //obstacle.transform.position
            }
        }


        // Send Operations ----------------------------------------------------------------
        public void SendGameData()
        {
            SendPlayerPosition();
            SendPlayerScore();
            SendObstacleData_A();
            SendObstacleData_B();
        }
        private void SendPlayerPosition()
        {
            if (netManager.m_isHost)
            {
                netManager.m_players[netManager.m_hostEndPoint].m_netEndPos = m_player.transform.position;
                netManager.m_players[netManager.m_hostEndPoint].m_netEndVel = m_player.GetComponent<Rigidbody>().velocity;

                Packet playersPos = new Packet();
                playersPos.WriteByte((byte)PacketType.player_position);
                playersPos.WriteString("hekbas_todo_use_token_:)");
                playersPos.WriteInt(netManager.m_players.Count);

                foreach (var player in netManager.m_players)
                {
                    playersPos.WriteString(player.Key.ToString());
                    playersPos.WriteVector3(player.Value.m_netEndPos);
                    playersPos.WriteVector3(player.Value.m_netEndVel);
                }

                netManager.EnqueueSend(new PacketData(playersPos, netManager.m_hostEndPoint, true));

                playersPos.SetStreamPos(0);
                playersPos.ReadByte();
                playersPos.ReadString();
                OnNetworkPlayerPosition(new PacketData(playersPos, netManager.m_hostEndPoint, true));
            }
            else
            {
                Packet playerPos = new Packet();
                playerPos.WriteByte((byte)PacketType.player_position);
                playerPos.WriteString("hekbas_todo_use_token_:)");
                playerPos.WriteString(netManager.m_localEndPoint.ToString());
                playerPos.WriteVector3(m_player.transform.position);
                playerPos.WriteVector3(m_player.GetComponent<Rigidbody>().velocity);
                netManager.EnqueueSend(new PacketData(playerPos, netManager.m_hostEndPoint));
            }
        }

        private void SendPlayerScore()
        {
            if (netManager.m_isHost)
            {
                Packet playersScore = new Packet();
                playersScore.WriteByte((byte)PacketType.player_score);
                playersScore.WriteString("hekbas_todo_use_token_:)");
                playersScore.WriteInt(netManager.m_players.Count);

                foreach (var player in netManager.m_players)
                {
                    playersScore.WriteString(player.Key.ToString());
                    playersScore.WriteInt(player.Value.m_score.Count);

                    foreach (var score in player.Value.m_score)
                        playersScore.WriteInt(score);
                }
                netManager.EnqueueSend(new PacketData(playersScore, netManager.m_hostEndPoint, true));
            }
        }

        private void SendObstacleData_A()
        {
            Packet obsData = new Packet();
            obsData.WriteByte((byte)PacketType.obstacle_data_A);
            obsData.WriteString("hekbas_todo_use_token_:)");
            obsData.WriteInt(m_obstacles_A.Count);

            foreach (var obstacle in m_obstacles_A)
            {
                var obs = obstacle.GetComponent<DynamicObstacles>();
                obsData.WriteFloat(obs.m_totalTime);
                obsData.WriteFloat(obs.m_easedTime);
                obsData.WriteBool(obs.m_reverse);
            }

            netManager.EnqueueSend(new PacketData(obsData, netManager.m_hostEndPoint, true));
        }

        private void SendObstacleData_B()
        {
            Packet obsData = new Packet();
            obsData.WriteByte((byte)PacketType.obstacle_data_B);
            obsData.WriteString("hekbas_todo_use_token_:)");
            obsData.WriteInt(m_obstacles_B.Count);

            foreach (var obstacle in m_obstacles_B)
                obsData.WriteFloat(obstacle.transform.eulerAngles.y);

            netManager.EnqueueSend(new PacketData(obsData, netManager.m_hostEndPoint, true));
        }

        public void PlayerInHole()
        {
            if (netManager.MyPlayer().m_inHole) return;

            Packet holeData = new Packet();
            holeData.WriteByte((byte)PacketType.player_in_hole);
            holeData.WriteString("hekbas_todo_use_token_:)");
            holeData.WriteString(netManager.m_localEndPoint.ToString());
            netManager.EnqueueSend(new PacketData(holeData, netManager.m_hostEndPoint));

            UI_InGame.Instance.DebugScreenLog("Me cai :( - " + currentHole);
        }


        // Recieve Operations -------------------------------------------------------------
        public void OnBallStrike(PacketData packetData)
        {
            IPEndPoint ipEndPoint = netManager.ParseIPEndPoint(packetData.m_packet.ReadString());

            if (netManager.m_players.ContainsKey(ipEndPoint))
            {
                netManager.m_players[ipEndPoint].m_golfBall.GetComponent<Rigidbody>().AddForce(
                    -packetData.m_packet.ReadVector3() *
                    packetData.m_packet.ReadFloat() *
                    packetData.m_packet.ReadFloat());
            }
        }

        public void OnNetworkPlayerPosition(PacketData packetData)
        {
            m_maxLerpTime = m_lastSnapTime;
            m_lastSnapTime = 0f;
            m_curentLerpTime = 0f;

            int playerCount = packetData.m_packet.ReadInt();

            for (int i = 0; i < playerCount; i++)
            {
                IPEndPoint ipEndPoint = netManager.ParseIPEndPoint(packetData.m_packet.ReadString());

                if (ipEndPoint.Equals(netManager.m_localEndPoint) ||
                    !netManager.m_players.ContainsKey(ipEndPoint))
                {
                    packetData.m_packet.ReadVector3();
                    packetData.m_packet.ReadVector3();
                    continue;
                }

                netManager.m_players[ipEndPoint].m_netInitPos = netManager.m_players[ipEndPoint].m_golfBall.transform.position;
                netManager.m_players[ipEndPoint].m_netInitVel = netManager.m_players[ipEndPoint].m_golfBall.GetComponent<Rigidbody>().velocity;
                netManager.m_players[ipEndPoint].m_netEndPos = packetData.m_packet.ReadVector3();
                netManager.m_players[ipEndPoint].m_netEndVel = packetData.m_packet.ReadVector3();
            }
        }

        public void OnNetworkObstacleData_A(PacketData packetData)
        {
            int obsCount = packetData.m_packet.ReadInt();

            for (int i = 0; i < obsCount; i++)
            {
                var obs = m_obstacles_A[i].GetComponent<DynamicObstacles>();
                obs.m_totalTime = packetData.m_packet.ReadFloat();
                obs.m_easedTime = packetData.m_packet.ReadFloat();
                obs.m_reverse = packetData.m_packet.ReadBool();
            }
        }

        public void OnNetworkObstacleData_B(PacketData packetData)
        {
            int obsCount = packetData.m_packet.ReadInt();

            for (int i = 0; i < obsCount; i++)
            {
                float angleY = packetData.m_packet.ReadFloat();
                UI_InGame.Instance.DebugScreenLog(angleY.ToString());
                m_obstacles_B[i].transform.rotation = Quaternion.Euler(
                    transform.eulerAngles.x, angleY, transform.eulerAngles.z);
            }
        }

        public void OnAllPlayersInHole(PacketData packetData)
        {
            if (currentHole == 5)
            {
                Packet gameEnd = new Packet();
                gameEnd.WriteByte((byte)PacketType.game_end);
                gameEnd.WriteString("hekbas_todo_use_token_:)");
                NetworkManager.Instance.EnqueueSend(new PacketData(gameEnd, NetworkManager.Instance.m_hostEndPoint, true, false));
            }
            else
            {
                currentHole++;

                Packet obsData = new Packet();
                obsData.WriteByte((byte)PacketType.next_hole);
                obsData.WriteString("hekbas_todo_use_token_:)");
                obsData.WriteInt(currentHole);
                NetworkManager.Instance.EnqueueSend(new PacketData(obsData, NetworkManager.Instance.m_hostEndPoint, true));

                obsData.SetStreamPos(0);
                obsData.ReadByte();
                obsData.ReadString();
                OnNextHole(new PacketData(obsData, NetworkManager.Instance.m_hostEndPoint));
            }
        }

        public void OnNextHole(PacketData packetData)
        {
            GameManager.Instance.m_gameState = GameState.changing_hole;
            currentHole = packetData.m_packet.ReadInt();
            newPosition = m_mapData.GetComponent<MapData>().m_Holes[currentHole].spawnPoint.transform.position;
        }

        public void OnGameEnd()
        {
            GameManager.Instance.m_gameState = GameState.game_end;
            UI_InGame.Instance.ToggleScoreWindow();
            UI_InGame.Instance.ToggleExit();
        }
    }
}
