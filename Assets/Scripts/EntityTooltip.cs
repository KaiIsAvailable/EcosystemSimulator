using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows entity health/biomass details when hovering over them
/// Attach to a Canvas with a Text component for the tooltip display
/// </summary>
public class EntityTooltip : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Text component that displays tooltip info")]
    public Text tooltipText;
    
    [Tooltip("Optional: Background panel for the tooltip")]
    public GameObject tooltipPanel;
    
    [Header("Settings")]
    [Tooltip("Keep tooltip fixed at bottom-left corner instead of following cursor")]
    public bool fixedPosition = false;
    
    [Tooltip("Offset from mouse cursor (or from bottom-left if fixedPosition=true)")]
    public Vector2 tooltipOffset = new Vector2(20f, -20f);
    
    [Tooltip("How close to entity to show tooltip (world units)")]
    public float hoverDistance = 0.5f;
    
    private Camera mainCamera;
    private BiomassEnergy hoveredEntity;
    private RectTransform tooltipRect;
    private RectTransform panelRect;
    
    void Start()
    {
        mainCamera = Camera.main;
        tooltipRect = GetComponent<RectTransform>();
        
        // Get panel RectTransform
        if (tooltipPanel != null)
            panelRect = tooltipPanel.GetComponent<RectTransform>();
        
        // Start hidden
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        else if (tooltipText != null)
            tooltipText.enabled = false;
    }
    
    void Update()
    {
        // Get mouse position in world space (with proper camera distance)
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z); // Distance from camera to world plane
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // Check if hovering over any entity with BiomassEnergy
        Collider2D[] colliders = Physics2D.OverlapCircleAll(mouseWorldPos, hoverDistance);
        
        BiomassEnergy foundEntity = null;
        foreach (Collider2D col in colliders)
        {
            BiomassEnergy biomass = col.GetComponent<BiomassEnergy>();
            if (biomass != null && biomass.isAlive)
            {
                foundEntity = biomass;
                break;
            }
        }
        
        // Update tooltip
        if (foundEntity != null)
        {
            ShowTooltip(foundEntity);
        }
        else
        {
            HideTooltip();
        }
    }
    
    void ShowTooltip(BiomassEnergy entity)
    {
        hoveredEntity = entity;
        
        // Enable tooltip
        if (tooltipPanel != null)
            tooltipPanel.SetActive(true);
        else if (tooltipText != null)
            tooltipText.enabled = true;
        
        // Update text
        if (tooltipText != null)
        {
            string entityName = entity.gameObject.name;
            string typeLabel = GetEntityTypeLabel(entity.entityType);
            string statusInfo = entity.GetStatusString();
            
            tooltipText.text = $"<b>{entityName}</b>\n{typeLabel}\n{statusInfo}";
            tooltipText.color = entity.GetHealthColor();
        }
        
        // Position tooltip
        if (fixedPosition && panelRect != null)
        {
            // Keep at bottom-left corner with offset
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = tooltipOffset;
        }
        else
        {
            // Follow mouse cursor
            Vector2 mouseScreenPos = Input.mousePosition;
            if (panelRect != null)
                panelRect.position = mouseScreenPos + tooltipOffset;
            
            // Clamp to screen bounds
            ClampToScreen();
        }
    }
    
    void HideTooltip()
    {
        hoveredEntity = null;
        
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        else if (tooltipText != null)
            tooltipText.enabled = false;
    }
    
    void ClampToScreen()
    {
        if (panelRect == null) return;
        
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        
        Vector3 pos = panelRect.position;
        
        // Check if out of bounds and adjust
        if (corners[2].x > Screen.width)
            pos.x -= corners[2].x - Screen.width + 10;
        if (corners[0].x < 0)
            pos.x += Mathf.Abs(corners[0].x) + 10;
        if (corners[2].y > Screen.height)
            pos.y -= corners[2].y - Screen.height + 10;
        if (corners[0].y < 0)
            pos.y += Mathf.Abs(corners[0].y) + 10;
        
        panelRect.position = pos;
    }
    
    string GetEntityTypeLabel(BiomassEnergy.EntityType type)
    {
        switch (type)
        {
            case BiomassEnergy.EntityType.Plant:
                return "🌿 Plant";
            case BiomassEnergy.EntityType.Herbivore:
                return "🐰 Herbivore";
            case BiomassEnergy.EntityType.Carnivore:
                return "🧑 Carnivore";
            default:
                return "Unknown";
        }
    }
}
