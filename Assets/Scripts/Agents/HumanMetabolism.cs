using UnityEngine;

/// <summary>
/// Scientific metabolism for humans (carnivores/omnivores).
/// Larger body mass, higher metabolism, hunts animals, tool use.
/// Based on: M_total = M_Base × C_temp × C_thermoreg × C_activity
/// </summary>
public class HumanMetabolism : MonoBehaviour
{
    [Header("Physical Properties")]
    [Tooltip("Body mass in kg (adult human)")]
    public float biomass = 70f;  // Average adult human
    public float maxBiomass = 100f;
    
    [Header("Basal Metabolism")]
    [Tooltip("Base O₂ consumption at rest (mol/s/kg at 20°C)")]
    public float basalMetabolicRate = 0.0025f;  // Increased 100× for visibility (0.000025 → 0.0025)
    
    [Tooltip("Q10 temperature coefficient (humans less affected)")]
    [Range(1.2f, 2.0f)]
    public float Q10_factor = 1.5f;  // Lower - better thermoregulation
    
    [Tooltip("Comfort temperature (°C)")]
    public float comfortTemperature = 24f;
    
    [Header("Activity")]
    public ActivityState currentActivity = ActivityState.Resting;
    
    public enum ActivityState
    {
        Resting,    // 1.0× (sleeping)
        Walking,    // 1.5× (normal movement)
        Working,    // 2.0× (building, crafting)
        Hunting     // 3.5× (active pursuit)
    }
    
    [Header("Metabolism Scale")]
    [Tooltip("Global metabolism rate scaling factor (matches PlantAgent)")]
    public float metabolismScale = 1.0f;  // Increased 10× (0.1 → 1.0) for visible atmosphere impact
    
    [Header("Hunting Behavior")]
    [Tooltip("Search radius to find prey")]
    public float searchRadius = 5f;    // Larger search radius
    [Tooltip("How often to search for food (seconds)")]
    public float searchInterval = 5f;
    [Tooltip("Trophic efficiency (15% energy transfer)")]
    [Range(0.1f, 0.3f)]
    public float trophicEfficiency = 0.15f;  // Better digestion
    
    [Header("Status")]
    public bool isAlive = true;
    public float hungerThreshold = 40f;
    
    [Header("Debug Info")]
    public float totalRespiration = 0f;  // mol O₂/s
    
    // Internal
    private float searchTimer = 0f;
    private SunMoonController sunMoon;
    private AtmosphereManager atmosphere;
    private float o2Accumulator = 0f;
    private float co2Accumulator = 0f;
    private const float ACCUMULATOR_THRESHOLD = 0.01f;
    
    void Start()
    {
        sunMoon = FindAnyObjectByType<SunMoonController>();
        atmosphere = AtmosphereManager.Instance;
        searchTimer = Random.Range(0f, searchInterval);
        
        if (atmosphere != null)
        {
            atmosphere.RegisterHumanAgent(this);
            //Debug.Log($"[HumanMetabolism] {gameObject.name} registered with {biomass:F1} kg biomass");
        }
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        CalculateMetabolism();
        ProcessGasExchange();
        
        if (biomass < hungerThreshold)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                currentActivity = ActivityState.Hunting;
                TryHuntAnimal();
                currentActivity = ActivityState.Resting;
            }
        }
        
        if (biomass <= 0f) Die();
    }
    
    void CalculateMetabolism()
    {
        if (atmosphere == null || sunMoon == null) return;
        
        float localTemp = sunMoon.currentTemperature;
        
        // A. Basal Metabolism: M_Base = basalMetabolicRate × biomass (higher base rate than animals)
        float M_base = basalMetabolicRate * biomass;
        
        // B. Q10 temperature response (weaker for humans - clothing/shelter)
        float deltaT = localTemp - 20f;
        float C_temp = Mathf.Pow(Q10_factor, deltaT / 10f);
        
        // C. Thermoregulation (humans better at this)
        float tempStress = Mathf.Abs(localTemp - comfortTemperature);
        float C_thermoreg = 1f + (tempStress * 0.02f);  // +2% per °C (vs 3% for animals)
        
        // D. Activity modifier (humans can sustain higher activity)
        float C_activity = GetActivityMultiplier();
        
        // Total Metabolism: M_total = M_Base × C_temp × C_thermoreg × C_activity
        totalRespiration = M_base * C_temp * C_thermoreg * C_activity;
        
        // Debug log metabolism calculation (every 120 frames)
        if (Time.frameCount % 120 == 0)
        {
            //Debug.Log($"[HumanMetabolism] {gameObject.name} | Temp={localTemp:F1}°C | " +
            //          $"M_base={M_base:F8} | C_temp={C_temp:F3} | C_thermoreg={C_thermoreg:F3} | C_activity={C_activity:F1} | " +
            //          $"totalRespiration={totalRespiration:F8} mol/s | biomass={biomass:F1} kg");
        }
        
        // Biomass loss (energy consumed)
        float energyLoss = totalRespiration * Time.deltaTime * metabolismScale;
        biomass -= energyLoss;
        biomass = Mathf.Clamp(biomass, 0f, maxBiomass);
    }
    
    float GetActivityMultiplier()
    {
        switch (currentActivity)
        {
            case ActivityState.Resting: return 1.0f;
            case ActivityState.Walking: return 1.5f;
            case ActivityState.Working: return 2.0f;
            case ActivityState.Hunting: return 3.5f;
            default: return 1.0f;
        }
    }
    
    void ProcessGasExchange()
    {
        if (atmosphere == null) return;
        
        // O₂ consumption and CO₂ production (1:1 ratio for aerobic respiration)
        float o2Change = -totalRespiration * Time.deltaTime * metabolismScale;
        float co2Change = totalRespiration * Time.deltaTime * metabolismScale;
        
        // Accumulate changes to avoid float precision loss
        o2Accumulator += o2Change;
        co2Accumulator += co2Change;
        
        // Apply when threshold reached
        if (Mathf.Abs(o2Accumulator) >= ACCUMULATOR_THRESHOLD)
        {
            atmosphere.oxygenMoles += o2Accumulator;
            //Debug.Log($"[HumanMetabolism] {gameObject.name} applied O₂: {o2Accumulator:F4} mol (consumed), CO₂: {co2Accumulator:F4} mol (produced)");
            o2Accumulator = 0f;
        }
        
        if (Mathf.Abs(co2Accumulator) >= ACCUMULATOR_THRESHOLD)
        {
            atmosphere.carbonDioxideMoles += co2Accumulator;
            co2Accumulator = 0f;
        }
        
        // Debug accumulator status (every 180 frames)
        if (Time.frameCount % 180 == 0)
        {
            //Debug.Log($"[HumanMetabolism] {gameObject.name} accumulators: O₂={o2Accumulator:F6} mol, CO₂={co2Accumulator:F6} mol (threshold={ACCUMULATOR_THRESHOLD})");
        }
    }
    
    void TryHuntAnimal()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        
        AnimalMetabolism nearestAnimal = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D col in colliders)
        {
            AnimalMetabolism animal = col.GetComponent<AnimalMetabolism>();
            if (animal != null && animal.isAlive)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestAnimal = animal;
                }
            }
        }
        
        if (nearestAnimal != null)
        {
            float biomassTaken = nearestAnimal.biomass;
            nearestAnimal.Die();  // Humans kill efficiently with tools
            
            float biomassGained = biomassTaken * trophicEfficiency;
            biomass += biomassGained;
            biomass = Mathf.Clamp(biomass, 0f, maxBiomass);
            
            //Debug.Log($"[HumanMetabolism] {gameObject.name} hunted animal, gained {biomassGained:F1} kg biomass");
        }
    }
    
    public void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        
        if (atmosphere != null)
        {
            atmosphere.oxygenMoles += o2Accumulator;
            atmosphere.carbonDioxideMoles += co2Accumulator;
            atmosphere.UnregisterHumanAgent(this);
        }
        
        //Debug.Log($"[HumanMetabolism] {gameObject.name} died (starvation)! Biomass: {biomass:F1} kg");
        
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        
        Destroy(gameObject, 3f);
    }
    
    void OnDestroy()
    {
        if (atmosphere != null && isAlive)
        {
            atmosphere.oxygenMoles += o2Accumulator;
            atmosphere.carbonDioxideMoles += co2Accumulator;
            atmosphere.UnregisterHumanAgent(this);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = biomass < hungerThreshold ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}
