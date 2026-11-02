using UnityEngine;

namespace BalloonDash.Game.Challenge3
{
    /// <summary>Challenge 3: meant to jump the scrolling backdrop back to its start every half of its width.</summary>
    public class RepeatBackgroundX : MonoBehaviour
    {
        Vector3 startPosition;

        void Start()
        {
            startPosition = transform.position;
        }

        void Update()
        {
            if (transform.position.x < GetComponent<BoxCollider>().size.x / 2)
            {
                transform.position = startPosition;
            }
        }
    }
}
