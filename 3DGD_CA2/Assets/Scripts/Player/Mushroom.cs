using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mushroom : MonoBehaviour
{

    public float hoverSpeed = 2f;    // Speed of hovering
    public float hoverHeight = 0.3f; // Maximum height variation
    private Transform targetP2;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponent<ThirdPersonController>();
        if (player != null)
        {
            // slow target
            player.CollectMushroom();
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
