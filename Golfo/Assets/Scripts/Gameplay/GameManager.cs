using lv.network;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace lv.gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameObject golfBallPrefab;
        public Dictionary<IPEndPoint, Player> m_players = NetworkManager.Instance.m_connectedPlayers;

        private void Awake()
        {
            InstantiatePlayers();
        }

        void Start()
        {

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
                player.m_golfBall = Instantiate(golfBallPrefab, transform.position, Quaternion.identity);
        }

        public void BallStrike(PacketData packetData)
        {
            if (m_players.ContainsKey(packetData.m_remoteEP))
            {
                m_players[packetData.m_remoteEP].m_golfBall.GetComponent<Rigidbody>().AddForce(
                    - packetData.m_packet.ReadVector3() *
                    packetData.m_packet.ReadFloat() *
                    packetData.m_packet.ReadFloat());
            }
        }

        private void OnEnable()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnBallStrike.AddListener(BallStrike);
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnBallStrike.RemoveListener(BallStrike);
            }
        }
    }
}
