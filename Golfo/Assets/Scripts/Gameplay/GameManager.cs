using lv.network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace lv.gameplay
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject golfBallPrefab;
        List<Player> players = NetworkManager.Instance.GetConnectedPlayers();

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
            foreach (var player in players)
                player.m_golfBall = Instantiate(golfBallPrefab, transform.position, Quaternion.identity);
        }
    }
}
