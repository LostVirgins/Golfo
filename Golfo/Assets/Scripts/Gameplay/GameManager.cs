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
        player_in_hole,
        game_finished
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        NetworkManager networkManager = NetworkManager.Instance;

        public GameState m_gameState = GameState.playing;

        public GameObject m_player;

        [SerializeField] private GameObject m_camera;
        [SerializeField] private GameObject m_golfBallPrefab;
        [SerializeField] private GameObject m_spawner;
        [SerializeField] private GameObject m_obstacleParent;
        [SerializeField] private GameObject m_mapData;

        List<GameObject> m_obstacles = new List<GameObject>();

        [SerializeField] private float m_lerpSpeed = 0.01f;
        private float m_lastSnapTime = 0f;
        private float m_maxLerpTime = 0f;
        private float m_curentLerpTime = 0f;
        private bool lerpingPos = false;

        private int currentHole = 0;

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

            //if (m_gameState == GameState.playing)
            InterpolateBalls();

            if (networkManager.m_isHost)
                SendObstacleData();
            //else InterpolateObstacles();
        }

        void InstantiatePlayers()
        {
            foreach (var player in networkManager.m_players.Values)
            {
                player.m_golfBall = Instantiate(m_golfBallPrefab, m_spawner.transform.position, Quaternion.identity);
                player.m_netInitPos = m_spawner.transform.position;
                player.m_netEndPos = m_spawner.transform.position;
            }

            m_player = networkManager.m_players[networkManager.m_localEndPoint].m_golfBall;

            m_camera.AddComponent<CameraFollow>();
            m_camera.GetComponent<CameraFollow>().m_target = m_player.transform;
            m_player.AddComponent<EnterHole>();

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

        public void PlayerInHole()
        {
            if (networkManager.m_players[networkManager.m_localEndPoint].m_inHole) return;

            Packet holeData = new Packet();
            holeData.WriteByte((byte)PacketType.player_in_hole);
            holeData.WriteString("hekbas_todo_use_token_:)");
            holeData.WriteString(networkManager.m_localEndPoint.ToString());
            NetworkManager.Instance.EnqueueSend(new PacketData(holeData, NetworkManager.Instance.m_hostEndPoint));

            UI_InGame.Instance.DebugScreenLog("Me cai :( - " + currentHole);
        }

        private void InterpolateBalls()
        {
            if (m_curentLerpTime >= m_maxLerpTime) return;

            m_curentLerpTime += Time.deltaTime;
            float t = m_curentLerpTime / m_maxLerpTime;

            foreach (var player in networkManager.m_players)
            {
                if (player.Key.Equals(networkManager.m_localEndPoint)) continue;
                player.Value.m_golfBall.transform.position = Vector3.Lerp(player.Value.m_netInitPos, player.Value.m_netEndPos, t);
                player.Value.m_golfBall.GetComponent<Rigidbody>().velocity = Vector3.Lerp(player.Value.m_netInitVel, player.Value.m_netEndVel, t);
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

                if (ipEndPoint.Equals(networkManager.m_localEndPoint) ||
                    !networkManager.m_players.ContainsKey(ipEndPoint))
                {
                    packetData.m_packet.ReadVector3();
                    packetData.m_packet.ReadVector3();
                    continue;
                }

                networkManager.m_players[ipEndPoint].m_netInitPos = networkManager.m_players[ipEndPoint].m_golfBall.transform.position;
                networkManager.m_players[ipEndPoint].m_netInitVel = networkManager.m_players[ipEndPoint].m_golfBall.GetComponent<Rigidbody>().velocity;
                networkManager.m_players[ipEndPoint].m_netEndPos = packetData.m_packet.ReadVector3();
                networkManager.m_players[ipEndPoint].m_netEndVel = packetData.m_packet.ReadVector3();
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

        public void OnAllPlayersInHole(PacketData packetData)
        {
            if (currentHole == 5)
            {
                // Game Finished
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
            GameManager.Instance.m_gameState = GameState.player_in_hole;

            currentHole = packetData.m_packet.ReadInt();
            Vector3 newPosition = m_mapData.GetComponent<MapData>().m_Holes[currentHole].spawnPoint.transform.position;
            m_mapData.GetComponent<MapData>().m_Holes[currentHole-1].bound.SetActive(false);

            m_player.transform.position = newPosition;
            m_player.GetComponent<Rigidbody>().velocity = Vector3.zero;

            UI_InGame.Instance.DebugScreenLog(m_player.transform.position.ToString());
            UI_InGame.Instance.DebugScreenLog(m_player.GetComponent<Rigidbody>().velocity.ToString());

            m_mapData.GetComponent<MapData>().m_Holes[currentHole].bound.SetActive(true);

            if (networkManager.m_isHost)
            {
                foreach (var player in networkManager.m_players.Values)
                {
                    player.m_netEndPos = newPosition;
                    player.m_netEndVel = Vector3.zero;
                    player.m_inHole = false;
                }
            }
        }
    }
}
