using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerX : MonoBehaviour
{
    [SerializeField] bool gameOver;
    [SerializeField] float floatForce;
    [SerializeField] float gravityModifier = 1.5f;

    [Header("Particules")]
    [SerializeField] ParticleSystem explosionParticle;
    [SerializeField] ParticleSystem fireworksParticle;

    [Header("Audios")]
    [SerializeField] AudioClip moneySound;
    [SerializeField] AudioClip explodeSound;

    Rigidbody playerRb;
    AudioSource playerAudio;

    void Start()
    {
        Physics.gravity *= gravityModifier;
        playerAudio = GetComponent<AudioSource>();
        playerRb = GetComponent<Rigidbody>();

        playerRb.AddForce(Vector3.up * 5, ForceMode.Impulse);

    }
    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && !gameOver)
        {

            playerRb.AddForce(Vector3.up * floatForce);
        }

        if (transform.position.y > 15)
        {
            transform.position = new Vector3(transform.position.x, 15, transform.position.z);
            playerRb.AddForce(Vector3.down * 5, ForceMode.Impulse);
        }

    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Bomb"))
        {
            explosionParticle.Play();
            playerAudio.PlayOneShot(explodeSound, 1.0f);
            gameOver = true;
            Debug.Log("Game Over!");
            Destroy(other.gameObject);
        } 

        else if (other.gameObject.CompareTag("Money"))
        {
            fireworksParticle.Play();
            playerAudio.PlayOneShot(moneySound, 1.0f);
            Destroy(other.gameObject);

        }

        else if (other.gameObject.CompareTag("Ground"))
        {
            playerRb.AddForce(Vector3.up * 10, ForceMode.Impulse);
        }
    }

    public bool GetGameOver() 
    {
        return gameOver;
    }

}
