using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text to display the score/time of the last played session.")]
    public TMP_Text lastSessionText;
    
    [Tooltip("Text to display the best score/longest time.")]
    public TMP_Text highScoreText;

    private void Start()
    {
        // Load and display last session time
        float lastSessionTime = PlayerPrefs.GetFloat("LastSessionTime", 0f);
        if (lastSessionText != null)
        {
            lastSessionText.text = $"Last Session: {FormatTime(lastSessionTime)}";
        }

        // Load and display high score
        float highScoreTime = PlayerPrefs.GetFloat("HighScoreTime", 0f);
        if (highScoreText != null)
        {
            highScoreText.text = $"High Score: {FormatTime(highScoreTime)}";
        }
    }

    /// <summary>
    /// Formats a float in seconds to a MM:SS string.
    /// </summary>
    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0f) return "--:--";

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Call this from a UI Button OnClick event to load the LEVEL scene.
    /// </summary>
    public void LoadLevelScene()
    {
        SceneManager.LoadScene("LEVEL");
    }

    /// <summary>
    /// Call this from a UI Button OnClick event to load the RisaLevel scene.
    /// </summary>
    public void LoadRisaLevelScene()
    {
        SceneManager.LoadScene("RisaLevel");
    }
}
