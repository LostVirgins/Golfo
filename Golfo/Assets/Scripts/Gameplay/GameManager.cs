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

            int index = 0;
            foreach (var player in m_players.Values)
            {
                Color ballColor = Color.white;
                switch (index)
                {
                    case 0: ballColor = Color.blue;     break;
                    case 1: ballColor = Color.red;      break;
                    case 2: ballColor = Color.yellow;   break;
                    case 3: ballColor = Color.green;    break;
                    case 4: ballColor = Color.magenta;  break;
                    case 5: ballColor = Color.black;    break;
                    case 6: ballColor = Color.cyan;     break;
                    case 7: ballColor = Color.white;    break;
                    case 8: ballColor = Color.grey;     break;
                    default: ballColor = Color.white;   break;
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
            {
                player.m_golfBall = Instantiate(m_golfBallPrefab, m_spawner.transform.position, Quaternion.identity);
                //player.m_golfBall.layer = LayerMask.NameToLayer("GolfBall");
            }
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
