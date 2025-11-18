using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WorldLogic : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject treePrefab;
    public GameObject grassPrefab;
    public GameObject animalPrefab;
    public GameObject humanPrefab;
    public GameObject sunPrefab;
    public GameObject moonPrefab;
    public GameObject oxygenPrefab;
    public GameObject carbonDioxidePrefab;
    public GameObject oceanWaterPrefab;

    [Header("Counts")]
    public int treeCount = 10; // Balanced for 24h cycle (was 5)
    public int grassCount = 55;      
    public int animalCount = 10;
    public int humanCount = 1;
    public int sunCount = 1;
    public int moonCount = 1;
    

    [Header("Spawn Settings")]
    public Vector2 padding = new Vector2(0.5f, 0.5f);
    public float minSpacing = 0.75f;
    public int maxTriesPerSpawn = 30;
    
    [Header("Ocean Settings")]
    [Tooltip("Create ocean at bottom of map (20% of height)")]
    public bool spawnOcean = true;
    [Tooltip("Ocean height as percentage of map (0.2 = 20% of map height)")]
    [Range(0f, 0.5f)]
    public float oceanHeightPercent = 0.2f;

    private SpriteRenderer groundRenderer;
    private Vector3 areaCenter;
    private Vector2 halfExtents;
    private readonly List<Vector2> occupied = new();    // used spots
    private float oceanTopY;  // Y-coordinate where ocean ends (entities can't spawn below this)

    void Awake()
    {
        groundRenderer = GetComponent<SpriteRenderer>();

        // Fit ground to camera
        var cam = Camera.main;
        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        Vector2 spriteSize = groundRenderer.sprite.bounds.size;
        transform.localScale = new Vector3(worldW / spriteSize.x, worldH / spriteSize.y, 1f);

        // Cache spawn area
        areaCenter = transform.position;
        Vector2 size = groundRenderer.bounds.size;
        halfExtents = new Vector2(
            Mathf.Max(0f, size.x / 2f - padding.x),
            Mathf.Max(0f, size.y / 2f - padding.y)
        );
    }

    void Start()
    {
        SpawnOcean();      // Spawn ocean first (background layer)
        SpawnTrees();
        SpawnGrass();      // Spawn grass randomly (no longer around trees)
        SpawnAnimals();
        SpawnHuman();
        SpawnSunAndMoon();
    }
    
    void SpawnOcean()
    {
        if (!oceanWaterPrefab || !spawnOcean)
        {
            Debug.Log("[WorldLogic] Ocean disabled or no ocean prefab assigned");
            oceanTopY = areaCenter.y - halfExtents.y; // Bottom of map
            return;
        }
        
        // Calculate ocean dimensions
        float oceanHeight = halfExtents.y * 2f * oceanHeightPercent;  // e.g., 20% of map height
        float oceanWidth = halfExtents.x * 2f;  // Full map width
        
        // Position ocean at bottom of map
        float oceanY = areaCenter.y - halfExtents.y + (oceanHeight / 2f);
        oceanTopY = oceanY + (oceanHeight / 2f);  // Store where ocean ends
        
        Vector3 oceanPosition = new Vector3(areaCenter.x, oceanY, 0f);
        
        // Spawn single ocean object
        GameObject ocean = Instantiate(oceanWaterPrefab, oceanPosition, Quaternion.identity);
        
        // Scale ocean to cover bottom portion of map
        ocean.transform.localScale = new Vector3(oceanWidth, oceanHeight, 1f);
        
        Debug.Log($"[WorldLogic] Spawned ocean: width={oceanWidth:F1}, height={oceanHeight:F1}, top Y={oceanTopY:F2}");
        Debug.Log($"[WorldLogic] Entities will spawn above Y={oceanTopY:F2} to avoid water");
    }

    void SpawnTrees()
    {
        if (!treePrefab) { Debug.LogWarning("⚠️ Tree prefab not assigned!"); return; }

        for (int i = 0; i < treeCount; i++)
        {
            if (TryGetFreePos(out Vector3 pos))
            {
                GameObject tree = Instantiate(treePrefab, pos, Quaternion.identity);
                occupied.Add(pos);

                // Add PlantAgent for scientific metabolism
                AddPlantAgent(tree, PlantAgent.PlantType.Tree);

                // Add emission capability (visual particles)
                AddPlantEmitter(tree, 5f); // Trees emit every 5 seconds
                
                // Add breathing animation
                AddBreathingAnimation(tree, 0.8f, 0.03f); // Slow, subtle breathing
            }
            else Debug.LogWarning("Could not find free spot for a tree.");
        }
    }

    void SpawnGrass()
    {
        if (!grassPrefab) { Debug.LogWarning("⚠️ Grass prefab not assigned!"); return; }

        for (int i = 0; i < grassCount; i++)
        {
            if (TryGetFreePos(out Vector3 pos))
            {
                GameObject grassObj = Instantiate(grassPrefab, pos, Quaternion.identity);
                occupied.Add(pos);

                // Optional: slight rotation variation for natural look
                grassObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

                // Add PlantAgent for scientific metabolism
                AddPlantAgent(grassObj, PlantAgent.PlantType.Grass);

                // Add emission capability (visual particles - grass emits more frequently than trees)
                AddPlantEmitter(grassObj, 3f); // Grass emits every 3 seconds
                
                // Add breathing animation
                AddBreathingAnimation(grassObj, 1.2f, 0.04f); // Faster, slightly more visible than trees
            }
            else Debug.LogWarning("Could not find free spot for grass.");
        }
    }

    void AddPlantAgent(GameObject plant, PlantAgent.PlantType type)
    {
        // 🛑 CRITICAL: Remove GasExchanger if it exists on prefab (prevents double metabolism)
        GasExchanger oldExchanger = plant.GetComponent<GasExchanger>();
        if (oldExchanger != null)
        {
            // Unregister from AtmosphereManager first
            if (AtmosphereManager.Instance != null)
            {
                AtmosphereManager.Instance.UnregisterExchanger(oldExchanger);
            }
            Destroy(oldExchanger);
            Debug.LogWarning($"[WorldLogic] Removed GasExchanger from {plant.name} - plants use PlantAgent now!");
        }
        
        // Check if PlantAgent already exists (from prefab)
        PlantAgent agent = plant.GetComponent<PlantAgent>();
        
        if (agent == null)
        {
            // Add PlantAgent component
            agent = plant.AddComponent<PlantAgent>();
        }
        
        // Configure based on plant type
        agent.plantType = type;
        agent.metabolismScale = 0.1f;  // Dramatic visible gas exchange
        
        if (type == PlantAgent.PlantType.Tree)
        {
            agent.R_base = 0.0001f;
            agent.Q10_factor = 2.5f;
            agent.P_max = 0.0012f;
            agent.biomass = 10f;
        }
        else // Grass
        {
            agent.R_base = 0.00005f;
            agent.Q10_factor = 2.0f;
            agent.P_max = 0.0008f;
            agent.biomass = 5f;
        }
        
        Debug.Log($"[WorldLogic] Added PlantAgent to {plant.name}: type={type}, metabolismScale={agent.metabolismScale}");
        
        // Disable BiomassEnergy if it exists (avoid conflicts)
        var biomassEnergy = plant.GetComponent<BiomassEnergy>();
        if (biomassEnergy != null)
        {
            biomassEnergy.enabled = false;
        }
    }
    
    void AddGasExchanger(GameObject entity, GasExchanger.EntityType type)
    {
        // Only for animals and humans now - plants use PlantAgent
        GasExchanger exchanger = entity.AddComponent<GasExchanger>();
        exchanger.entityType = type;
        // Default rates are set automatically in GasExchanger.Start()
    }

    void AddPlantEmitter(GameObject plant, float interval)
    {
        if (!oxygenPrefab && !carbonDioxidePrefab) return;

        PlantEmitter emitter = plant.AddComponent<PlantEmitter>();
        emitter.oxygenPrefab = oxygenPrefab;
        emitter.carbonDioxidePrefab = carbonDioxidePrefab;
        emitter.emissionInterval = interval;
        emitter.emissionRadius = 0.3f;
        emitter.emissionForce = 0.5f;
    }
    
    void AddBreathingAnimation(GameObject entity, float speed, float amount)
    {
        BreathingAnimation breathing = entity.AddComponent<BreathingAnimation>();
        breathing.breathingSpeed = speed;
        breathing.breathingAmount = amount;
        breathing.randomizePhase = true;
    }

    void SpawnAnimals()
    {
        if (!animalPrefab) { Debug.LogWarning("⚠️ Animal prefab not assigned!"); return; }

        for (int i = 0; i < animalCount; i++)
        {
            if (TryGetFreePos(out Vector3 pos))
            {
                var go = Instantiate(animalPrefab, pos, Quaternion.identity);
                occupied.Add(pos);

                // Add gas exchange capability
                AddGasExchanger(go, GasExchanger.EntityType.Animal);
                
                // Add breathing animation (animals breathe visibly)
                AddBreathingAnimation(go, 1.5f, 0.05f);

                var mover = go.GetComponent<AnimalWander>();
                if (mover)
                {
                    mover.areaCenter = areaCenter;
                    mover.areaHalfExtents = halfExtents;
                }
            }
            else Debug.LogWarning("Could not find free spot for an animal.");
        }
    }

    void SpawnHuman()
    {
        if (!humanPrefab || humanCount <= 0) return;

        for (int i = 0; i < humanCount; i++)
        {
            if (TryGetFreePos(out Vector3 pos))
            {
                var go = Instantiate(humanPrefab, pos, Quaternion.identity);
                occupied.Add(pos);

                // Add gas exchange capability
                AddGasExchanger(go, GasExchanger.EntityType.Human);
                
                // Add breathing animation (humans breathe slower and more visible)
                AddBreathingAnimation(go, 1.2f, 0.06f);

                // Reuse the same wander script (set different speed in Inspector if you like)
                var mover = go.GetComponent<HumanAgent>();
                if (mover)
                {
                    mover.areaCenter = areaCenter;
                    mover.areaHalfExtents = halfExtents;
                }
            }
            else Debug.LogWarning("Could not find free spot for the human.");
        }
    }

    bool TryGetFreePos(out Vector3 pos)
    {
        for (int t = 0; t < maxTriesPerSpawn; t++)
        {
            float x = Random.Range(-halfExtents.x, halfExtents.x);
            float y = Random.Range(-halfExtents.y, halfExtents.y);
            Vector3 candidate = new Vector3(areaCenter.x + x, areaCenter.y + y, 0f);

            // Don't spawn in ocean area (below oceanTopY)
            if (candidate.y < oceanTopY)
            {
                continue; // Try again
            }

            bool tooClose = false;
            foreach (var used in occupied)
            {
                if (Vector2.Distance(candidate, used) < minSpacing) { tooClose = true; break; }
            }
            if (!tooClose) { pos = candidate; return true; }
        }
        pos = default;
        return false;
    }

    void SpawnSunAndMoon()
    {
        // Instantiate (or find existing) sun & moon
        Transform sunT = null, moonT = null;

        if (sunPrefab && sunCount > 0)
        {
            var go = Instantiate(sunPrefab, Vector3.zero, Quaternion.identity);
            sunT = go.transform;
            // Make sure it renders above trees/ground
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 100; // or use a dedicated "Sky" sorting layer
        }

        if (moonPrefab && moonCount > 0)
        {
            var go = Instantiate(moonPrefab, Vector3.zero, Quaternion.identity);
            moonT = go.transform;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 100;
        }

        // Attach / configure controller
        var ctrl = GetComponent<SunMoonController>();
        if (!ctrl) ctrl = gameObject.AddComponent<SunMoonController>();

        ctrl.sun = sunT;
        ctrl.moon = moonT;

        // Pass the same bounds we computed for spawning
        ctrl.areaCenter = areaCenter;
        ctrl.areaHalfExtents = halfExtents;

        // Tweakables (optional defaults)
        ctrl.fullDaySeconds = 120f;  // 2 minutes for a full 24h cycle
        ctrl.topMargin = 1f;
        ctrl.arcHeight = 1.5f;
        ctrl.horizontalPadding = 0.5f;
    }
}
