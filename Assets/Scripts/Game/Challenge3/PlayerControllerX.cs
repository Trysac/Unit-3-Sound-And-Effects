using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game.Challenge3
{
    /// <summary>Challenge 3: hold Space to float the balloon, collect money, a bomb ends the game.</summary>
    public class PlayerControllerX : MonoBehaviour
    {
        const float Ceiling = 15f;

        [SerializeField] bool gameOver;
        [SerializeField] float floatForce;
        [SerializeField] float gravityModifier = 1.5f;

        [Header("Particles")]
        [SerializeField] ParticleSystem explosionParticle;
        [SerializeField] ParticleSystem fireworksParticle;

        [Header("Audio")]
        [SerializeField] AudioClip moneySound;
        [SerializeField] AudioClip explodeSound;

        Rigidbody body;
        AudioSource audioSource;

        void Start()
        {
            Physics.gravity = new Vector3(0f, WorldGravity.Scaled(gravityModifier), 0f);
            audioSource = GetComponent<AudioSource>();
            body = GetComponent<Rigidbody>();

            body.AddForce(Vector3.up * 5, ForceMode.Impulse);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.Space) && !gameOver)
            {
                body.AddForce(Vector3.up * floatForce);
            }

            if (transform.position.y > Ceiling)
            {
                transform.position = new Vector3(transform.position.x, Ceiling, transform.position.z);
                body.AddForce(Vector3.down * 5, ForceMode.Impulse);
            }
        }

        void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Bomb"))
            {
                explosionParticle.Play();
                audioSource.PlayOneShot(explodeSound, 1.0f);
                gameOver = true;
                Debug.Log("Game Over!");
                Destroy(other.gameObject);
            }
            else if (other.gameObject.CompareTag("Money"))
            {
                fireworksParticle.Play();
                audioSource.PlayOneShot(moneySound, 1.0f);
                Destroy(other.gameObject);
            }
            else if (other.gameObject.CompareTag("Ground"))
            {
                body.AddForce(Vector3.up * 10, ForceMode.Impulse);
            }
        }

        public bool GetGameOver() => gameOver;
    }
}
