using UnityEngine;
using System.Collections;

/// <summary>
/// Scientific metabolism for humans (carnivores/omnivores).
/// Larger body mass, higher metabolism, hunts animals, tool use.
/// Based on: M_total = M_Base × C_temp × C_thermoreg × C_activity
/// </summary>
public class HumanMetabolism : MonoBehaviour
{
    [Header("Physical Properties")]
    [Tooltip("Body mass in kg (adult human)")]
    public float biomass = 70f;  // Average adult human
    public float maxBiomass = 100f;
    
    [Header("Hunger System")]
    [Tooltip("Current hunger level (0-100)")]
    public float hunger = 50f;
    public float maxHunger = 100f;
    [Tooltip("Hunger depletion rate per second")]
    public float hungerDepletionRate = 8.31f;  // Adjusted for 720× time compression (0.15 × 55.4)
    [Tooltip("Hunger percentage to start searching for food")]
    [Range(0f, 100f)]
    public float hungerSearchThreshold = 40f;
    [Tooltip("Hunger gain from eating one animal")]
    public float hungerGainPerAnimal = 90f;
    
    [Header("Basal Metabolism")]
    [Tooltip("Base O₂ consumption at rest (mol/s/kg at 20°C). CORRECTED RATE.")]
    public float basalMetabolicRate = 0.018f;  // <--- CORRECTED VALUE (was 0.18f)
    
    [Tooltip("Q10 temperature coefficient (humans less affected)")]
    [Range(1.2f, 2.0f)]
    public float Q10_factor = 1.5f;  // Lower - better thermoregulation
    
    [Tooltip("Comfort temperature (°C)")]
    public float comfortTemperature = 24f;
    
    [Header("Activity")]
    public ActivityState currentActivity = ActivityState.Resting;
    
    public enum ActivityState
    {
        Sleeping,   // 0.75× (nighttime sleep - reduced metabolism)
        Resting,    // 1.0× (awake but not moving)
        Walking,    // 1.5× (normal movement)
        Working,    // 2.0× (building, crafting)
        Hunting     // 3.5× (active pursuit)
    }
    
    [Header("Metabolism Scale")]
    [Tooltip("Global metabolism rate scaling factor (matches PlantAgent)")]
    public float metabolismScale = 1.0f;  // Used as the Simulation Speed Multiplier (720x is baked into the BMR)
    
    [Header("Hunting Behavior")]
    [Tooltip("Search radius to find prey")]
    public float searchRadius = 5f;    // Larger search radius
    [Tooltip("How often to search for food (seconds)")]
    public float searchInterval = 5f;
    [Tooltip("Trophic efficiency (15% energy transfer)")]
    [Range(0.1f, 0.3f)]
    public float trophicEfficiency = 0.15f;  // Better digestion
    
    [Header("Status")]
    public bool isAlive = true;
    
    [Header("Movement")]
    [Tooltip("Let HumanAgent handle movement (disable metabolism movement)")]
    public bool useOwnMovement = false;  // DISABLED - HumanAgent handles movement now
    public float moveSpeed = 1.5f;
    public float wanderRadius = 3f;
    public float eatingRange = 0.6f;
    private Vector2 wanderTarget;
    private float wanderTimer = 0f;
    public float wanderInterval = 3f;
    private AnimalMetabolism targetAnimal = null;
    
    [Header("Debug Info")]
    public float totalRespiration = 0f;  // mol O₂/s
    
    // Internal
    private float searchTimer = 0f;
    private SunMoonController sunMoon;
    private AtmosphereManager atmosphere;
    private float o2Accumulator = 0f;
    private float co2Accumulator = 0f;
    private const float ACCUMULATOR_THRESHOLD = 0.001f;  // Lowered for faster response with many entities
    private int animalID;
    private static int nextAnimalID = 1;
    
    void Start()
    {
        sunMoon = FindAnyObjectByType<SunMoonController>();
        atmosphere = AtmosphereManager.Instance;
        searchTimer = Random.Range(0f, searchInterval);
        // initialize wandering
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
        wanderTimer = Random.Range(0f, wanderInterval);
        
        // Assign unique ID
        animalID = nextAnimalID++;
        gameObject.name = $"Human({animalID})";
        
        if (atmosphere != null)
        {
            atmosphere.RegisterHumanAgent(this);
            //Debug.Log($"[HumanMetabolism] {gameObject.name} registered with {biomass:F1} kg biomass");
        }
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        // Check if it's nighttime
        bool isNightTime = (sunMoon != null && sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night);
        
        BurnHunger();
        CalculateMetabolism();
        ProcessGasExchange();
        
        // Wake up mechanic: If hungry at night (hunger < 30%), wake up to find food
        bool shouldWakeUpForFood = isNightTime && hunger < maxHunger * 0.3f;
        
        // Sleep logic: Only sleep if it's night AND not too hungry
        if (isNightTime && !shouldWakeUpForFood)
        {
            currentActivity = ActivityState.Sleeping;
            return; // Skip hunting and movement
        }
        
        // Calculate hunger for emergency hunting check
        float hungerPercent = (hunger / maxHunger) * 100f;
        bool isEmergencyHunger = hungerPercent < hungerSearchThreshold;
        
        // Allow emergency hunting even if useOwnMovement is disabled (survival priority)
        // Or if useOwnMovement is enabled (normal behavior)
        bool allowHunting = isEmergencyHunger || useOwnMovement;
        
        // MOVEMENT DISABLED - Let HumanAgent handle all movement for breeding
        // EXCEPT when emergency hunger (need to hunt to survive)
        if (!allowHunting) return;

        if (hungerPercent < hungerSearchThreshold)
        {
            // Attempt to hunt nearby animals
            currentActivity = ActivityState.Hunting;

            // If we don't have a target, search periodically
            if (targetAnimal == null)
            {
                searchTimer += Time.deltaTime;
                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
                    TryFindNearestAnimalTarget();
                }
            }

            // If we have a target, move towards it and eat when in range
            if (targetAnimal != null && targetAnimal.isAlive)
            {
                MoveTowardsAnimal(targetAnimal);
                float dist = Vector2.Distance(transform.position, targetAnimal.transform.position);
                if (dist <= eatingRange)
                {
                    EatAnimal(targetAnimal);
                    targetAnimal = null;
                    currentActivity = ActivityState.Resting;
                }
            }
            else
            {
                // No target found - wander while searching
                Wander();
            }
        }
        else
        {
            // Not hungry: wander around
            currentActivity = ActivityState.Walking;
            Wander();
        }
        
        // Die when biomass is depleted (after hunger has been exhausted and biomass is consumed)
        if (biomass <= 0f) Die();
    }
    
    void BurnHunger()
    {
        // Apply hunger reduction based on activity state
        float hungerMultiplier = 1.0f;
        if (currentActivity == ActivityState.Sleeping)
            hungerMultiplier = 0.1f;  // 90% reduction during sleep (sleeping slows hunger)
        else if (currentActivity == ActivityState.Resting)
            hungerMultiplier = 0.5f;  // 50% reduction when awake but still
        
        float hungerBurn = hungerDepletionRate * hungerMultiplier * Time.deltaTime;

        if (hunger > 0f)
        {
            // Tank has fuel - burn hunger
            hunger -= hungerBurn;
            hunger = Mathf.Max(0f, hunger);

            // Notify when starving threshold crossed (below 20%) occasionally
            if (hunger < maxHunger * 0.2f && Time.frameCount % 300 == 0)
            {
                if (EventNotificationUI.Instance != null)
                {
                    EventNotificationUI.Instance.NotifyHumanStarving(gameObject.name);
                }
            }
        }
        else
        {
            // Hunger is zero - burn biomass to survive
            
            // If sleeping, burn biomass much slower (0.1× rate)
            float starvationMultiplier = 1.0f;
            if (currentActivity == ActivityState.Sleeping)
                starvationMultiplier = 0.1f;  // 90% slower biomass burn during sleep
            
            // Hunger empty and awake: consume biomass slowly
            // Hunger Change = -(Depletion Rate * A_Hunger) * Δt (This is hungerBurn)
            float biomassBurn = hungerBurn * 0.5f * starvationMultiplier;
            biomass -= biomassBurn;
            biomass = Mathf.Max(0f, biomass);

            // ⚠️ CRITICAL FIX: CHECK FOR DEATH IMMEDIATELY AFTER BIOMASS BURN
            if (biomass <= 0f) 
            {
                Die(); 
                return; // Exit if dead, prevents unnecessary logging/updates
            }

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[Human] {gameObject.name} STARVING! Hunger empty, burning biomass: {biomass:F1} kg left");
                // Notify UI about starvation while burning biomass
                if (EventNotificationUI.Instance != null)
                {
                    EventNotificationUI.Instance.NotifyHumanStarving(gameObject.name);
                }
            }
        }
    }
    
    void CalculateMetabolism()
    {
        if (atmosphere == null || sunMoon == null) return;
        
        float localTemp = sunMoon.currentTemperature;
        
        // A. Basal Metabolism: M_Base = basalMetabolicRate × biomass 
        // basalMetabolicRate now includes the BMR Adjustment Factor (A_BMR)
        float M_base = basalMetabolicRate * biomass;
        
        // B. Q10 temperature response (weaker for humans - clothing/shelter)
        float deltaT = localTemp - 20f;
        float C_temp = Mathf.Pow(Q10_factor, deltaT / 10f);
        
        // C. Thermoregulation (humans better at this)
        float tempStress = Mathf.Abs(localTemp - comfortTemperature);
        float C_thermoreg = 1f + (tempStress * 0.02f);  // +2% per °C (vs 3% for animals)
        
        // D. Activity modifier (humans can sustain higher activity)
        float C_activity = GetActivityMultiplier();
        
        // Total Metabolism: M_total = M_Base × C_temp × C_thermoreg × C_activity
        totalRespiration = M_base * C_temp * C_thermoreg * C_activity;
        
        // Debug log metabolism calculation (every 120 frames)
        if (Time.frameCount % 120 == 0)
        {
            //Debug.Log($"[HumanMetabolism] {gameObject.name} | Temp={localTemp:F1}°C | " +
            //          $"M_base={M_base:F8} | C_temp={C_temp:F3} | C_thermoreg={C_thermoreg:F3} | C_activity={C_activity:F1} | " +
            //          $"totalRespiration={totalRespiration:F8} mol/s | biomass={biomass:F1} kg");
        }
        
        // **REMOVED:** The extraneous biomass loss logic that causes double-dipping.
        // Energy loss (biomass burn) is now correctly handled ONLY in BurnHunger() when starving.
    }
    
    float GetActivityMultiplier()
    {
        switch (currentActivity)
        {
            case ActivityState.Sleeping: return 0.75f;  // 25% metabolic reduction during sleep
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
        // O₂ Change = -M_total × Δt × metabolismScale
        // CO₂ Change = +M_total × Δt × metabolismScale
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
    
    // Original TryHuntAnimal() method removed as it did not handle movement/range
    // Keeping TryFindNearestAnimalTarget and EatAnimal and MoveTowardsAnimal
    
    void TryFindNearestAnimalTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        AnimalMetabolism nearest = null;
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
                    nearest = animal;
                }
            }
        }

        targetAnimal = nearest;
    }

    void MoveTowardsAnimal(AnimalMetabolism animal)
    {
        if (animal == null) return;
        Vector2 targetPos = animal.transform.position;
        float step = moveSpeed * Time.deltaTime;
        Vector2 newPos = Vector2.MoveTowards(transform.position, targetPos, step);
        transform.position = WorldBounds.ClampToLand(newPos);
    }

    // Eat the provided animal (handles biomass gain, hunger restore, notification and respawn trigger)
    public void EatAnimal(AnimalMetabolism animal)
    {
        if (animal == null) return;
        string animalName = animal.gameObject.name;
        float biomassTaken = animal.biomass;

        // Kill the animal
        animal.Die();

        // Gain biomass from eating
        float biomassGained = biomassTaken * trophicEfficiency;
        biomass += biomassGained;
        biomass = Mathf.Clamp(biomass, 0f, maxBiomass);

        // Restore hunger
        hunger += hungerGainPerAnimal;
        hunger = Mathf.Clamp(hunger, 0f, maxHunger);

        // Notify about hunting
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.NotifyHumanHunt(gameObject.name, animalName);
        }

        // Trigger animal reproduction to balance ecosystem after a delay (10s)
        StartCoroutine(DelayedAnimalReproduction(10f));

        Debug.Log($"[HumanMetabolism] {gameObject.name} hunted {animalName}, gained {biomassGained:F1} kg biomass, +{hungerGainPerAnimal} hunger");
    }

    IEnumerator DelayedAnimalReproduction(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AnimalRespawnManager.Instance != null)
        {
            AnimalRespawnManager.Instance.TriggerAnimalReproduction();
        }
    }

    void Wander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;
            // Use existing helper to find a land target
            SetNewWanderTarget(); 
        }

        float step = (moveSpeed * 0.6f) * Time.deltaTime; // slower wandering
        Vector2 newPos = Vector2.MoveTowards(transform.position, wanderTarget, step);
        transform.position = WorldBounds.ClampToLand(newPos);
    }
    
    // Needed by Wander()
    void SetNewWanderTarget()
    {
        // Simple placeholder for WorldBounds check (assuming WorldBounds is defined elsewhere)
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
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
        
        // Notify about death
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.NotifyHumanDeath(gameObject.name);
        }
        
        Debug.Log($"[HumanMetabolism] {gameObject.name} died (starvation)! Hunger: {hunger:F1}, Biomass: {biomass:F1} kg");
        
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
        float hungerPercent = (hunger / maxHunger) * 100f;
        Gizmos.color = hungerPercent < hungerSearchThreshold ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}