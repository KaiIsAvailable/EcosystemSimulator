using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple developer control panel: press keys to spawn entities or trigger actions.
/// D = Spawn Human, F = Spawn Animal, G = Command a human to kill a nearby animal (instant)
/// </summary>
public class ControlPanelUI : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnRadiusFromCenter = 2f;
    
    [Header("Human Prefabs")]
    public GameObject manPrefab;
    public GameObject womanPrefab;
    
    [Header("UI Buttons")]
    public Button spawnHumanButton;
    public Button spawnAnimalButton;
    public Button quickKillButton;

    void Start()
    {
        // Setup button click listeners
        if (spawnHumanButton != null)
        {
            spawnHumanButton.onClick.AddListener(SpawnHuman);
        }
        
        if (spawnAnimalButton != null)
        {
            spawnAnimalButton.onClick.AddListener(SpawnAnimal);
        }
        
        if (quickKillButton != null)
        {
            quickKillButton.onClick.AddListener(QuickHumanKill);
        }
    }

    void Update()
    {
        // Spawn Human
        if (Input.GetKeyDown(KeyCode.D))
        {
            SpawnHuman();
        }

        // Spawn Animal
        if (Input.GetKeyDown(KeyCode.F))
        {
            SpawnAnimal();
        }

        // Quick kill/hunt: nearest human kills nearest animal (instant death)
        if (Input.GetKeyDown(KeyCode.G))
        {
            QuickHumanKill();
        }
    }

    void SpawnHuman()
    {
        // Randomly choose to spawn man or woman
        GameObject prefab = Random.value > 0.5f ? manPrefab : womanPrefab;
        
        // Fallback: Try to find prefabs from WorldLogic if not assigned
        if (prefab == null)
        {
            var world = FindAnyObjectByType<WorldLogic>();
            if (world != null)
            {
                prefab = Random.value > 0.5f ? world.manPrefab : world.womanPrefab;
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning("[ControlPanelUI] No man/woman prefab assigned or found on WorldLogic.");
            return;
        }

        Vector3 pos = GetRandomLandPosition();
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        
        string genderName = go.name.Contains("Man") || go.name.Contains("man") ? "Man" : "Woman";
        Debug.Log($"[ControlPanelUI] Spawned {genderName} at {pos}");

        // Notify EventNotificationUI about the new human
        if (EventNotificationUI.Instance != null)
        {
            string humanName = go != null ? go.name : genderName;
            EventNotificationUI.Instance.AddNotification($"[SPAWN] {humanName} spawned", Color.black);
        }
    }

    void SpawnAnimal()
    {
        // Prefer using AnimalRespawnManager if present (it may have spawn rules)
        var respawn = FindAnyObjectByType<AnimalRespawnManager>();
        if (respawn != null)
        {
            // Manual spawn - call immediate spawn which bypasses the population cap
            respawn.SpawnAnimalImmediate();
            Debug.Log("[ControlPanelUI] Triggered AnimalRespawnManager immediate spawn (manual).");
            return;
        }

        // Fallback: instantiate animal prefab from WorldLogic
        var world = FindAnyObjectByType<WorldLogic>();
        GameObject prefab = world != null ? world.animalPrefab : null;

        if (prefab == null)
        {
            Debug.LogWarning("[ControlPanelUI] No animal prefab found and no AnimalRespawnManager present.");
            return;
        }

        Vector3 pos = GetRandomLandPosition();
        Instantiate(prefab, pos, Quaternion.identity);
        Debug.Log($"[ControlPanelUI] Spawned Animal at {pos}");
    }

    void QuickHumanKill()
    {
        // Find any human in scene
        HumanMetabolism human = FindAnyObjectByType<HumanMetabolism>();
        if (human == null)
        {
            Debug.LogWarning("[ControlPanelUI] No human found to command.");
            return;
        }

        // Find nearest animal to that human
        AnimalMetabolism[] animals = FindObjectsOfType<AnimalMetabolism>();
        if (animals == null || animals.Length == 0)
        {
            Debug.LogWarning("[ControlPanelUI] No animals found to kill.");
            return;
        }

        AnimalMetabolism nearest = null;
        float bestDist = float.MaxValue;
        foreach (var a in animals)
        {
            if (!a.isAlive) continue;
            float d = Vector2.Distance(human.transform.position, a.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = a;
            }
        }

        if (nearest != null)
        {
            // Instant kill (no eating)
            nearest.Die();
            Debug.Log($"[ControlPanelUI] Commanded {human.gameObject.name} to kill {nearest.gameObject.name} (instant)");
        }
        else
        {
            Debug.LogWarning("[ControlPanelUI] No alive animal found to kill.");
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
        return new Vector3(Random.Range(-spawnRadiusFromCenter, spawnRadiusFromCenter), Random.Range(-spawnRadiusFromCenter, spawnRadiusFromCenter), 0f);
    }
}
