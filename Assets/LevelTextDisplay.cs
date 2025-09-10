using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MarsFPSKit;


public class LevelTextDisplay : MonoBehaviour
{
    public MarsFPSKit.UI.Kit_LevelingPlayerPrefs levelingData;
    public Text levelText;
    public GameObject winPanel;

    private int lastKnownLevel = -1;
    public Kit_IngameMain Kit_IngameMain;
    void Start()
    {
        if (levelingData != null)
        {
            lastKnownLevel = levelingData.GetLevel();
            UpdateLevelText();
        }
    }

    void Update()
    {
         // Ensure the cursor stays visible and unlocked every frame
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
        if (levelingData != null)
        {
            int currentLevel = levelingData.GetLevel();

            if (currentLevel != lastKnownLevel)
            {
                OnLevelUp(currentLevel);
                lastKnownLevel = currentLevel;
            }
        }
    }

    void OnLevelUp(int newLevel)
    {
        Debug.Log($"[Update()] Level Up Detected: Level {newLevel}");

        UpdateLevelText();

        if (winPanel != null)
        {
            winPanel.SetActive(true); // Show win panel
            Time.timeScale = 0f;
        }
    }

    void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + levelingData.GetLevel();
        }
    }
    public void Gotomain()
{
    // Find the Kit_IngameMain instance in the scene
    Kit_IngameMain ingameMainInstance = FindObjectOfType<Kit_IngameMain>();
    if (ingameMainInstance != null)
    {
        ingameMainInstance.Disconnect();
        Debug.Log("Disconnected from lobby/server");
    }
    else
    {
        Debug.LogWarning("Kit_IngameMain not found in scene!");
    }

    // Reset time scale in case it was paused
    Time.timeScale = 1f;

    // Now load the main menu scene
   // SceneManager.LoadScene("MainMenu");
}
    public void gotomianemu()
    {
        SceneManager.LoadScene(0);
}

}
