using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    GameObject player1;
    GameObject player2;

    NavMeshAgent agent;

    GameObject terrain;


    // Patrol
    Vector3 destPoint;
    bool walkpointSet;
    [SerializeField] float range; // patrol range

    // State change
    [SerializeField] float sightRange, attackRange;
    bool playerInSight, playerInAttackRange;

    // Original position
    private Vector3 originalPosition;
    private bool returningToOriginal = false;

    Animator anim;

    [SerializeField] private float maxHealth = 3;
    private float currentHealth;
    [SerializeField] private EnemyHealthBar healthBar;

    // Attack cooldown
    [SerializeField] private float timeBetweenAttacks = 5f; //Cooldown time in seconds
    private float lastAttackTime = -Mathf.Infinity; // To track when the last attack occured



    // Start is called before the first frame update
    void Start()
    {
        player1 = GameObject.FindWithTag("P1");
        player2 = GameObject.FindWithTag("P2");

        terrain = GameObject.Find("Terrain");

        agent = GetComponent<NavMeshAgent>();

        originalPosition = transform.position;

        anim = GetComponent<Animator>();

        currentHealth = maxHealth;
        healthBar.UpdateHealthBar(maxHealth, currentHealth);
    }

    // Update is called once per frame
    void Update()
    {
        if (anim.GetBool("isDead")) return;

        GameObject nearestPlayer = GetNearestPlayer();

        if (nearestPlayer != null)
        {

            float distance = Vector3.Distance(transform.position, nearestPlayer.transform.position);
            playerInSight = distance <= sightRange;
            playerInAttackRange = distance <= attackRange;

            if (!playerInSight && !playerInAttackRange)
            {
                if (returningToOriginal)
                {
                    ReturnToOriginalPosition();
                }
                else
                {
                    Patrol();
                }
            }
            else
            {
                returningToOriginal = false;

                if (playerInSight && !playerInAttackRange)
                {
                   Debug.Log("Detecting player, now chase");
                    Chase(nearestPlayer);
                }

                // Attack only if cooldown has passed and not already attacking
                if (playerInAttackRange && Time.time - lastAttackTime >= timeBetweenAttacks && !anim.GetBool("isAttacking"))
                {
                    Attack(nearestPlayer);
                }
            }
        }
        else
        {
            Debug.Log("No player detected, returning to original position.");
            ReturnToOriginalPosition();
        }

        // Ensure animations transition properly
        if (!anim.GetBool("isAttacking"))
        {
            anim.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
        }
    }


    void Chase(GameObject target)
    {
        if (target != null)
        {
            agent.SetDestination(target.transform.position);
        }
    }


    void Attack(GameObject target)
    {
        if (!anim.GetBool("isAttacking")) // Prevents animation from being retriggered too quickly
        {
            anim.SetBool("isAttacking", true);
            agent.SetDestination(transform.position);
            lastAttackTime = Time.time; //Update last attack time
            Invoke("StopAttack", 1.0f); // Adjust timing based on animation length
        }
        
    }

    void StopAttack()
    {
        anim.SetBool("isAttacking", false);
        Debug.Log("Stopping attack, transitioning to idle/walk");

        // Allow movement if needed
        if (!playerInAttackRange)
        {
            anim.SetBool("isWalking", true);
        }
    }

    void Patrol()
    {
        if (!walkpointSet) SearchForDest();
        if (walkpointSet) agent.SetDestination(destPoint);
        if (Vector3.Distance(transform.position, destPoint) < 2f) walkpointSet = false;
    }

    void ReturnToOriginalPosition()
    {
        if (!returningToOriginal)
        {
            returningToOriginal = true;
            agent.SetDestination(originalPosition);
        }

        if (Vector3.Distance(transform.position, originalPosition) < 2f)
        {
            returningToOriginal = false;
            Patrol();
        }
    }

    void SearchForDest()
    {
        float randomZ = Random.Range(-range, range);
        float randomX = Random.Range(-range, range);

        Vector3 potentialDest = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        // Use Raycast to check if the point is on the terrain
        if (Physics.Raycast(potentialDest + Vector3.up * 5, Vector3.down, out RaycastHit hit, Mathf.Infinity))
        {
            if (hit.collider.gameObject == terrain) // Ensure it's the terrain
            {
                destPoint = hit.point;
                walkpointSet = true;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Prevent damage after death

        currentHealth -= damage;
        healthBar.UpdateHealthBar(maxHealth, currentHealth);

        // Play "Get Hit" animation using a bool
        anim.SetBool("isHit", true);

        // Temporarily stop movement
        agent.isStopped = true;

        // If enemy is still alive, reset hit animation after delay
        if (currentHealth > 0)
        {
            StartCoroutine(ResetHitAnimation());
        }
        else
        {
            anim.SetBool("isDead", true);
            Die();
        }
    }
    IEnumerator ResetHitAnimation()
    {
        yield return new WaitForSeconds(0.5f); // Adjust based on animation length
        anim.SetBool("isHit", false);
        agent.isStopped = false; // Resume movement after getting hit
    }

    void Die()
    { // Stop enemy movement
        agent.isStopped = true;

        // Set the "Die" trigger in Animator
        anim.SetTrigger("Die");

        // Disable collision so the enemy doesn't block anything after dying
        GetComponent<Collider>().enabled = false;

        // Disable NavMeshAgent to prevent unwanted movement
        agent.enabled = false;

        // Destroy the enemy after animation plays (adjust time if needed)
        Destroy(gameObject, 2f);
    }

    GameObject GetNearestPlayer()
    {
        if (player1 == null && player2 == null) return null;
        if (player1 == null) return player2;
        if (player2 == null) return player1;

        float dist1 = Vector3.Distance(transform.position, player1.transform.position);
        float dist2 = Vector3.Distance(transform.position, player2.transform.position);

        GameObject nearest = (dist1 < dist2) ? player1 : player2;
        Debug.Log("Nearest player detected: " + nearest.name); // Debugging
        return nearest;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = GetNearestPlayer();

        if (player != null)
        {
            print("Hit" + GetNearestPlayer());

        }
    }
}
