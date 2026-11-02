using BalloonDash.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace BalloonDash.Game
{
    /// <summary>Spawns an obstacle after a delay and then at a fixed interval until the run ends.</summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] GameObject obstaclePrefab;
        [FormerlySerializedAs("SpawPos")]
        [SerializeField] Vector3 spawnPosition = new Vector3(18, 0, 0);

        [SerializeField] float startDelay = 1f;
        [SerializeField] float repeatDelay = 3f;

        PlayerController player;
        SpawnTimer timer;

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            timer = new SpawnTimer(startDelay, repeatDelay);
        }

        void Update()
        {
            if (player.GetGameOver())
            {
                timer.Stop();
            }
            for (int due = timer.Tick(Time.deltaTime); due > 0; due--)
            {
                SpawnObstacle();
            }
        }

        public void SpawnObstacle()
        {
            Instantiate(obstaclePrefab, spawnPosition, obstaclePrefab.transform.rotation);
        }
    }
}
