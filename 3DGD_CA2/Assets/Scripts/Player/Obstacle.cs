using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private float knockbackStrength = 3f;

    void OnCollisionEnter(Collision other)
    {
        // Attempt to get the ThirdPersonController component from the object that collided
        ThirdPersonController player = other.gameObject.GetComponent<ThirdPersonController>();

        // If we found a ThirdPersonController on the colliding object, apply the knockback effect
        if (player != null)
        {
            // Calculate the knockback direction (away from the obstacle)
            Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;

            // Call the KnockbackAndBlink method on the player with the knockback direction and strength
            player.KnockbackAndBlink(knockbackDirection, knockbackStrength, 0.5f);
        }
    }
}
