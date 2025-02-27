using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCanvas : MonoBehaviour
{
    public GameObject control;

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Level1");
    }

    public void LoadInstructions()
    {
        SceneManager.LoadScene("Instructions");
    }

    public void Credits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void P1Win()
    {
        SceneManager.LoadScene("P1WinScene");
    }

    public void P2Win()
    {
        SceneManager.LoadScene("P2WinScene");
    }

    public void Lose()
    {
        SceneManager.LoadScene("Lose");
    }

    public void Toggle()
    {
        control.SetActive(!control.activeSelf);
    }

}
