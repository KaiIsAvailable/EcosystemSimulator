using UnityEngine;
using UnityEngine.UI;

public class AtmosphereUI : MonoBehaviour{
    [Header("Ecosystem Stats UI (Optional)")]
    public Text ecosystemStatsText;
    public Text titleText;
    
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
    }

    void Update()
    {
        if (!atmosphere || !ecosystemStatsText || !titleText) return;

        // DEBUG: Track O₂ value read by UI (every 180 frames = ~3 seconds)
        if (Time.frameCount % 180 == 0)
        {
            Debug.Log($"[AtmosphereUI] Reading O₂ from atmosphere: {atmosphere.oxygenMoles:F3} mol");
        }

        // Get real data from PlantAgent system with breakdown
        atmosphere.GetEcosystemStatsWithPlantAgents(out int trees, out int grass, out int animals, out int humans,
                                                      out float totalO2_molPerSec, out float totalCO2_molPerSec,
                                                      out float plantPhotosynthesisO2, out float plantRespirationO2,
                                                      out float animalO2, out float animalCO2,
                                                      out float humanO2, out float humanCO2,
                                                      out float oceanCO2);

        // Convert mol/s to mol/day for display
        float totalO2_molPerDay = totalO2_molPerSec * atmosphere.secondsPerDay;
        float totalCO2_molPerDay = totalCO2_molPerSec * atmosphere.secondsPerDay;
        
        // Breakdown in mol/day
        float plantPhotoO2_day = plantPhotosynthesisO2 * atmosphere.secondsPerDay;
        float plantRespO2_day = plantRespirationO2 * atmosphere.secondsPerDay;
        float animalO2_day = animalO2 * atmosphere.secondsPerDay;
        float animalCO2_day = animalCO2 * atmosphere.secondsPerDay;
        float humanO2_day = humanO2 * atmosphere.secondsPerDay;
        float humanCO2_day = humanCO2 * atmosphere.secondsPerDay;
        float oceanCO2_day = oceanCO2 * atmosphere.secondsPerDay;

        // Determine if values are significant enough to show
        bool o2IsZero = Mathf.Abs(totalO2_molPerDay) < 0.001f;
        bool co2IsZero = Mathf.Abs(totalCO2_molPerDay) < 0.001f;

        string ecosystemSection = $"<b>ECOSYSTEM (Day {ctrl.day}) Time {ctrl.hours:00}:{ctrl.minutes:00} ({ctrl.currentTimeOfDay})</b>\n" +
                                 $"Trees: {trees}  Grass: {grass}\n" +
                                 $"Animals: {animals}  Humans: {humans}\n";

        // Use arrows to show direction, but handle near-zero values
        string o2Arrow = o2IsZero ? "→" : (totalO2_molPerDay > 0 ? "↑" : "↓");
        string co2Arrow = co2IsZero ? "→" : (totalCO2_molPerDay > 0 ? "↑" : "↓");

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
        if (atmosphere.oceanAbsorptionRate > 0)
        {
            oceanSection = $"Ocean CO₂ Sink: {atmosphere.oceanAbsorptionRate:F2} mol/day\n";
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
}
