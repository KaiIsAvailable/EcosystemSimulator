using UnityEngine;

/// <summary>
/// Scientific metabolism for herbivorous animals.
/// Smaller body mass, temperature-sensitive, eats plants.
/// Based on: M_total = M_Base × C_temp × C_thermoreg × C_activity
/// </summary>
public class AnimalMetabolism : MonoBehaviour
{
    [Header("Physical Properties")]
    [Tooltip("Body mass in kg")]
    public float biomass = 30f;  // Smaller animals (deer, rabbit)
    public float maxBiomass = 60f;
    
    [Header("Basal Metabolism")]
    [Tooltip("Base O₂ consumption at rest (mol/s/kg at 20°C)")]
    public float basalMetabolicRate = 0.002f;  // Increased 100× for visibility (0.00002 → 0.002)
    
    [Tooltip("Q10 temperature coefficient")]
    [Range(1.5f, 2.5f)]
    public float Q10_factor = 2.0f;
    
    [Tooltip("Comfort temperature (°C)")]
    public float comfortTemperature = 22f;
    
    [Header("Activity")]
    public ActivityState currentActivity = ActivityState.Resting;
    
    public enum ActivityState
    {
        Resting,   // 1.0× (sleeping, standing)
        Grazing,   // 1.2× (eating plants)
        Walking,   // 1.5× (normal movement)
        Fleeing    // 3.0× (running from predators)
    }
    
    [Header("Metabolism Scale")]
    [Tooltip("Global metabolism rate scaling factor (matches PlantAgent)")]
    public float metabolismScale = 1.0f;  // Increased 10× (0.1 → 1.0) for visible atmosphere impact
    
    [Header("Eating Behavior")]
    [Tooltip("How much biomass to consume when eating")]
    public float eatingAmount = 5f;
    [Tooltip("Search radius to find food")]
    public float searchRadius = 3f;
    [Tooltip("How often to search for food (seconds)")]
    public float searchInterval = 3f;
    [Tooltip("Trophic efficiency (10% energy transfer)")]
    [Range(0.05f, 0.2f)]
    public float trophicEfficiency = 0.1f;
    
    [Header("Status")]
    public bool isAlive = true;
    public float hungerThreshold = 20f;
    
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
            atmosphere.RegisterAnimalAgent(this);
            //Debug.Log($"[AnimalMetabolism] {gameObject.name} registered with {biomass:F1} kg biomass");
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
                currentActivity = ActivityState.Grazing;
                TryEatPlant();
                currentActivity = ActivityState.Resting;
            }
        }
        
        if (biomass <= 0f) Die();
    }
    
    void CalculateMetabolism()
    {
        if (atmosphere == null || sunMoon == null) return;
        
        float localTemp = sunMoon.currentTemperature;
        
        // A. Basal Metabolism: M_Base = basalMetabolicRate × biomass
        float M_base = basalMetabolicRate * biomass;
        
        // B. Q10 temperature response
        float deltaT = localTemp - 20f;
        float C_temp = Mathf.Pow(Q10_factor, deltaT / 10f);
        
        // C. Thermoregulation cost (animals maintain body temp)
        float tempStress = Mathf.Abs(localTemp - comfortTemperature);
        float C_thermoreg = 1f + (tempStress * 0.03f);  // +3% per °C
        
        // D. Activity modifier
        float C_activity = GetActivityMultiplier();
        
        // Total Metabolism: M_total = M_Base × C_temp × C_thermoreg × C_activity
        totalRespiration = M_base * C_temp * C_thermoreg * C_activity;
        
        // Debug log metabolism calculation (every 120 frames)
        if (Time.frameCount % 120 == 0)
        {
            //Debug.Log($"[AnimalMetabolism] {gameObject.name} | Temp={localTemp:F1}°C | " +
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
            case ActivityState.Grazing: return 1.2f;
            case ActivityState.Walking: return 1.5f;
            case ActivityState.Fleeing: return 3.0f;
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
            //Debug.Log($"[AnimalMetabolism] {gameObject.name} applied O₂: {o2Accumulator:F4} mol (consumed), CO₂: {co2Accumulator:F4} mol (produced)");
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
            //Debug.Log($"[AnimalMetabolism] {gameObject.name} accumulators: O₂={o2Accumulator:F6} mol, CO₂={co2Accumulator:F6} mol (threshold={ACCUMULATOR_THRESHOLD})");
        }
    }
    
    void TryEatPlant()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        
        PlantAgent nearestPlant = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D col in colliders)
        {
            PlantAgent plant = col.GetComponent<PlantAgent>();
            if (plant != null && plant.biomass > 5f)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlant = plant;
                }
            }
        }
        
        if (nearestPlant != null)
        {
            float biomassTaken = Mathf.Min(eatingAmount, nearestPlant.biomass);
            nearestPlant.biomass -= biomassTaken;
            
            float biomassGained = biomassTaken * trophicEfficiency;
            biomass += biomassGained;
            biomass = Mathf.Clamp(biomass, 0f, maxBiomass);
            
            if (Time.frameCount % 60 == 0)
            {
                //Debug.Log($"[AnimalMetabolism] {gameObject.name} ate {biomassTaken:F1} kg from {nearestPlant.gameObject.name}, gained {biomassGained:F1} kg");
            }
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
            atmosphere.UnregisterAnimalAgent(this);
        }
        
        //Debug.Log($"[AnimalMetabolism] {gameObject.name} died (starvation)! Biomass: {biomass:F1} kg");
        
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
            atmosphere.UnregisterAnimalAgent(this);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = biomass < hungerThreshold ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}
