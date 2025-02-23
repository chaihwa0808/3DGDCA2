using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] public float timeRemaining = 2000f;  // Set the starting time in seconds
    [SerializeField] private TextMeshProUGUI timerText;  // The UI Text element to display the countdown

    private bool timerRunning = false;

    void Start()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(timeRemaining);  // Display the initial time
            timerRunning = true;
        }
    }

    void Update()
    {
        if (timerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;  // Decrease the remaining time
                timerText.text = FormatTime(timeRemaining);  // Update the UI with the new time
            }
            else
            {
                timeRemaining = 0;
                timerRunning = false;  // Stop the timer when it reaches zero
                timerText.text = "Time's Up!";
                // Optionally, you can trigger any event when the time is up here
            }
        }
    }

    // Start the countdown timer
    public void StartTimer()
    {
        timerRunning = true;
    }

    // Format the time as minutes:seconds (e.g., 02:30)
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
