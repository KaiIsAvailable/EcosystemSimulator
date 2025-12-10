using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages grass respawning - when grass is eaten, spawn new grass randomly
/// Maintains grass population balance in the ecosystem
/// </summary>
public class GrassRespawnManager : MonoBehaviour
{
    public static GrassRespawnManager Instance { get; private set; }
    
    [Header("References")]
    public GameObject grassPrefab;
    public WorldLogic worldLogic;
    
    [Header("Respawn Settings")]
    [Tooltip("Time interval between each grass spawn (seconds). NOTE: This is now unused for queue processing.")]
    public float respawnInterval = 0.5f; // <<<<<< NO LONGER USED IN UPDATE()
    
    [Header("Spawn Area")]
    private Vector3 areaCenter;
    private Vector2 areaHalfExtents;
    private float oceanTopY;
    private int nextGrassID = 1;
    
    private int pendingRespawns = 0; // How many grass need to be respawned (The queue count)
    private float nextSpawnTime = 0f; // Kept for initialization, but now mostly unused in Update()
    
    // --- New settings fields for spawned grass ---
    [Header("Spawned Grass Parameters (CRITICAL FOR BALANCE)")]
    public float spawnedBiomass = 30f; // Recommended for stability
    [Tooltip("Corrected mol/s/kg rate (P_max * A_P)")]
    public float grassPMaxRate = 0.0072f; 
    [Tooltip("Corrected mol/s/kg rate (R_base * A_R)")]
    public float grassRBaseRate = 0.00036f;
    // --- End new settings fields ---
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        if (worldLogic == null)
        {
            worldLogic = FindAnyObjectByType<WorldLogic>();
        }
        
        // Get world bounds
        if (WorldBounds.IsInitialized)
        {
            areaCenter = WorldBounds.areaCenter;
            areaHalfExtents = WorldBounds.areaHalfExtents;
            oceanTopY = WorldBounds.oceanTopY;
        }
        
        // Find the highest grass ID that exists (Correct ID initialization logic kept)
        PlantAgent[] allGrass = FindObjectsOfType<PlantAgent>();
        foreach (var grass in allGrass)
        {
            if (grass.plantType == PlantAgent.PlantType.Grass)
            {
                // Extract number from name like "Grass(15)"
                string name = grass.gameObject.name;
                int startIdx = name.IndexOf('(');
                int endIdx = name.IndexOf(')');
                if (startIdx >= 0 && endIdx > startIdx)
                {
                    string numStr = name.Substring(startIdx + 1, endIdx - startIdx - 1);
                    if (int.TryParse(numStr, out int id))
                    {
                        if (id >= nextGrassID)
                        {
                            nextGrassID = id + 1;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"[GrassRespawn] Initialized. Next grass ID: {nextGrassID}");
        
        // Initialize spawn timer (mostly vestigial now)
        nextSpawnTime = Time.time + respawnInterval;
    }
    
    void Update()
    {
        // -------------------------------------------------------------------
        // FIX: Process the pendingRespawns queue on every frame for fast recovery
        // This ensures resource replacement matches accelerated consumption speed.
        // -------------------------------------------------------------------
        
        if (pendingRespawns > 0)
        {
            // Spawn one grass patch this frame
            SpawnGrass();
            pendingRespawns--;
            
            // Note: The timer logic is removed, allowing the queue to clear over successive frames.
            Debug.Log($"[GrassRespawn] Remaining to spawn: {pendingRespawns}");
        }
    }
    
    /// <summary>
    /// Call this when grass is eaten - adds to pending count
    /// </summary>
    public void OnGrassEaten()
    {
        pendingRespawns++;
        Debug.Log($"[GrassRespawn] Grass eaten. Pending respawns: {pendingRespawns}");
    }
    
    /// <summary>
    /// Spawn a new grass at random location
    /// </summary>
    void SpawnGrass()
    {
        if (grassPrefab == null)
        {
            Debug.LogWarning("[GrassRespawn] No grass prefab assigned!");
            return;
        }
        
        Vector3 spawnPos;
        // Try up to 50 times to find a land spot
        if (TryGetRandomPosition(out spawnPos, 50))
        {
            GameObject grassObj = Instantiate(grassPrefab, spawnPos, Quaternion.identity);
            int grassID = nextGrassID++;
            grassObj.name = $"Grass({grassID})";
            
            // Random rotation for variety
            grassObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
            
            // Add PlantAgent
            PlantAgent agent = grassObj.GetComponent<PlantAgent>();
            if (agent == null)
            {
                agent = grassObj.AddComponent<PlantAgent>();
            }
            
            // Apply CORRECTED, FINAL RATES and BIOMASS for stability
            agent.plantType = PlantAgent.PlantType.Grass;
            agent.metabolismScale = 1.0f; 
            agent.R_base = grassRBaseRate;
            agent.Q10_factor = 2.0f;
            agent.P_max = grassPMaxRate;
            agent.biomass = spawnedBiomass;
            
            // Ensure collider exists
            if (grassObj.GetComponent<Collider2D>() == null)
            {
                CircleCollider2D collider = grassObj.AddComponent<CircleCollider2D>();
                collider.radius = 0.3f;
                collider.isTrigger = true;
            }
            
            // Add breathing animation (optional, for visual effect)
            // Assuming BreathingAnimation exists elsewhere.
            // BreathingAnimation breathing = grassObj.AddComponent<BreathingAnimation>();
            // breathing.breathingSpeed = 1.2f;
            // breathing.breathingAmount = 0.04f;
            // breathing.randomizePhase = true;
            
            // Notify UI
            // Assuming EventNotificationUI exists elsewhere.
            // if (EventNotificationUI.Instance != null)
            // {
            //     EventNotificationUI.Instance.NotifyGrassGrow($"Grass({grassID})");
            // }
            
            Debug.Log($"[GrassRespawn] Spawned Grass({grassID}) with {spawnedBiomass}kg biomass at {spawnPos}");
        }
        else
        {
            Debug.LogWarning("[GrassRespawn] Failed to find spawn position for grass!");
        }
    }
    
    /// <summary>
    /// Find a random valid position on land
    /// </summary>
    bool TryGetRandomPosition(out Vector3 pos, int maxAttempts)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(-areaHalfExtents.x, areaHalfExtents.x);
            float minY = oceanTopY - areaCenter.y + 0.2f; // Above ocean
            float y = Random.Range(minY, areaHalfExtents.y);
            
            Vector3 candidate = new Vector3(areaCenter.x + x, areaCenter.y + y, 0f);
            
            // Check if position is on land
            if (WorldBounds.IsOnLand(candidate))
            {
                pos = candidate;
                return true;
            }
        }
        
        pos = Vector3.zero;
        return false;
    }
}