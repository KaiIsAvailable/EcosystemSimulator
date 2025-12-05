using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Displays event notifications in the UI (animal eats, starves, dies, etc.)
/// Shows notifications as a scrolling list that saves all history
/// User can scroll up/down to view previous notifications
/// </summary>
public class EventNotificationUI : MonoBehaviour
{
    public static EventNotificationUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Text component to display notifications")]
    public Text notificationText;
    
    [Tooltip("ScrollRect component for scrolling (optional but recommended)")]
    public ScrollRect scrollRect;
    
    [Header("Settings")]
    [Tooltip("Maximum number of notifications to keep in history (0 = unlimited)")]
    public int maxHistorySize = 100;
    
    [Tooltip("Enable/disable notifications")]
    public bool showNotifications = true;
    
    [Tooltip("Auto-scroll to bottom when new notification arrives")]
    public bool autoScrollToBottom = true;

    private List<NotificationEntry> notificationHistory = new List<NotificationEntry>();
    private Coroutine scrollCoroutine = null;
    private int lastNotificationCount = 0;
    
    private class NotificationEntry
    {
        public string message;
        public float timestamp;
        public Color color;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (notificationText == null)
        {
            Debug.LogError("[EventNotificationUI] notificationText is not assigned!");
        }
        
        // Try to find ScrollRect if not assigned
        if (scrollRect == null)
        {
            // First check if this GameObject has ScrollRect
            scrollRect = GetComponent<ScrollRect>();
            
            // Then check parent
            if (scrollRect == null)
            {
                scrollRect = GetComponentInParent<ScrollRect>();
            }
            
            // Finally check children
            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>();
            }
        }
        
        if (scrollRect != null)
        {
            Debug.Log($"[EventNotificationUI] ScrollRect found on: {scrollRect.gameObject.name}");
            Debug.Log($"[EventNotificationUI] This script is on: {gameObject.name}");
            Debug.Log($"[EventNotificationUI] Content assigned: {(scrollRect.content != null ? scrollRect.content.name : "NOT assigned")}");
            Debug.Log($"[EventNotificationUI] Viewport assigned: {(scrollRect.viewport != null ? scrollRect.viewport.name : "NOT assigned")}");
            Debug.Log($"[EventNotificationUI] Vertical scrollbar: {(scrollRect.verticalScrollbar != null ? scrollRect.verticalScrollbar.name : "NOT assigned")}");
            
            // Force enable vertical scrolling
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 20f;  // Increased sensitivity
            
            // Make scrollbar always visible if assigned
            if (scrollRect.verticalScrollbar != null)
            {
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scrollRect.verticalScrollbar.gameObject.SetActive(true);
                Debug.Log("[EventNotificationUI] Scrollbar set to PERMANENT visibility");
            }
            
            // Verify viewport has Image component (needed for raycasting)
            if (scrollRect.viewport != null)
            {
                Image viewportImage = scrollRect.viewport.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.raycastTarget = true;
                    Debug.Log("[EventNotificationUI] Viewport raycast enabled");
                }
                else
                {
                    Debug.LogWarning("[EventNotificationUI] Viewport missing Image component! Add one for mouse scrolling to work.");
                }
            }
        }
        
        UpdateDisplay();
    }

    void Update()
    {
        // Arrow key scrolling
        if (scrollRect != null && gameObject.activeInHierarchy)
        {
            float scrollAmount = 0.5f; // Scroll speed
            
            if (Input.GetKey(KeyCode.UpArrow))
            {
                // Scroll up (increase normalized position)
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollAmount * Time.deltaTime);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                // Scroll down (decrease normalized position)
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - scrollAmount * Time.deltaTime);
            }
        }
    }

    void LateUpdate()
    {
        // Continuously force scroll to bottom if new notifications arrived
        if (autoScrollToBottom && scrollRect != null && notificationHistory.Count > lastNotificationCount)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
            scrollRect.verticalNormalizedPosition = 0f;
            lastNotificationCount = notificationHistory.Count;
        }
    }

    /// <summary>
    /// Add a notification with default white color
    /// </summary>
    public void AddNotification(string message)
    {
        AddNotification(message, Color.white);
    }

    /// <summary>
    /// Add a notification with custom color
    /// </summary>
    public void AddNotification(string message, Color color)
    {
        if (!showNotifications) return;
        
        NotificationEntry entry = new NotificationEntry
        {
            message = message,
            timestamp = Time.time,
            color = color
        };
        
        notificationHistory.Add(entry);
        
        // Limit history size if needed
        if (maxHistorySize > 0 && notificationHistory.Count > maxHistorySize)
        {
            notificationHistory.RemoveAt(0);
        }
        
        UpdateDisplay();
        
        // Auto-scroll to bottom to show newest notification
        // Only use coroutine if GameObject is active, otherwise set directly
        if (autoScrollToBottom && scrollRect != null)
        {
            if (gameObject.activeInHierarchy)
            {
                // Stop any existing scroll coroutine to prevent conflicts
                if (scrollCoroutine != null)
                {
                    StopCoroutine(scrollCoroutine);
                }
                scrollCoroutine = StartCoroutine(ScrollToBottomDelayed());
            }
            else
            {
                // GameObject is inactive, set scroll position directly
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
    
    /// <summary>
    /// Scroll to bottom after UI updates (needs delay for proper layout calculation)
    /// Uses realtime waiting so it works even when Time.timeScale = 0
    /// </summary>
    System.Collections.IEnumerator ScrollToBottomDelayed()
    {
        if (scrollRect == null) yield break;
        
        // Immediate scroll
        scrollRect.verticalNormalizedPosition = 0f;
        
        // Wait for next frame
        yield return null;
        
        // Force rebuild and scroll
        Canvas.ForceUpdateCanvases();
        if (scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }
        scrollRect.verticalNormalizedPosition = 0f;
        
        // Wait using realtime (works when paused)
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Final attempt
        Canvas.ForceUpdateCanvases();
        if (scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            
            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0f;
            Debug.Log($"[EventNotificationUI] Final scroll - Content: {contentHeight}, Viewport: {viewportHeight}");
        }
        scrollRect.verticalNormalizedPosition = 0f;
        
        Debug.Log($"[EventNotificationUI] Scroll complete. Position: {scrollRect.verticalNormalizedPosition}");
    }

    /// <summary>
    /// Predefined notification for animal eating grass
    /// </summary>
    public void NotifyAnimalEat(string animalName, string grassName)
    {
        AddNotification($"[EAT] {animalName} ate {grassName}", Color.black);
    }

    /// <summary>
    /// Predefined notification for animal starving
    /// </summary>
    public void NotifyAnimalStarving(string animalName)
    {
        AddNotification($"[STARVING] {animalName} is starving!", Color.black);
    }

    /// <summary>
    /// Predefined notification for animal death
    /// </summary>
    public void NotifyAnimalDeath(string animalName)
    {
        AddNotification($"[DEATH] {animalName} died", Color.black);
    }

    /// <summary>
    /// Predefined notification for grass depleted
    /// </summary>
    public void NotifyGrassDepleted(string grassName)
    {
        AddNotification($"[CONSUMED] {grassName} was eaten", Color.black);
    }

    /// <summary>
    /// Predefined notification for grass growing
    /// </summary>
    public void NotifyGrassGrow(string grassName)
    {
        AddNotification($"[GROW] {grassName} grew", Color.black);
    }
    
    /// <summary>
    /// Predefined notification for human hunting animal
    /// </summary>
    public void NotifyHumanHunt(string humanName, string animalName)
    {
        AddNotification($"[HUNT] {humanName} hunted {animalName}", Color.black);
    }
    
    /// <summary>
    /// Predefined notification for human starving
    /// </summary>
    public void NotifyHumanStarving(string humanName)
    {
        AddNotification($"[STARVING] {humanName} is starving!", Color.black);
    }
    
    /// <summary>
    /// Predefined notification for human death
    /// </summary>
    public void NotifyHumanDeath(string humanName)
    {
        AddNotification($"[DEATH] {humanName} died", Color.black);
    }
    
    /// <summary>
    /// Predefined notification for animal birth
    /// </summary>
    public void NotifyAnimalBirth(string babyName, string parent1Name, string parent2Name)
    {
        AddNotification($"[BIRTH] {babyName} born from {parent1Name} & {parent2Name}", Color.black);
    }

    void UpdateDisplay()
    {
        if (notificationText == null) return;
        
        if (notificationHistory.Count == 0)
        {
            notificationText.text = "";
            return;
        }
        
        // Build notification text from all history (oldest at top, newest at bottom)
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        foreach (var entry in notificationHistory)
        {
            // Show all notifications in black
            sb.AppendLine(entry.message);
        }
        
        notificationText.text = sb.ToString();
        
        // Don't scroll here - let AddNotification handle it to avoid double-scrolling
    }

    /// <summary>
    /// Clear all notifications
    /// </summary>
    public void ClearNotifications()
    {
        notificationHistory.Clear();
        UpdateDisplay();
    }
    
    /// <summary>
    /// Get total notification count in history
    /// </summary>
    public int GetNotificationCount()
    {
        return notificationHistory.Count;
    }
}
