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
    
    [Header("Ocean CO₂ Sink (Optional)")]
    public float oceanAbsorptionRate = 200.0f;
    
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
        float timeFraction = Time.deltaTime; 
        
        // --- 1. Aggregation of all entity rates (mol/s) ---
        float netO2Rate_total = 0f;
        float netCO2Rate_total = 0f;
        
        // Aggregate Plant Agents (Photosynthesis & Respiration)
        foreach (var agent in plantAgents)
        {
            if (agent == null) continue;
            
            float pNetO2 = agent.P_net * agent.metabolismScale;
            float pNetCO2 = -pNetO2;
            
            netO2Rate_total += pNetO2;
            netCO2Rate_total += pNetCO2;
        }
        
        // Aggregate Animal Respiration
        foreach (var animal in animalAgents)
        {
            if (animal == null || !animal.isAlive) continue;
            
            float animalRespiration = animal.totalRespiration * animal.metabolismScale;
            
            netO2Rate_total -= animalRespiration;
            netCO2Rate_total += animalRespiration;
        }
        
        // Aggregate Human Respiration
        foreach (var human in humanAgents)
        {
            if (human == null || !human.isAlive) continue;
            
            float humanRespiration = human.totalRespiration * human.metabolismScale;
            
            netO2Rate_total -= humanRespiration;
            netCO2Rate_total += humanRespiration;
        }
        
        // --- 2. Add Ocean Absorption ---
        float oceanCO2RatePerSec = oceanAbsorptionRate / secondsPerDay;
        netCO2Rate_total -= oceanCO2RatePerSec;
        
        // --- 3. Integrate Change ---
        float oxygenMolesChange = netO2Rate_total * timeFraction * speedMultiplier;
        float co2MolesChange = netCO2Rate_total * timeFraction * speedMultiplier;
        
        // Update MOLAR COUNTS (source of truth)
        oxygenMoles += oxygenMolesChange;
        carbonDioxideMoles += co2MolesChange;
        
        oxygenMoles = Mathf.Max(0f, oxygenMoles);
        carbonDioxideMoles = Mathf.Max(0f, carbonDioxideMoles);
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
            
            float photosynthesisO2 = agent.P_gross * agent.metabolismScale;
            float respirationO2 = -agent.R_total * agent.metabolismScale;
            
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
            float o2RatePerSec = -animal.totalRespiration * animal.metabolismScale;
            float co2RatePerSec = animal.totalRespiration * animal.metabolismScale;
            
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
            float o2RatePerSec = -human.totalRespiration * human.metabolismScale;
            float co2RatePerSec = human.totalRespiration * human.metabolismScale;
            
            humanO2 += o2RatePerSec;
            humanCO2 += co2RatePerSec;
            
            totalO2_molPerSec += o2RatePerSec;
            totalCO2_molPerSec += co2RatePerSec;
        }
        
        // Ocean Absorption
        if (oceanAbsorptionRate > 0f)
        {
            oceanCO2 = -oceanAbsorptionRate / secondsPerDay;
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