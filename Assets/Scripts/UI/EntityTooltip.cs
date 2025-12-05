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
    private GameObject hoveredEntity;
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
        
        // Debug: Check if everything is assigned
        Debug.Log($"[EntityTooltip] Initialized - tooltipText: {tooltipText != null}, tooltipPanel: {tooltipPanel != null}, mainCamera: {mainCamera != null}");
    }
    
    void Update()
    {
        // Skip tooltip detection if mouse is over UI elements (buttons, panels, etc.)
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            HideTooltip();
            return;
        }
        
        // Get mouse position in world space (with proper camera distance)
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z); // Distance from camera to world plane
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // Check if hovering over any entity
        Collider2D[] colliders = Physics2D.OverlapCircleAll(mouseWorldPos, hoverDistance);
        
        GameObject foundEntity = null;
        foreach (Collider2D col in colliders)
        {
            // Check for any living entity (BiomassEnergy, AnimalMetabolism, or HumanAgent)
            BiomassEnergy biomass = col.GetComponent<BiomassEnergy>();
            AnimalMetabolism animal = col.GetComponent<AnimalMetabolism>();
            
            if ((biomass != null && biomass.isAlive) || (animal != null && animal.isAlive))
            {
                foundEntity = col.gameObject;
                Debug.Log($"[Tooltip] Found entity: {col.gameObject.name}");
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
    
    void ShowTooltip(GameObject entity)
    {
        hoveredEntity = entity;
        
        // Enable tooltip
        if (tooltipPanel != null)
            tooltipPanel.SetActive(true);
        else if (tooltipText != null)
            tooltipText.enabled = true;
        
        // Update text based on entity type
        if (tooltipText != null)
        {
            string tooltipContent = GetTooltipContent(entity);
            tooltipText.text = tooltipContent;
            tooltipText.color = GetEntityColor(entity);
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
    
    string GetTooltipContent(GameObject entity)
    {
        string content = $"<b>{entity.name}</b>\n";
        
        // Check for AnimalMetabolism (new hunger system)
        AnimalMetabolism animal = entity.GetComponent<AnimalMetabolism>();
        if (animal != null && animal.isAlive)
        {
            content += "🐰 <b>HERBIVORE</b>\n";
            content += "━━━━━━━━━━━━━━━━\n";
            
            // Hunger (Gas Tank)
            float hungerPercent = (animal.hunger / animal.maxHunger) * 100f;
            string hungerBar = CreateMiniBar(animal.hunger, animal.maxHunger, 10);
            string hungerStatus = hungerPercent < 30f ? "🔴 STARVING" : hungerPercent < 60f ? "🟡 Hungry" : "🟢 Full";
            content += $"⛽ Hunger: {hungerBar}\n";
            content += $"   {animal.hunger:F1}/{animal.maxHunger:F0} ({hungerPercent:F0}%) {hungerStatus}\n\n";
            
            // Biomass (Car Frame)
            float biomassPercent = (animal.biomass / animal.maxBiomass) * 100f;
            string biomassBar = CreateMiniBar(animal.biomass, animal.maxBiomass, 10);
            content += $"🚗 Biomass: {biomassBar}\n";
            content += $"   {animal.biomass:F1}/{animal.maxBiomass:F0} kg ({biomassPercent:F0}%)\n\n";
            
            // Activity
            string activityIcon = GetActivityIcon(animal.currentActivity);
            content += $"💫 Activity: {activityIcon} {animal.currentActivity}\n\n";
            
            // Metabolism
            content += $"🫁 O₂ Usage: {animal.totalRespiration:F6} mol/s\n";
            
            // Temperature info
            SunMoonController sunMoon = FindAnyObjectByType<SunMoonController>();
            if (sunMoon != null)
            {
                content += $"🌡️ Temp: {sunMoon.currentTemperature:F1}°C";
            }
            
            return content;
        }
        
        // Check for BiomassEnergy (old system - plants, etc.)
        BiomassEnergy biomass = entity.GetComponent<BiomassEnergy>();
        if (biomass != null && biomass.isAlive)
        {
            string typeLabel = GetEntityTypeLabel(biomass.entityType);
            string statusInfo = biomass.GetStatusString();
            
            content += $"{typeLabel}\n{statusInfo}";
            return content;
        }
        
        return content + "Unknown Entity";
    }
    
    string CreateMiniBar(float value, float maxValue, int barLength)
    {
        float percentage = Mathf.Clamp01(value / maxValue);
        int filled = Mathf.RoundToInt(percentage * barLength);
        int empty = barLength - filled;
        return "[" + new string('█', filled) + new string('·', empty) + "]";
    }
    
    string GetActivityIcon(AnimalMetabolism.ActivityState activity)
    {
        switch (activity)
        {
            case AnimalMetabolism.ActivityState.Resting: return "😴";
            case AnimalMetabolism.ActivityState.Grazing: return "🍽️";
            case AnimalMetabolism.ActivityState.Walking: return "🚶";
            case AnimalMetabolism.ActivityState.Fleeing: return "🏃";
            default: return "❓";
        }
    }
    
    Color GetEntityColor(GameObject entity)
    {
        // Check AnimalMetabolism first
        AnimalMetabolism animal = entity.GetComponent<AnimalMetabolism>();
        if (animal != null)
        {
            float hungerPercent = animal.hunger / animal.maxHunger;
            
            if (hungerPercent > 0.6f)
                return Color.green;
            else if (hungerPercent > 0.3f)
                return Color.yellow;
            else
                return Color.red;
        }
        
        // Fall back to BiomassEnergy
        BiomassEnergy biomass = entity.GetComponent<BiomassEnergy>();
        if (biomass != null)
        {
            return biomass.GetHealthColor();
        }
        
        return Color.white;
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
