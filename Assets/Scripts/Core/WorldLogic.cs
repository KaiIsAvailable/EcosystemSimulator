using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class WorldLogic : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject treePrefab;
    public GameObject grassPrefab;
    public GameObject animalPrefab;
    public GameObject manPrefab;
    public GameObject womanPrefab;
    public GameObject sunPrefab;
    public GameObject moonPrefab;
    public GameObject oxygenPrefab;
    public GameObject carbonDioxidePrefab;
    public GameObject oceanWaterPrefab;

    [Header("Counts")]
    // Recommended balanced starting numbers:
    public int treeCount = 400; // INCREASED: Required for atmospheric balance
    public int grassCount = 55;      
    public int animalCount = 15; // Increased animal count to match your previous test data
    public int manCount = 1;
    public int womanCount = 1;
    public int sunCount = 1;
    public int moonCount = 1;
    
    [Header("Spawn Settings")]
    public Vector2 padding = new Vector2(0.5f, 0.5f);
    public float minSpacing = 0.5f;
    public int maxTriesPerSpawn = 100;
    
    [Header("Ocean Settings")]
    public bool spawnOcean = true;
    [Range(0f, 0.5f)]
    public float oceanHeightPercent = 0.2f;

    private SpriteRenderer groundRenderer;
    private Vector3 areaCenter;
    private Vector2 halfExtents;
    private readonly List<Vector2> occupied = new();
    private float oceanTopY;

    void Awake()
    {
        groundRenderer = GetComponent<SpriteRenderer>();

        var cam = Camera.main;
        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        Vector2 spriteSize = groundRenderer.sprite.bounds.size;
        transform.localScale = new Vector3(worldW / spriteSize.x, worldH / spriteSize.y, 1f);

        areaCenter = transform.position;
        Vector2 size = groundRenderer.bounds.size;
        halfExtents = new Vector2(
            Mathf.Max(0f, size.x / 2f - padding.x),
            Mathf.Max(0f, size.y / 2f - padding.y)
        );
    }

    void Start()
    {
        SpawnOcean();
        WorldBounds.Initialize(areaCenter, halfExtents, oceanTopY);
        
        SpawnTrees();
        SpawnGrass();
        SpawnAnimals();
        SpawnMen();
        SpawnWomen();
        SpawnSunAndMoon();
        
        SetupGrassRespawn();
        SetupHumanRespawn();
    }
    
    void SpawnOcean()
    {
        if (!oceanWaterPrefab || !spawnOcean)
        {
            oceanTopY = areaCenter.y - halfExtents.y;
            return;
        }
        
        float oceanHeight = halfExtents.y * 2f * oceanHeightPercent;
        float oceanWidth = halfExtents.x * 2f;
        
        float oceanY = areaCenter.y - halfExtents.y + (oceanHeight / 2f);
        oceanTopY = oceanY + (oceanHeight / 2f);
        
        Vector3 oceanPosition = new Vector3(areaCenter.x, oceanY, 0f);
        
        GameObject ocean = Instantiate(oceanWaterPrefab, oceanPosition, Quaternion.identity);
        ocean.transform.localScale = new Vector3(oceanWidth, oceanHeight, 1f);
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
                AddPlantAgent(tree, PlantAgent.PlantType.Tree);
                AddPlantEmitter(tree, 5f);
                AddBreathingAnimation(tree, 0.8f, 0.03f);
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
                grassObj.name = $"Grass({i + 1})";
                occupied.Add(pos);

                grassObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

                AddPlantAgent(grassObj, PlantAgent.PlantType.Grass);

                if (grassObj.GetComponent<Collider2D>() == null)
                {
                    CircleCollider2D collider = grassObj.AddComponent<CircleCollider2D>();
                    collider.radius = 0.3f;
                    collider.isTrigger = true;
                }

                AddPlantEmitter(grassObj, 3f);
                AddBreathingAnimation(grassObj, 1.2f, 0.04f);
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
            if (AtmosphereManager.Instance != null)
            {
                AtmosphereManager.Instance.UnregisterExchanger(oldExchanger);
            }
            Destroy(oldExchanger);
        }
        
        PlantAgent agent = plant.GetComponent<PlantAgent>();
        if (agent == null)
        {
            agent = plant.AddComponent<PlantAgent>();
        }
        
        agent.plantType = type;
        agent.metabolismScale = 1.0f; // Ensure scale is 1.0f for accuracy (0.1f was for visual)
        
        if (type == PlantAgent.PlantType.Tree)
        {
            agent.R_base = 0.00072f;
            agent.Q10_factor = 2.5f;
            // CRITICAL FIX: Restore P_max to scientifically derived, high growth rate
            agent.P_max = 0.0108f; 
            agent.biomass = 10f;
            agent.maxBiomass = 500f; // Apply max biomass cap
        }
        else // Grass
        {
            agent.R_base = 0.00036f;
            agent.Q10_factor = 2.0f;
            agent.P_max = 0.0072f;
            agent.biomass = 5f;
            agent.maxBiomass = 30f; // Apply max biomass cap
        }
        
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

                // IMPORTANT: The AnimalMetabolism script must be on the prefab or added here 
                // to use the scientific metabolism system, but the old GasExchanger is used below.
                // We assume the AnimalMetabolism script is now handling the gas exchange logic internally.
                
                AddGasExchanger(go, GasExchanger.EntityType.Animal);
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

    void SpawnMen()
    {
        if (!manPrefab || manCount <= 0) return;

        for (int i = 0; i < manCount; i++)
        {
            if (TryGetFreePos(out Vector3 pos))
            {
                var go = Instantiate(manPrefab, pos, Quaternion.identity);
                occupied.Add(pos);

                AddGasExchanger(go, GasExchanger.EntityType.Human);
                AddBreathingAnimation(go, 1.2f, 0.06f);

                var mover = go.GetComponent<HumanAgent>();
                if (mover)
                {
                    mover.areaCenter = areaCenter;
                    mover.areaHalfExtents = halfExtents;
                    mover.canMove = true;
                    mover.seekMateForBreeding = true;
                }
            }
            else Debug.LogWarning("Could not find free spot for a man.");
        }
    }

    void SpawnWomen()
    {
        if (!womanPrefab || womanCount <= 0) return;

        for (int i = 0; i < womanCount; i++)
        {
            if (TryGetFreePos(out Vector3 pos))
            {
                var go = Instantiate(womanPrefab, pos, Quaternion.identity);
                occupied.Add(pos);

                AddGasExchanger(go, GasExchanger.EntityType.Human);
                AddBreathingAnimation(go, 1.2f, 0.06f);

                var mover = go.GetComponent<HumanAgent>();
                if (mover)
                {
                    mover.areaCenter = areaCenter;
                    mover.areaHalfExtents = halfExtents;
                    mover.canMove = true;
                    mover.seekMateForBreeding = true;
                }
            }
            else Debug.LogWarning("Could not find free spot for a woman.");
        }
    }

    bool TryGetFreePos(out Vector3 pos)
    {
        for (int t = 0; t < maxTriesPerSpawn; t++)
        {
            float x = Random.Range(-halfExtents.x, halfExtents.x);
            
            float minY = oceanTopY - areaCenter.y + 0.2f;
            float y = Random.Range(minY, halfExtents.y);
            
            Vector3 candidate = new Vector3(areaCenter.x + x, areaCenter.y + y, 0f);

            bool tooClose = false;
            foreach (var used in occupied)
            {
                if (Vector2.Distance(candidate, used) < minSpacing) { tooClose = true; break; }
            }
            if (!tooClose) { pos = candidate; return true; }
        }
        
        float reducedSpacing = minSpacing * 0.5f;
        
        for (int t = 0; t < maxTriesPerSpawn; t++)
        {
            float x = Random.Range(-halfExtents.x, halfExtents.x);
            float minY = oceanTopY - areaCenter.y + 0.2f;
            float y = Random.Range(minY, halfExtents.y);
            Vector3 candidate = new Vector3(areaCenter.x + x, areaCenter.y + y, 0f);

            bool tooClose = false;
            foreach (var used in occupied)
            {
                if (Vector2.Distance(candidate, used) < reducedSpacing) { tooClose = true; break; }
            }
            if (!tooClose) { pos = candidate; return true; }
        }
        
        Debug.LogError($"[WorldLogic] FAILED to find free position after {maxTriesPerSpawn * 2} attempts! Occupied: {occupied.Count}");
        pos = default;
        return false;
    }

    void SpawnSunAndMoon()
    {
        Transform sunT = null, moonT = null;

        if (sunPrefab && sunCount > 0)
        {
            var go = Instantiate(sunPrefab, Vector3.zero, Quaternion.identity);
            sunT = go.transform;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 100;
        }

        if (moonPrefab && moonCount > 0)
        {
            var go = Instantiate(moonPrefab, Vector3.zero, Quaternion.identity);
            moonT = go.transform;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 100;
        }

        var ctrl = GetComponent<SunMoonController>();
        if (!ctrl) ctrl = gameObject.AddComponent<SunMoonController>();

        ctrl.sun = sunT;
        ctrl.moon = moonT;
        ctrl.areaCenter = areaCenter;
        ctrl.areaHalfExtents = halfExtents;

        ctrl.fullDaySeconds = 120f;
        ctrl.topMargin = 1f;
        ctrl.arcHeight = 1.5f;
        ctrl.horizontalPadding = 0.5f;
    }
    
    void SetupGrassRespawn()
    {
        GrassRespawnManager respawnManager = FindAnyObjectByType<GrassRespawnManager>();
        if (respawnManager == null)
        {
            GameObject managerObj = new GameObject("GrassRespawnManager");
            respawnManager = managerObj.AddComponent<GrassRespawnManager>();
        }
        
        respawnManager.grassPrefab = grassPrefab;
        respawnManager.worldLogic = this;
    }
    
    void SetupHumanRespawn()
    {
        HumanRespawnManager humanManager = FindAnyObjectByType<HumanRespawnManager>();
        if (humanManager == null)
        {
            GameObject managerObj = new GameObject("HumanRespawnManager");
            humanManager = managerObj.AddComponent<HumanRespawnManager>();
        }
        
        humanManager.manPrefab = manPrefab;
        humanManager.womanPrefab = womanPrefab;
        humanManager.breedOncePerDay = true;
        humanManager.breedingDistance = 1.5f;
        humanManager.maxPopulation = 6;
        humanManager.maxMen = 3;
        humanManager.maxWomen = 3;
    }
}