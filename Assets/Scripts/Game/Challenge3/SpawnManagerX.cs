using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game.Challenge3
{
    /// <summary>Challenge 3: spawns money or a bomb at a random height on a fixed interval while the game lasts.</summary>
    public class SpawnManagerX : MonoBehaviour
    {
        [SerializeField] GameObject[] objectPrefabs;
        [SerializeField] float spawnDelay = 2;
        [SerializeField] float spawnInterval = 1.5f;

        PlayerControllerX player;
        SpawnTimer timer;

        void Start()
        {
            timer = new SpawnTimer(spawnDelay, spawnInterval);
            player = GameObject.Find("Player").GetComponent<PlayerControllerX>();
        }

        void Update()
        {
            for (int due = timer.Tick(Time.deltaTime); due > 0; due--)
            {
                SpawnObject();
            }
        }

        void SpawnObject()
        {
            var spawnLocation = new Vector3(30, Random.Range(5, 15), 0);
            int index = Random.Range(0, objectPrefabs.Length);

            if (!player.GetGameOver())
            {
                Instantiate(objectPrefabs[index], spawnLocation, objectPrefabs[index].transform.rotation);
            }
        }
    }
}
