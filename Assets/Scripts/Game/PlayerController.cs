using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>The course runner: Space jumps, landing kicks up dust, an obstacle ends the run.</summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float jumpForce = 10f;
        [SerializeField] float gravityModifier = 1f;
        [SerializeField] ParticleSystem explosionParticle;
        [SerializeField] ParticleSystem dirtParticle;
        [SerializeField] AudioClip jumpSound;
        [SerializeField] AudioClip crashSound;

        readonly RunnerState state = new RunnerState();
        Rigidbody body;
        Animator animator;
        AudioSource audioSource;

        public RunnerState State => state;

        void Start()
        {
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();

            Physics.gravity *= gravityModifier;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
        }

        /// <summary>What Space does: a jump with its animation and sound, when the runner may jump.</summary>
        public void Jump()
        {
            if (!state.TryJump())
            {
                return;
            }
            body.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger("Jump_trig");
            audioSource.clip = jumpSound;
            audioSource.Play();
            dirtParticle.Stop();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                if (state.Land())
                {
                    dirtParticle.Play();
                }
            }
            else if (collision.gameObject.CompareTag("Obstacle"))
            {
                state.Crash();
                audioSource.clip = crashSound;
                audioSource.Play();
                animator.SetBool("Death_b", true);
                animator.SetInteger("DeathType_int", 1);
                dirtParticle.Stop();
                explosionParticle.Play();
                Debug.Log("Game Over");
            }
        }

        public bool GetGameOver() => state.GameOver;
    }
}
