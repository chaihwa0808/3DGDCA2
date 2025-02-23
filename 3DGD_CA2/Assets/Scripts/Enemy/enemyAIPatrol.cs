using System.Collections;
using System.Collections.Generic;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.AI;

public class enemyAIPatrol : MonoBehaviour
{
    GameObject player1;
    GameObject player2;

    NavMeshAgent agent;

    GameObject terrain;
    GameObject Players;

    //patrol
    Vector3 destPoint;
    bool walkpointSet;
    [SerializeField] float range;

    // Start is called before the first frame update
    void Start()
    {
        player1 = GameObject.FindWithTag("P1");
        player2 = GameObject.FindWithTag("P2");

        terrain = GameObject.Find("Terrain");

        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        Patrol();
    }
    void Patrol()
    {
        if (!walkpointSet) SearchForDest();
        if (walkpointSet) agent.SetDestination(destPoint);
        if(Vector3.Distance(transform.position,destPoint) < 10) walkpointSet = false ;
    }

    void SearchForDest()
    {
        float randomZ = Random.Range(-range, range);
        float randomX = Random.Range(-range,range);

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
}
