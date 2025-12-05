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
    [Tooltip("Minimum number of living humans required to continue the simulation. If living humans < this value, Game Over triggers.")]
    public int minHumansToContinue = 1;
    
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
    // Track whether the simulation has ever seen a human so we don't trigger
    // Game Over immediately in scenes without humans.
    private bool everHadHuman = false;
    
    void Start()
    {
        gameStartTime = Time.time;
        
        // Hide game over UI at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Auto-wire buttons if assigned
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        
        Debug.Log("[GameOverManager] Initialized. Monitoring for human death...");
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
        // Count living humans using HumanMetabolism when available.
        int livingHumans = 0;

        HumanMetabolism[] humans = FindObjectsOfType<HumanMetabolism>();
        foreach (var h in humans)
        {
            if (h != null && h.isAlive) livingHumans++;
        }

        // Fallback: include any BiomassEnergy carnivores that don't have a HumanMetabolism component
        BiomassEnergy[] allEntities = FindObjectsOfType<BiomassEnergy>();
        foreach (var be in allEntities)
        {
            if (be == null) continue;

            // Only consider carnivores
            if (be.entityType != BiomassEnergy.EntityType.Carnivore) continue;

            // If this GameObject already has a HumanMetabolism component we already counted it
            if (be.GetComponent<HumanMetabolism>() != null) continue;

            if (be.isAlive) livingHumans++;
        }

        // Debug: report living humans and threshold
        string names = "";
        foreach (var h in humans)
        {
            if (h != null && h.isAlive)
            {
                names += h.gameObject.name + ", ";
            }
        }
        // Include fallback carnivores without HumanMetabolism
        foreach (var be in allEntities)
        {
            if (be == null) continue;
            if (be.entityType != BiomassEnergy.EntityType.Carnivore) continue;
            if (be.GetComponent<HumanMetabolism>() != null) continue;
            if (be.isAlive)
            {
                names += be.gameObject.name + ", ";
            }
        }

        Debug.Log($"[GameOverManager] LivingHumans={livingHumans}, MinRequired={minHumansToContinue}. Humans: {names}");

        // Mark that we've seen a human if any are currently alive
        if (livingHumans > 0) everHadHuman = true;

        // Trigger game over only when we've seen at least one human previously
        // and there are now zero living humans.
        if (everHadHuman && livingHumans == 0)
        {
            TriggerGameOver();
        }
    }
    
    public void TriggerGameOver()
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
        Debug.Log("[GameOverManager] RestartGame() called!");
        
        // Use GameStartManager's restart to skip start screen
        if (GameStartManager.Instance != null)
        {
            GameStartManager.Instance.RestartGame();
        }
        else
        {
            // Fallback if no GameStartManager
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("[GameOverManager] QuitGame() called!");
        
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

    public void CloseGameOver()
    {
        gameOverPanel.SetActive(false);
        Time.timeScale = 1f;
    }

}
