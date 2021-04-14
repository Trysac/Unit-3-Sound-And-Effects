using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] Vector3 SpawPos = new Vector3(18,0,0);

    [SerializeField] float startDelay = 1f;
    [SerializeField] float repeatDelay = 3f;

    PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        InvokeRepeating("InstantiateObstacules", startDelay, repeatDelay);
    }
    private void Update()
    {
        if (player.GetGameOver()) 
        {
            CancelInvoke();
        }
    }

    public void InstantiateObstacules() 
    { 
        Instantiate(obstaclePrefab, SpawPos, obstaclePrefab.transform.rotation);
    }
}
