using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GateController : MonoBehaviour
{
    GameObject P1;
    GameObject P2;

    private int p1KillCount;
    private int p2KillCount;

    private Animator animator;

    public int requiredKillCount = 1; // Example required kills

    private bool gateOpened;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        P1 = GameObject.FindWithTag("P1");
        P2 = GameObject.FindWithTag("P2");
    }

    // Update is called once per frame
    void Update()
    {
        if (P1 != null)
        {
            var playerScript1 = P1.GetComponent<ThirdPersonController>();
            if (playerScript1 != null)
                p1KillCount = playerScript1.killCount;
        }

        if (P2 != null)
        {
            var playerScript2 = P2.GetComponent<ThirdPersonController>();
            if (playerScript2 != null)
                p2KillCount = playerScript2.killCount;
        }

        if (p1KillCount >= requiredKillCount && p2KillCount >= requiredKillCount)
        {
            if(!gateOpened)
            animator.SetTrigger("Open");
            gateOpened = true;
        }
    }
}
