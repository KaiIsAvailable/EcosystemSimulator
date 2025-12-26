using UnityEngine;
using System.Collections;
using System.Linq; // Added for simplified array manipulation

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
    
    [Tooltip("Maximum number of animals allowed for AUTOMATIC reproduction (system-triggered). User interactions have no limit.")]
    public int maxAnimalsAutomatic = 10;
    
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
    /// Trigger reproduction when an animal dies or nighttime hunger triggers.
    /// Finds 2 random alive animals and spawns 1 new offspring.
    /// </summary>
    /// <param name="isAutomatic">True if triggered by system (death/hunger), False if triggered by user interaction</param>
    public void TriggerAnimalReproduction(bool isAutomatic = true)
    {
        // Find all alive animals
        AnimalMetabolism[] allAnimals = FindObjectsOfType<AnimalMetabolism>();
        AnimalMetabolism[] aliveAnimals = System.Array.FindAll(allAnimals, a => a.isAlive);

        // Only enforce limit for automatic reproduction (user interactions have no limit)
        if (isAutomatic && aliveAnimals.Length >= maxAnimalsAutomatic)
        {
            Debug.Log($"[AnimalRespawn] Population at or above automatic max ({aliveAnimals.Length}/{maxAnimalsAutomatic}), skipping auto-reproduction.");
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
        
        // Ensure parent 2 is different from parent 1, if possible
        if (aliveAnimals.Length > 1)
        {
            while (parent2Index == parent1Index)
            {
                parent2Index = Random.Range(0, aliveAnimals.Length);
            }
        }
        
        AnimalMetabolism parent1 = aliveAnimals[parent1Index];
        AnimalMetabolism parent2 = aliveAnimals[parent2Index];
        
        // Calculate birth position
        Vector3 birthPosition = (parent1.transform.position + parent2.transform.position) / 2f;
        
        // Apply random offset
        birthPosition += new Vector3(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset), 0f);
        
        // CRITICAL FIX: Clamp to land before instantiation
        birthPosition = WorldBounds.ClampToLand(birthPosition);
        
        // Instantiate new animal (cloning an existing one preserves the prefab link)
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
        Vector3 spawnPos = Vector3.zero;

        if (aliveAnimals.Length >= 1)
        {
            // Clone a random alive animal as a template
            AnimalMetabolism template = aliveAnimals[Random.Range(0, aliveAnimals.Length)];
            
            // Calculate a random spawn position near the template, clamped to land
            spawnPos = template.transform.position + new Vector3(
                Random.Range(-spawnOffset, spawnOffset), 
                Random.Range(-spawnOffset, spawnOffset), 
                0f
            );
            
            // CRITICAL FIX: Clamp position after calculating offset
            spawnPos = WorldBounds.ClampToLand(spawnPos); 

            newAnimal = Instantiate(template.gameObject, spawnPos, Quaternion.identity);
        }
        else
        {
            // No template animals available - try to use WorldLogic's prefab
            WorldLogic world = FindAnyObjectByType<WorldLogic>();
            if (world != null && world.animalPrefab != null)
            {
                // CRITICAL FIX: Ensure WorldBounds is initialized before trying to use it
                if (WorldBounds.IsInitialized)
                {
                    // Calculate a random spawn position within the bounds but above the ocean
                    spawnPos = WorldBounds.ClampToLand(new Vector3(
                        Random.Range(WorldBounds.areaCenter.x - WorldBounds.areaHalfExtents.x, WorldBounds.areaCenter.x + WorldBounds.areaHalfExtents.x),
                        Random.Range(WorldBounds.oceanTopY + 0.2f, WorldBounds.areaCenter.y + WorldBounds.areaHalfExtents.y),
                        0f)
                    );
                }
                else
                {
                    // Fallback to center if WorldBounds is not initialized
                    spawnPos = Vector3.zero; 
                }
                
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