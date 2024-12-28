using lv.network;
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
        playing
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        NetworkManager networkManager = NetworkManager.Instance;

        public GameState m_gameState { get; private set; } = GameState.playing;

        public GameObject m_player;

        [SerializeField] private GameObject m_camera;
        [SerializeField] private GameObject m_golfBallPrefab;
        [SerializeField] private GameObject m_spawner;
        [SerializeField] private GameObject m_obstacleParent;

        List<GameObject> m_obstacles = new List<GameObject>();

        [SerializeField] private float m_lerpSpeed = 0.01f;
        private float m_lastSnapTime = 0f;
        private float m_maxLerpTime = 0f;
        private float m_curentLerpTime = 0f;

        private bool lerpingPos = false;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            InstantiatePlayers();
            m_obstacles = GetAllChildren(m_obstacleParent);
        }

        void Update()
        {
            m_lastSnapTime += Time.deltaTime;

            if (networkManager.isHost)
                networkManager.m_players[networkManager.m_localEndPoint].m_golfBall = m_player;

            InterpolateBalls();

            if (networkManager.isHost)
                SendObstacleData();
            //else InterpolateObstacles();

            // game state machine
            // ball stopped?
            // ball inside hole?
        }

        void InstantiatePlayers()
        {
            foreach (var player in networkManager.m_players.Values)
                player.m_golfBall = Instantiate(m_golfBallPrefab, m_spawner.transform.position, Quaternion.identity);

            m_player = networkManager.m_players[networkManager.m_localEndPoint].m_golfBall;

            m_camera.AddComponent<CameraFollow>();
            m_camera.GetComponent<CameraFollow>().m_target = m_player.transform;

            int index = 0;
            foreach (var player in networkManager.m_players.Values)
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
                    default:ballColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); break;  // Gray
                }

                if (player.m_golfBall == m_player)
                {
                    player.m_golfBall.layer = LayerMask.NameToLayer("PlayerBall");
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

        private void SendObstacleData()
        {
            Packet obsData = new Packet();
            obsData.WriteByte((byte)PacketType.obstacle1_data);
            obsData.WriteString("hekbas_todo_use_token_:)");
            obsData.WriteInt(m_obstacles.Count);

            foreach (var obstacle in m_obstacles)
            {
                var obs = obstacle.GetComponent<DynamicObstacles>();
                obsData.WriteFloat(obs.m_totalTime);
                obsData.WriteFloat(obs.m_easedTime);
                obsData.WriteBool(obs.m_reverse);
            }

            NetworkManager.Instance.EnqueueSend(new PacketData(obsData, NetworkManager.Instance.m_hostEndPoint, true));
        }

        private void InterpolateBalls()
        {
            if (m_curentLerpTime > m_maxLerpTime) return;

            m_curentLerpTime += Time.deltaTime;
            float t = 1 / m_maxLerpTime * m_curentLerpTime;

            foreach (var player in networkManager.m_players.Values)
            {
                player.transform.position = Vector3.Lerp(player.m_netInitPos, player.m_netEndPos, t);
                player.GetComponent<Rigidbody>().velocity = Vector3.Lerp(player.m_netInitVel, player.m_netEndVel, t);
            }
        }

        private void InterpolateObstacles()
        {
            foreach (var obstacle in m_obstacles)
            {
                //obstacle.transform.position
            }
        }

        public void OnBallStrike(PacketData packetData)
        {
            IPEndPoint ipEndPoint = networkManager.ParseIPEndPoint(packetData.m_packet.ReadString());

            if (networkManager.m_players.ContainsKey(ipEndPoint))
            {
                networkManager.m_players[ipEndPoint].m_golfBall.GetComponent<Rigidbody>().AddForce(
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
                IPEndPoint ipEndPoint = networkManager.ParseIPEndPoint(packetData.m_packet.ReadString());

                if (networkManager.m_players.ContainsKey(ipEndPoint))
                {
                    networkManager.m_players[ipEndPoint].m_netEndPos = packetData.m_packet.ReadVector3();
                    networkManager.m_players[ipEndPoint].m_netInitPos = networkManager.m_players[ipEndPoint].m_golfBall.transform.position;
                    networkManager.m_players[ipEndPoint].m_netEndVel = packetData.m_packet.ReadVector3();
                    networkManager.m_players[ipEndPoint].m_netInitVel = networkManager.m_players[ipEndPoint].m_golfBall.GetComponent<Rigidbody>().velocity;
                }
            }
        }

        public void OnNetworkObstacleData(PacketData packetData)
        {
            int obsCount = packetData.m_packet.ReadInt();

            for (int i = 0; i < obsCount; i++)
            {
                var obs = m_obstacles[i].GetComponent<DynamicObstacles>();
                obs.m_totalTime = packetData.m_packet.ReadFloat();
                obs.m_easedTime = packetData.m_packet.ReadFloat();
                obs.m_reverse = packetData.m_packet.ReadBool();
            }
        }
    }
}
