using System.Collections.Generic;
using UnityEngine;

public class AtmosphereManager : MonoBehaviour
{
    public static AtmosphereManager Instance { get; private set; }

    [Header("Atmospheric Composition - MOLAR COUNTS (Source of Truth)")]
    [Tooltip("Water Vapor - H₂O (moles)")]
    public float waterVaporMoles = 4000f;
    
    [Tooltip("Nitrogen - N₂ (moles) - INERT, never changes")]
    public float nitrogenMoles = 780800f;
    
    [Tooltip("Oxygen - O₂ (moles) - Changes via photosynthesis/respiration")]
    public float oxygenMoles = 209500f;
    
    [Tooltip("Argon - Ar (moles) - INERT, never changes")]
    public float argonMoles = 9300f;
    
    [Tooltip("Carbon Dioxide - CO₂ (moles) - Changes via photosynthesis/respiration")]
    public float carbonDioxideMoles = 415f;
    
    [Header("Atmospheric Composition - PERCENTAGES (Calculated from Moles)")]
    [Tooltip("Water Vapor - H₂O (%)")]
    [HideInInspector]
    public float waterVapor = 0.4f;
    
    [Tooltip("Nitrogen - N₂ (%)")]
    [HideInInspector]
    public float nitrogen = 78.08f;
    
    [Tooltip("Oxygen - O₂ (%)")]
    [HideInInspector]
    public float oxygen = 20.95f;
    
    [Tooltip("Argon - Ar (%)")]
    [HideInInspector]
    public float argon = 0.93f;
    
    [Tooltip("Carbon Dioxide - CO₂ (%)")]
    [HideInInspector]
    public float carbonDioxide = 0.0415f;

    [Header("Biochemical Simulation")]
    [Tooltip("Seconds per simulated day (should match SunMoonController base day)")]
    public float secondsPerDay = 120f;
    
    [Tooltip("Speed multiplier for gas exchange (set by TimeSpeedController)")]
    [HideInInspector]
    public float speedMultiplier = 1f;
    
    [Tooltip("Total moles of gas in atmosphere (recalculated every frame)")]
    [HideInInspector]
    public float totalAtmosphereMoles = 1004015f;
    
    [Tooltip("Enable biochemical gas exchange simulation")]
    public bool useBiochemicalModel = true;
    
    [Header("Ocean CO₂ Sink (Optional)")]
    [Tooltip("Moles of CO₂ absorbed by ocean per day (0 = no ocean)")]
    public float oceanAbsorptionRate = 20.0f;  // Balanced to create net CO₂ decrease at night despite plant respiration
    
    [Header("Environmental Limits & Warnings")]
    [Tooltip("O₂ below this % shows warning (normal: 19-21%)")]
    public float oxygenWarningThreshold = 19.0f;
    
    [Tooltip("O₂ below this % is dangerous (hypoxia zone)")]
    public float oxygenDangerThreshold = 15.0f;
    
    [Tooltip("O₂ below this % is lethal")]
    public float oxygenCriticalThreshold = 10.0f;
    
    [Tooltip("CO₂ above this % shows warning (normal: < 0.1%)")]
    public float co2WarningThreshold = 0.1f;
    
    [Tooltip("CO₂ above this % is dangerous")]
    public float co2DangerThreshold = 0.5f;
    
    [Tooltip("CO₂ above this % is lethal")]
    public float co2CriticalThreshold = 5.0f;
    
    [Header("Environmental State")]
    [HideInInspector]
    public EnvironmentalStatus environmentalStatus = EnvironmentalStatus.Healthy;
    
    // Track previous day values for 24h delta calculation
    private float previousDayO2Moles = 0f;
    private float previousDayCO2Moles = 0f;
    
    public enum EnvironmentalStatus
    {
        Healthy,        // All parameters normal
        Warning,        // Approaching dangerous levels
        Danger,         // Dangerous levels, consequences imminent
        Critical        // Lethal levels, entities dying
    }

    [Header("Legacy Particle System (Optional)")]
    [Tooltip("How much % oxygen increases per O₂ particle absorbed")]
    public float oxygenIncreasePerParticle = 0.001f;
    
    [Tooltip("How much % CO₂ increases per CO₂ particle absorbed")]
    public float co2IncreasePerParticle = 0.001f;

    // Default molar values for reset (Earth-like atmosphere)
    private const float DEFAULT_WATER_VAPOR_MOLES = 4000f;      // 0.4%
    private const float DEFAULT_NITROGEN_MOLES = 780800f;       // 78.08% (INERT)
    private const float DEFAULT_OXYGEN_MOLES = 209500f;         // 20.95%
    private const float DEFAULT_ARGON_MOLES = 9300f;            // 0.93% (INERT)
    private const float DEFAULT_CO2_MOLES = 415f;               // 0.0415%

    // Biochemical model
    private List<GasExchanger> exchangers = new List<GasExchanger>();
    private List<PlantAgent> plantAgents = new List<PlantAgent>();  // 新的PlantAgent系统
    private List<AnimalMetabolism> animalAgents = new List<AnimalMetabolism>();  // Scientific animal metabolism
    private List<HumanMetabolism> humanAgents = new List<HumanMetabolism>();  // Scientific human metabolism
    private float dayTimer = 0f;
    public int currentDay = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize molar counts and calculate initial percentages
        InitializeAtmosphere();
    }
    
    /// <summary>
    /// Initialize atmosphere with default Earth-like molar composition
    /// </summary>
    void InitializeAtmosphere()
    {
        // Set default molar counts
        waterVaporMoles = DEFAULT_WATER_VAPOR_MOLES;
        nitrogenMoles = DEFAULT_NITROGEN_MOLES;
        oxygenMoles = DEFAULT_OXYGEN_MOLES;
        argonMoles = DEFAULT_ARGON_MOLES;
        carbonDioxideMoles = DEFAULT_CO2_MOLES;
        
        // Calculate total and percentages
        UpdatePercentagesFromMoles();
        
        /*Debug.Log($"[Atmosphere] Initialized with {totalAtmosphereMoles:F0} total moles");
        Debug.Log($"  N₂: {nitrogenMoles:F0} mol ({nitrogen:F2}%) - INERT");
        Debug.Log($"  O₂: {oxygenMoles:F0} mol ({oxygen:F2}%)");
        Debug.Log($"  Ar: {argonMoles:F0} mol ({argon:F2}%) - INERT");
        Debug.Log($"  H₂O: {waterVaporMoles:F0} mol ({waterVapor:F2}%)");
        Debug.Log($"  CO₂: {carbonDioxideMoles:F0} mol ({carbonDioxide:F4}%)");*/
    }

    void Update()
    {
        if (useBiochemicalModel)
        {
            // Process continuous gas exchange (frame-by-frame) using MOLAR calculations
            ProcessContinuousGasExchange();
            
            // Track full days for logging (uses speed multiplier)
            dayTimer += Time.deltaTime * speedMultiplier;
            if (dayTimer >= secondsPerDay)
            {
                dayTimer -= secondsPerDay;
                currentDay++;
                LogDailyStats();
            }
        }
        
        // Recalculate percentages from moles (percentages are derived, NOT source of truth)
        UpdatePercentagesFromMoles();
        
        // Check environmental limits and update status
        CheckEnvironmentalLimits();
    }
    
    /// <summary>
    /// Process continuous gas exchange every frame using CORRECT MOLAR CALCULATION
    /// NOTE: Now only processes GasExchanger entities (Animals/Humans)
    /// Plants are handled by PlantAgent system directly
    /// </summary>
    void ProcessContinuousGasExchange()
    {
        // Time Fraction = Time.deltaTime / secondsPerDay
        float timeFraction = Time.deltaTime / secondsPerDay;
        
        // Calculate Net Flux Rates (mol/day) - Only from GasExchanger (Animals/Humans)
        float netO2Rate = 0f; // mol/day
        float netCO2Rate = 0f; // mol/day
        
        int skippedTrees = 0, skippedGrass = 0, processedAnimals = 0, processedHumans = 0;
        
        // Sum up only non-plant entity contributions
        foreach (GasExchanger exchanger in exchangers)
        {
            if (exchanger != null)
            {
                // Skip plant entities - they use PlantAgent system now
                if (exchanger.entityType == GasExchanger.EntityType.Tree)
                {
                    skippedTrees++;
                    continue;
                }
                if (exchanger.entityType == GasExchanger.EntityType.Grass)
                {
                    skippedGrass++;
                    continue;
                }
                
                netO2Rate += exchanger.GetCurrentO2Rate();
                netCO2Rate += exchanger.GetCurrentCO2Rate();
                
                if (exchanger.entityType == GasExchanger.EntityType.Animal) processedAnimals++;
                if (exchanger.entityType == GasExchanger.EntityType.Human) processedHumans++;
            }
        }
        
        // Debug log what was processed vs skipped (every 120 frames)
        if (Time.frameCount % 120 == 0)
        {
            //Debug.Log($"[ProcessGasExchange] OLD SYSTEM: Skipped: {skippedTrees} trees + {skippedGrass} grass | " +
            //          $"Processed: {processedAnimals} animals + {processedHumans} humans | " +
            //          $"NetCO2 before ocean: {netCO2Rate:F2} mol/day");
            //Debug.Log($"[ProcessGasExchange] NEW SYSTEM: AnimalMetabolism={animalAgents.Count}, HumanMetabolism={humanAgents.Count}");
        }
        
        // Add ocean CO₂ absorption (ocean acts as carbon sink)
        if (oceanAbsorptionRate > 0f)
        {
            netCO2Rate -= oceanAbsorptionRate;  // Negative = removes CO₂ from atmosphere
            
            // Debug log ocean sink activity (every 60 frames = ~1 second)
            if (Time.frameCount % 60 == 0)
            {
                //Debug.Log($"[Ocean Sink] Removing {oceanAbsorptionRate:F2} mol/day CO₂ " +
                //          $"(Current CO₂: {carbonDioxideMoles:F1} mol)");
            }
        }
        
        // Integrate Net Flux into Moles
        // O₂ Moles += (Net O₂ Rate × Time Fraction × Speed Multiplier)
        // CO₂ Moles += (Net CO₂ Rate × Time Fraction × Speed Multiplier)
        float oxygenMolesChange = netO2Rate * timeFraction * speedMultiplier;
        float co2MolesChange = netCO2Rate * timeFraction * speedMultiplier;
        
        // DEBUG: Track O₂ before ProcessGasExchange modification (every 60 frames)
        if (Time.frameCount % 60 == 0)
        {
            //Debug.Log($"[AtmosphereManager] BEFORE ProcessGasExchange: O₂={oxygenMoles:F3} mol | " +
            //          $"About to add: {oxygenMolesChange:F10} mol from animals/ocean");
        }
        
        // Update MOLAR COUNTS (source of truth)
        oxygenMoles += oxygenMolesChange;
        carbonDioxideMoles += co2MolesChange;
        
        // DEBUG: Track O₂ after ProcessGasExchange modification (every 60 frames)
        if (Time.frameCount % 60 == 0)
        {
            //Debug.Log($"[AtmosphereManager] AFTER ProcessGasExchange: O₂={oxygenMoles:F3} mol");
        }
        
        // Debug log actual changes (every 120 frames = ~2 seconds)
        if (Time.frameCount % 120 == 0)
        {
            //Debug.Log($"[Atmosphere] Frame changes: O₂ {oxygenMolesChange:+0.000000;-0.000000} mol, " +
            //          $"CO₂ {co2MolesChange:+0.000000;-0.000000} mol | " +
            //          $"Total: O₂={oxygenMoles:F0} mol, CO₂={carbonDioxideMoles:F0} mol");
        }
        
        // Clamp to prevent negative values
        oxygenMoles = Mathf.Max(0f, oxygenMoles);
        carbonDioxideMoles = Mathf.Max(0f, carbonDioxideMoles);
        
        // Note: PlantAgent handles plant gas exchange directly in their Update()
        // Note: N₂ and Ar are INERT and NEVER change!
    }
    
    /// <summary>
    /// Log statistics once per day
    /// </summary>
    void LogDailyStats()
    {
        float netO2Rate = 0f;
        float netCO2Rate = 0f;
        
        // Track entity counts and contributions
        int trees = 0, grass = 0, animals = 0, humans = 0;
        float treeO2 = 0f, grassO2 = 0f, animalO2 = 0f, humanO2 = 0f;
        
        // Calculate total gas exchange from all registered entities
        foreach (var exchanger in exchangers)
        {
            if (exchanger != null)
            {
                float o2 = exchanger.GetCurrentO2Rate();
                float co2 = exchanger.GetCurrentCO2Rate();
                
                netO2Rate += o2;
                netCO2Rate += co2;
                
                // Track by type
                switch (exchanger.entityType)
                {
                    case GasExchanger.EntityType.Tree:
                        trees++;
                        treeO2 += o2;
                        break;
                    case GasExchanger.EntityType.Grass:
                        grass++;
                        grassO2 += o2;
                        break;
                    case GasExchanger.EntityType.Animal:
                        animals++;
                        animalO2 += o2;
                        break;
                    case GasExchanger.EntityType.Human:
                        humans++;
                        humanO2 += o2;
                        break;
                }
            }
        }
        
        // Calculate net CO₂ after ocean absorption for logging display
        float netCO2WithOcean = netCO2Rate;
        if (oceanAbsorptionRate > 0f)
        {
            netCO2WithOcean -= oceanAbsorptionRate;
        }
        
        // Calculate ACTUAL 24h changes (if we have previous day data)
        string deltaLog = "";
        if (currentDay > 1 && previousDayO2Moles > 0)
        {
            float actualO2Change = oxygenMoles - previousDayO2Moles;
            float actualCO2Change = carbonDioxideMoles - previousDayCO2Moles;
            deltaLog = $"\n  Actual 24h Change → O₂: {actualO2Change:+0.0;-0.0} mol, CO₂: {actualCO2Change:+0.0;-0.0} mol";
        }
        
        // Store current values for next day comparison
        previousDayO2Moles = oxygenMoles;
        previousDayCO2Moles = carbonDioxideMoles;
        
        // Log current state (no changes applied here, just logging)
        //Debug.Log($"[Atmosphere] Day {currentDay}: O₂={oxygen:F3}%, CO₂={carbonDioxide:F4}%");
        //Debug.Log($"  Population → Trees: {trees}, Grass: {grass}, Animals: {animals}, Humans: {humans}");
        //Debug.Log($"  Breakdown → Trees O₂: {treeO2:F1}, Grass O₂: {grassO2:F1}, Animals O₂: {animalO2:F1}, Humans O₂: {humanO2:F1}");
        //Debug.Log($"  Ocean → CO₂ absorption: {oceanAbsorptionRate:F1} mol/day");
        //Debug.Log($"  Net Rates (instantaneous) → O₂: {netO2Rate:F1} mol/day, CO₂: {netCO2WithOcean:F1} mol/day (at time of log)");
        if (!string.IsNullOrEmpty(deltaLog))
        {
            //Debug.Log(deltaLog);
        }
    }

    /// <summary>
    /// Register a gas exchanger (called automatically by GasExchanger.Start)
    /// </summary>
    public void RegisterExchanger(GasExchanger exchanger)
    {
        if (!exchangers.Contains(exchanger))
        {
            exchangers.Add(exchanger);
            //Debug.Log($"[Atmosphere] Registered {exchanger.entityType}: O₂={exchanger.oxygenRate:F1} mol/day, CO₂={exchanger.co2Rate:F1} mol/day");
        }
    }
    
    /// <summary>
    /// Unregister a gas exchanger (called when entity is destroyed)
    /// </summary>
    public void UnregisterExchanger(GasExchanger exchanger)
    {
        exchangers.Remove(exchanger);
    }
    
    /// <summary>
    /// Register a PlantAgent (新系统)
    /// </summary>
    public void RegisterPlantAgent(PlantAgent agent)
    {
        if (!plantAgents.Contains(agent))
        {
            plantAgents.Add(agent);
            //Debug.Log($"[Atmosphere] Registered PlantAgent #{plantAgents.Count}: {agent.plantType} (biomass={agent.biomass:F1})");
        }
        else
        {
            //Debug.LogWarning($"[Atmosphere] DUPLICATE REGISTRATION BLOCKED: {agent.plantType} already in list!");
        }
    }
    
    /// <summary>
    /// Unregister a PlantAgent (called when entity is destroyed)
    /// </summary>
    public void UnregisterPlantAgent(PlantAgent agent)
    {
        plantAgents.Remove(agent);
    }
    
    /// <summary>
    /// Register an AnimalMetabolism (scientific animal system)
    /// </summary>
    public void RegisterAnimalAgent(AnimalMetabolism agent)
    {
        if (!animalAgents.Contains(agent))
        {
            animalAgents.Add(agent);
            //Debug.Log($"[Atmosphere] Registered AnimalMetabolism #{animalAgents.Count}: {agent.gameObject.name} (biomass={agent.biomass:F1} kg)");
        }
    }
    
    /// <summary>
    /// Unregister an AnimalMetabolism (called when entity is destroyed)
    /// </summary>
    public void UnregisterAnimalAgent(AnimalMetabolism agent)
    {
        animalAgents.Remove(agent);
    }
    
    /// <summary>
    /// Register a HumanMetabolism (scientific human system)
    /// </summary>
    public void RegisterHumanAgent(HumanMetabolism agent)
    {
        if (!humanAgents.Contains(agent))
        {
            humanAgents.Add(agent);
           //Debug.Log($"[Atmosphere] Registered HumanMetabolism #{humanAgents.Count}: {agent.gameObject.name} (biomass={agent.biomass:F1} kg)");
        }
    }
    
    /// <summary>
    /// Unregister a HumanMetabolism (called when entity is destroyed)
    /// </summary>
    public void UnregisterHumanAgent(HumanMetabolism agent)
    {
        humanAgents.Remove(agent);
    }
    
    /// <summary>
    /// Add a one-time CO₂ spike (e.g., from tree death/burning)
    /// Uses MOLAR calculation (correct method)
    /// </summary>
    public void AddCO2Spike(float moles)
    {
        carbonDioxideMoles += moles;
        //Debug.LogWarning($"[Atmosphere] CO₂ Spike! Released {moles} moles. New CO₂: {carbonDioxideMoles:F0} mol ({carbonDioxide:F4}%)");
    }

    /// <summary>
    /// Legacy: Call this when an oxygen particle is absorbed by the atmosphere
    /// Now uses MOLAR calculation
    /// </summary>
    public void AddOxygen()
    {
        // Convert percentage increase to molar increase
        float molesChange = (oxygenIncreasePerParticle / 100f) * totalAtmosphereMoles;
        oxygenMoles += molesChange;
    }

    /// <summary>
    /// Legacy: Call this when a CO₂ particle is absorbed by the atmosphere
    /// Now uses MOLAR calculation
    /// </summary>
    public void AddCarbonDioxide()
    {
        // Convert percentage increase to molar increase
        float molesChange = (co2IncreasePerParticle / 100f) * totalAtmosphereMoles;
        carbonDioxideMoles += molesChange;
    }

    /// <summary>
    /// STEP C & D: Recalculate total moles and update percentages from molar counts
    /// This is the CORRECT way to calculate percentages (derived from moles, not source of truth)
    /// </summary>
    void UpdatePercentagesFromMoles()
    {
        // Step C: Recalculate Total Moles
        totalAtmosphereMoles = nitrogenMoles + argonMoles + oxygenMoles + carbonDioxideMoles + waterVaporMoles;
        
        // Step D: Calculate New Percentages
        // Gas Percentage = (Gas Moles / Total Moles) × 100
        if (totalAtmosphereMoles > 0f)
        {
            waterVapor = (waterVaporMoles / totalAtmosphereMoles) * 100f;
            nitrogen = (nitrogenMoles / totalAtmosphereMoles) * 100f;
            oxygen = (oxygenMoles / totalAtmosphereMoles) * 100f;
            argon = (argonMoles / totalAtmosphereMoles) * 100f;
            carbonDioxide = (carbonDioxideMoles / totalAtmosphereMoles) * 100f;
        }
    }

    /// <summary>
    /// Reset atmosphere to default Earth-like composition (MOLAR method)
    /// </summary>
    public void ResetToDefault()
    {
        waterVaporMoles = DEFAULT_WATER_VAPOR_MOLES;
        nitrogenMoles = DEFAULT_NITROGEN_MOLES;
        oxygenMoles = DEFAULT_OXYGEN_MOLES;
        argonMoles = DEFAULT_ARGON_MOLES;
        carbonDioxideMoles = DEFAULT_CO2_MOLES;
        
        UpdatePercentagesFromMoles();
        
        //Debug.Log("[Atmosphere] Reset to default Earth composition");
        //Debug.Log($"  Total: {totalAtmosphereMoles:F0} mol");
        //Debug.Log($"  O₂: {oxygenMoles:F0} mol ({oxygen:F2}%)");
        //Debug.Log($"  CO₂: {carbonDioxideMoles:F0} mol ({carbonDioxide:F4}%)");
    }

    /// <summary>
    /// Get total percentage (should always be ~100%)
    /// </summary>
    public float GetTotalPercentage()
    {
        return waterVapor + nitrogen + oxygen + argon + carbonDioxide;
    }
    
    /// <summary>
    /// Get current entity counts and total gas exchange rates (旧系统 - 仅GasExchanger)
    /// </summary>
    public void GetEcosystemStats(out int trees, out int grass, out int animals, out int humans, 
                                   out float totalO2, out float totalCO2)
    {
        trees = 0; grass = 0; animals = 0; humans = 0;
        totalO2 = 0f; totalCO2 = 0f;
        
        foreach (var exchanger in exchangers)
        {
            if (exchanger == null || !exchanger.isAlive) continue;
            
            switch (exchanger.entityType)
            {
                case GasExchanger.EntityType.Tree: trees++; break;
                case GasExchanger.EntityType.Grass: grass++; break;
                case GasExchanger.EntityType.Animal: animals++; break;
                case GasExchanger.EntityType.Human: humans++; break;
            }
            
            totalO2 += exchanger.GetCurrentO2Rate();
            totalCO2 += exchanger.GetCurrentCO2Rate();
        }
        
        // Add ocean sink
        if (oceanAbsorptionRate > 0f)
        {
            totalCO2 -= oceanAbsorptionRate;
        }
    }
    
    /// <summary>
    /// Get ecosystem stats using NEW PlantAgent system (real data)
    /// Returns actual mol/s rates, UI converts to mol/day for display
    /// </summary>
    public void GetEcosystemStatsWithPlantAgents(out int trees, out int grass, out int animals, out int humans,
                                                   out float totalO2_molPerSec, out float totalCO2_molPerSec,
                                                   out float plantPhotosynthesisO2, out float plantRespirationO2,
                                                   out float animalO2, out float animalCO2,
                                                   out float humanO2, out float humanCO2,
                                                   out float oceanCO2)
    {
        trees = 0; grass = 0; animals = 0; humans = 0;
        totalO2_molPerSec = 0f; totalCO2_molPerSec = 0f;
        
        // Initialize breakdown variables
        plantPhotosynthesisO2 = 0f;  // Photosynthesis produces O₂
        plantRespirationO2 = 0f;     // Respiration consumes O₂ (stored as negative)
        animalO2 = 0f; animalCO2 = 0f;
        humanO2 = 0f; humanCO2 = 0f;
        oceanCO2 = 0f;
        
        // Calculate PlantAgent contributions (new system - real data)
        foreach (var agent in plantAgents)
        {
            if (agent == null) continue;
            
            // Count entities
            if (agent.plantType == PlantAgent.PlantType.Tree) trees++;
            else grass++;
            
            // Separate photosynthesis and respiration (both use metabolismScale)
            // P_gross = photosynthesis rate, R_total = respiration rate
            float photosynthesisO2 = agent.P_gross * agent.metabolismScale;  // Always >= 0
            float respirationO2 = -agent.R_total * agent.metabolismScale;   // Always <= 0
            
            plantPhotosynthesisO2 += photosynthesisO2;
            plantRespirationO2 += respirationO2;
            
            // Net effect (P_net = P_gross - R_total)
            totalO2_molPerSec += photosynthesisO2 + respirationO2;
            totalCO2_molPerSec += -(photosynthesisO2 + respirationO2);
        }
        
        // Debug log plant contributions (every 120 frames)
        if (Time.frameCount % 120 == 0 && plantAgents.Count > 0)
        {
            float netO2 = plantPhotosynthesisO2 + plantRespirationO2;
            //Debug.Log($"[Statistics] {plantAgents.Count} PlantAgents | Photosynthesis: {plantPhotosynthesisO2:F8} mol/s | " +
            //          $"Respiration: {plantRespirationO2:F8} mol/s | Net O₂: {netO2:F8} mol/s ({netO2 * secondsPerDay:F4} mol/day)");
        }
        
        // Calculate AnimalMetabolism contributions (NEW scientific system)
        foreach (var animal in animalAgents)
        {
            if (animal == null || !animal.isAlive) continue;
            
            animals++;
            // totalRespiration is already in mol/s
            float o2RatePerSec = -animal.totalRespiration * animal.metabolismScale;  // Negative = consumption
            float co2RatePerSec = animal.totalRespiration * animal.metabolismScale;   // Positive = production
            
            animalO2 += o2RatePerSec;
            animalCO2 += co2RatePerSec;
            
            totalO2_molPerSec += o2RatePerSec;
            totalCO2_molPerSec += co2RatePerSec;
        }
        
        // Calculate HumanMetabolism contributions (NEW scientific system)
        foreach (var human in humanAgents)
        {
            if (human == null || !human.isAlive) continue;
            
            humans++;
            // totalRespiration is already in mol/s
            float o2RatePerSec = -human.totalRespiration * human.metabolismScale;  // Negative = consumption
            float co2RatePerSec = human.totalRespiration * human.metabolismScale;   // Positive = production
            
            humanO2 += o2RatePerSec;
            humanCO2 += co2RatePerSec;
            
            totalO2_molPerSec += o2RatePerSec;
            totalCO2_molPerSec += co2RatePerSec;
        }
        
        // Debug log animal/human contributions (every 120 frames)
        if (Time.frameCount % 120 == 0 && (animalAgents.Count > 0 || humanAgents.Count > 0))
        {
            //Debug.Log($"[Statistics] Animals={animals} (O₂: {animalO2:F8} mol/s, CO₂: {animalCO2:F8} mol/s) | " +
            //          $"Humans={humans} (O₂: {humanO2:F8} mol/s, CO₂: {humanCO2:F8} mol/s)");
        }
        
        // Legacy GasExchanger system removed - now using PlantAgent/AnimalMetabolism/HumanMetabolism
        // If any entities still have GasExchanger components, they should be removed to avoid double-counting
        
        // Add ocean absorption (mol/day → mol/s)
        if (oceanAbsorptionRate > 0f)
        {
            oceanCO2 = -oceanAbsorptionRate / secondsPerDay;  // Negative = absorption
            totalCO2_molPerSec += oceanCO2;
        }
        
        // Log combined totals from all systems
        //Debug.Log($"[COMBINED ECOSYSTEM TOTALS] O₂: {totalO2_molPerSec:F8} mol/s ({totalO2_molPerSec * secondsPerDay:F4} mol/day) | " +
        //          $"CO₂: {totalCO2_molPerSec:F8} mol/s ({totalCO2_molPerSec * secondsPerDay:F4} mol/day)");
    }
    
    /// <summary>
    /// Check environmental limits and warn about dangerous conditions
    /// </summary>
    void CheckEnvironmentalLimits()
    {
        EnvironmentalStatus previousStatus = environmentalStatus;
        environmentalStatus = EnvironmentalStatus.Healthy;
        
        // Check O₂ levels
        if (oxygen < oxygenCriticalThreshold)
        {
            environmentalStatus = EnvironmentalStatus.Critical;
            if (previousStatus != EnvironmentalStatus.Critical)
            {
                //Debug.LogError($"[Atmosphere] ⚠️ CRITICAL: O₂ at {oxygen:F2}% - LETHAL LEVELS! Life cannot survive!");
            }
        }
        else if (oxygen < oxygenDangerThreshold)
        {
            if (environmentalStatus < EnvironmentalStatus.Danger)
                environmentalStatus = EnvironmentalStatus.Danger;
            
            if (previousStatus != EnvironmentalStatus.Danger && previousStatus != EnvironmentalStatus.Critical)
            {
                //Debug.LogWarning($"[Atmosphere] ⚠️ DANGER: O₂ at {oxygen:F2}% - Hypoxia! Add more plants!");
            }
        }
        else if (oxygen < oxygenWarningThreshold)
        {
            if (environmentalStatus < EnvironmentalStatus.Warning)
                environmentalStatus = EnvironmentalStatus.Warning;
            
            if (previousStatus == EnvironmentalStatus.Healthy)
            {
                //Debug.LogWarning($"[Atmosphere] ⚠️ WARNING: O₂ at {oxygen:F2}% - Below normal (19-21%)");
            }
        }
        
        // Check CO₂ levels
        if (carbonDioxide > co2CriticalThreshold)
        {
            environmentalStatus = EnvironmentalStatus.Critical;
            if (previousStatus != EnvironmentalStatus.Critical)
            {
                Debug.LogError($"[Atmosphere] ⚠️ CRITICAL: CO₂ at {carbonDioxide:F2}% - TOXIC! Life cannot survive!");
            }
        }
        else if (carbonDioxide > co2DangerThreshold)
        {
            if (environmentalStatus < EnvironmentalStatus.Danger)
                environmentalStatus = EnvironmentalStatus.Danger;
            
            if (previousStatus != EnvironmentalStatus.Danger && previousStatus != EnvironmentalStatus.Critical)
            {
                Debug.LogWarning($"[Atmosphere] ⚠️ DANGER: CO₂ at {carbonDioxide:F2}% - Dangerous levels! Reduce animals or add plants!");
            }
        }
        else if (carbonDioxide > co2WarningThreshold)
        {
            if (environmentalStatus < EnvironmentalStatus.Warning)
                environmentalStatus = EnvironmentalStatus.Warning;
            
            if (previousStatus == EnvironmentalStatus.Healthy)
            {
                Debug.LogWarning($"[Atmosphere] ⚠️ WARNING: CO₂ at {carbonDioxide:F2}% - Above normal (< 0.1%)");
            }
        }
        
        // Log status change to healthy
        if (environmentalStatus == EnvironmentalStatus.Healthy && previousStatus != EnvironmentalStatus.Healthy)
        {
            Debug.Log($"[Atmosphere] ✅ HEALTHY: Atmosphere returned to normal levels (O₂: {oxygen:F2}%, CO₂: {carbonDioxide:F4}%)");
        }
    }
    
    /// <summary>
    /// Get human-readable environmental status message
    /// </summary>
    public string GetEnvironmentalStatusMessage()
    {
        switch (environmentalStatus)
        {
            case EnvironmentalStatus.Healthy:
                return "✅ Healthy - All parameters normal";
            
            case EnvironmentalStatus.Warning:
                string warnings = "";
                if (oxygen < oxygenWarningThreshold)
                    warnings += $"Low O₂ ({oxygen:F2}%) ";
                if (carbonDioxide > co2WarningThreshold)
                    warnings += $"High CO₂ ({carbonDioxide:F4}%) ";
                return $"⚠️ Warning - {warnings}";
            
            case EnvironmentalStatus.Danger:
                string dangers = "";
                if (oxygen < oxygenDangerThreshold)
                    dangers += $"Hypoxia! O₂={oxygen:F2}% ";
                if (carbonDioxide > co2DangerThreshold)
                    dangers += $"CO₂ Toxicity! CO₂={carbonDioxide:F2}% ";
                return $"⚠️ DANGER - {dangers}";
            
            case EnvironmentalStatus.Critical:
                string critical = "";
                if (oxygen < oxygenCriticalThreshold)
                    critical += $"Lethal O₂! ({oxygen:F2}%) ";
                if (carbonDioxide > co2CriticalThreshold)
                    critical += $"Lethal CO₂! ({carbonDioxide:F2}%) ";
                return $"💀 CRITICAL - {critical}";
            
            default:
                return "Unknown status";
        }
    }
}
