using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] float leftBoundary = -10f;

    PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();    
    }


    void Update()
    {
        if (player.GetGameOver().Equals(false)) 
        { 
            transform.Translate(Vector3.left * movementSpeed * Time.deltaTime);
        }
        if (transform.position.x < leftBoundary && gameObject.tag.Equals("Obstacle")) 
        {
            Destroy(gameObject);
        }
        
    }
}
