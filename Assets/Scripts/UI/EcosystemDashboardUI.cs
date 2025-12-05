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
    public Button toggleButtonActivities;
    public GameObject panelToToggleActivities;
    public Button toggleButtonControls;
    public GameObject panelToToggleControls;
    
    [Header("Auto Layout")]
    public RectTransform container;
    public float updateInterval = 0.1f;  // Faster updates for smoother display
    
    [Header("Dashboard Sections")]
    private Text headerText;
    private Text populationText;
    private Text gasFlowText;
    private Text atmosphereText;
    private Text environmentText;
    
    private float updateTimer = 0f;
    private bool isInitialized = false;
    private bool isDashboardVisible = false;  // Track dashboard panel separately
    private bool isActivitiesVisible = true;  // Track activities panel separately
    private bool isControlsVisible = true;  // Track controls panel separately
    
    // Smoothing variables for smooth transitions
    private float smoothedPhotoO2 = 0f;
    private float smoothedRespO2 = 0f;
    private float smoothedAnimalO2 = 0f;
    private float smoothedHumanO2 = 0f;
    private float smoothedOceanCO2 = 0f;
    private float smoothedN2 = 0f;
    private float smoothedO2 = 0f;
    private float smoothedAr = 0f;
    private float smoothedCO2 = 0f;
    private float smoothedTemp = 0f;
    private float smoothedHours = 0f;
    private float smoothedMinutes = 0f;
    private float smoothingSpeed = 5f;  // Higher = faster response
    
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
        
        // Initialize visibility state based on actual panel state
        if (panelToToggle != null)
        {
            isDashboardVisible = panelToToggle.activeSelf;
            // Ensure panel starts hidden
            panelToToggle.SetActive(false);
            isDashboardVisible = false;
        }
        
        if (panelToToggleActivities != null)
        {
            // Ensure activities panel starts VISIBLE
            panelToToggleActivities.SetActive(true);
            isActivitiesVisible = true;
        }

        if (panelToToggleControls != null)
        {
            // Ensure controls panel starts VISIBLE
            panelToToggleControls.SetActive(true);
            isControlsVisible = true;
        }
        
        isInitialized = true;
        Debug.Log("[Dashboard] Initialization complete! Press E to show dashboard, R to show activities.");
    }
    
    void Update()
    {
        if (!atmosphere) return;
        
        // Toggle with 'E' key
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleDashboard();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleDashboardActivities();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleDashboardControls();
        }
        
        // Update every frame for smooth transitions (only when dashboard is visible)
        if (isDashboardVisible)
        {
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
        
        // Smooth all values to prevent jumping
        float deltaTime = Time.deltaTime;
        smoothedPhotoO2 = Mathf.Lerp(smoothedPhotoO2, photoO2Day, deltaTime * smoothingSpeed);
        smoothedRespO2 = Mathf.Lerp(smoothedRespO2, respO2Day, deltaTime * smoothingSpeed);
        smoothedAnimalO2 = Mathf.Lerp(smoothedAnimalO2, animalO2Day, deltaTime * smoothingSpeed);
        smoothedHumanO2 = Mathf.Lerp(smoothedHumanO2, humanO2Day, deltaTime * smoothingSpeed);
        smoothedOceanCO2 = Mathf.Lerp(smoothedOceanCO2, oceanCO2Day, deltaTime * smoothingSpeed);
        smoothedN2 = Mathf.Lerp(smoothedN2, atmosphere.nitrogen, deltaTime * smoothingSpeed);
        smoothedO2 = Mathf.Lerp(smoothedO2, atmosphere.oxygen, deltaTime * smoothingSpeed);
        smoothedAr = Mathf.Lerp(smoothedAr, atmosphere.argon, deltaTime * smoothingSpeed);
        smoothedCO2 = Mathf.Lerp(smoothedCO2, atmosphere.carbonDioxide, deltaTime * smoothingSpeed);
        smoothedTemp = Mathf.Lerp(smoothedTemp, sunMoon.currentTemperature, deltaTime * smoothingSpeed);
        
        float netO2 = smoothedPhotoO2 + smoothedRespO2 + smoothedAnimalO2 + smoothedHumanO2;
        float netCO2 = -smoothedPhotoO2 - smoothedRespO2 + animalCO2Day + humanCO2Day + smoothedOceanCO2;
        
        // ═══════════════════════════════════════════════════════════
        // HEADER SECTION
        // ═══════════════════════════════════════════════════════════
        string timeIcon = sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night ? "🌙" : "☀️";
        string statusIcon = atmosphere.environmentalStatus == AtmosphereManager.EnvironmentalStatus.Healthy ? "🟢" : "⚠️";
        string statusText = atmosphere.environmentalStatus.ToString().ToUpper();
        
        // Use actual time values for accuracy (no smoothing)
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
        // Use dynamic max based on actual values for realistic scaling
        float maxFlowRate = Mathf.Max(5f, 
            Mathf.Abs(smoothedPhotoO2), 
            Mathf.Abs(smoothedRespO2), 
            Mathf.Abs(smoothedAnimalO2), 
            Mathf.Abs(smoothedHumanO2), 
            Mathf.Abs(smoothedOceanCO2)) * 1.1f; // 10% margin
        
        string photoBar = CreateMiniBar(smoothedPhotoO2, maxFlowRate, 25);
        string respBar = CreateMiniBar(Mathf.Abs(smoothedRespO2), maxFlowRate, 25);
        string animalBar = CreateMiniBar(Mathf.Abs(smoothedAnimalO2), maxFlowRate, 25);
        string humanBar = CreateMiniBar(Mathf.Abs(smoothedHumanO2), maxFlowRate, 25);
        string oceanBar = CreateMiniBar(Mathf.Abs(smoothedOceanCO2), maxFlowRate, 25);
        
        gasFlowText.text = $"<b>GAS FLOW</b>\n\n" +
                           $"{o2Arrow} {netO2:+0.00;-0.00} O₂\n" +
                           $"{co2Arrow} {netCO2:+0.00;-0.00} CO₂\n\n" +
                           $"<size=10><b>Flow Breakdown:</b>\n" +
                           $"Photo {photoBar} {smoothedPhotoO2:+0.0}\n" +
                           $"Resp  {respBar} {smoothedRespO2:+0.0}\n" +
                           $"Animal {animalBar} {smoothedAnimalO2:+0.0}\n" +
                           $"Human  {humanBar} {smoothedHumanO2:+0.0}\n" +
                           $"Ocean  {oceanBar} {smoothedOceanCO2:+0.0}</size>";
        
        // ═══════════════════════════════════════════════════════════
        // ATMOSPHERE SECTION (Bottom Left)
        // ═══════════════════════════════════════════════════════════
        // Calculate moles for each gas
        float totalMoles = atmosphere.totalAtmosphereMoles;
        float n2Moles = totalMoles * (smoothedN2 / 100f);
        float o2Moles = totalMoles * (smoothedO2 / 100f);
        float arMoles = totalMoles * (smoothedAr / 100f);
        float co2Moles = totalMoles * (smoothedCO2 / 100f);
        
        // Create animated bars for each gas (based on percentage, not absolute moles)
        // This makes bars proportional to composition
        string n2Bar = CreateMiniBar(smoothedN2, 100f, 30);
        string o2Bar = CreateMiniBar(smoothedO2, 100f, 30);
        string arBar = CreateMiniBar(smoothedAr, 100f, 30);
        string co2Bar = CreateMiniBar(smoothedCO2, 100f, 30);
        
        atmosphereText.text = $"<b>ATMOSPHERE</b>\n\n" +
                              $"N₂ {n2Bar} {n2Moles:F2} ({smoothedN2:F3}%)\n" +
                              $"Ar {arBar} {arMoles:F2} ({smoothedAr:F3}%)\n" +
                              $"O₂ {o2Bar} {o2Moles:F2} ({smoothedO2:F3}%)\n" +
                              $"CO₂ {co2Bar} {co2Moles:F2} ({smoothedCO2:F3}%)\n\n" +
                              $"Total: {totalMoles:F2} mol";
        
        // ═══════════════════════════════════════════════════════════
        // ENVIRONMENT SECTION (Bottom Right)
        // ═══════════════════════════════════════════════════════════
        float oceanSink = Mathf.Abs(smoothedOceanCO2);
        float respirationBaseline = sunMoon.GetRespirationMultiplier();
        
        environmentText.text = $"<b>ENVIRONMENT</b>\n\n" +
                               $"{smoothedTemp:F1}°C\n\n" +
                               $"{oceanSink:F2} ocean sink\n\n" +
                               $"{respirationBaseline:F2}× baseline";
    }
    
    string CreateMiniBar(float value, float maxValue, int barLength)
    {
        float percentage = Mathf.Abs(value) / maxValue;
        int filled = Mathf.RoundToInt(percentage * barLength);
        
        // Ensure at least 1 block shows if value > 0
        if (value > 0 && filled == 0)
            filled = 1;
        
        filled = Mathf.Clamp(filled, 0, barLength);
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
        isDashboardVisible = !isDashboardVisible;
        
        // Toggle the panel GameObject
        if (panelToToggle != null)
        {
            panelToToggle.SetActive(isDashboardVisible);
        }
        
        // Change button color
        if (toggleButton != null)
        {
            ColorBlock colors = toggleButton.colors;
            colors.normalColor = isDashboardVisible ? Color.white : new Color(0.5f, 0.8f, 1f);
            toggleButton.colors = colors;
        }
        
        Debug.Log($"[Dashboard] {(isDashboardVisible ? "Shown" : "Hidden")}");
    }

    public void ToggleDashboardActivities()
    {
        isActivitiesVisible = !isActivitiesVisible;

        // Toggle panel only
        if (panelToToggleActivities != null)
        {
            panelToToggleActivities.SetActive(isActivitiesVisible);
        }

        // Move Button
        if (toggleButtonActivities != null)
        {
            // Change button color
            ColorBlock colors = toggleButtonActivities.colors;
            colors.normalColor = isActivitiesVisible ? Color.white : new Color(0.5f, 0.8f, 1f);
            toggleButtonActivities.colors = colors;
        }

        Debug.Log($"[Activities Log] {(isActivitiesVisible ? "Shown" : "Hidden")}");
    }

    public void ToggleDashboardControls()
    {
        isControlsVisible = !isControlsVisible;

        // Toggle panel only
        if (panelToToggleControls != null)
        {
            panelToToggleControls.SetActive(isControlsVisible);
        }

        // Move Button
        if (toggleButtonControls != null)
        {
            // Change button color
            ColorBlock colors = toggleButtonControls.colors;
            colors.normalColor = isControlsVisible ? Color.white : new Color(0.5f, 0.8f, 1f);
            toggleButtonControls.colors = colors;
        }

        Debug.Log($"[Controls Log] {(isControlsVisible ? "Shown" : "Hidden")}");
    }

}
