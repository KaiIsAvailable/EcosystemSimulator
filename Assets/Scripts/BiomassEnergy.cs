using UnityEngine;

/// <summary>
/// Unified Biomass/Energy/Health system for all entities in the ecosystem.
/// - Plants have BIOMASS (increased by photosynthesis, decreased by respiration and being eaten)
/// - Animals/Humans have HEALTH (decreased by respiration, increased by eating)
/// </summary>
public class BiomassEnergy : MonoBehaviour
{
    [Header("Entity Configuration")]
    public EntityType entityType;
    
    public enum EntityType
    {
        Plant,      // Trees and Grass - has biomass
        Herbivore,  // Animals - eats plants, has health
        Carnivore   // Humans - eats animals, has health
    }
    
    [Header("Biomass/Health")]
    [Tooltip("For Plants: Biomass stock (energy reserve). For Animals/Humans: Health.")]
    public float currentEnergy = 100f;
    public float maxEnergy = 100f;
    
    [Header("Energy Loss (Respiration)")]
    [Tooltip("Energy lost per second due to respiration/metabolism")]
    public float respirationLossRate = 0.1f;
    
    [Header("Energy Gain (For Plants Only)")]
    [Tooltip("Biomass gained per second during photosynthesis (day only)")]
    public float photosynthesisGainRate = 0.5f;
    
    [Header("Eating Behavior (For Animals/Humans)")]
    [Tooltip("How much energy to gain when eating")]
    public float eatingAmount = 10f;
    [Tooltip("Search radius to find food")]
    public float searchRadius = 2f;
    [Tooltip("How often to search for food (seconds)")]
    public float searchInterval = 2f;
    [Tooltip("Trophic efficiency - percentage of food energy absorbed (0.1 = 10%)")]
    [Range(0.05f, 0.5f)]
    public float trophicEfficiency = 0.15f;
    
    [Header("Status")]
    public bool isAlive = true;
    public bool isHungry = false;
    public float hungerThreshold = 50f;  // Below this, entity seeks food
    
    // Internal state
    private float searchTimer = 0f;
    private SunMoonController timeController;
    private GasExchanger gasExchanger;
    
    void Start()
    {
        timeController = FindAnyObjectByType<SunMoonController>();
        gasExchanger = GetComponent<GasExchanger>();
        
        // Randomize search timer so not all entities search at once
        searchTimer = Random.Range(0f, searchInterval);
        
        // Set initial energy
        currentEnergy = maxEnergy;
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        // Check if hungry
        isHungry = currentEnergy < hungerThreshold;
        
        switch (entityType)
        {
            case EntityType.Plant:
                UpdatePlant();
                break;
                
            case EntityType.Herbivore:
                UpdateHerbivore();
                break;
                
            case EntityType.Carnivore:
                UpdateCarnivore();
                break;
        }
        
        // Clamp energy
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        
        // Check for death
        if (currentEnergy <= 0f)
        {
            Die();
        }
    }
    
    void UpdatePlant()
    {
        // Plants gain biomass during photosynthesis (day only)
        if (IsDaytime())
        {
            currentEnergy += photosynthesisGainRate * Time.deltaTime;
        }
        
        // Plants lose biomass to respiration (24/7)
        currentEnergy -= respirationLossRate * Time.deltaTime;
    }
    
    void UpdateHerbivore()
    {
        // Animals lose health to respiration (24/7)
        currentEnergy -= respirationLossRate * Time.deltaTime;
        
        // Search for food if hungry
        if (isHungry)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                TryEatPlant();
            }
        }
    }
    
    void UpdateCarnivore()
    {
        // Humans lose health to respiration (24/7)
        currentEnergy -= respirationLossRate * Time.deltaTime;
        
        // Search for food if hungry
        if (isHungry)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                TryHuntAnimal();
            }
        }
    }
    
    void TryEatPlant()
    {
        // Find nearest plant within search radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        
        BiomassEnergy nearestPlant = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D col in colliders)
        {
            BiomassEnergy biomass = col.GetComponent<BiomassEnergy>();
            if (biomass != null && biomass.entityType == EntityType.Plant && biomass.isAlive)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance && biomass.currentEnergy > 5f)  // Don't eat dead plants
                {
                    nearestDistance = distance;
                    nearestPlant = biomass;
                }
            }
        }
        
        // Eat the plant
        if (nearestPlant != null)
        {
            EatPlant(nearestPlant);
        }
    }
    
    void EatPlant(BiomassEnergy plant)
    {
        // Take energy from plant
        float energyTaken = Mathf.Min(eatingAmount, plant.currentEnergy);
        plant.currentEnergy -= energyTaken;
        
        // Gain energy with trophic efficiency (only 10-15% absorbed)
        float energyGained = energyTaken * trophicEfficiency;
        currentEnergy += energyGained;
        
        // Debug.Log($"[BiomassEnergy] {gameObject.name} ate {energyTaken:F1} from {plant.gameObject.name}, gained {energyGained:F1} health");
    }
    
    void TryHuntAnimal()
    {
        // Find nearest herbivore within search radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        
        BiomassEnergy nearestAnimal = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D col in colliders)
        {
            BiomassEnergy biomass = col.GetComponent<BiomassEnergy>();
            if (biomass != null && biomass.entityType == EntityType.Herbivore && biomass.isAlive)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestAnimal = biomass;
                }
            }
        }
        
        // Hunt the animal
        if (nearestAnimal != null)
        {
            HuntAnimal(nearestAnimal);
        }
    }
    
    void HuntAnimal(BiomassEnergy animal)
    {
        // Kill animal and gain its energy
        float energyTaken = animal.currentEnergy;
        animal.Die();
        
        // Gain energy with trophic efficiency
        float energyGained = energyTaken * trophicEfficiency;
        currentEnergy += energyGained;
        
        Debug.Log($"[BiomassEnergy] {gameObject.name} hunted {animal.gameObject.name}, gained {energyGained:F1} health");
    }
    
    bool IsDaytime()
    {
        if (timeController == null) return true;
        
        float sunriseH = timeController.sunriseHour + timeController.sunriseMin / 60f;
        float sunsetH = timeController.sunsetHour + timeController.sunsetMin / 60f;
        float clockH = timeController.time01 * 24f;
        
        return (clockH >= sunriseH) && (clockH < sunsetH);
    }
    
    public void Die()
    {
        if (!isAlive) return;
        
        isAlive = false;
        
        string deathReason = entityType == EntityType.Plant ? "withered" : "starved";
        Debug.Log($"[BiomassEnergy] {gameObject.name} died ({deathReason})! Energy: {currentEnergy:F1}");
        
        // Trigger death effects in GasExchanger
        if (gasExchanger != null)
        {
            gasExchanger.Die();
        }
        
        // Visual feedback - fade out
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);  // Gray and transparent
        }
        
        // Destroy after delay
        Destroy(gameObject, 3f);
    }
    
    /// <summary>
    /// Get status string for UI display
    /// </summary>
    public string GetStatusString()
    {
        string energyLabel = entityType == EntityType.Plant ? "Biomass" : "Health";
        float percentage = (currentEnergy / maxEnergy) * 100f;
        string status = isHungry ? "🍽️ Hungry" : "✅ Satisfied";
        
        if (entityType == EntityType.Plant)
        {
            bool isDay = IsDaytime();
            string activity = isDay ? "🌞 Photosynthesis" : "🌙 Respiration";
            return $"{energyLabel}: {currentEnergy:F1}/{maxEnergy:F0} ({percentage:F0}%)\n{activity}";
        }
        else
        {
            return $"{energyLabel}: {currentEnergy:F1}/{maxEnergy:F0} ({percentage:F0}%)\n{status}";
        }
    }
    
    /// <summary>
    /// Get color based on energy level (for UI/visualization)
    /// </summary>
    public Color GetHealthColor()
    {
        float percentage = currentEnergy / maxEnergy;
        
        if (percentage > 0.7f)
            return Color.green;  // Healthy
        else if (percentage > 0.4f)
            return Color.yellow;  // Moderate
        else if (percentage > 0.2f)
            return new Color(1f, 0.5f, 0f);  // Orange - Danger
        else
            return Color.red;  // Critical
    }
    
    // Visualize search radius in Scene view
    void OnDrawGizmosSelected()
    {
        if (entityType != EntityType.Plant)
        {
            Gizmos.color = isHungry ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}
