using UnityEngine;

/// <summary>
/// Manages animal reproduction to balance the ecosystem.
/// When an animal dies (from hunting or starvation), triggers reproduction
/// from 2 random living animals to maintain population stability.
/// </summary>
public class AnimalRespawnManager : MonoBehaviour
{
    public static AnimalRespawnManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Number of parent animals required for reproduction")]
    public int parentsRequired = 2;
    
    [Tooltip("Maximum number of animals allowed in the world. Reproduction will be skipped when this limit is reached.")]
    public int maxAnimals = 10;
    
    [Tooltip("Starting hunger percentage for newborns (0-1)")]
    [Range(0f, 1f)]
    public float newbornHungerPercent = 0.8f;
    
    [Tooltip("Starting biomass percentage for newborns (0-1)")]
    [Range(0f, 1f)]
    public float newbornBiomassPercent = 0.5f;
    
    [Tooltip("Random spawn offset from parents")]
    public float spawnOffset = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Trigger reproduction when an animal dies.
    /// Finds 2 random alive animals and spawns 1 new offspring.
    /// </summary>
    public void TriggerAnimalReproduction()
    {
        // Find all alive animals
        AnimalMetabolism[] allAnimals = FindObjectsOfType<AnimalMetabolism>();
        AnimalMetabolism[] aliveAnimals = System.Array.FindAll(allAnimals, a => a.isAlive);

        // Enforce maximum animal population
        if (aliveAnimals.Length >= maxAnimals)
        {
            Debug.Log($"[AnimalRespawn] Population at or above max ({aliveAnimals.Length}/{maxAnimals}), skipping reproduction.");
            return;
        }

        if (aliveAnimals.Length < parentsRequired)
        {
            Debug.LogWarning($"[AnimalRespawn] Not enough animals for reproduction. Need {parentsRequired}, found {aliveAnimals.Length}");
            return;
        }
        
        // Pick 2 random parents
        int parent1Index = Random.Range(0, aliveAnimals.Length);
        int parent2Index = Random.Range(0, aliveAnimals.Length);
        
        // Make sure they're different
        while (parent2Index == parent1Index && aliveAnimals.Length > 1)
        {
            parent2Index = Random.Range(0, aliveAnimals.Length);
        }
        
        AnimalMetabolism parent1 = aliveAnimals[parent1Index];
        AnimalMetabolism parent2 = aliveAnimals[parent2Index];
        
        // Spawn new animal between parents
        Vector3 birthPosition = (parent1.transform.position + parent2.transform.position) / 2f;
        birthPosition += new Vector3(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset), 0f);
        
        // Instantiate new animal
        GameObject newAnimal = Instantiate(parent1.gameObject, birthPosition, Quaternion.identity);
        AnimalMetabolism newAnimalScript = newAnimal.GetComponent<AnimalMetabolism>();
        
        if (newAnimalScript != null)
        {
            // Reset newborn stats
            newAnimalScript.hunger = newAnimalScript.maxHunger * newbornHungerPercent; // Start 80% full
            newAnimalScript.biomass = newAnimalScript.maxBiomass * newbornBiomassPercent; // Smaller at birth
            newAnimalScript.isAlive = true;
            
            // Notify about birth
            if (EventNotificationUI.Instance != null)
            {
                EventNotificationUI.Instance.NotifyAnimalBirth(
                    newAnimal.name, 
                    parent1.gameObject.name, 
                    parent2.gameObject.name
                );
            }
            
            Debug.Log($"[AnimalRespawn] Birth! {newAnimal.name} born from {parent1.gameObject.name} & {parent2.gameObject.name}");
        }
    }

        /// <summary>
        /// Spawn an animal immediately, bypassing the maxAnimals cap.
        /// This is intended for manual / developer-triggered spawns.
        /// </summary>
        public void SpawnAnimalImmediate()
        {
            // Try to spawn by cloning a random existing animal if possible
            AnimalMetabolism[] allAnimals = FindObjectsOfType<AnimalMetabolism>();
            AnimalMetabolism[] aliveAnimals = System.Array.FindAll(allAnimals, a => a.isAlive);

            GameObject newAnimal = null;

            if (aliveAnimals.Length >= 1)
            {
                // Clone the first alive animal as a simple immediate spawn
                AnimalMetabolism template = aliveAnimals[Random.Range(0, aliveAnimals.Length)];
                Vector3 spawnPos = WorldBounds.IsInitialized ? (Vector3)WorldBounds.areaCenter : template.transform.position;
                if (WorldBounds.IsInitialized)
                {
                    // pick a random nearby offset
                    spawnPos = template.transform.position + new Vector3(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset), 0f);
                    spawnPos = WorldBounds.ClampToLand(spawnPos);
                }

                newAnimal = Instantiate(template.gameObject, spawnPos, Quaternion.identity);
            }
            else
            {
                // No template animals available - try to use WorldLogic's prefab
                WorldLogic world = FindAnyObjectByType<WorldLogic>();
                if (world != null && world.animalPrefab != null)
                {
                    Vector3 spawnPos = WorldBounds.IsInitialized ? new Vector3(
                        Random.Range(WorldBounds.areaCenter.x - WorldBounds.areaHalfExtents.x, WorldBounds.areaCenter.x + WorldBounds.areaHalfExtents.x),
                        Random.Range(WorldBounds.oceanTopY + 0.2f, WorldBounds.areaCenter.y + WorldBounds.areaHalfExtents.y),
                        0f) : Vector3.zero;

                    spawnPos = WorldBounds.IsInitialized ? WorldBounds.ClampToLand(spawnPos) : spawnPos;
                    newAnimal = Instantiate(world.animalPrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("[AnimalRespawn] No animal template or prefab found to spawn immediate animal.");
                    return;
                }
            }

            if (newAnimal != null)
            {
                AnimalMetabolism am = newAnimal.GetComponent<AnimalMetabolism>();
                if (am != null)
                {
                    am.hunger = am.maxHunger * newbornHungerPercent;
                    am.biomass = am.maxBiomass * newbornBiomassPercent;
                    am.isAlive = true;
                }

                if (EventNotificationUI.Instance != null)
                {
                    EventNotificationUI.Instance.NotifyAnimalBirth(newAnimal.name, "(manual)", "(manual)");
                }

                Debug.Log($"[AnimalRespawn] Immediate spawn: {newAnimal.name}");
            }
        }
}
