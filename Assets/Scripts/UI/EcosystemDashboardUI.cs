using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Comprehensive ecosystem dashboard with visual representations:
/// - Population icon grids
/// - Gas flow arrows  
/// - Atmosphere composition
/// - Environmental gauges
/// - Time & health status
/// </summary>
public class EcosystemDashboardUI : MonoBehaviour
{
    [Header("References")]
    public AtmosphereManager atmosphere;
    public SunMoonController sunMoon;
    public Button toggleButton;
    public GameObject panelToToggle;  // The BarChartPanel GameObject to hide/show
    
    [Header("Auto Layout")]
    public RectTransform container;
    public float updateInterval = 0.5f;
    
    [Header("Dashboard Sections")]
    private Text headerText;
    private Text populationText;
    private Text gasFlowText;
    private Text atmosphereText;
    private Text environmentText;
    
    private float updateTimer = 0f;
    private bool isInitialized = false;
    private bool isVisible = false;  // Start hidden
    
    void Start()
    {
        // Check for duplicates
        EcosystemDashboardUI[] instances = FindObjectsOfType<EcosystemDashboardUI>();
        if (instances.Length > 1)
        {
            if (instances[0] != this)
            {
                Debug.LogError($"[Dashboard] Destroying duplicate on {gameObject.name}");
                Destroy(this);
                return;
            }
        }
        
        if (isInitialized) return;
        
        if (atmosphere == null)
            atmosphere = AtmosphereManager.Instance;
        
        if (sunMoon == null)
            sunMoon = FindAnyObjectByType<SunMoonController>();
        
        if (container == null)
            container = GetComponent<RectTransform>();
        
        if (panelToToggle == null)
            panelToToggle = container.gameObject;
        
        CreateDashboard();
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleDashboard);
        }
        
        // Hide dashboard on start
        if (panelToToggle != null)
        {
            panelToToggle.SetActive(false);
        }
        
        isInitialized = true;
        Debug.Log("[Dashboard] Initialization complete! Press E to show dashboard.");
    }
    
    void Update()
    {
        if (!atmosphere) return;
        
        // Toggle with 'E' key
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleDashboard();
        }
        
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateDashboard();
        }
    }
    
    void CreateDashboard()
    {
        if (container == null) return;
        
        float yPos = 0f;
        float sectionWidth = 0f; // Will be calculated
        
        // ═══════════════════════════════════════════════════════════
        // HEADER SECTION (Full Width)
        // ═══════════════════════════════════════════════════════════
        headerText = CreateTextSection("Header", 0, yPos, 1f, 40f);
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.fontSize = 16;
        headerText.fontStyle = FontStyle.Bold;
        yPos -= 45f;
        
        // ═══════════════════════════════════════════════════════════
        // TOP ROW: POPULATION (Left) | GAS FLOW (Right)
        // ═══════════════════════════════════════════════════════════
        populationText = CreateTextSection("Population", 0, yPos, 0.5f, 180f);
        populationText.alignment = TextAnchor.UpperLeft;
        populationText.fontSize = 14;
        
        gasFlowText = CreateTextSection("GasFlow", 0.5f, yPos, 0.5f, 180f);
        gasFlowText.alignment = TextAnchor.UpperLeft;
        gasFlowText.fontSize = 14;
        yPos -= 185f;
        
        // ═══════════════════════════════════════════════════════════
        // BOTTOM ROW: ATMOSPHERE (Left) | ENVIRONMENT (Right)
        // ═══════════════════════════════════════════════════════════
        atmosphereText = CreateTextSection("Atmosphere", 0, yPos, 0.5f, 120f);
        atmosphereText.alignment = TextAnchor.UpperLeft;
        atmosphereText.fontSize = 12;
        
        environmentText = CreateTextSection("Environment", 0.5f, yPos, 0.5f, 120f);
        environmentText.alignment = TextAnchor.UpperLeft;
        environmentText.fontSize = 12;
        yPos -= 125f;
        
        // Adjust container size
        if (container != null)
        {
            float totalHeight = Mathf.Abs(yPos) + 20f;
            container.sizeDelta = new Vector2(container.sizeDelta.x, totalHeight);
        }
    }
    
    Text CreateTextSection(string name, float xStart, float yPos, float widthPercent, float height)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(container, false);
        
        RectTransform rect = section.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(xStart, 1);
        rect.anchorMax = new Vector2(xStart + widthPercent, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, yPos - 5); // Add padding
        rect.sizeDelta = new Vector2(-20, height); // Subtract padding from width
        
        Text text = section.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = 14;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.black;
        text.supportRichText = true;
        text.resizeTextForBestFit = false;
        
        // Optional: Add subtle shadow for readability (remove if still blurry)
        // Shadow shadow = section.AddComponent<Shadow>();
        // shadow.effectColor = new Color(0, 0, 0, 0.5f);
        // shadow.effectDistance = new Vector2(1, -1);
        
        return text;
    }
    
    void UpdateDashboard()
    {
        if (!atmosphere || !sunMoon) return;
        
        // Get data
        atmosphere.GetEcosystemStatsWithPlantAgents(
            out int trees, out int grass, out int animals, out int humans,
            out float totalO2, out float totalCO2,
            out float plantPhotoO2, out float plantRespO2,
            out float animalO2, out float animalCO2,
            out float humanO2, out float humanCO2,
            out float oceanCO2);
        
        // Convert to mol/day
        float photoO2Day = plantPhotoO2 * atmosphere.secondsPerDay;
        float respO2Day = plantRespO2 * atmosphere.secondsPerDay;
        float animalO2Day = animalO2 * atmosphere.secondsPerDay;
        float animalCO2Day = animalCO2 * atmosphere.secondsPerDay;
        float humanO2Day = humanO2 * atmosphere.secondsPerDay;
        float humanCO2Day = humanCO2 * atmosphere.secondsPerDay;
        float oceanCO2Day = oceanCO2 * atmosphere.secondsPerDay;
        
        float netO2 = photoO2Day + respO2Day + animalO2Day + humanO2Day;
        float netCO2 = -photoO2Day - respO2Day + animalCO2Day + humanCO2Day + oceanCO2Day;
        
        // ═══════════════════════════════════════════════════════════
        // HEADER SECTION
        // ═══════════════════════════════════════════════════════════
        string timeIcon = sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night ? "🌙" : "☀️";
        string statusIcon = atmosphere.environmentalStatus == AtmosphereManager.EnvironmentalStatus.Healthy ? "🟢" : "⚠️";
        string statusText = atmosphere.environmentalStatus.ToString().ToUpper();
        
        headerText.text = $"DAY {sunMoon.day} | {sunMoon.hours:00}:{sunMoon.minutes:00} {timeIcon} | {statusIcon} {statusText}";
        
        // ═══════════════════════════════════════════════════════════
        // POPULATION SECTION (Left Column)
        // ═══════════════════════════════════════════════════════════
        populationText.text = $"<b>POPULATION</b>\n\n" +
                              $"TREE ({trees})\n\n" +
                              $"GRASS ({grass})\n\n" +
                              $"ANIMAL ({animals})\n\n" +
                              $"HUMAN ({humans})";
        
        // ═══════════════════════════════════════════════════════════
        // GAS FLOW SECTION (Right Column)
        // ═══════════════════════════════════════════════════════════
        string o2Arrow = netO2 > 0 ? "↑" : "↓";
        string co2Arrow = netCO2 > 0 ? "↑" : "↓";
        
        // Create mini bar chart for gas flow breakdown
        float maxRate = 30f; // Max mol/day for scale
        string photoBar = CreateMiniBar(photoO2Day, maxRate, 10);
        string respBar = CreateMiniBar(Mathf.Abs(respO2Day), maxRate, 10);
        string animalBar = CreateMiniBar(Mathf.Abs(animalO2Day), maxRate, 10);
        string humanBar = CreateMiniBar(Mathf.Abs(humanO2Day), maxRate, 10);
        string oceanBar = CreateMiniBar(Mathf.Abs(oceanCO2Day), maxRate, 10);
        
        gasFlowText.text = $"<b>GAS FLOW</b>\n\n" +
                           $"{o2Arrow} {netO2:+0.00;-0.00} O₂\n" +
                           $"{co2Arrow} {netCO2:+0.00;-0.00} CO₂\n\n" +
                           $"<size=10><b>Flow Breakdown:</b>\n" +
                           $"Photo {photoBar} {photoO2Day:+0.0}\n" +
                           $"Resp  {respBar} {respO2Day:+0.0}\n" +
                           $"Animal {animalBar} {animalO2Day:+0.0}\n" +
                           $"Human  {humanBar} {humanO2Day:+0.0}\n" +
                           $"Ocean  {oceanBar} {oceanCO2Day:+0.0}</size>";
        
        // ═══════════════════════════════════════════════════════════
        // ATMOSPHERE SECTION (Bottom Left)
        // ═══════════════════════════════════════════════════════════
        // Create simple text-based pie chart visualization
        string pieChart = CreateTextPieChart(
            atmosphere.nitrogen,
            atmosphere.oxygen,
            atmosphere.argon,
            atmosphere.carbonDioxide,
            atmosphere.waterVapor
        );
        
        atmosphereText.text = $"<b>ATMOSPHERE</b>\n\n" +
                              $"{pieChart}\n\n" +
                              $"<size=10>⚪ N₂: {atmosphere.nitrogen:F1}%\n" +
                              $"🔵 O₂: {atmosphere.oxygen:F1}%\n" +
                              $"🟣 Ar: {atmosphere.argon:F2}%\n" +
                              $"⚫ CO₂: {atmosphere.carbonDioxide:F3}%\n" +
                              $"💧 H₂O: {atmosphere.waterVapor:F2}%</size>\n\n" +
                              $"Total: {atmosphere.totalAtmosphereMoles:F0} mol";
        
        // ═══════════════════════════════════════════════════════════
        // ENVIRONMENT SECTION (Bottom Right)
        // ═══════════════════════════════════════════════════════════
        float temperature = sunMoon.currentTemperature;
        float oceanSink = Mathf.Abs(oceanCO2Day);
        float respirationBaseline = sunMoon.GetRespirationMultiplier();
        
        environmentText.text = $"<b>ENVIRONMENT</b>\n\n" +
                               $"🌡️ {temperature:F1}°C\n\n" +
                               $"🌊 {oceanSink:F2} ocean sink\n\n" +
                               $"📊 {respirationBaseline:F2}× baseline";
    }
    
    string CreateTextPieChart(float n2, float o2, float ar, float co2, float h2o)
    {
        // Simple 3-line pie chart using blocks
        // N₂ is largest (78%), O₂ is second (21%), others are tiny
        
        int totalChars = 20;
        int n2Chars = Mathf.RoundToInt((n2 / 100f) * totalChars);
        int o2Chars = Mathf.RoundToInt((o2 / 100f) * totalChars);
        int arChars = Mathf.Max(1, Mathf.RoundToInt((ar / 100f) * totalChars));
        int co2Chars = Mathf.Max(1, Mathf.RoundToInt((co2 / 100f) * totalChars));
        int h2oChars = totalChars - n2Chars - o2Chars - arChars - co2Chars;
        h2oChars = Mathf.Max(0, h2oChars);
        
        // Build visual bar
        string bar = "";
        bar += new string('█', n2Chars);  // N₂ (white/gray)
        bar += new string('█', o2Chars);  // O₂ (blue)
        bar += new string('▓', arChars);  // Ar (purple)
        bar += new string('▒', co2Chars); // CO₂ (black)
        bar += new string('░', h2oChars); // H₂O (light)
        
        return $"[{bar}]";
    }
    
    string CreateMiniBar(float value, float maxValue, int barLength)
    {
        int filled = Mathf.Clamp(Mathf.RoundToInt((Mathf.Abs(value) / maxValue) * barLength), 0, barLength);
        int empty = barLength - filled;
        return "[" + new string('█', filled) + new string('·', empty) + "]";
    }
    
    string GetIconGrid(string icon, int count, int maxIcons)
    {
        if (count == 0) return "";
        if (count <= maxIcons)
        {
            string result = "";
            for (int i = 0; i < count; i++)
                result += icon;
            return result;
        }
        else
        {
            // Show limited icons + number
            string result = "";
            for (int i = 0; i < maxIcons; i++)
                result += icon;
            return result + $"...";
        }
    }
    
    Font GetDefaultFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch { }
        
        if (font == null)
            font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        
        if (font == null)
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            if (fonts.Length > 0)
                font = fonts[0];
        }
        
        return font;
    }
    
    public void ToggleDashboard()
    {
        isVisible = !isVisible;
        
        // Toggle the panel GameObject
        if (panelToToggle != null)
        {
            panelToToggle.SetActive(isVisible);
        }
        
        // Change button color
        if (toggleButton != null)
        {
            ColorBlock colors = toggleButton.colors;
            colors.normalColor = isVisible ? Color.white : new Color(0.5f, 0.8f, 1f);
            toggleButton.colors = colors;
        }
        
        Debug.Log($"[Dashboard] {(isVisible ? "Shown" : "Hidden")}");
    }
}
