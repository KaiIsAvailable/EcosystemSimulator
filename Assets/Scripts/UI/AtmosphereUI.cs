using UnityEngine;
using UnityEngine.UI;

public class AtmosphereUI : MonoBehaviour{
    [Header("Ecosystem Stats UI (Optional)")]
    public Text ecosystemStatsText;
    public Text titleText;
    
    [Header("Day & Time Display")]
    public Text dayTimeText;
    
    [Header("Status Display")]
    public Text statusText;
    
    [Header("Atmosphere Gas Displays")]
    public Text N2text;
    public Image N2bar;
    public Text Artext;
    public Image Arbar;
    public Text O2text;
    public Image O2bar;
    public Text CO2text;
    public Image CO2bar;
    
    [Header("Population Displays")]
    public Text treeText;
    public Text grassText;
    public Text animalText;
    public Text humanText;
    
    [Header("Gas Flow & Temperature Displays")]
    public Text o2FlowText;
    public Text co2FlowText;
    [Tooltip("O2 flow bar (grows up when positive, down when negative)")]
    public Image o2FlowBar;
    [Tooltip("CO2 flow bar (grows up when positive, down when negative)")]
    public Image co2FlowBar;
    public Text temperatureText;
    public Image hotTempImage;
    public Image coldTempImage;
    
    [Header("Breakdown & Environmental Factors")]
    public Text oceanSinkText;
    public Text baselineText;
    
    [Header("Breakdown Bars")]
    public Image photoO2Bar;
    public Image photoC02Bar;
    public Image respO2Bar;
    public Image respCO2Bar;
    public Image animalO2Bar;
    public Image animalCO2Bar;
    public Image humanO2Bar;
    public Image humanCO2Bar;
    public Image oceanO2Bar;
    public Image oceanCO2Bar;
    
    [Header("Breakdown Value Texts")]
    public Text photoO2Text;
    public Text photoCO2Text;
    public Text respO2Text;
    public Text respCO2Text;
    public Text animalO2Text;
    public Text animalCO2Text;
    public Text humanO2Text;
    public Text humanCO2Text;
    public Text oceanO2Text;
    public Text oceanCO2Text;
    
    [Header("Bar Scaling")]
    [Tooltip("Scale factor for breakdown bars: 1 mol/day = 0.01 pixels")]
    public float barScaleFactor = 0.01f;
    
    [Header("Environmental Status UI (Optional)")]
    public Text environmentalStatusText;

    [Header("Color Coding (Optional)")]
    public bool useColorCoding = true;
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [Header("Warning Thresholds")]
    [Tooltip("Oxygen below this % shows warning")]
    public float oxygenWarningThreshold = 18f;
    [Tooltip("Oxygen below this % shows danger")]
    public float oxygenDangerThreshold = 15f;
    
    [Tooltip("CO₂ above this % shows warning")]
    public float co2WarningThreshold = 0.1f;
    [Tooltip("CO₂ above this % shows danger")]
    public float co2DangerThreshold = 0.5f;

    private AtmosphereManager atmosphere;
    private SunMoonController sunMoon;
    private SunMoonController ctrl;

    void Start()
    {
        atmosphere = AtmosphereManager.Instance;
        
        if (!atmosphere)
        {
            Debug.LogError("[AtmosphereUI] No AtmosphereManager found in scene!");
        }

        sunMoon = FindAnyObjectByType<SunMoonController>();

        if (!sunMoon)
        {
            Debug.LogError("[AtmosphereUI] No SunMoonController found in scene!");
        }
    }

    void Awake()
    {
        ctrl = FindAnyObjectByType<SunMoonController>();
        if (ctrl != null)
        {
            Debug.Log("[AtmosphereUI] SunMoonController found in Awake!");
        }
        else
        {
            Debug.LogError("[AtmosphereUI] SunMoonController NOT found in Awake!");
        }
    }

    void Update()
    {
        // Update Day & Time Display (independent of main UI)
        if (dayTimeText != null && ctrl != null)
        {
            string newDayTime = $"DAY: {ctrl.day} | {ctrl.hours:00}:{ctrl.minutes:00}";
            dayTimeText.text = newDayTime;
            
            // Debug log every 5 seconds
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[AtmosphereUI] Updating dayTimeText: '{newDayTime}'");
            }
        }
        else if (dayTimeText == null && Time.frameCount % 300 == 0)
        {
            Debug.LogWarning("[AtmosphereUI] dayTimeText is not assigned in Inspector!");
        }
        else if (ctrl == null && Time.frameCount % 300 == 0)
        {
            Debug.LogWarning("[AtmosphereUI] SunMoonController (ctrl) not found!");
        }
        
        // Update Status Display (independent of main UI)
        if (statusText != null && atmosphere != null)
        {
            string newStatus = atmosphere.environmentalStatus.ToString().ToUpper();
            statusText.text = newStatus;
            
            // Debug log every 5 seconds
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[AtmosphereUI] Updating statusText: '{newStatus}'");
            }
            
            // Color based on status
            switch (atmosphere.environmentalStatus)
            {
                case AtmosphereManager.EnvironmentalStatus.Healthy:
                    statusText.color = Color.green;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Warning:
                    statusText.color = warningColor;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Danger:
                    statusText.color = dangerColor;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Critical:
                    statusText.color = Color.red;
                    break;
            }
        }
        else if (statusText == null && Time.frameCount % 300 == 0)
        {
            Debug.LogWarning("[AtmosphereUI] statusText is not assigned in Inspector!");
        }
        
        // Get population data once for all displays (to avoid duplicate variable declarations)
        int trees = 0, grass = 0, animals = 0, humans = 0;
        float totalO2_molPerSec = 0, totalCO2_molPerSec = 0;
        float plantPhotosynthesisO2 = 0, plantRespirationO2 = 0;
        float animalO2 = 0, animalCO2 = 0;
        float humanO2 = 0, humanCO2 = 0;
        float oceanCO2 = 0;
        
        // Calculate mol/day values once for all displays
        float totalO2_molPerDay = 0;
        float totalCO2_molPerDay = 0;
        
        // Breakdown values in mol/day (used by both breakdown UI and main panel)
        float plantPhotoO2_day = 0;
        float plantRespO2_day = 0;
        float animalO2_day = 0;
        float humanO2_day = 0;
        float animalCO2_day = 0;
        float humanCO2_day = 0;
        float oceanCO2_day = 0;
        
        // Arrow indicators (calculated once, used in multiple displays)
        string o2Arrow = "→";
        string co2Arrow = "→";
        
        if (atmosphere != null)
        {
            atmosphere.GetEcosystemStatsWithPlantAgents(out trees, out grass, out animals, out humans,
                                                         out totalO2_molPerSec, out totalCO2_molPerSec,
                                                         out plantPhotosynthesisO2, out plantRespirationO2,
                                                         out animalO2, out animalCO2,
                                                         out humanO2, out humanCO2,
                                                         out oceanCO2);
            
            // Convert mol/s to mol/day for all displays
            totalO2_molPerDay = totalO2_molPerSec * atmosphere.secondsPerDay;
            totalCO2_molPerDay = totalCO2_molPerSec * atmosphere.secondsPerDay;
            
            // Calculate breakdown values once
            plantPhotoO2_day = plantPhotosynthesisO2 * atmosphere.secondsPerDay;
            plantRespO2_day = plantRespirationO2 * atmosphere.secondsPerDay;
            animalO2_day = animalO2 * atmosphere.secondsPerDay;
            humanO2_day = humanO2 * atmosphere.secondsPerDay;
            animalCO2_day = animalCO2 * atmosphere.secondsPerDay;
            humanCO2_day = humanCO2 * atmosphere.secondsPerDay;
            oceanCO2_day = oceanCO2 * atmosphere.secondsPerDay;
            
            // Calculate arrows once for all displays
            o2Arrow = totalO2_molPerDay > 0 ? "↑" : (totalO2_molPerDay < 0 ? "↓" : "→");
            co2Arrow = totalCO2_molPerDay > 0 ? "↑" : (totalCO2_molPerDay < 0 ? "↓" : "→");
        }
        
        // Update Atmosphere Gas Displays (independent of main UI)
        if (atmosphere != null)
        {
            // Update N2 (Nitrogen)
            if (N2text != null)
            {
                N2text.text = $"N₂: {atmosphere.nitrogen:F4}% ({atmosphere.nitrogenMoles:F1} mol)";
            }
            if (N2bar != null)
            {
                RectTransform rt = N2bar.rectTransform;
                rt.offsetMax = new Vector2(-(670 - (670 * atmosphere.nitrogen / 100f)), rt.offsetMax.y);
            }
            
            // Update Ar (Argon)
            if (Artext != null)
            {
                Artext.text = $"Ar: {atmosphere.argon:F4}% ({atmosphere.argonMoles:F1} mol)";
            }
            if (Arbar != null)
            {
                RectTransform rt = Arbar.rectTransform;
                rt.offsetMax = new Vector2(-(670 - (670 * atmosphere.argon / 100f)), rt.offsetMax.y);
            }
            
            // Update O2 (Oxygen)
            if (O2text != null)
            {
                O2text.text = $"O₂: {atmosphere.oxygen:F4}% ({atmosphere.oxygenMoles:F1} mol)";
            }
            if (O2bar != null)
            {
                RectTransform rt = O2bar.rectTransform;
                rt.offsetMax = new Vector2(-(670 - (670 * atmosphere.oxygen / 100f)), rt.offsetMax.y);
            }
            
            // Update CO2 (Carbon Dioxide)
            if (CO2text != null)
            {
                CO2text.text = $"CO₂: {atmosphere.carbonDioxide:F4}% ({atmosphere.carbonDioxideMoles:F1} mol)";
            }
            if (CO2bar != null)
            {
                RectTransform rt = CO2bar.rectTransform;
                rt.offsetMax = new Vector2(-(670 - (670 * atmosphere.carbonDioxide / 100f)), rt.offsetMax.y);
            }
        }
        
        // Update Population Displays (independent of main UI)
        // Uses the population data already retrieved above
        if (treeText != null)
        {
            treeText.text = $"{trees}";
        }
        
        if (grassText != null)
        {
            grassText.text = $"{grass}";
        }
        
        if (animalText != null)
        {
            animalText.text = $"{animals}";
        }
        
        if (humanText != null)
        {
            humanText.text = $"{humans}";
        }
        
        // Update Gas Flow Displays (O2 and CO2 flow rates)
        if (atmosphere != null)
        {
            // Use already calculated mol/day values and arrows
            
            // Update O2 Flow Text
            if (o2FlowText != null)
            {
                o2FlowText.text = $"{totalO2_molPerDay:+0.00;-0.00} mol/day {o2Arrow}";
            }
            
            // Update CO2 Flow Text
            if (co2FlowText != null)
            {
                co2FlowText.text = $"{totalCO2_molPerDay:+0.00;-0.00} mol/day {co2Arrow}";
            }
            
            // Debug: Check if bars are assigned
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[AtmosphereUI] Bar Assignment Check: o2FlowBar={(o2FlowBar != null ? "ASSIGNED" : "NULL")}, co2FlowBar={(co2FlowBar != null ? "ASSIGNED" : "NULL")}");
                Debug.Log($"[AtmosphereUI] Flow Values: O2={totalO2_molPerDay:F2} mol/day, CO2={totalCO2_molPerDay:F2} mol/day");
            }
            
            // Update O2 Flow Bar (grows up when positive, down when negative)
            UpdateFlowBar(o2FlowBar, totalO2_molPerDay);
            
            // Update CO2 Flow Bar (grows up when positive, down when negative)
            UpdateFlowBar(co2FlowBar, totalCO2_molPerDay);
        }
        
        // Update Temperature Display
        if (sunMoon != null)
        {
            float temp = sunMoon.currentTemperature;
            
            // Update Temperature Text
            if (temperatureText != null)
            {
                temperatureText.text = $"{temp:F1}°C";
            }
            
            // Update Temperature Images (hot >= 30°C, cold < 30°C)
            bool isHot = temp >= 30f;
            
            if (hotTempImage != null)
            {
                hotTempImage.gameObject.SetActive(isHot);
            }
            
            if (coldTempImage != null)
            {
                coldTempImage.gameObject.SetActive(!isHot);
            }
            
            // Update Baseline Text
            if (baselineText != null)
            {
                float respMultiplier = sunMoon.GetRespirationMultiplier();
                baselineText.text = $"{respMultiplier:F2}×";
            }
        }
        
        // Update Ocean Sink & Breakdown Bars
        if (atmosphere != null)
        {
            // Update Ocean Sink Text (uses oceanCO2_day already calculated above)
            if (oceanSinkText != null)
            {
                oceanSinkText.text = $"{Mathf.Abs(oceanCO2_day):F2} mol/day";
            }
            
            // Update Breakdown Bars (centered at Left: 92, Right: 92)
            // Positive values expand right (decrease right offset)
            // Negative values expand left (decrease left offset)
            
            UpdateBreakdownBar(photoO2Bar, plantPhotoO2_day);      // Photosynthesis O2 (usually positive)
            UpdateBreakdownBar(photoC02Bar, -plantPhotoO2_day);    // Photosynthesis CO2 (negative of O2)
            UpdateBreakdownBar(respO2Bar, plantRespO2_day);        // Respiration O2 (usually negative)
            UpdateBreakdownBar(respCO2Bar, -plantRespO2_day);      // Respiration CO2 (positive of O2)
            UpdateBreakdownBar(animalO2Bar, animalO2_day);         // Animal O2 (negative)
            UpdateBreakdownBar(animalCO2Bar, animalCO2_day);       // Animal CO2 (positive)
            UpdateBreakdownBar(humanO2Bar, humanO2_day);           // Human O2 (negative)
            UpdateBreakdownBar(humanCO2Bar, humanCO2_day);         // Human CO2 (positive)
            UpdateBreakdownBar(oceanO2Bar, 0);                     // Ocean O2 (none)
            UpdateBreakdownBar(oceanCO2Bar, oceanCO2_day);         // Ocean CO2 (negative = absorbing)
            
            // Update Breakdown Value Texts
            UpdateBreakdownText(photoO2Text, plantPhotoO2_day);
            UpdateBreakdownText(photoCO2Text, -plantPhotoO2_day);
            UpdateBreakdownText(respO2Text, plantRespO2_day);
            UpdateBreakdownText(respCO2Text, -plantRespO2_day);
            UpdateBreakdownText(animalO2Text, animalO2_day);
            UpdateBreakdownText(animalCO2Text, animalCO2_day);
            UpdateBreakdownText(humanO2Text, humanO2_day);
            UpdateBreakdownText(humanCO2Text, humanCO2_day);
            UpdateBreakdownText(oceanO2Text, 0);
            UpdateBreakdownText(oceanCO2Text, oceanCO2_day);
        }
        
        // Main ecosystem stats UI (optional - can be disabled)
        if (!atmosphere || !ecosystemStatsText || !titleText) return;

        // DEBUG: Track O₂ value read by UI (every 180 frames = ~3 seconds)
        if (Time.frameCount % 180 == 0)
        {
            Debug.Log($"[AtmosphereUI] Reading O₂ from atmosphere: {atmosphere.oxygenMoles:F3} mol");
        }

        // Population data already retrieved above, just use it for display
        // totalO2_molPerDay and totalCO2_molPerDay already calculated above
        // Breakdown values (plantPhotoO2_day, etc.) already calculated in breakdown section above

        string ecosystemSection = $"<b>ECOSYSTEM (Day {ctrl.day}) Time {ctrl.hours:00}:{ctrl.minutes:00} ({ctrl.currentTimeOfDay})</b>\n" +
                                 $"Trees: {trees}  Grass: {grass}\n" +
                                 $"Animals: {animals}  Humans: {humans}\n";

        // Arrows already calculated above, handle near-zero edge cases
        bool o2IsZero = Mathf.Abs(totalO2_molPerDay) < 0.001f;
        bool co2IsZero = Mathf.Abs(totalCO2_molPerDay) < 0.001f;
        
        if (o2IsZero) o2Arrow = "→";
        if (co2IsZero) co2Arrow = "→";

        // Show appropriate precision based on magnitude
        string o2Display = Mathf.Abs(totalO2_molPerDay) >= 0.01f 
            ? $"{totalO2_molPerDay:+0.00;-0.00} mol/day" 
            : $"{totalO2_molPerSec:+0.000000;-0.000000} mol/s";
            
        string co2Display = Mathf.Abs(totalCO2_molPerDay) >= 0.01f 
            ? $"{totalCO2_molPerDay:+0.00;-0.00} mol/day" 
            : $"{totalCO2_molPerSec:+0.000000;-0.000000} mol/s";

        string gasExchangeSection = $"<b>GAS EXCHANGE (Real-time)</b>\n" +
                                        $"{o2Arrow} Net O₂: {o2Display}\n" +
                                        $"{co2Arrow} Net CO₂: {co2Display}\n" +
                                        $"\n<b>Breakdown:</b>\n" +
                                        $" Photosynthesis: +{plantPhotoO2_day:F2} O₂, {-plantPhotoO2_day:F2} CO₂\n" +
                                        $" Plant Respiration: {plantRespO2_day:F2} O₂, +{-plantRespO2_day:F2} CO₂\n" +
                                        $" Animals ({animals}): {animalO2_day:F2} O₂, +{animalCO2_day:F2} CO₂\n" +
                                        $" Humans ({humans}): {humanO2_day:F2} O₂, +{humanCO2_day:F2} CO₂\n" +
                                        $" Ocean: {oceanCO2_day:F2} CO₂\n";

        string atmosphereSection = $"<b>ATMOSPHERE</b>\n" +
                                    $"H₂O: {atmosphere.waterVapor:F3}% ({atmosphere.waterVaporMoles:F0} mol)\n" +
                                    $"N₂: {atmosphere.nitrogen:F3}% ({atmosphere.nitrogenMoles:F0} mol)\n" +     
                                    $"Ar: {atmosphere.argon:F3}% ({atmosphere.argonMoles:F0} mol)\n" +
                                    $"O₂: {atmosphere.oxygen:F4}% ({atmosphere.oxygenMoles:F1} mol)\n" +
                                    $"CO₂: {atmosphere.carbonDioxide:F4}% ({atmosphere.carbonDioxideMoles:F1} mol)\n" + 
                                    $"<b>Total: {atmosphere.totalAtmosphereMoles:F0} mol ({atmosphere.GetTotalPercentage():F2}%)</b>\n";

        string statusColor = GetStatusColorHex();
        string statusSection = $"<b>{atmosphere.environmentalStatus}</b>";

        string tempSection = "";

        if (sunMoon != null)
        {
            float temp = sunMoon.currentTemperature;
            float respMultiplier = sunMoon.GetRespirationMultiplier();
            string timeOfDay = sunMoon.currentTimeOfDay.ToString();
            
            string tempColor = GetTemperatureColorTag(temp);
            tempSection = $"\n━━━━━━━━━━━━━━━━━━\n" +
                $"Temp: {temp:F1}°C | " +
                $"Respiration: {respMultiplier:F2}× baseline";

            ecosystemStatsText.color = GetEcosystemTextColor();
            titleText.color = GetEcosystemTextColor();
        }

        string oceanSection = "";
        if (atmosphere.baseOceanAbsorption > 0)
        {
            // Calculate dynamic ocean rate based on current temperature
            SunMoonController sunMoon = FindAnyObjectByType<SunMoonController>();
            float currentTemp = sunMoon != null ? sunMoon.currentTemperature : atmosphere.referenceTemperature;
            float tempDifference = atmosphere.referenceTemperature - currentTemp;
            float modifier = 1.0f + (tempDifference * atmosphere.henryFactor);
            float dynamicRate = atmosphere.baseOceanAbsorption * modifier;
            
            oceanSection = $"Ocean CO₂ Sink: {dynamicRate:F2} mol/day (Temp: {currentTemp:F1}°C)\n";
        }

        titleText.text = "Atmosphere Composition (Rain Forest)";
        ecosystemStatsText.text = "━━━━━━━━━━━━━━━━━━\n" +
                                  ecosystemSection + "\n" +
                                  gasExchangeSection + "\n" +
                                  atmosphereSection + "\n" +
                                  statusSection + "\n" +
                                  tempSection + "\n" +
                                  oceanSection +
                                  "━━━━━━━━━━━━━━━━━━";
        
        // Update environmental status (optional)
        if (environmentalStatusText)
        {
            environmentalStatusText.text = atmosphere.GetEnvironmentalStatusMessage();
            
            // Color based on status
            switch (atmosphere.environmentalStatus)
            {
                case AtmosphereManager.EnvironmentalStatus.Healthy:
                    environmentalStatusText.color = Color.green;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Warning:
                    environmentalStatusText.color = warningColor;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Danger:
                    environmentalStatusText.color = dangerColor;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Critical:
                    environmentalStatusText.color = Color.red;
                    break;
            }
        }
    }

    void UpdateGasDisplay(Text textElement, string gasName, float percentage, Color color)
    {
        if (!textElement) return;

        // Get actual moles directly from AtmosphereManager (source of truth)
        float moles = 0f;
        switch (gasName)
        {
            case "H₂O": moles = atmosphere.waterVaporMoles; break;
            case "N₂": moles = atmosphere.nitrogenMoles; break;
            case "O₂": moles = atmosphere.oxygenMoles; break;
            case "Ar": moles = atmosphere.argonMoles; break;
            case "CO₂": moles = atmosphere.carbonDioxideMoles; break;
        }
        
        // Format: "CO₂: 0.041% / 410 mol"
        textElement.text = $"{gasName}: {percentage:F3}% / {moles:F0} mol";
        
        if (useColorCoding)
        {
            textElement.color = color;
        }
    }

    string GetTemperatureColorTag(float temperature)
    {
        if (!useColorCoding) return "";

        if (temperature < 15f)
            return "<color=#00BFFF>"; // DeepSkyBlue
        else if (temperature < 20f)
            return "<color=#87CEEB>"; // LightSkyBlue
        else if (temperature < 25f)
            return "<color=#90EE90>"; // LightGreen
        else if (temperature < 30f)
            return "<color=#FFFF00>"; // Yellow
        else if (temperature < 35f)
            return "<color=#FFA500>"; // Orange
        else
            return "<color=#FF4500>"; // OrangeRed
    }

    string GetStatusColorHex()
    {
        switch (atmosphere.environmentalStatus)
        {
            case AtmosphereManager.EnvironmentalStatus.Healthy:
                return "#4CAF50";  // 绿色
            case AtmosphereManager.EnvironmentalStatus.Warning:
                return "#FFC107";  // 黄色
            case AtmosphereManager.EnvironmentalStatus.Danger:
                return "#FF9800";  // 橙色
            case AtmosphereManager.EnvironmentalStatus.Critical:
                return "#F44336";  // 红色
            default:
                return "#FFFFFF";
        }
    }

    Color GetEcosystemTextColor()
    {
        if (sunMoon == null) return Color.white;

        switch (sunMoon.currentTimeOfDay)
        {
            case SunMoonController.TimeOfDay.Night:
                return Color.white;
            case SunMoonController.TimeOfDay.Dawn:
                return Color.black;
            case SunMoonController.TimeOfDay.Morning:
                return Color.black;
            case SunMoonController.TimeOfDay.Noon:
                return Color.black;
            case SunMoonController.TimeOfDay.Afternoon:
                return Color.black;
            case SunMoonController.TimeOfDay.Dusk:
                return Color.white;
            default:
                return Color.white;
        }
    }

    Color GetOxygenColor()
    {
        if (!useColorCoding) return normalColor;
        
        if (atmosphere.oxygen < oxygenDangerThreshold)
            return dangerColor;
        else if (atmosphere.oxygen < oxygenWarningThreshold)
            return warningColor;
        else
            return normalColor;
    }

    Color GetCO2Color()
    {
        if (!useColorCoding) return normalColor;
        
        if (atmosphere.carbonDioxide > co2DangerThreshold)
            return dangerColor;
        else if (atmosphere.carbonDioxide > co2WarningThreshold)
            return warningColor;
        else
            return normalColor;
    }
    
    /// <summary>
    /// Updates a breakdown bar's position based on value.
    /// Positive values expand right (decrease right offset).
    /// Negative values expand left (decrease left offset).
    /// Bar starts centered at Left: 92, Right: 92
    /// Max expansion is 92 pixels (clamps at -92 to 0 for left, 0 to -92 for right)
    /// </summary>
    void UpdateBreakdownBar(Image barImage, float value)
    {
        if (barImage == null) return;
        
        RectTransform rt = barImage.rectTransform;
        float scaledValue = Mathf.Abs(value) * barScaleFactor;
        
        // Debug logging every 2 seconds
        if (Time.frameCount % 120 == 0 && barImage == photoO2Bar)
        {
            Debug.Log($"[UpdateBreakdownBar] photoO2Bar: value={value:F2}, scaledValue BEFORE clamp={scaledValue:F2}, barScaleFactor={barScaleFactor}");
        }
        
        // Clamp scaledValue to max 92 pixels expansion
        scaledValue = Mathf.Min(scaledValue, 92f);
        
        if (Time.frameCount % 120 == 0 && barImage == photoO2Bar)
        {
            Debug.Log($"[UpdateBreakdownBar] photoO2Bar: scaledValue AFTER clamp={scaledValue:F2}");
        }
        
        if (value > 0)
        {
            // Positive: Expand to the right (decrease right offset)
            rt.offsetMin = new Vector2(92, rt.offsetMin.y);  // Left stays at 92
            rt.offsetMax = new Vector2(-(92 - scaledValue), rt.offsetMax.y);  // Right decreases, clamped at 0
            
            if (Time.frameCount % 120 == 0 && barImage == photoO2Bar)
            {
                Debug.Log($"[UpdateBreakdownBar] POSITIVE: offsetMin.x=92, offsetMax.x={-(92 - scaledValue):F2} (RIGHT edge, should be negative)");
            }
        }
        else if (value < 0)
        {
            // Negative: Expand to the left (decrease left offset)
            rt.offsetMin = new Vector2(92 - scaledValue, rt.offsetMin.y);  // Left decreases, clamped at 0
            rt.offsetMax = new Vector2(-92, rt.offsetMax.y);  // Right stays at 92
            
            if (Time.frameCount % 120 == 0 && barImage == photoO2Bar)
            {
                Debug.Log($"[UpdateBreakdownBar] NEGATIVE: offsetMin.x={92 - scaledValue:F2} (LEFT edge), offsetMax.x=-92");
            }
        }
        else
        {
            // Zero: Reset to center
            rt.offsetMin = new Vector2(92, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-92, rt.offsetMax.y);
        }
    }
    
    /// <summary>
    /// Updates a breakdown text to show value with sign.
    /// Examples: +3.5, -0.6, -
    /// </summary>
    void UpdateBreakdownText(Text textElement, float value)
    {
        if (textElement == null) return;
        
        if (Mathf.Abs(value) < 0.01f)
        {
            // Near-zero values show as "-"
            textElement.text = "-";
        }
        else
        {
            // Show with sign: +3.5 or -0.6
            textElement.text = $"{value:+0.0;-0.0}";
        }
    }
    
    /// <summary>
    /// Updates a flow bar to grow upward (positive) or downward (negative).
    /// Container has 250px total height, with text at bottom 50px, leaving 200px for bars (50-250 range).
    /// Uses stretch-stretch anchoring:
    /// - Positive: Bottom FIXED at 50, Top changes 200→0 (value 100 → top=100, value 50 → top=150)
    /// - Negative: Top FIXED at 0, Bottom changes 250→50 (value -100 → bottom=150, value -50 → bottom=200)
    /// </summary>
    void UpdateFlowBar(Image barImage, float value)
    {
        if (barImage == null)
        {
            if (Time.frameCount % 120 == 0)
            {
                Debug.LogWarning("[UpdateFlowBar] barImage is NULL!");
            }
            return;
        }
        
        // Make sure the bar GameObject is active
        if (!barImage.gameObject.activeSelf)
        {
            barImage.gameObject.SetActive(true);
            Debug.Log($"[UpdateFlowBar] Activated {barImage.name}");
        }
        
        RectTransform rt = barImage.rectTransform;
        
        // Scale: 1 mol/day = 0.02 pixels, so 5000 mol/day = 100px (half bar), 10000 mol/day = 200px (full bar)
        float scaledValue = value * 0.02f;
        float clampedValue = Mathf.Clamp(scaledValue, -200f, 200f);
        
        // Debug logging (every 60 frames for O2, every 61 frames for CO2)
        bool isO2 = barImage == o2FlowBar;
        bool isCO2 = barImage == co2FlowBar;
        bool shouldLog = (isO2 && Time.frameCount % 60 == 0) || (isCO2 && Time.frameCount % 61 == 0);
        
        if (shouldLog)
        {
            string barName = isO2 ? "O2" : "CO2";
            Debug.Log($"[UpdateFlowBar {barName}] value={value:F2}, clampedValue={clampedValue:F2}");
            Debug.Log($"  Before: offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");
        }
        
        if (clampedValue > 0.01f)
        {
            // Positive (increase): Bar grows UPWARD
            // Bottom FIXED at 50, Top = 200 - value (so value increases → top decreases toward 0)
            // Examples: value=100 → top=100, value=50 → top=150, value=200 → top=0
            float topPos = 200f - clampedValue;
            rt.offsetMin = new Vector2(rt.offsetMin.x, 50);  // Bottom FIXED at 50
            rt.offsetMax = new Vector2(rt.offsetMax.x, -topPos);  // Top position directly from value
            
            if (shouldLog)
            {
                Debug.Log($"  → POSITIVE: topPos={topPos:F1}, offsetMin.y=50 (FIXED), offsetMax.y={-topPos:F1}");
                Debug.Log($"  After: offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");
            }
        }
        else if (clampedValue < -0.01f)
        {
            // Negative (decrease): Bar grows DOWNWARD
            // Top FIXED at 0, Bottom = 250 + value (value is negative, so this decreases toward 50)
            // Examples: value=-100 → bottom=150, value=-50 → bottom=200, value=-200 → bottom=50
            float bottomPos = 250f + clampedValue; // value is negative, so this subtracts
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottomPos);  // Bottom position directly from value
            rt.offsetMax = new Vector2(rt.offsetMax.x, 0);  // Top FIXED at 0
            
            if (shouldLog)
            {
                Debug.Log($"  → NEGATIVE: bottomPos={bottomPos:F1}, offsetMin.y={bottomPos:F1}, offsetMax.y=0 (FIXED)");
                Debug.Log($"  After: offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");
            }
        }
        else
        {
            // Zero: Minimal bar at boundary (1px visible)
            rt.offsetMin = new Vector2(rt.offsetMin.x, 50);  // Bottom at 50
            rt.offsetMax = new Vector2(rt.offsetMax.x, -200);  // Top at 200 (hidden)
            
            if (shouldLog)
            {
                Debug.Log($"  → ZERO: offsetMin.y=50, offsetMax.y=-200 (hidden)");
                Debug.Log($"  After: offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}");
            }
        }
    }
}