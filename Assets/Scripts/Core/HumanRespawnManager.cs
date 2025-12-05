using UnityEngine;

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
            // Check if they're close enough to breed
            float distance = Vector3.Distance(seekingMan.transform.position, targetWoman.transform.position);
            
            if (distance <= breedingDistance)
            {
                // Breed!
                HumanMetabolism father = seekingMan.GetComponent<HumanMetabolism>();
                HumanMetabolism mother = targetWoman.GetComponent<HumanMetabolism>();
                
                if (father != null && mother != null && father.isAlive && mother.isAlive)
                {
                    SpawnChild(father, mother);
                }
                
                // Stop seeking
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
        // Find all alive men and women
        HumanMetabolism[] allHumans = FindObjectsOfType<HumanMetabolism>();
        System.Collections.Generic.List<HumanAgent> men = new System.Collections.Generic.List<HumanAgent>();
        System.Collections.Generic.List<HumanAgent> women = new System.Collections.Generic.List<HumanAgent>();
        
        foreach (var human in allHumans)
        {
            if (!human.isAlive) continue;
            
            HumanAgent agent = human.GetComponent<HumanAgent>();
            if (agent == null) continue;
            
            // Identify gender by sprite color
            SpriteRenderer sr = human.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                if (color.r > color.b && color.g > color.b) // Yellow = man
                {
                    men.Add(agent);
                }
                else if (color.r > color.g && color.r > 0.5f) // Pink = woman
                {
                    women.Add(agent);
                }
            }
        }
        
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
        // Count alive humans by checking their names or components
        currentMenCount = 0;
        currentWomenCount = 0;
        
        // Find all humans with HumanMetabolism component
        HumanMetabolism[] allHumans = FindObjectsOfType<HumanMetabolism>();
        
        foreach (var human in allHumans)
        {
            if (!human.isAlive) continue;
            
            // Try to identify gender by checking sprite color or other distinguishing features
            string name = human.gameObject.name.ToLower();
            
            // Check sprite renderer color to distinguish gender
            SpriteRenderer sr = human.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                
                // Yellow/warm colors = man, Pink/cool colors = woman
                if (color.r > color.b && color.g > color.b) // More red+green than blue = yellow = man
                {
                    currentMenCount++;
                    Debug.Log($"[UpdatePopulation] {human.gameObject.name} counted as MAN (color: {color})");
                }
                else if (color.r > color.g && color.r > 0.5f) // More red = pink = woman
                {
                    currentWomenCount++;
                    Debug.Log($"[UpdatePopulation] {human.gameObject.name} counted as WOMAN (color: {color})");
                }
                else
                {
                    Debug.LogWarning($"[UpdatePopulation] {human.gameObject.name} - Cannot determine gender from color: {color}");
                }
            }
            else
            {
                Debug.LogWarning($"[UpdatePopulation] {human.gameObject.name} has no SpriteRenderer!");
            }
        }
        
        Debug.Log($"[HumanRespawnManager] Current population: {currentMenCount} men, {currentWomenCount} women (Total: {currentMenCount + currentWomenCount})");
    }
    
    void TrySpawnHuman()
    {
        int totalPopulation = currentMenCount + currentWomenCount;
        
        // Check if we've reached max population
        if (totalPopulation >= maxPopulation)
        {
            Debug.Log($"[HumanRespawnManager] Max population reached ({totalPopulation}/{maxPopulation}). No spawn.");
            return;
        }
        
        // Find all alive men and women
        HumanMetabolism[] allHumans = FindObjectsOfType<HumanMetabolism>();
        System.Collections.Generic.List<HumanMetabolism> men = new System.Collections.Generic.List<HumanMetabolism>();
        System.Collections.Generic.List<HumanMetabolism> women = new System.Collections.Generic.List<HumanMetabolism>();
        
        foreach (var human in allHumans)
        {
            if (!human.isAlive) continue;
            
            // Identify gender by sprite color
            SpriteRenderer sr = human.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                string humanName = human.gameObject.name;
                Debug.Log($"[HumanRespawnManager] Checking human: '{humanName}' (color: {color})");
                
                // Yellow/warm colors = man, Pink/cool colors = woman
                if (color.r > color.b && color.g > color.b) // More red+green than blue = yellow = man
                {
                    men.Add(human);
                    Debug.Log($"  -> Identified as MAN (yellow color)");
                }
                else if (color.r > color.g && color.r > 0.5f) // More red = pink = woman
                {
                    women.Add(human);
                    Debug.Log($"  -> Identified as WOMAN (pink color)");
                }
                else
                {
                    Debug.LogWarning($"  -> Could not identify gender from color: {color}");
                }
            }
        }
        
        Debug.Log($"[HumanRespawnManager] Found {men.Count} men, {women.Count} women alive");
        
        // Try to find a breeding pair
        foreach (var man in men)
        {
            foreach (var woman in women)
            {
                float distance = Vector2.Distance(man.transform.position, woman.transform.position);
                Debug.Log($"[HumanRespawnManager] {man.gameObject.name} <-> {woman.gameObject.name}: distance = {distance:F2} (need ≤{breedingDistance})");
                
                if (distance <= breedingDistance)
                {
                    // Found a breeding pair! Spawn a child
                    SpawnChild(man, woman);
                    return;
                }
            }
        }
        
        // No breeding pair found close enough
        Debug.Log($"[HumanRespawnManager] No breeding pair within {breedingDistance} units found");
    }
    
    /// <summary>
    /// Spawns a child based on breeding pair.
    /// Balances gender: spawns the gender with fewer count.
    /// </summary>
    void SpawnChild(HumanMetabolism father, HumanMetabolism mother)
    {
        // Determine child gender based on population balance
        bool spawnMale = ShouldSpawnMan();
        
        if (spawnMale && currentMenCount >= maxMen)
        {
            spawnMale = false; // Max men reached, spawn female instead
        }
        else if (!spawnMale && currentWomenCount >= maxWomen)
        {
            spawnMale = true; // Max women reached, spawn male instead
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
        
        GameObject child = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Enable movement for newborn
        HumanAgent childAgent = child.GetComponent<HumanAgent>();
        if (childAgent != null)
        {
            childAgent.canMove = true;
            childAgent.seekMateForBreeding = true;
            
            // Copy parent boundaries
            HumanAgent fatherAgent = father.GetComponent<HumanAgent>();
            if (fatherAgent != null)
            {
                childAgent.areaCenter = fatherAgent.areaCenter;
                childAgent.areaHalfExtents = fatherAgent.areaHalfExtents;
            }
        }
        
        string gender = spawnMale ? "boy" : "girl";
        string childName = child.name;
        
        // Update counts
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
    
    void SpawnMan()
    {
        if (manPrefab == null)
        {
            Debug.LogWarning("[HumanRespawnManager] Man prefab not assigned!");
            return;
        }
        
        Vector3 spawnPos = GetRandomLandPosition();
        GameObject man = Instantiate(manPrefab, spawnPos, Quaternion.identity);
        
        currentMenCount++;
        
        Debug.Log($"[HumanRespawnManager] Spawned MAN at {spawnPos}. Population: {currentMenCount} men, {currentWomenCount} women");
        
        // Notify UI
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.AddNotification($"[BIRTH] {man.name} born (Day {GetCurrentDay()})", new Color(0.2f, 0.8f, 0.2f));
        }
    }
    
    void SpawnWoman()
    {
        if (womanPrefab == null)
        {
            Debug.LogWarning("[HumanRespawnManager] Woman prefab not assigned!");
            return;
        }
        
        Vector3 spawnPos = GetRandomLandPosition();
        GameObject woman = Instantiate(womanPrefab, spawnPos, Quaternion.identity);
        
        currentWomenCount++;
        
        Debug.Log($"[HumanRespawnManager] Spawned WOMAN at {spawnPos}. Population: {currentMenCount} men, {currentWomenCount} women");
        
        // Notify UI
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.AddNotification($"[BIRTH] {woman.name} born (Day {GetCurrentDay()})", new Color(0.2f, 0.8f, 0.2f));
        }
    }
    
    Vector3 GetRandomLandPosition()
    {
        if (WorldBounds.IsInitialized)
        {
            Vector3 center = WorldBounds.areaCenter;
            Vector2 half = WorldBounds.areaHalfExtents;

            float x = Random.Range(center.x - half.x, center.x + half.x);
            float y = Random.Range(WorldBounds.oceanTopY + 0.2f, center.y + half.y);
            Vector3 candidate = new Vector3(x, y, 0f);
            return WorldBounds.ClampToLand(candidate);
        }

        // Fallback near origin
        return new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);
    }
    
    int GetCurrentDay()
    {
        // Calculate current day based on elapsed time
        var sunMoon = FindAnyObjectByType<SunMoonController>();
        if (sunMoon != null)
        {
            return Mathf.FloorToInt(Time.time / sunMoon.fullDaySeconds);
        }
        return Mathf.FloorToInt(Time.time / 120f); // Fallback: 120 seconds per day
    }
}
