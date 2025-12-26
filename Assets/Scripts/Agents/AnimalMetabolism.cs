using UnityEngine;

/// <summary>
/// Scientific metabolism for herbivorous animals.
/// Smaller body mass, temperature-sensitive, eats plants.
/// Based on: M_total = M_Base × C_temp × C_thermoreg × C_activity
/// </summary>
public class AnimalMetabolism : MonoBehaviour
{
    [Header("Physical Properties")]
    [Tooltip("Body mass in kg (the car frame)")]
    public float biomass = 30f;  // Smaller animals (deer, rabbit)
    public float maxBiomass = 60f;
    
    [Header("Hunger System (Gas Tank)")]
    [Tooltip("Current hunger level (fuel in tank)")]
    public float hunger = 50f;
    [Tooltip("Maximum hunger capacity (tank size)")]
    public float maxHunger = 100f;
    [Tooltip("Hunger depletes every second (engine always burns gas)")]
    public float hungerDepletionRate = 8.0f;  // Adjusted for 720× time compression (0.2 × 40)
    
    [Header("Basal Metabolism")]
    [Tooltip("Base O₂ consumption at rest (mol/s/kg at 20°C)")]
    public float basalMetabolicRate = 0.0144f;  // Adjusted for 720× time compression (0.002 × 72)
    
    [Tooltip("Q10 temperature coefficient")]
    [Range(1.5f, 2.5f)]
    public float Q10_factor = 2.0f;
    
    [Tooltip("Comfort temperature (°C)")]
    public float comfortTemperature = 22f;
    
    [Header("Activity")]
    public ActivityState currentActivity = ActivityState.Resting;

    private float starvationTimer = 0f;
    [Tooltip("Seconds the animal can survive with 0 hunger before biomass drops")]
    public float starvationGracePeriod = 1.0f; // 1 second grace period
    
    public enum ActivityState
    {
        Sleeping,  // 0.75× (nighttime sleep - reduced metabolism)
        Resting,   // 1.0× (awake but not moving)
        Grazing,   // 1.2× (eating plants)
        Walking,   // 1.5× (normal movement)
        Fleeing    // 3.0× (running from predators)
    }
    
    [Header("Metabolism Scale")]
    [Tooltip("Global metabolism rate scaling factor (matches PlantAgent)")]
    public float metabolismScale = 1.0f;  // Increased 10× (0.1 → 1.0) for visible atmosphere impact
    
    [Header("Eating Behavior")]
    [Tooltip("How much biomass to consume when eating")]
    public float eatingAmount = 10f;  // Eat 10kg per bite
    [Tooltip("How often to search for food (seconds)")]
    public float searchInterval = 3f;
    [Tooltip("Trophic efficiency (100% means eating 10kg grass = 10 hunger, but capped at maxHunger)")]
    [Range(0.05f, 1.0f)]
    public float trophicEfficiency = 1.0f;  // 100% efficiency: eating 10kg grass = 10 hunger, but hunger gain is +50
    
    [Header("Movement")]
    [Tooltip("How fast the animal moves")]
    public float moveSpeed = 1.5f;
    [Tooltip("How close to get before eating (eating range)")]
    public float eatingRange = 0.3f;
    [Tooltip("Large search radius to find food from far away")]
    public float visionRadius = 10f;
    [Tooltip("Wandering enabled when not hungry")]
    public bool enableWandering = true;
    [Tooltip("How far to wander randomly")]
    public float wanderRadius = 5f;
    
    [Header("Status")]
    public bool isAlive = true;
    
    [Header("Identification")]
    public int animalID = 0;
    private static int nextAnimalID = 1;
    
    [Header("Debug Info")]
    public float totalRespiration = 0f;  // mol O₂/s
    
    // Internal
    private float searchTimer = 0f;
    private SunMoonController sunMoon;
    private AtmosphereManager atmosphere;
    private float o2Accumulator = 0f;
    private float co2Accumulator = 0f;
    private const float ACCUMULATOR_THRESHOLD = 0.001f;  // Lowered for faster response with many entities
    private PlantAgent targetPlant = null;  // Plant the animal is moving towards
    private Vector2 wanderTarget;  // Random position to wander to
    private float wanderTimer = 0f;
    private float wanderInterval = 5f;  // Change wander direction every 5 seconds
    
    void Start()
    {
        // Assign unique ID to this animal
        animalID = nextAnimalID++;
        gameObject.name = $"Animal({animalID})";
        
        sunMoon = FindAnyObjectByType<SunMoonController>();
        atmosphere = AtmosphereManager.Instance;
        searchTimer = Random.Range(0f, searchInterval);
        wanderTimer = Random.Range(0f, wanderInterval);
        SetNewWanderTarget();
        
        if (atmosphere != null)
        {
            atmosphere.RegisterAnimalAgent(this);
            //Debug.Log($"[AnimalMetabolism] {gameObject.name} registered with {biomass:F1} kg biomass");
        }
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        // Check if it's nighttime
        bool isNightTime = (sunMoon != null && sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night);
        
        // Always burn hunger first (engine always runs)
        BurnHunger();
        
        CalculateMetabolism();
        ProcessGasExchange();
        
        // Wake up mechanic: If hungry at night (hunger < 30%), wake up to find food
        bool shouldWakeUpForFood = isNightTime && hunger < maxHunger * 0.3f;
        
        // Sleep logic: Only sleep if it's night AND not too hungry
        if (isNightTime && !shouldWakeUpForFood)
        {
            currentActivity = ActivityState.Sleeping;
            return; // Skip food searching and movement
        }
        
        // If we woke up at night due to hunger, trigger faster reproduction (balance mechanism)
        if (shouldWakeUpForFood && Time.frameCount % 600 == 0)
        {
            TriggerNighttimeReproduction();
        }
        
        // If hunger is low, search for food (works both day and night now)
        if (hunger < maxHunger * 0.5f)  // Start looking when below 50%
        {
            // Search for new target if we don't have one
            if (targetPlant == null || targetPlant.biomass < 5f)
            {
                searchTimer += Time.deltaTime;
                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
                    FindNearestPlant();
                }
            }
            
            // Move towards target plant
            if (targetPlant != null)
            {
                MoveTowardsPlant();
            }
        }
        else
        {
            // Not hungry (hunger >= 50%) - wander around
            targetPlant = null;
            
            if (enableWandering)
            {
                Wander();
            }
            else
            {
                currentActivity = ActivityState.Resting;
            }
        }
        
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
            // Tank has fuel - reset timer and burn hunger
            starvationTimer = 0f;
            hunger -= hungerBurn;
            hunger = Mathf.Max(0f, hunger);
            
            // ... (rest of hunger burn logic)
        }
        else
        {
            // Hunger is zero - burn biomass to survive
            
            // If sleeping, burn biomass much slower (0.1× rate)
            float starvationMultiplier = 1.0f;
            if (currentActivity == ActivityState.Sleeping)
                starvationMultiplier = 0.1f;  // 90% slower biomass burn during sleep
            
            // Advance starvation timer
            starvationTimer += Time.deltaTime;

            if (starvationTimer >= starvationGracePeriod)
            {
                // Grace period over: consume biomass
                float biomassBurn = hungerBurn * 0.5f * starvationMultiplier;
                biomass -= biomassBurn;
                biomass = Mathf.Max(0f, biomass);

                // CRITICAL FIX: CHECK FOR DEATH IMMEDIATELY AFTER BIOMASS BURN
                if (biomass <= 0f) 
                {
                    Die(); 
                    return;
                }
                
                // ... (rest of starvation logging/notification)
            }
            else
            {
                // Debug: Animal is hungry but still in grace period.
                // (Optional logging to confirm this state)
            }
        }
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
            //          $"totalRespiration={totalRespiration:F8} mol/s | hunger={hunger:F1}/{maxHunger} | biomass={biomass:F1} kg");
        }
        
        // Note: Biomass is now only lost when hunger = 0 (handled in BurnHunger)
        // Metabolism only affects gas exchange, not biomass directly
    }
    
    float GetActivityMultiplier()
    {
        switch (currentActivity)
        {
            case ActivityState.Sleeping: return 0.75f;  // 25% metabolic reduction during sleep
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
    
    void FindNearestPlant()
    {
        // Search in large radius to find food
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, visionRadius);
        
        PlantAgent nearestPlant = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D col in colliders)
        {
            if (col == null || col.gameObject == null) continue;
            
            PlantAgent plant = col.GetComponent<PlantAgent>();
            // Look for grass with at least 1kg biomass (lowered from 5kg)
            if (plant != null && plant.biomass >= 1f && plant.plantType == PlantAgent.PlantType.Grass)
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
            targetPlant = nearestPlant;
            currentActivity = ActivityState.Walking;
            Debug.Log($"[Animal] {gameObject.name} found grass with {nearestPlant.biomass:F1}kg at {nearestDistance:F1}m, moving towards it");
        }
    }
    
    void MoveTowardsPlant()
    {
        if (targetPlant == null || targetPlant.gameObject == null) 
        {
            targetPlant = null;
            return;
        }
        
        float distance = Vector2.Distance(transform.position, targetPlant.transform.position);
        
        // Close enough to eat
        if (distance <= eatingRange)
        {
            currentActivity = ActivityState.Grazing;
            EatPlant();
        }
        else
        {
            // Move towards plant (but stay on land)
            currentActivity = ActivityState.Walking;
            Vector2 direction = ((Vector2)targetPlant.transform.position - (Vector2)transform.position).normalized;
            Vector3 newPosition = transform.position + (Vector3)(direction * moveSpeed * Time.deltaTime);
            
            // Clamp to land boundaries (prevent walking into ocean)
            transform.position = WorldBounds.ClampToLand(newPosition);
        }
    }
    
    void EatPlant()
    {
        if (targetPlant == null || targetPlant.gameObject == null)
        {
            targetPlant = null;
            return;
        }
        
        // Eat whatever biomass is left (even if less than 5kg)
        float biomassTaken = Mathf.Min(eatingAmount, targetPlant.biomass);
        
        if (biomassTaken <= 0f)
        {
            Debug.Log($"[Animal] {gameObject.name} found empty grass, destroying it");
            Destroy(targetPlant.gameObject);
            targetPlant = null;
            return;
        }
        
        Debug.Log($"[Animal] {gameObject.name} eating {biomassTaken:F1}kg from grass (had {targetPlant.biomass:F1}kg)");
        
        targetPlant.biomass -= biomassTaken;
        
        Debug.Log($"[Animal] Grass now has {targetPlant.biomass:F1}kg biomass remaining");
        
        // Notify UI about eating with unique names
        if (EventNotificationUI.Instance != null)
        {
            string grassName = targetPlant.gameObject.name;
            EventNotificationUI.Instance.NotifyAnimalEat($"Animal({animalID})", grassName);
        }
        
        // Fixed +50 hunger gain per grass eaten (ensures animal is satisfied)
        float hungerGain = 50f;
        
        // Rule 3: Fill hunger tank first
        if (hunger < maxHunger)
        {
            float hungerSpace = maxHunger - hunger;
            float hungerFill = Mathf.Min(hungerGain, hungerSpace);
            hunger += hungerFill;
            hungerGain -= hungerFill;
            
            Debug.Log($"[Animal] {gameObject.name} ate grass: +{hungerFill:F1} hunger (now {hunger:F1}/{maxHunger})");
        }
        
        // Rule 4: Excess energy goes to biomass if hunger is full
        if (hungerGain > 0f && hunger >= maxHunger)
        {
            biomass += hungerGain * 0.1f;  // Convert excess to biomass (10% conversion)
            biomass = Mathf.Clamp(biomass, 0f, maxBiomass);
            
            Debug.Log($"[Animal] {gameObject.name} converted excess to biomass: +{hungerGain * 0.1f:F1} kg (now {biomass:F1} kg)");
        }
        
        // Destroy grass immediately after eating
        Debug.Log($"[Animal] {gameObject.name} DESTROYING grass {targetPlant.gameObject.name}");
        
        // Notify UI about grass depletion with grass name
        if (EventNotificationUI.Instance != null)
        {
            string grassName = targetPlant.gameObject.name;
            EventNotificationUI.Instance.NotifyGrassDepleted(grassName);
        }
        
        // Notify respawn manager to grow new grass
        if (GrassRespawnManager.Instance != null)
        {
            GrassRespawnManager.Instance.OnGrassEaten();
        }
        
        GameObject grassToDestroy = targetPlant.gameObject;
        targetPlant = null;
        Destroy(grassToDestroy);
    }
    
    void Wander()
    {
        // Check arrival distance (0.5f is a sensible arrival radius)
        float distance = Vector2.Distance(transform.position, wanderTarget);

        // PRIORITY: If arrived at the current target, pick a new one immediately.
        if (distance <= 0.5f)
        {
            wanderTimer = 0f; // Reset timer so it doesn't immediately fire again
            SetNewWanderTarget();
        }
        // SECONDARY: If the timer runs out, pick a new target (in case the current one was unreachable/stuck)
        else if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;
            SetNewWanderTarget();
        }

        // Increment timer for the next check
        wanderTimer += Time.deltaTime;
        
        // Move towards wander target
        if (distance > 0.5f)
        {
            currentActivity = ActivityState.Walking;
            Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
            // The movement step uses a half-speed multiplier (0.5f) when wandering
            Vector3 newPosition = transform.position + (Vector3)(direction * moveSpeed * 0.5f * Time.deltaTime); 
            
            // Clamp to land boundaries (prevent walking into ocean)
            transform.position = WorldBounds.ClampToLand(newPosition);
        }
        else
        {
            // If distance is <= 0.5f, the animal is resting/arrived.
            currentActivity = ActivityState.Resting;
        }
    }
    
    void SetNewWanderTarget()
    {
        // Pick random point within wanderRadius that stays on land
        for (int i = 0; i < 10; i++)  // Try 10 times to find valid land position
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(1f, wanderRadius);
            Vector2 candidateTarget = (Vector2)transform.position + randomDirection * randomDistance;
            
            // Check if target is on land (not in ocean)
            if (WorldBounds.IsOnLand(candidateTarget))
            {
                wanderTarget = candidateTarget;
                return;
            }
        }
        
        // Fallback: stay near current position if all attempts failed
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;
    }
    
    /// <summary>
    /// Trigger faster reproduction when animals wake up at night to find food.
    /// This balances the ecosystem by compensating for nighttime activity stress.
    /// </summary>
    void TriggerNighttimeReproduction()
    {
        if (AnimalRespawnManager.Instance != null)
        {
            // 50% chance to trigger reproduction when waking up hungry at night
            if (Random.value > 0.5f)
            {
                AnimalRespawnManager.Instance.TriggerAnimalReproduction();
                Debug.Log($"[Animal] {gameObject.name} triggered nighttime reproduction (hunger={hunger:F1})");
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
        
        Debug.Log($"[AnimalMetabolism] {gameObject.name} died (starvation)! Biomass: {biomass:F1} kg");
        
        // Notify UI about death
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.NotifyAnimalDeath($"Animal({animalID})");
        }
        
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
        // Red if hunger is low, green if hunger is good
        Gizmos.color = hunger < maxHunger * 0.3f ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
        
        // Show eating range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, eatingRange);
        
        // Draw line to target plant
        if (targetPlant != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPlant.transform.position);
        }
    }
}
