using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for simplified array functions

/// <summary>
/// Manages human population by spawning men/women to maintain balance.
/// Spawns 1 human per day cycle until population reaches 6 (3 men + 3 women).
/// Balances gender: spawns the gender with fewer count.
/// </summary>
public class HumanRespawnManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject manPrefab;
    public GameObject womanPrefab;
    
    [Header("Population Settings")]
    [Tooltip("Maximum total human population (men + women)")]
    public int maxPopulation = 6;
    
    [Tooltip("Maximum men (half of max population)")]
    public int maxMen = 3;
    
    [Tooltip("Maximum women (half of max population)")]
    public int maxWomen = 3;
    
    [Header("Spawn Timing")]
    [Tooltip("Check for breeding once per day")]
    public bool breedOncePerDay = true;
    
    [Header("Breeding Settings")]
    [Tooltip("Distance required between man and woman to breed (world units)")]
    public float breedingDistance = 1.5f;
    
    [Tooltip("Duration to seek mate before giving up (seconds)")]
    public float seekingDuration = 10f;
    
    private int lastBreedingDay = -1;
    private int currentMenCount = 0;
    private int currentWomenCount = 0;
    
    // Track current breeding pair
    private HumanAgent seekingMan = null;
    private HumanAgent targetWoman = null;
    private float seekStartTime = 0f;
    
    void Start()
    {
        // Initialize to current day to prevent immediate breeding on game start
        lastBreedingDay = GetCurrentDay();
        
        Debug.Log($"[HumanRespawnManager] Initialized. Max population: {maxPopulation} ({maxMen} men, {maxWomen} women)");
        Debug.Log($"[HumanRespawnManager] Breeding once per day (distance: {breedingDistance} units)");
    }
    
    void Update()
    {
        // Get current day from SunMoonController
        int currentDay = GetCurrentDay();
        
        // Check if it's a new day and we should try breeding
        if (breedOncePerDay && currentDay > lastBreedingDay)
        {
            lastBreedingDay = currentDay;
            
            UpdatePopulationCounts();
            
            // Check if we can breed
            int totalPopulation = currentMenCount + currentWomenCount;
            if (totalPopulation < maxPopulation)
            {
                // Randomly select 1 man to seek 1 woman
                AssignBreedingPair();
            }
        }
        
        // Monitor active breeding pair
        if (seekingMan != null && targetWoman != null)
        {
            // Ensure they are both alive and available
            if (!seekingMan.gameObject.activeInHierarchy || !targetWoman.gameObject.activeInHierarchy || 
                !seekingMan.GetComponent<HumanMetabolism>().isAlive || !targetWoman.GetComponent<HumanMetabolism>().isAlive)
            {
                seekingMan.StopSeeking();
                seekingMan = null;
                targetWoman = null;
                return;
            }

            // Check if they're close enough to breed
            float distance = Vector3.Distance(seekingMan.transform.position, targetWoman.transform.position);
            
            if (distance <= breedingDistance)
            {
                // Breed!
                HumanMetabolism father = seekingMan.GetComponent<HumanMetabolism>();
                HumanMetabolism mother = targetWoman.GetComponent<HumanMetabolism>();
                
                // Final check before spawning
                if (father != null && mother != null && father.isAlive && mother.isAlive)
                {
                    SpawnChild(father, mother);
                }
                
                // Stop seeking and reset pair
                seekingMan.StopSeeking();
                seekingMan = null;
                targetWoman = null;
            }
            else if (Time.time - seekStartTime > seekingDuration)
            {
                // Give up after duration
                seekingMan.StopSeeking();
                seekingMan = null;
                targetWoman = null;
                Debug.Log("[HumanRespawnManager] Breeding attempt timed out");
            }
        }
    }
    
    void AssignBreedingPair()
    {
        // Find all alive HumanAgents
        HumanAgent[] allAgents = FindObjectsOfType<HumanAgent>().Where(a => a.GetComponent<HumanMetabolism>().isAlive).ToArray();
        
        List<HumanAgent> men = allAgents.Where(a => a.isMale).ToList();
        List<HumanAgent> women = allAgents.Where(a => !a.isMale).ToList();
        
        // Need at least 1 man and 1 woman
        if (men.Count == 0 || women.Count == 0)
        {
            Debug.Log($"[HumanRespawnManager] Cannot breed: {men.Count} men, {women.Count} women");
            return;
        }
        
        // Randomly select 1 man and 1 woman
        HumanAgent randomMan = men[Random.Range(0, men.Count)];
        HumanAgent randomWoman = women[Random.Range(0, women.Count)];
        
        // Assign them to seek each other
        seekingMan = randomMan;
        targetWoman = randomWoman;
        seekStartTime = Time.time;
        
        randomMan.SeekMate(randomWoman);
        
        Debug.Log($"[HumanRespawnManager] {randomMan.gameObject.name} seeking {randomWoman.gameObject.name} for breeding");
    }
    
    void UpdatePopulationCounts()
    {
        // Rely on the isMale variable on HumanAgent
        HumanAgent[] allAgents = FindObjectsOfType<HumanAgent>().Where(a => a.GetComponent<HumanMetabolism>()?.isAlive == true).ToArray();
        
        currentMenCount = allAgents.Count(a => a.isMale);
        currentWomenCount = allAgents.Count(a => !a.isMale);
        
        // Automatically adjust max counts if maxPopulation is odd
        maxMen = Mathf.CeilToInt(maxPopulation / 2f);
        maxWomen = Mathf.FloorToInt(maxPopulation / 2f);
        
        Debug.Log($"[HumanRespawnManager] Current population: {currentMenCount} men, {currentWomenCount} women (Total: {currentMenCount + currentWomenCount})");
    }
    
    /// <summary>
    /// Spawns a child based on breeding pair.
    /// Balances gender: spawns the gender with fewer count.
    /// </summary>
    void SpawnChild(HumanMetabolism father, HumanMetabolism mother)
    {
        UpdatePopulationCounts(); // Re-run counts right before spawn
        
        // Determine child gender based on population balance
        bool spawnMale = ShouldSpawnMan();
        
        // Override check based on capacity
        if (spawnMale && currentMenCount >= maxMen)
        {
            spawnMale = false; 
        }
        else if (!spawnMale && currentWomenCount >= maxWomen)
        {
            spawnMale = true; 
        }
        
        GameObject prefab = spawnMale ? manPrefab : womanPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[HumanRespawnManager] {(spawnMale ? "Man" : "Woman")} prefab not assigned!");
            return;
        }
        
        // Spawn near the breeding pair (midpoint between them)
        Vector3 midpoint = (father.transform.position + mother.transform.position) / 2f;
        Vector3 spawnPos = midpoint + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
        
        // CRITICAL FIX: Ensure child spawns on land
        spawnPos = WorldBounds.ClampToLand(spawnPos); 
        
        GameObject child = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Update new child's isMale flag based on what was spawned
        HumanAgent childAgent = child.GetComponent<HumanAgent>();
        HumanMetabolism childMetabolism = child.GetComponent<HumanMetabolism>();
        
        if (childAgent != null)
        {
            childAgent.isMale = spawnMale; // Set the reliable flag
            childAgent.canMove = true; // Enable movement
            // childAgent.seekMateForBreeding = true; // Default should be true from prefab
        }
        
        // Reset hunger/biomass (optional, usually done in prefab or HumanMetabolism.Start)
        if (childMetabolism != null)
        {
            childMetabolism.hunger = childMetabolism.maxHunger * 0.8f; // Start with energy
        }
        
        string gender = spawnMale ? "boy" : "girl";
        string childName = child.name;
        
        // Update counts (these are handled again in the next daily check, but helpful for immediate debug)
        if (spawnMale)
            currentMenCount++;
        else
            currentWomenCount++;
        
        Debug.Log($"[HumanRespawnManager] {father.gameObject.name} + {mother.gameObject.name} had a {gender}! Population: {currentMenCount} men, {currentWomenCount} women");
        
        // Notify UI
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.AddNotification(
                $"[BIRTH] {childName} born from {father.gameObject.name} and {mother.gameObject.name}!", 
                new Color(0.2f, 0.8f, 0.2f)
            );
        }
    }
    
    /// <summary>
    /// Determines which gender to spawn based on current balance.
    /// Logic: Spawn the gender with fewer count. If equal, spawn man.
    /// </summary>
    bool ShouldSpawnMan()
    {
        if (currentMenCount < currentWomenCount)
        {
            return true; // Fewer men, spawn man
        }
        else if (currentWomenCount < currentMenCount)
        {
            return false; // Fewer women, spawn woman
        }
        else
        {
            return true; // Equal counts, default to man
        }
    }
    
    // Removed old SpawnMan/SpawnWoman functions as they were not used in the final logic
    // and relied on external, unused counting variables.
    
    int GetCurrentDay()
    {
        // Get current day from SunMoonController
        var sunMoon = FindAnyObjectByType<SunMoonController>();
        if (sunMoon != null)
        {
            return sunMoon.day; // Assuming SunMoonController tracks currentDay
        }
        
        // Fallback: Calculate current day based on elapsed time (less accurate if simulation speed changes)
        return Mathf.FloorToInt(Time.time / 120f); 
    }
}