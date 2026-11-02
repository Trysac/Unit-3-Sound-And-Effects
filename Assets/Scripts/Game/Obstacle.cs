using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>Scrolls an obstacle (or the backdrop) left while the run lasts and removes obstacles that leave the screen.</summary>
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] float movementSpeed = 1f;
        [SerializeField] float leftBoundary = -10f;

        PlayerController player;

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        void Update()
        {
            if (!player.GetGameOver())
            {
                transform.Translate(Vector3.left * movementSpeed * Time.deltaTime);
            }
            if (transform.position.x < leftBoundary && gameObject.CompareTag("Obstacle"))
            {
                Destroy(gameObject);
            }
        }
    }
}
