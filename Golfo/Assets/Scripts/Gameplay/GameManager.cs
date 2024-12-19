using Cinemachine;
using lv.network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace lv.gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameObject m_camera;
        [SerializeField] private GameObject m_golfBallPrefab;
        [SerializeField] private GameObject m_spawner;

        public GameObject m_player;
        public Dictionary<IPEndPoint, Player> m_players = NetworkManager.Instance.m_connectedPlayers;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            InstantiatePlayers();
            m_player = m_players[NetworkManager.Instance.m_localEndPoint].m_golfBall;
            m_camera.GetComponent<CinemachineVirtualCamera>().m_LookAt = m_player.transform;
            // added component for camera to follow player
            m_camera.AddComponent<CameraFollow>();
            m_camera.GetComponent<CameraFollow>().target = m_player.transform;

            int index = 0;
            foreach (var player in m_players.Values)
            {
                Color ballColor = Color.white;
                switch (index)
                {
                    case 0: ballColor = new Color(1.0f, 0.0f, 0.0f, 0.4f);  break;  // Red
                    case 1: ballColor = new Color(0.0f, 1.0f, 0.0f, 0.4f);  break;  // Green
                    case 2: ballColor = new Color(0.0f, 0.0f, 1.0f, 0.4f);  break;  // Blue
                    case 3: ballColor = new Color(1.0f, 1.0f, 0.0f, 0.4f);  break;  // Yellow
                    case 4: ballColor = new Color(1.0f, 0.0f, 1.0f, 0.4f);  break;  // Magenta
                    case 5: ballColor = new Color(0.0f, 1.0f, 1.0f, 0.4f);  break;  // Cyan
                    case 6: ballColor = new Color(1.0f, 0.5f, 0.0f, 0.4f);  break;  // Orange
                    case 7: ballColor = new Color(0.5f, 0.0f, 0.5f, 0.4f);  break;  // Purple
                    default:ballColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);  break;  // Gray
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

        void Update()
        {
            // game state machine
            // ball stopped?
            // ball inside hole?
        }

        void InstantiatePlayers()
        {
            foreach (var player in m_players.Values)
                player.m_golfBall = Instantiate(m_golfBallPrefab, m_spawner.transform.position, Quaternion.identity);
        }

        public void OnBallStrike(PacketData packetData)
        {
            IPEndPoint ipEndPoint = NetworkManager.Instance.ParseIPEndPoint(packetData.m_packet.ReadString());

            if (m_players.ContainsKey(ipEndPoint))
            {
                m_players[ipEndPoint].m_golfBall.GetComponent<Rigidbody>().AddForce(
                    -packetData.m_packet.ReadVector3() *
                    packetData.m_packet.ReadFloat() *
                    packetData.m_packet.ReadFloat());
            }
        }
    }
}
