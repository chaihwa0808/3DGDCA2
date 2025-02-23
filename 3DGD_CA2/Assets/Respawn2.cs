using System.Collections;
using UnityEngine;

public class Respawn2 : MonoBehaviour
{
    [SerializeField] private float respawnTime = 5f; // Time to wait before respawning
    [SerializeField] private Vector3 respawnPosition; // The original position to respawn at

    private Enemy enemyScript;

    void Start()
    {
        enemyScript = GetComponent<Enemy>();
        respawnPosition = enemyScript.originalPosition; // Default to current position
    }

    public void HandleRespawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        gameObject.SetActive(false); // Disable enemy
        yield return new WaitForSeconds(respawnTime); // Wait for respawn time
        Respawn();
    }

    private void Respawn()
    {
        gameObject.SetActive(true); // Enable the enemy
        transform.position = respawnPosition; // Set position back to original
        enemyScript.ResetHealth(); // Reset health or any other properties
        // Re-enable other components as needed, like Animator, NavMeshAgent, etc.
    }
}
