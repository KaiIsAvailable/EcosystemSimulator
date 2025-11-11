using UnityEngine;

/// <summary>
/// Simple breathing/pulsing animation for any GameObject
/// Shows that plants, animals, and humans are all "alive" and exchanging gases
/// </summary>
public class BreathingAnimation : MonoBehaviour
{
    [Header("Breathing Settings")]
    [Tooltip("Enable/disable the breathing animation")]
    public bool isBreathing = true;
    
    [Tooltip("How many breaths per second")]
    public float breathingSpeed = 1.0f;
    
    [Tooltip("How much to expand/contract (0.05 = 5% scale change)")]
    public float breathingAmount = 0.05f;
    
    [Tooltip("Randomize the starting phase so not all entities breathe in sync")]
    public bool randomizePhase = true;
    
    private Vector3 originalScale;
    private float breathingTimer = 0f;
    
    void Start()
    {
        // Store original scale
        originalScale = transform.localScale;
        
        // Randomize breathing phase for variety
        if (randomizePhase)
        {
            breathingTimer = Random.Range(0f, Mathf.PI * 2f);
        }
    }
    
    void Update()
    {
        if (!isBreathing) return;
        
        // Update breathing timer
        breathingTimer += Time.deltaTime * breathingSpeed;
        
        // Calculate breathing scale using sine wave
        // sin(x) oscillates between -1 and 1
        float breathScale = 1f + Mathf.Sin(breathingTimer) * breathingAmount;
        
        // Apply scale
        transform.localScale = originalScale * breathScale;
    }
    
    /// <summary>
    /// Stop breathing animation (e.g., when entity dies)
    /// </summary>
    public void StopBreathing()
    {
        isBreathing = false;
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Resume breathing animation
    /// </summary>
    public void StartBreathing()
    {
        isBreathing = true;
    }
    
    /// <summary>
    /// Change breathing speed (e.g., faster when stressed)
    /// </summary>
    public void SetBreathingSpeed(float speed)
    {
        breathingSpeed = speed;
    }
}
