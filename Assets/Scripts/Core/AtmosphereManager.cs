using System.Collections.Generic;
using UnityEngine;

public class AtmosphereManager : MonoBehaviour
{
    public static AtmosphereManager Instance { get; private set; }

    [Header("Atmospheric Composition - MOLAR COUNTS (Source of Truth)")]
    public float waterVaporMoles = 4000f;
    public float nitrogenMoles = 780800f;
    public float oxygenMoles = 209500f;
    public float argonMoles = 9300f;
    public float carbonDioxideMoles = 415f;
    
    [Header("Atmospheric Composition - PERCENTAGES (Calculated from Moles)")]
    [HideInInspector] public float waterVapor = 0.4f;
    [HideInInspector] public float nitrogen = 78.08f;
    [HideInInspector] public float oxygen = 20.95f;
    [HideInInspector] public float argon = 0.93f;
    [HideInInspector] public float carbonDioxide = 0.0415f;

    [Header("Biochemical Simulation")]
    public float secondsPerDay = 120f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float totalAtmosphereMoles = 1004015f;
    public bool useBiochemicalModel = true;
    
    [Header("Ocean CO₂ Sink (Dynamic Henry's Law)")]
    [Tooltip("Base ocean CO₂ absorption rate at 25°C (mol/day)")]
    public float baseOceanAbsorption = 800.0f;  // Increased 10× to balance nighttime respiration (was 800)
    [Tooltip("Henry's Law: Temperature sensitivity factor (10% change per °C - stronger effect)")]
    public float henryFactor = 0.10f;  // Increased from 0.05 to 0.10 for stronger temperature effect
    [Tooltip("Reference temperature for calculations (°C)")]
    public float referenceTemperature = 25f;
    
    [Header("Environmental Limits & Warnings")]
    public float oxygenWarningThreshold = 19.0f;
    public float oxygenDangerThreshold = 15.0f;
    public float oxygenCriticalThreshold = 10.0f;
    public float co2WarningThreshold = 0.1f;
    public float co2DangerThreshold = 0.5f;
    public float co2CriticalThreshold = 5.0f;
    
    [Header("Environmental State")]
    [HideInInspector] public EnvironmentalStatus environmentalStatus = EnvironmentalStatus.Healthy;
    
    private float previousDayO2Moles = 0f;
    private float previousDayCO2Moles = 0f;
    
    // Track CO₂ changes by time of day
    private float nightStartCO2 = 0f;
    private float dayStartCO2 = 0f;
    private bool wasNight = false;
    
    public enum EnvironmentalStatus { Healthy, Warning, Danger, Critical }

    [Header("Legacy Particle System (Optional)")]
    public float oxygenIncreasePerParticle = 0.001f;
    public float co2IncreasePerParticle = 0.001f;

    // Default molar values for reset (Unchanged)
    private const float DEFAULT_WATER_VAPOR_MOLES = 4000f;
    private const float DEFAULT_NITROGEN_MOLES = 780800f;
    private const float DEFAULT_OXYGEN_MOLES = 209500f;
    private const float DEFAULT_ARGON_MOLES = 9300f;
    private const float DEFAULT_CO2_MOLES = 415f;

    // Biochemical model lists
    private List<GasExchanger> exchangers = new List<GasExchanger>(); 
    private List<PlantAgent> plantAgents = new List<PlantAgent>();
    private List<AnimalMetabolism> animalAgents = new List<AnimalMetabolism>();
    private List<HumanMetabolism> humanAgents = new List<HumanMetabolism>();
    private float dayTimer = 0f;
    public int currentDay = 0;

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
        
        InitializeAtmosphere();
    }
    
    void InitializeAtmosphere()
    {
        waterVaporMoles = DEFAULT_WATER_VAPOR_MOLES;
        nitrogenMoles = DEFAULT_NITROGEN_MOLES;
        oxygenMoles = DEFAULT_OXYGEN_MOLES;
        argonMoles = DEFAULT_ARGON_MOLES;
        carbonDioxideMoles = DEFAULT_CO2_MOLES;
        
        // Initialize CO₂ tracking
        nightStartCO2 = carbonDioxideMoles;
        dayStartCO2 = carbonDioxideMoles;
        
        // Set initial time of day state
        SunMoonController sunMoon = FindAnyObjectByType<SunMoonController>();
        if (sunMoon != null)
        {
            wasNight = (sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night);
            Debug.LogError($"[CO₂ TRACKING INITIALIZED] Time: {sunMoon.currentTimeOfDay}, wasNight: {wasNight}, CO₂: {carbonDioxideMoles:F0} mol");
        }
        else
        {
            Debug.LogError("[CO₂ TRACKING] ERROR: SunMoonController not found at initialization!");
        }
        
        UpdatePercentagesFromMoles();
    }

    void Update()
    {
        if (useBiochemicalModel)
        {
            ProcessContinuousGasExchange();
            
            dayTimer += Time.deltaTime * speedMultiplier;
            if (dayTimer >= secondsPerDay)
            {
                dayTimer -= secondsPerDay;
                currentDay++;
                LogDailyStats();
            }
        }
        
        UpdatePercentagesFromMoles();
        CheckEnvironmentalLimits();
    }
    
    void ProcessContinuousGasExchange()
    {
        // DEBUG: Verify this function is being called
        if (Time.frameCount == 300) // At frame 300 (5 seconds at 60fps)
        {
            Debug.LogError("[DEBUG] ProcessContinuousGasExchange IS RUNNING!");
        }
        
        // Use game time (accelerated) instead of real time
        // speedMultiplier converts real seconds to game seconds (e.g., 1 real sec = speedMultiplier game sec)
        float timeFraction = Time.deltaTime * speedMultiplier; 
        
        // --- 1. Aggregation of all entity rates (mol/s) ---
        float netO2Rate_total = 0f;
        float netCO2Rate_total = 0f;
        
        // DEBUG: Track individual contributions
        float plantO2Rate = 0f;
        float animalO2Rate = 0f;
        float humanO2Rate = 0f;
        
        // Aggregate Plant Agents (Photosynthesis & Respiration)
        foreach (var agent in plantAgents)
        {
            if (agent == null) continue;
            
            // Use P_gross and R_total separately for accurate CO₂ balance
            // P_gross = O₂ production (photosynthesis)
            // R_total = O₂ consumption (respiration)
            float photosynthesisO2 = agent.P_gross;  // Positive (produces O₂)
            float respirationO2 = -agent.R_total;    // Negative (consumes O₂)
            
            // O₂ balance: Photosynthesis produces, Respiration consumes
            float netO2 = photosynthesisO2 + respirationO2;
            netO2Rate_total += netO2;
            plantO2Rate += netO2;
            
            // CO₂ balance: Photosynthesis CONSUMES CO₂, Respiration PRODUCES CO₂
            float photosynthesisCO2 = -photosynthesisO2;  // Negative (consumes CO₂)
            float respirationCO2 = -respirationO2;        // Positive (produces CO₂)
            
            float netCO2 = photosynthesisCO2 + respirationCO2;
            netCO2Rate_total += netCO2;
        }
        
        // Aggregate Animal Respiration
        foreach (var animal in animalAgents)
        {
            if (animal == null || !animal.isAlive) continue;
            
            // totalRespiration already includes metabolismScale (applied in AnimalMetabolism)
            float animalRespiration = animal.totalRespiration;
            
            netO2Rate_total -= animalRespiration;
            netCO2Rate_total += animalRespiration;
            animalO2Rate -= animalRespiration;
        }
        
        // Aggregate Human Respiration
        foreach (var human in humanAgents)
        {
            if (human == null || !human.isAlive) continue;
            
            // totalRespiration already includes metabolismScale (applied in HumanMetabolism)
            float humanRespiration = human.totalRespiration;
            
            netO2Rate_total -= humanRespiration;
            netCO2Rate_total += humanRespiration;
            humanO2Rate -= humanRespiration;
        }
        
        // --- 2. Add Ocean Absorption (Dynamic Henry's Law) ---
        // Get current temperature from SunMoonController
        SunMoonController sunMoon = FindAnyObjectByType<SunMoonController>();
        float currentTemp = sunMoon != null ? sunMoon.currentTemperature : referenceTemperature;
        
        // Calculate temperature difference from reference (25°C)
        float tempDifference = referenceTemperature - currentTemp; // Positive if cooler
        
        // Apply Henry's Law modifier: Cooler water = Higher CO₂ absorption
        // Example: 20°C → tempDiff = +5 → modifier = 1.25 (25% increase)
        // Example: 30°C → tempDiff = -5 → modifier = 0.75 (25% decrease)
        float modifier = 1.0f + (tempDifference * henryFactor);
        
        // Calculate dynamic absorption rate based on temperature
        float dynamicRate = baseOceanAbsorption * modifier;
        
        // Convert to per-second rate and apply to CO₂
        float oceanCO2RatePerSec = dynamicRate / secondsPerDay;
        netCO2Rate_total -= oceanCO2RatePerSec;
        
        // DEBUG: Log rates every 5 seconds
        if (Time.frameCount % 300 == 0)
        {
            Debug.LogWarning($"[RATES] Plant: {plantO2Rate:F2} O₂/s, {-plantO2Rate:F2} CO₂/s | Animals: {animalO2Rate:F2} O₂/s | Humans: {humanO2Rate:F2} O₂/s | Ocean: -{oceanCO2RatePerSec:F2} CO₂/s (temp {currentTemp:F1}°C, modifier {modifier:F2}) | NET CO₂: {netCO2Rate_total:F2} mol/s");
        }
        
        // --- 3. Integrate Change ---
        // timeFraction already includes speedMultiplier (game time acceleration)
        float oxygenMolesChange = netO2Rate_total * timeFraction;
        float co2MolesChange = netCO2Rate_total * timeFraction;
        
        // Update MOLAR COUNTS (source of truth)
        float oldO2 = oxygenMoles;
        float oldCO2 = carbonDioxideMoles;
        
        oxygenMoles += oxygenMolesChange;
        carbonDioxideMoles += co2MolesChange;
        
        oxygenMoles = Mathf.Max(0f, oxygenMoles);
        carbonDioxideMoles = Mathf.Max(0f, carbonDioxideMoles);
        
        // Track day/night CO₂ changes
        if (sunMoon != null)
        {
            // DEBUG: Verify we reach this code
            if (Time.frameCount == 310)
            {
                Debug.LogError($"[DEBUG] Tracking code reached! Time: {sunMoon.currentTimeOfDay}, wasNight: {wasNight}");
            }
            
            bool isNight = (sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night);
            
            // Detect transition from day to night
            if (isNight && !wasNight)
            {
                // Just became night - log day's CO₂ change
                float dayCO2Change = carbonDioxideMoles - dayStartCO2;
                Debug.LogWarning($"[CO₂ TRACKING] DAY ENDED: CO₂ changed by {dayCO2Change:F1} mol (from {dayStartCO2:F0} to {carbonDioxideMoles:F0})");
                nightStartCO2 = carbonDioxideMoles;
            }
            // Detect transition from night to day
            else if (!isNight && wasNight)
            {
                // Just became day - log night's CO₂ change
                float nightCO2Change = carbonDioxideMoles - nightStartCO2;
                Debug.LogWarning($"[CO₂ TRACKING] NIGHT ENDED: CO₂ changed by {nightCO2Change:F1} mol (from {nightStartCO2:F0} to {carbonDioxideMoles:F0})");
                dayStartCO2 = carbonDioxideMoles;
            }
            
            wasNight = isNight;
            
            // DEBUG: Log current tracking state every 5 seconds
            if (Time.frameCount % 300 == 0)
            {
                string timeColor = isNight ? "NIGHT" : "DAY";
                Debug.LogWarning($"[CO₂ STATUS] {timeColor} | CO₂: {carbonDioxideMoles:F0} mol | Change from {(isNight ? "night start" : "day start")}: {(isNight ? (carbonDioxideMoles - nightStartCO2) : (carbonDioxideMoles - dayStartCO2)):F1}");
            }
        }
        else
        {
            // Warn if SunMoonController is missing
            if (Time.frameCount % 300 == 0)
            {
                Debug.LogWarning("[CO₂ TRACKING] SunMoonController NOT FOUND!");
            }
        }
        
        // DEBUG: Warn if actual change doesn't match expected
        float actualO2Change = oxygenMoles - oldO2;
        float actualCO2Change = carbonDioxideMoles - oldCO2;
        
        if (Mathf.Abs(actualO2Change - oxygenMolesChange) > 0.001f)
        {
            Debug.LogWarning($"[ATMOSPHERE] O₂ MISMATCH! Expected: {oxygenMolesChange:F6}, Actual: {actualO2Change:F6}");
        }
        
        if (Mathf.Abs(actualCO2Change - co2MolesChange) > 0.001f)
        {
            Debug.LogWarning($"[ATMOSPHERE] CO₂ MISMATCH! Expected: {co2MolesChange:F6}, Actual: {actualCO2Change:F6}");
        }
    }
    
    void LogDailyStats()
    {
        // Logging function body removed for brevity
    }

    void UpdatePercentagesFromMoles()
    {
        totalAtmosphereMoles = nitrogenMoles + argonMoles + oxygenMoles + carbonDioxideMoles + waterVaporMoles;
        
        if (totalAtmosphereMoles > 0f)
        {
            waterVapor = (waterVaporMoles / totalAtmosphereMoles) * 100f;
            nitrogen = (nitrogenMoles / totalAtmosphereMoles) * 100f;
            oxygen = (oxygenMoles / totalAtmosphereMoles) * 100f;
            argon = (argonMoles / totalAtmosphereMoles) * 100f;
            carbonDioxide = (carbonDioxideMoles / totalAtmosphereMoles) * 100f;
        }
    }

    void CheckEnvironmentalLimits()
    {
        EnvironmentalStatus previousStatus = environmentalStatus;
        environmentalStatus = EnvironmentalStatus.Healthy;
        
        if (oxygen < oxygenCriticalThreshold) environmentalStatus = EnvironmentalStatus.Critical;
        else if (oxygen < oxygenDangerThreshold) environmentalStatus = EnvironmentalStatus.Danger;
        else if (oxygen < oxygenWarningThreshold) environmentalStatus = EnvironmentalStatus.Warning;
        
        if (carbonDioxide > co2CriticalThreshold) environmentalStatus = EnvironmentalStatus.Critical;
        else if (carbonDioxide > co2DangerThreshold) environmentalStatus = EnvironmentalStatus.Danger;
        else if (carbonDioxide > co2WarningThreshold) 
        {
            if (environmentalStatus < EnvironmentalStatus.Warning) environmentalStatus = EnvironmentalStatus.Warning;
        }
    }
    
    public string GetEnvironmentalStatusMessage()
    {
        switch (environmentalStatus)
        {
            case EnvironmentalStatus.Healthy: return "✅ Healthy - All parameters normal";
            case EnvironmentalStatus.Warning: return "⚠️ Warning - Levels approaching limits";
            case EnvironmentalStatus.Danger: return "⚠️ DANGER - Hypoxia/Toxicity imminent";
            case EnvironmentalStatus.Critical: return "💀 CRITICAL - Lethal levels reached";
            default: return "Unknown status";
        }
    }
    
    // --- Registration/Unregistration Functions (Kept for list management) ---
    public void RegisterExchanger(GasExchanger exchanger) { if (!exchangers.Contains(exchanger)) exchangers.Add(exchanger); }
    public void UnregisterExchanger(GasExchanger exchanger) { exchangers.Remove(exchanger); }
    public void RegisterPlantAgent(PlantAgent agent) { if (!plantAgents.Contains(agent)) plantAgents.Add(agent); }
    public void UnregisterPlantAgent(PlantAgent agent) { plantAgents.Remove(agent); }
    public void RegisterAnimalAgent(AnimalMetabolism agent) { if (!animalAgents.Contains(agent)) animalAgents.Add(agent); }
    public void UnregisterAnimalAgent(AnimalMetabolism agent) { animalAgents.Remove(agent); }
    public void RegisterHumanAgent(HumanMetabolism agent) { if (!humanAgents.Contains(agent)) humanAgents.Add(agent); }
    public void UnregisterHumanAgent(HumanMetabolism agent) { humanAgents.Remove(agent); }
    
    public void GetEcosystemStatsWithPlantAgents(out int trees, out int grass, out int animals, out int humans,
                                                 out float totalO2_molPerSec, out float totalCO2_molPerSec,
                                                 out float plantPhotosynthesisO2, out float plantRespirationO2,
                                                 out float animalO2, out float animalCO2,
                                                 out float humanO2, out float humanCO2,
                                                 out float oceanCO2)
    {
        // This function duplicates the logic in ProcessContinuousGasExchange to provide instantaneous UI rates.
        
        trees = 0; grass = 0; animals = 0; humans = 0;
        totalO2_molPerSec = 0f; totalCO2_molPerSec = 0f;
        plantPhotosynthesisO2 = 0f; plantRespirationO2 = 0f;
        animalO2 = 0f; animalCO2 = 0f;
        humanO2 = 0f; humanCO2 = 0f;
        oceanCO2 = 0f;
        
        // Plant Agents
        foreach (var agent in plantAgents)
        {
            if (agent == null) continue;
            if (agent.plantType == PlantAgent.PlantType.Tree) trees++;
            else grass++;
            
            // P_gross and R_total already include metabolismScale (applied in PlantAgent.Update)
            float photosynthesisO2 = agent.P_gross;
            float respirationO2 = -agent.R_total;
            
            plantPhotosynthesisO2 += photosynthesisO2;
            plantRespirationO2 += respirationO2;
            
            totalO2_molPerSec += photosynthesisO2 + respirationO2;
            totalCO2_molPerSec += -(photosynthesisO2 + respirationO2);
        }
        
        // Animal Respiration
        foreach (var animal in animalAgents)
        {
            if (animal == null || !animal.isAlive) continue;
            animals++;
            // totalRespiration already includes metabolismScale (applied in AnimalMetabolism)
            float o2RatePerSec = -animal.totalRespiration;
            float co2RatePerSec = animal.totalRespiration;
            
            animalO2 += o2RatePerSec;
            animalCO2 += co2RatePerSec;
            
            totalO2_molPerSec += o2RatePerSec;
            totalCO2_molPerSec += co2RatePerSec;
        }
        
        // Human Respiration
        foreach (var human in humanAgents)
        {
            if (human == null || !human.isAlive) continue;
            humans++;
            // totalRespiration already includes metabolismScale (applied in HumanMetabolism)
            float o2RatePerSec = -human.totalRespiration;
            float co2RatePerSec = human.totalRespiration;
            
            humanO2 += o2RatePerSec;
            humanCO2 += co2RatePerSec;
            
            totalO2_molPerSec += o2RatePerSec;
            totalCO2_molPerSec += co2RatePerSec;
        }
        
        // Ocean Absorption (Dynamic Henry's Law)
        if (baseOceanAbsorption > 0f)
        {
            // Get current temperature
            SunMoonController sunMoon = FindAnyObjectByType<SunMoonController>();
            float currentTemp = sunMoon != null ? sunMoon.currentTemperature : referenceTemperature;
            
            // Calculate dynamic rate based on temperature
            float tempDifference = referenceTemperature - currentTemp;
            float modifier = 1.0f + (tempDifference * henryFactor);
            float dynamicRate = baseOceanAbsorption * modifier;
            
            oceanCO2 = -dynamicRate / secondsPerDay;
            totalCO2_molPerSec += oceanCO2;
        }
    }

    /// <summary>
    /// Add a one-time CO₂ spike (e.g., from tree death/burning)
    /// Uses MOLAR calculation (correct method)
    /// </summary>
    public void AddCO2Spike(float moles)
    {
        // Directly modify the molar count, which will be reflected in the next UpdatePercentagesFromMoles() call.
        carbonDioxideMoles += moles;
        Debug.LogWarning($"[Atmosphere] CO₂ Spike! Released {moles:F1} moles. New CO₂: {carbonDioxideMoles:F0} mol");
    }

    /// <summary>
    /// Get total percentage (should always be ~100%)
    /// </summary>
    public float GetTotalPercentage()
    {
        return waterVapor + nitrogen + oxygen + argon + carbonDioxide;
    }
}