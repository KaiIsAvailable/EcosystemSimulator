using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically creates tooltip UI on startup if it doesn't exist
/// Attach this to any GameObject in your scene (or create empty GameObject)
/// </summary>
public class TooltipSetup : MonoBehaviour
{
    [Header("Auto-Setup Settings")]
    [Tooltip("Run setup automatically on Start")]
    public bool autoSetup = true;
    
    [Tooltip("Tooltip text font size")]
    public int fontSize = 30;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupTooltipUI();
        }
    }
    
    [ContextMenu("Setup Tooltip UI")]
    public void SetupTooltipUI()
    {
        // Check if already exists
        if (FindAnyObjectByType<EntityTooltip>() != null)
        {
            Debug.Log("[TooltipSetup] EntityTooltip already exists!");
            return;
        }
        
        Debug.Log("[TooltipSetup] Creating tooltip UI system...");
        
        // Create Canvas
        GameObject canvasObj = new GameObject("TooltipCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Render on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Panel (background)
        GameObject panelObj = new GameObject("TooltipPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.sizeDelta = new Vector2(400, 200); // Increased size for larger font
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f); // Semi-transparent black
        
        // Create Text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        
        // Add EntityTooltip script
        EntityTooltip tooltip = canvasObj.AddComponent<EntityTooltip>();
        tooltip.tooltipText = text;
        tooltip.tooltipPanel = panelObj;
        tooltip.tooltipOffset = new Vector2(0, 0); // No offset - stays at bottom-left
        tooltip.hoverDistance = 0.8f; // Larger detection radius
        tooltip.fixedPosition = true; // Keep at bottom-left corner
        
        // Start hidden
        panelObj.SetActive(false);
        
        Debug.Log("[TooltipSetup] ✅ Tooltip UI created successfully!");
        Debug.Log("[TooltipSetup] Next: Add Circle Collider 2D to entity prefabs (see console)");
        Debug.Log("[TooltipSetup] Tree prefab: Add CircleCollider2D, radius=0.3, isTrigger=true");
        Debug.Log("[TooltipSetup] Grass prefab: Add CircleCollider2D, radius=0.2, isTrigger=true");
        Debug.Log("[TooltipSetup] Animal prefab: Add CircleCollider2D, radius=0.3, isTrigger=true");
        Debug.Log("[TooltipSetup] Human prefab: Add CircleCollider2D, radius=0.3, isTrigger=true");
    }
}
