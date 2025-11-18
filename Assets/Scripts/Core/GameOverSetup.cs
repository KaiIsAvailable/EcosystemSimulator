using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically creates Game Over UI on startup if it doesn't exist
/// Attach to same GameObject as GameOverManager or create new empty GameObject
/// </summary>
public class GameOverSetup : MonoBehaviour
{
    [Header("Auto-Setup Settings")]
    [Tooltip("Run setup automatically on Start")]
    public bool autoSetup = true;
    
    [Tooltip("Game Over title font size")]
    public int titleFontSize = 60;
    
    [Tooltip("Message text font size")]
    public int messageFontSize = 30;
    
    [Tooltip("Button text font size")]
    public int buttonFontSize = 24;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupGameOverUI();
        }
    }
    
    [ContextMenu("Setup Game Over UI")]
    public void SetupGameOverUI()
    {
        // Check if already exists
        GameOverManager existingManager = FindAnyObjectByType<GameOverManager>();
        if (existingManager != null && existingManager.gameOverPanel != null)
        {
            Debug.Log("[GameOverSetup] Game Over UI already exists!");
            return;
        }
        
        Debug.Log("[GameOverSetup] Creating Game Over UI system...");
        
        // Create Canvas
        GameObject canvasObj = new GameObject("GameOverCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Render on top of everything
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create full-screen overlay panel (semi-transparent black)
        GameObject overlayObj = new GameObject("OverlayPanel");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        
        Image overlayImage = overlayObj.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.4f); // Semi-transparent overlay (40% opacity)
        
        // Create Game Over Panel (center box)
        GameObject panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 400);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); // Semi-transparent dark gray
        
        // Create "GAME OVER" title text
        GameObject titleObj = new GameObject("GameOverTitle");
        titleObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.sizeDelta = Vector2.zero;
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "GAME OVER";
        titleText.fontSize = titleFontSize;
        titleText.color = Color.red;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        
        // Create message text (survival time, etc)
        GameObject messageObj = new GameObject("MessageText");
        messageObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform messageRect = messageObj.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.05f, 0.35f);
        messageRect.anchorMax = new Vector2(0.95f, 0.65f);
        messageRect.sizeDelta = Vector2.zero;
        
        Text messageText = messageObj.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.text = "The human has died!\nThe ecosystem simulation has ended.\n\nSurvival Time: 0m 0s";
        messageText.fontSize = messageFontSize;
        messageText.color = Color.white;
        messageText.alignment = TextAnchor.UpperCenter;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Create Restart Button
        GameObject restartBtnObj = new GameObject("RestartButton");
        restartBtnObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform restartRect = restartBtnObj.AddComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.1f, 0.15f);
        restartRect.anchorMax = new Vector2(0.45f, 0.3f);
        restartRect.sizeDelta = Vector2.zero;
        
        Image restartImage = restartBtnObj.AddComponent<Image>();
        restartImage.color = new Color(0.2f, 0.7f, 0.2f); // Green
        
        Button restartButton = restartBtnObj.AddComponent<Button>();
        restartButton.targetGraphic = restartImage;
        
        // Restart button text
        GameObject restartTextObj = new GameObject("Text");
        restartTextObj.transform.SetParent(restartBtnObj.transform, false);
        
        RectTransform restartTextRect = restartTextObj.AddComponent<RectTransform>();
        restartTextRect.anchorMin = Vector2.zero;
        restartTextRect.anchorMax = Vector2.one;
        restartTextRect.sizeDelta = Vector2.zero;
        
        Text restartText = restartTextObj.AddComponent<Text>();
        restartText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        restartText.text = "RESTART";
        restartText.fontSize = buttonFontSize;
        restartText.color = Color.white;
        restartText.alignment = TextAnchor.MiddleCenter;
        restartText.fontStyle = FontStyle.Bold;
        
        // Create Quit Button
        GameObject quitBtnObj = new GameObject("QuitButton");
        quitBtnObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform quitRect = quitBtnObj.AddComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.55f, 0.15f);
        quitRect.anchorMax = new Vector2(0.9f, 0.3f);
        quitRect.sizeDelta = Vector2.zero;
        
        Image quitImage = quitBtnObj.AddComponent<Image>();
        quitImage.color = new Color(0.7f, 0.2f, 0.2f); // Red
        
        Button quitButton = quitBtnObj.AddComponent<Button>();
        quitButton.targetGraphic = quitImage;
        
        // Quit button text
        GameObject quitTextObj = new GameObject("Text");
        quitTextObj.transform.SetParent(quitBtnObj.transform, false);
        
        RectTransform quitTextRect = quitTextObj.AddComponent<RectTransform>();
        quitTextRect.anchorMin = Vector2.zero;
        quitTextRect.anchorMax = Vector2.one;
        quitTextRect.sizeDelta = Vector2.zero;
        
        Text quitText = quitTextObj.AddComponent<Text>();
        quitText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quitText.text = "QUIT";
        quitText.fontSize = buttonFontSize;
        quitText.color = Color.white;
        quitText.alignment = TextAnchor.MiddleCenter;
        quitText.fontStyle = FontStyle.Bold;
        
        // Add GameOverManager script
        GameOverManager manager = canvasObj.AddComponent<GameOverManager>();
        manager.gameOverPanel = panelObj;
        manager.gameOverText = titleText;
        manager.survivalTimeText = messageText;
        manager.restartButton = restartButton;
        manager.quitButton = quitButton;
        
        // Wire up button events
        restartButton.onClick.AddListener(manager.RestartGame);
        quitButton.onClick.AddListener(manager.QuitGame);
        
        // Start hidden
        panelObj.SetActive(false);
        
        Debug.Log("[GameOverSetup] ✅ Game Over UI created successfully!");
        Debug.Log("[GameOverSetup] Will automatically detect when human dies");
    }
}
