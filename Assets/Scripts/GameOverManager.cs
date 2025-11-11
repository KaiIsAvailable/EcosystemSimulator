using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects when human dies and shows Game Over screen
/// Automatically monitors all entities with BiomassEnergy
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Game Over Detection")]
    [Tooltip("Check for human death every X seconds")]
    public float checkInterval = 0.5f;
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Text survivalTimeText;
    public Button restartButton;
    public Button quitButton;
    
    [Header("Settings")]
    public string gameOverMessage = "GAME OVER";
    public string humanDiedMessage = "The human has died!\nThe ecosystem simulation has ended.";
    public int gameOverFontSize = 60;
    public int messageFontSize = 30;
    
    private float gameStartTime;
    private bool gameOver = false;
    private float checkTimer = 0f;
    
    void Start()
    {
        gameStartTime = Time.time;
        
        // Hide game over UI at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    void Update()
    {
        if (gameOver) return;
        
        // Check periodically for human death
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckHumanAlive();
        }
    }
    
    void CheckHumanAlive()
    {
        // Find all BiomassEnergy components
        BiomassEnergy[] allEntities = FindObjectsByType<BiomassEnergy>(FindObjectsSortMode.None);
        
        bool humanFound = false;
        bool humanAlive = false;
        
        foreach (BiomassEnergy entity in allEntities)
        {
            // Check if this is the human
            if (entity.entityType == BiomassEnergy.EntityType.Carnivore)
            {
                humanFound = true;
                if (entity.isAlive)
                {
                    humanAlive = true;
                    break;
                }
            }
        }
        
        // Trigger game over if human is dead
        if (humanFound && !humanAlive)
        {
            TriggerGameOver();
        }
    }
    
    void TriggerGameOver()
    {
        if (gameOver) return;
        
        gameOver = true;
        float survivalTime = Time.time - gameStartTime;
        
        Debug.Log($"[GameOver] Human died! Survival time: {FormatTime(survivalTime)}");
        
        // Show game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // Update texts
            if (gameOverText != null)
                gameOverText.text = gameOverMessage;
            
            if (survivalTimeText != null)
            {
                survivalTimeText.text = $"{humanDiedMessage}\n\nSurvival Time: {FormatTime(survivalTime)}";
            }
        }
        
        // Pause the game
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        Debug.Log("[GameOver] Restarting game...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitGame()
    {
        Debug.Log("[GameOver] Quitting game...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes}m {secs}s";
    }
}
