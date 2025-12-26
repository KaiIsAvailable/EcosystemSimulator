using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Controls panel visibility based on button hover and click interactions.
/// Hover shows panel temporarily, click locks it visible.
/// Help panel is draggable.
/// </summary>
public class PanelToggleController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Atmosphere Button & Panel")]
    public Button atmosphereBtn;
    public GameObject atmospherePanel;
    
    [Header("Population Button & Panel")]
    public Button populationBtn;
    public GameObject populationPanel;
    
    [Header("Gas Flow Button & Panel")]
    public Button gasflowBtn;
    public GameObject gasflowPanel;
    
    [Header("Control Button & Panel")]
    public Button controlBtn;
    public GameObject controlPanel;
    
    [Header("Log Button & Notification Panel")]
    public Button logBtn;
    public GameObject notificationPanel;
    
    [Header("Help Button & Draggable Panel")]
    public Button helpBtn;
    public GameObject helpPanel;
    
    [Header("Breakdown Panel (shows on GasFlow hover)")]
    public GameObject breakdownPanel;
    
    // Track which panels are locked (clicked to stay visible)
    private bool atmosphereLocked = false;
    private bool populationLocked = false;
    private bool gasflowLocked = false;
    private bool controlLocked = false;
    private bool helpLocked = false;
    
    // Track notification panel position state
    private bool notificationPanelExtended = false;
    private float normalPanelTop = 0f;
    private bool normalTopSaved = false;
    
    // Help panel dragging
    private RectTransform helpPanelRect;
    private Vector2 dragOffset;
    private bool isDraggingHelp = false;
    
    void Start()
    {
        Debug.Log("[PanelToggleController] Start() called on GameObject: " + gameObject.name);
        
        // Hide all panels initially (only the specific panel GameObjects)
        if (atmospherePanel != null) 
        {
            Debug.Log("[PanelToggleController] Hiding: " + atmospherePanel.name);
            atmospherePanel.SetActive(false);
        }
        
        if (populationPanel != null) 
        {
            Debug.Log("[PanelToggleController] Hiding: " + populationPanel.name);
            populationPanel.SetActive(false);
        }
        
        if (gasflowPanel != null) 
        {
            Debug.Log("[PanelToggleController] Hiding: " + gasflowPanel.name);
            gasflowPanel.SetActive(false);
        }
        
        if (controlPanel != null) 
        {
            Debug.Log("[PanelToggleController] Hiding: " + controlPanel.name);
            controlPanel.SetActive(false);
        }
        
        if (breakdownPanel != null)
        {
            Debug.Log("[PanelToggleController] Hiding: " + breakdownPanel.name);
            breakdownPanel.SetActive(false);
        }
        
        if (helpPanel != null)
        {
            Debug.Log("[PanelToggleController] Hiding: " + helpPanel.name);
            helpPanel.SetActive(false);
            
            // Get RectTransform for dragging
            helpPanelRect = helpPanel.GetComponent<RectTransform>();
            if (helpPanelRect == null)
            {
                Debug.LogError("[PanelToggleController] Help panel must have a RectTransform!");
            }
        }
        
        // Setup atmosphere button
        SetupButton(atmosphereBtn, atmospherePanel, 
                    () => atmosphereLocked, 
                    (locked) => atmosphereLocked = locked);
        
        // Setup population button
        SetupButton(populationBtn, populationPanel, 
                    () => populationLocked, 
                    (locked) => populationLocked = locked);
        
        // Setup gas flow button
        SetupButton(gasflowBtn, gasflowPanel, 
                    () => gasflowLocked, 
                    (locked) => gasflowLocked = locked);
        
        // Setup control button
        SetupButton(controlBtn, controlPanel, 
                    () => controlLocked, 
                    (locked) => controlLocked = locked);
        
        // Setup help button (draggable panel)
        SetupButton(helpBtn, helpPanel, 
                    () => helpLocked, 
                    (locked) => helpLocked = locked);
        
        // Setup log button (notification panel) with hover effect
        if (logBtn != null && notificationPanel != null)
        {
            logBtn.onClick.AddListener(ToggleNotificationPanel);
            SetupButtonHoverEffect(logBtn);
        }
        
        // Setup help button hover effect (already has SetupButton, just add hover)
        if (helpBtn != null)
        {
            SetupButtonHoverEffect(helpBtn);
        }
        
        Debug.Log("[PanelToggleController] Setup complete!");
    }
    
    /// <summary>
    /// Sets up hover and click events for a button-panel pair
    /// </summary>
    void SetupButton(Button btn, GameObject panel, System.Func<bool> isLocked, System.Action<bool> setLocked)
    {
        if (btn == null || panel == null) return;
        
        // Add EventTrigger component if not present
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btn.gameObject.AddComponent<EventTrigger>();
        }
        
        // Hover Enter - Show panel
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { 
            OnPointerEnter(panel, isLocked);
            SetButtonAlpha(btn, 1f); // Set alpha to 1 on hover
        });
        trigger.triggers.Add(pointerEnter);
        
        // Hover Exit - Hide panel if not locked
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { 
            OnPointerExit(panel, isLocked);
            // Only reset alpha if not locked
            if (!isLocked())
            {
                SetButtonAlpha(btn, 0.5f); // Reset alpha when not hovering
            }
        });
        trigger.triggers.Add(pointerExit);
        
        // Click - Toggle lock state
        btn.onClick.AddListener(() => { 
            OnButtonClick(panel, isLocked, setLocked);
            // Keep alpha at 1 when locked, reset to 0.5 when unlocked
            SetButtonAlpha(btn, isLocked() ? 1f : 0.5f);
        });
    }
    
    /// <summary>
    /// Called when mouse enters button area
    /// </summary>
    void OnPointerEnter(GameObject panel, System.Func<bool> isLocked)
    {
        if (panel == null) return;
        
        // Always show panel on hover
        panel.SetActive(true);
    }
    
    /// <summary>
    /// Called when mouse exits button area
    /// </summary>
    void OnPointerExit(GameObject panel, System.Func<bool> isLocked)
    {
        if (panel == null) return;
        
        // Hide panel only if not locked
        if (!isLocked())
        {
            panel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called when button is clicked
    /// </summary>
    void OnButtonClick(GameObject panel, System.Func<bool> isLocked, System.Action<bool> setLocked)
    {
        if (panel == null) return;
        
        // Toggle lock state
        bool newLockedState = !isLocked();
        setLocked(newLockedState);
        
        // Update panel visibility based on lock state
        panel.SetActive(newLockedState);
    }
    
    /// <summary>
    /// Sets button alpha to full opacity (1)
    /// </summary>
    void SetButtonAlpha(Button btn, float alpha)
    {
        if (btn == null) return;
        
        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
        {
            Color color = btnImage.color;
            color.a = alpha;
            btnImage.color = color;
        }
    }
    
    /// <summary>
    /// Adds hover effect to buttons that don't have panel toggle behavior
    /// </summary>
    void SetupButtonHoverEffect(Button btn)
    {
        if (btn == null) return;
        
        // Add EventTrigger component if not present
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btn.gameObject.AddComponent<EventTrigger>();
        }
        
        // Hover Enter - Set alpha to 1
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { 
            SetButtonAlpha(btn, 1f);
        });
        trigger.triggers.Add(pointerEnter);
        
        // Hover Exit - Reset alpha to 0.5
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { 
            SetButtonAlpha(btn, 0.5f);
        });
        trigger.triggers.Add(pointerExit);
    }
    
    /// <summary>
    /// Toggles notification panel between normal position and top=500
    /// </summary>
    void ToggleNotificationPanel()
    {
        if (notificationPanel == null) return;
        
        RectTransform rectTransform = notificationPanel.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Save normal top position on first click
        if (!normalTopSaved)
        {
            normalPanelTop = rectTransform.offsetMax.y;
            normalTopSaved = true;
        }
        
        // Toggle between -500 (top=500) and normal top position
        notificationPanelExtended = !notificationPanelExtended;
        
        Vector2 offsetMax = rectTransform.offsetMax;
        offsetMax.y = notificationPanelExtended ? -500f : normalPanelTop;
        rectTransform.offsetMax = offsetMax;
    }
    
    void Update()
    {
        // Check if mouse is over the gasflow panel
        if (gasflowPanel != null && breakdownPanel != null && gasflowPanel.activeSelf)
        {
            // Check if mouse is hovering over the gasflow panel
            if (RectTransformUtility.RectangleContainsScreenPoint(
                gasflowPanel.GetComponent<RectTransform>(), 
                Input.mousePosition, 
                null))
            {
                breakdownPanel.SetActive(true);
            }
            else
            {
                breakdownPanel.SetActive(false);
            }
        }
        else if (breakdownPanel != null)
        {
            // Hide breakdown panel if gasflow panel is not active
            breakdownPanel.SetActive(false);
        }
    }
    
    // Drag handlers for help panel
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (helpPanel == null || !helpPanel.activeSelf || helpPanelRect == null) return;
        
        // Check if drag started on help panel
        if (RectTransformUtility.RectangleContainsScreenPoint(helpPanelRect, eventData.position, eventData.pressEventCamera))
        {
            isDraggingHelp = true;
            
            // Calculate offset between mouse and panel position
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                helpPanelRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                dragOffset = helpPanelRect.anchoredPosition - localPointerPosition;
            }
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingHelp || helpPanelRect == null) return;
        
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            helpPanelRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition))
        {
            helpPanelRect.anchoredPosition = localPointerPosition + dragOffset;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDraggingHelp = false;
    }
}
