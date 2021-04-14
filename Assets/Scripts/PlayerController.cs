using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float gravityModifier = 1f;
    [SerializeField] bool isOnGround;
    [SerializeField] ParticleSystem explosionParticle;
    [SerializeField] ParticleSystem dirtParticle;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip crashSound;

    Rigidbody myRigidbody;
    Animator playerAnim;
    AudioSource audioSource;

    bool gameOver;

    void Start()
    {
        gameOver = false;
        isOnGround = true;

        playerAnim = GetComponent<Animator>();
        myRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        Physics.gravity *= gravityModifier;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isOnGround && !gameOver)
        {

            myRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isOnGround = false;
            playerAnim.SetTrigger("Jump_trig");
            audioSource.clip = jumpSound;
            audioSource.Play();
            dirtParticle.Stop();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Ground")) 
        { 
            isOnGround = true;
            dirtParticle.Play();
        }
        else if (collision.gameObject.tag.Equals("Obstacle"))
        {
            gameOver = true;
            audioSource.clip = crashSound;
            audioSource.Play();
            playerAnim.SetBool("Death_b",true);
            playerAnim.SetInteger("DeathType_int",1);
            dirtParticle.Stop();
            explosionParticle.Play();
            Debug.Log("Game Over");
        }
    }

    public bool GetGameOver() 
    {
        return gameOver;
    }
}
