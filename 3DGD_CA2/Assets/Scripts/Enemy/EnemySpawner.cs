using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs; // Array to store multiple enemy types
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 15f;

    private List<GameObject> activeEnemies = new List<GameObject>();

    void Start()
    {
        SpawnAllEnemies();
    }

    void SpawnAllEnemies()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            SpawnEnemy(spawnPoint.position);
        }
    }

    void SpawnEnemy(Vector3 position)
    {
        // Randomly select an enemy type from the available prefabs
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject selectedEnemyPrefab = enemyPrefabs[randomIndex];

        GameObject newEnemy = Instantiate(selectedEnemyPrefab, position, Quaternion.identity);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();
        enemyScript.Initialize(position); // Initialize with spawn position
        activeEnemies.Add(newEnemy);
    }

    public void RespawnEnemy(Vector3 spawnPosition)
    {
        StartCoroutine(RespawnCoroutine(spawnPosition));
    }

    private IEnumerator RespawnCoroutine(Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnEnemy(spawnPosition);
    }
}
