using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepeatBackground : MonoBehaviour
{
    [SerializeField] Vector3 startPos;
    [SerializeField] float repeatWidth;

    void Start()
    {
        startPos = transform.position;
        repeatWidth = GetComponent<BoxCollider>().size.x / 2;
    }


    void Update()
    {
        if (transform.position.x < startPos.x - repeatWidth) 
        {
            transform.position = startPos;
        }
    }
}
