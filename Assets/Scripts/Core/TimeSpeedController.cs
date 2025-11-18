using UnityEngine;

public class TimeSpeedController : MonoBehaviour
{
    public static TimeSpeedController Instance { get; private set; }
    
    [Header("Time Speed Settings")]
    [Tooltip("Current time multiplier (1 = normal, 2 = 2x speed, etc.)")]
    public int currentSpeed = 1;
    
    [Tooltip("Available speed options")]
    public int[] speedOptions = { 1, 2, 4, 8, 12 };
    
    [Tooltip("Base seconds for one full day at 1x speed")]
    public float baseDaySeconds = 120f;
    
    private int currentSpeedIndex = 0;
    private SunMoonController sunMoonController;
    private AtmosphereManager atmosphereManager;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // Find controllers
        sunMoonController = FindAnyObjectByType<SunMoonController>();
        atmosphereManager = FindAnyObjectByType<AtmosphereManager>();
        
        // Find and update the speed index
        currentSpeedIndex = System.Array.IndexOf(speedOptions, currentSpeed);
        if (currentSpeedIndex < 0)
        {
            Debug.LogWarning($"[TimeSpeed] Invalid initial speed {currentSpeed}, resetting to 1x");
            currentSpeed = 1;
            currentSpeedIndex = 0;
        }
        
        // Set initial speed
        SetSpeed(currentSpeed);
        
        // Ensure game isn't paused
        if (Time.timeScale == 0)
        {
            Debug.LogWarning("[TimeSpeed] Game was paused (timeScale=0), unpausing...");
            Time.timeScale = 1;
        }
    }
    
    /// <summary>
    /// Set time speed to a specific multiplier
    /// </summary>
    public void SetSpeed(int speed)
    {
        // Validate and update speed index
        int speedIndex = System.Array.IndexOf(speedOptions, speed);
        if (speedIndex < 0)
        {
            Debug.LogWarning($"[TimeSpeed] Invalid speed {speed}. Valid options: {string.Join(", ", speedOptions)}");
            return;
        }
        
        currentSpeed = speed;
        currentSpeedIndex = speedIndex;
        
        // Calculate new day length (shorter = faster)
        float newDaySeconds = baseDaySeconds / currentSpeed;
        
        // Update SunMoonController
        if (sunMoonController != null)
        {
            sunMoonController.fullDaySeconds = newDaySeconds;
            Debug.Log($"[TimeSpeed] Speed set to ×{currentSpeed} (Day = {newDaySeconds:F1}s)");
        }
        
        // Update AtmosphereManager - DON'T change secondsPerDay, use speed multiplier instead
        if (atmosphereManager != null)
        {
            // Keep secondsPerDay constant, use speedMultiplier for gas exchange
            atmosphereManager.speedMultiplier = currentSpeed;
        }
    }
    
    /// <summary>
    /// Increase to next speed tier
    /// </summary>
    public void IncreaseSpeed()
    {
        currentSpeedIndex++;
        if (currentSpeedIndex >= speedOptions.Length)
        {
            currentSpeedIndex = speedOptions.Length - 1; // Cap at max
        }
        
        SetSpeed(speedOptions[currentSpeedIndex]);
    }
    
    /// <summary>
    /// Decrease to previous speed tier
    /// </summary>
    public void DecreaseSpeed()
    {
        currentSpeedIndex--;
        if (currentSpeedIndex < 0)
        {
            currentSpeedIndex = 0; // Cap at min
        }
        
        SetSpeed(speedOptions[currentSpeedIndex]);
    }
    
    /// <summary>
    /// Cycle to next speed option (wraps around)
    /// </summary>
    public void CycleSpeed()
    {
        currentSpeedIndex++;
        if (currentSpeedIndex >= speedOptions.Length)
        {
            currentSpeedIndex = 0; // Loop back to 1x
        }
        
        SetSpeed(speedOptions[currentSpeedIndex]);
    }
    
    /// <summary>
    /// Returns formatted text for current speed (e.g., "×4")
    /// </summary>
    public string GetSpeedText()
    {
        return $"×{currentSpeed}";
    }
    
    /// <summary>
    /// Reset to normal speed (1x)
    /// </summary>
    public void ResetSpeed()
    {
        currentSpeedIndex = 0;
        SetSpeed(speedOptions[0]);
    }
    
    /// <summary>
    /// Keyboard shortcuts
    /// </summary>
    void Update()
    {
        // Number keys 1-5 for quick speed selection
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeedByIndex(0); // ×1
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeedByIndex(1); // ×2
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeedByIndex(2); // ×4
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSpeedByIndex(3); // ×8
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetSpeedByIndex(4); // ×12
        
        // Arrow keys or +/- for increase/decrease
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            IncreaseSpeed();
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            DecreaseSpeed();
        }
        
        // Tab to cycle through speeds (Space removed to avoid conflicts)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleSpeed();
        }
    }
    
    void SetSpeedByIndex(int index)
    {
        if (index >= 0 && index < speedOptions.Length)
        {
            currentSpeedIndex = index;
            SetSpeed(speedOptions[index]);
        }
    }
}
