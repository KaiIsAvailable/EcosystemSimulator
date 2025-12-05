using UnityEngine;
using UnityEngine.UI;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject startPanel;     // Drag your Start Panel here
    public GameObject gameOverPanel;  // Drag your Game Over Panel here

    [Header("Buttons (optional - auto-wire if assigned)")]
    public Button startButton;
    public Button quitButton;
    
    [Header("Auto-created Canvas (set by GameStartSetup)")]
    public GameObject startCanvasRoot; // The auto-created canvas to destroy on start

    private bool gameStarted = false;
    
    // Static flag to skip start screen after restart (persists across scene reload)
    private static bool isRestarting = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // If restarting, skip start screen and go directly to gameplay
        if (isRestarting)
        {
            isRestarting = false; // Reset flag
            gameStarted = true;
            Time.timeScale = 1f;
            
            if (startPanel) startPanel.SetActive(false);
            if (startCanvasRoot != null) Destroy(startCanvasRoot);
            if (gameOverPanel) gameOverPanel.SetActive(false);
            
            Debug.Log("[GameStartManager] Restarted - skipping start screen.");
            return;
        }
        
        // Normal first start - pause game and show start panel
        Time.timeScale = 0f;

        if (startPanel) startPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Auto-wire buttons if assigned
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        Debug.Log("[GameStartManager] Initialized. Press ENTER or click Start to begin.");
    }

    void Update()
    {
        if (!gameStarted && Input.GetKeyDown(KeyCode.Return))
        {
            StartGame();
        }
    }

    // ------------------------------
    // ✔ FUNCTION for Start Button
    // ------------------------------
    public void StartGame()
    {
        Debug.Log("[GameStartManager] StartGame() called!");
        
        if (gameStarted) return;
        gameStarted = true;

        Time.timeScale = 1f; // Unpause game
        
        // Hide or destroy start UI
        if (startPanel) startPanel.SetActive(false);
        if (startCanvasRoot != null) Destroy(startCanvasRoot);
        
        Debug.Log("[GameStartManager] Game started! Time.timeScale = 1");
    }

    // ------------------------------
    // ✔ FUNCTION for Quit Button
    // ------------------------------
    public void QuitGame()
    {
        Debug.Log("[GameStartManager] QuitGame() called!");
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    // ------------------------------
    // ✔ FUNCTION for Restart Button
    // ------------------------------
    public void RestartGame()
    {
        Debug.Log("[GameStartManager] RestartGame() called - will skip start screen on reload.");
        isRestarting = true; // Set flag so next scene load skips start screen
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // ------------------------------
    // ✔ Function to call from GameOverManager
    // ------------------------------
    public void ShowGameOver()
    {
        Time.timeScale = 0f;
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }
}
