using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveLeftX : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float leftBound = -10;

    PlayerControllerX playerControllerScript;

    void Start()
    {
        playerControllerScript = GameObject.Find("Player").GetComponent<PlayerControllerX>();
    }

    void Update()
    {
        if (playerControllerScript.GetGameOver().Equals(false))
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);
        }

        // If object goes off screen that is NOT the background, destroy it
        if (transform.position.x < leftBound && !this.gameObject.CompareTag("Background"))
        {
            Destroy(gameObject);
        }

    }
}
