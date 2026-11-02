using UnityEngine;

namespace BalloonDash.Game.Challenge3
{
    /// <summary>Challenge 3: scrolls money, bombs and the backdrop left until game over.</summary>
    public class MoveLeftX : MonoBehaviour
    {
        [SerializeField] float speed;
        [SerializeField] float leftBound = -10;

        PlayerControllerX player;

        void Start()
        {
            player = GameObject.Find("Player").GetComponent<PlayerControllerX>();
        }

        void Update()
        {
            if (!player.GetGameOver())
            {
                transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);
            }

            // Objects that leave the screen are removed, except the looping backdrop.
            if (transform.position.x < leftBound && !gameObject.CompareTag("Background"))
            {
                Destroy(gameObject);
            }
        }
    }
}
