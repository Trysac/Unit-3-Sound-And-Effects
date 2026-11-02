using BalloonDash.Core;
using UnityEngine;

namespace BalloonDash.Game
{
    /// <summary>Jumps the scrolling backdrop back to its start every half of its width.</summary>
    public class RepeatBackground : MonoBehaviour
    {
        Vector3 startPosition;
        float repeatWidth;

        void Start()
        {
            startPosition = transform.position;
            repeatWidth = GetComponent<BoxCollider>().size.x / 2;
        }

        void Update()
        {
            if (BackgroundLoop.Wrap(transform.position.x, startPosition.x, repeatWidth) != transform.position.x)
            {
                transform.position = startPosition;
            }
        }
    }
}
