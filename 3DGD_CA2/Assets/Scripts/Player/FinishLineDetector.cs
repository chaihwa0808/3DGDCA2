using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLineDetector : MonoBehaviour
{
    private bool raceFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (raceFinished) return; // Ignore if already triggered

        if (other.CompareTag("P1"))
        {
            raceFinished = true;
            Debug.Log("Player 1 reached first! Loading scene...");
            SceneManager.LoadScene("P1WinScene");
        }
        else if (other.CompareTag("P2"))
        {
            raceFinished = true;
            Debug.Log("Player 2 reached first! Loading scene...");
            SceneManager.LoadScene("P2WinScene");
        }
    }
}
