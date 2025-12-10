using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq; // Added for simplified array functions

/// <summary>
/// Detects when human dies and shows Game Over screen, now with Post-Mortem Analysis.
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
    [Tooltip("Displays Survival Time and Detailed Analysis")]
    public Text survivalTimeText; 
    public Button restartButton;
    public Button quitButton;
    
    [Header("Settings")]
    public string gameOverMessage = "SIMULATION TERMINATED";
    public string initialMessage = "The civilization has fallen.";
    public int gameOverFontSize = 60;
    public int messageFontSize = 24; // Smaller font for analysis details
    
    private float gameStartTime;
    private bool gameOver = false;
    private float checkTimer = 0f;
    private bool everHadHuman = false;
    
    // Stored cause of death for analysis
    private string finalCause = "Unknown Failure";
    
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
        HumanMetabolism[] livingHumans = FindObjectsOfType<HumanMetabolism>().Where(h => h != null && h.isAlive).ToArray();
        
        // Check for humans that just died to capture the final cause
        HumanMetabolism[] recentlyDeadHumans = FindObjectsOfType<HumanMetabolism>().Where(h => h != null && !h.isAlive).ToArray();
        if (recentlyDeadHumans.Length > 0 && livingHumans.Length == 0)
        {
            // Find the last human to die and get their cause of death (Requires HumanMetabolism to set this string)
            HumanMetabolism lastDeadHuman = recentlyDeadHumans[0]; 
            // **NOTE**: Assuming HumanMetabolism sets a public string "lastCauseOfDeath" before dying.
            // If the HumanMetabolism script has this variable, we can grab it here:
            // this.finalCause = lastDeadHuman.lastCauseOfDeath; 
            
            // Fallback for demonstration:
            this.finalCause = "Starvation (Resource Depletion)";
        }

        // Mark that we've seen a human if any are currently alive
        if (livingHumans.Length > 0) everHadHuman = true;

        // Trigger game over only when we've seen at least one human previously
        // and there are now zero living humans.
        if (everHadHuman && livingHumans.Length == 0)
        {
            TriggerGameOver();
        }
    }
    
    public void TriggerGameOver()
    {
        if (gameOver) return;
        
        gameOver = true;
        float survivalTime = Time.time - gameStartTime;
        
        string analysisMessage = GenerateAnalysisMessage();
        
        Debug.Log($"[GameOver] Human civilization ended! Survival time: {FormatTime(survivalTime)}");
        
        // Show game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverText != null)
            {
                gameOverText.fontSize = gameOverFontSize;
                gameOverText.text = gameOverMessage;
            }
            
            if (survivalTimeText != null)
            {
                survivalTimeText.fontSize = messageFontSize;
                survivalTimeText.text = $"{initialMessage}\n\n" + analysisMessage +
                                        $"\n\nSurvival Time: {FormatTime(survivalTime)}";
            }
        }
        
        // Pause the game
        Time.timeScale = 0f;
    }
    
    /// <summary>
    /// Generates a human-readable analysis of why the simulation ended.
    /// </summary>
    private string GenerateAnalysisMessage()
    {
        AtmosphereManager atmosphere = AtmosphereManager.Instance;
        
        // 1. Get current population state
        int totalAnimals = FindObjectsOfType<AnimalMetabolism>().Count(a => a.isAlive);
        int totalGrass = FindObjectsOfType<PlantAgent>().Count(p => p.plantType == PlantAgent.PlantType.Grass);
        int totalTrees = FindObjectsOfType<PlantAgent>().Count(p => p.plantType == PlantAgent.PlantType.Tree);
        
        // 2. Get atmosphere state
        string atmState = atmosphere != null ? atmosphere.GetEnvironmentalStatusMessage() : "Atmosphere Status: Unknown";
        float co2 = atmosphere != null ? atmosphere.carbonDioxide : 0f;
        float o2 = atmosphere != null ? atmosphere.oxygen : 0f;
        
        // 3. Determine the Failure Chain
        string chain = $"--- POST-MORTEM ANALYSIS ---\n\n";

        // A. Primary Failure Cause
        if (finalCause.Contains("Starvation"))
        {
            chain += $"1. PRIMARY FAILURE: **{finalCause}**.\n";
            chain += "The population's energy needs could not be met by the available prey.\n\n";
            
            // B. Resource Chain Failure (Why starvation?)
            if (totalAnimals <= 5)
            {
                chain += $"2. ECOLOGICAL COLLAPSE: **The Animal Population Collapsed.** (Remaining: {totalAnimals})\n";
                chain += "Humans consumed the prey faster than the Animal Respawn Manager could replace them, leading to a critical shortage of meat.\n\n";
            }
            else
            {
                 chain += $"2. ECOLOGICAL PROBLEM: **Inefficient Hunting**.\n";
                 chain += $"Though {totalAnimals} animals remained, they were not found or hunted fast enough to sustain the Human Metabolism.\n\n";
            }
            
            // C. Atmospheric Influence (Is the food source okay?)
            if (o2 < atmosphere.oxygenDangerThreshold || co2 > atmosphere.co2DangerThreshold)
            {
                 chain += $"3. ATMOSPHERIC STRESS: **The Environment Was Dangerous ({atmState})**.\n";
                 chain += $"The low oxygen ({o2:F2}%) or high CO₂ ({co2:F2}%) increased metabolic stress, accelerating energy burn and hunger.\n";
            }
        }
        else if (finalCause.Contains("CO2 Toxicity") || finalCause.Contains("Hypoxia"))
        {
            chain += $"1. PRIMARY FAILURE: **Atmospheric Crisis**.\n";
            chain += $"Death was caused by environmental conditions: **{finalCause}**.\n\n";
            chain += $"2. IMBALANCE: **The Ecosystem Could Not Cope**.\n";
            chain += $"Respiration from all entities ({totalAnimals} animals) and Humans ({totalTrees} trees, {totalGrass} grass) overwhelmed the photosynthetic capacity, leading to a critical gas buildup/shortage.\n";
        }
        else
        {
            chain += $"1. PRIMARY FAILURE: **{finalCause}**.\n";
            chain += $"The end cause did not fit a standard ecological model. Check Debug Logs.\n";
        }
        
        chain += $"\n--- FINAL ATMOSPHERE ---\n";
        chain += $"O₂: {o2:F2}% (Limit: {atmosphere.oxygenDangerThreshold}%) | CO₂: {co2:F4}% (Limit: {atmosphere.co2DangerThreshold}%)\n";
        chain += $"Plant Mass: {totalGrass} Grass, {totalTrees} Trees\n";

        return chain;
    }

    // --- Scene Management (kept the same) ---
    
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