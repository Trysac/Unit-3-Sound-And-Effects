using UnityEngine;

namespace BalloonDash.Game.Challenge3
{
    /// <summary>Challenge 3: spins money and bombs around their vertical axis.</summary>
    public class SpinObjectsX : MonoBehaviour
    {
        public float spinSpeed;

        void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
        }
    }
}
