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
    [Tooltip("Time interval between each grass spawn (seconds)")]
    public float respawnInterval = 5f;
    
    [Header("Spawn Area")]
    private Vector3 areaCenter;
    private Vector2 areaHalfExtents;
    private float oceanTopY;
    private int nextGrassID = 1;
    
    private int pendingRespawns = 0;  // How many grass need to be respawned
    private float nextSpawnTime = 0f;  // When to spawn next grass
    
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
        
        // Find the highest grass ID that exists
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
        
        // Initialize spawn timer
        nextSpawnTime = Time.time + respawnInterval;
    }
    
    void Update()
    {
        // Spawn 1 grass every respawnInterval seconds if there are pending respawns
        if (pendingRespawns > 0 && Time.time >= nextSpawnTime)
        {
            SpawnGrass();
            pendingRespawns--;
            nextSpawnTime = Time.time + respawnInterval;
            
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
            
            agent.plantType = PlantAgent.PlantType.Grass;
            agent.metabolismScale = 0.1f;
            agent.R_base = 0.00005f;
            agent.Q10_factor = 2.0f;
            agent.P_max = 0.0008f;
            agent.biomass = 5f;
            
            // Ensure collider exists
            if (grassObj.GetComponent<Collider2D>() == null)
            {
                CircleCollider2D collider = grassObj.AddComponent<CircleCollider2D>();
                collider.radius = 0.3f;
                collider.isTrigger = true;
            }
            
            // Add breathing animation
            BreathingAnimation breathing = grassObj.AddComponent<BreathingAnimation>();
            breathing.breathingSpeed = 1.2f;
            breathing.breathingAmount = 0.04f;
            breathing.randomizePhase = true;
            
            // Notify UI
            if (EventNotificationUI.Instance != null)
            {
                EventNotificationUI.Instance.NotifyGrassGrow($"Grass({grassID})");
            }
            
            Debug.Log($"[GrassRespawn] Spawned Grass({grassID}) at {spawnPos}");
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
