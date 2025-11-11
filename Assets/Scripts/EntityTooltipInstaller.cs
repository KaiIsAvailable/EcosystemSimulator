using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unity Editor tool to automatically add BiomassEnergy and Colliders to entity prefabs
/// Menu: Tools → Setup Entity Tooltip System
/// </summary>
public class EntityTooltipInstaller : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Setup Entity Tooltip System")]
    public static void SetupEntities()
    {
        Debug.Log("=== Starting Entity Tooltip System Setup ===");
        
        // Find prefabs
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tree.prefab");
        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grass.prefab");
        GameObject animalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Animal.prefab");
        GameObject humanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Human.prefab");
        
        int setupCount = 0;
        
        // Setup Tree
        if (treePrefab != null)
        {
            SetupPrefab(treePrefab, "Tree", 0.4f, BiomassEnergy.EntityType.Plant);
            setupCount++;
        }
        else Debug.LogWarning("Tree prefab not found!");
        
        // Setup Grass
        if (grassPrefab != null)
        {
            SetupPrefab(grassPrefab, "Grass", 0.25f, BiomassEnergy.EntityType.Plant);
            setupCount++;
        }
        else Debug.LogWarning("Grass prefab not found!");
        
        // Setup Animal
        if (animalPrefab != null)
        {
            SetupPrefab(animalPrefab, "Animal", 0.35f, BiomassEnergy.EntityType.Herbivore);
            setupCount++;
        }
        else Debug.LogWarning("Animal prefab not found!");
        
        // Setup Human
        if (humanPrefab != null)
        {
            SetupPrefab(humanPrefab, "Human", 0.4f, BiomassEnergy.EntityType.Carnivore);
            setupCount++;
        }
        else Debug.LogWarning("Human prefab not found!");
        
        Debug.Log($"=== ✅ Setup Complete! {setupCount}/4 prefabs configured ===");
        Debug.Log("Next: Enter Play Mode and hover over entities to see tooltips!");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    static void SetupPrefab(GameObject prefab, string name, float colliderRadius, BiomassEnergy.EntityType type)
    {
        Debug.Log($"Setting up {name} prefab...");
        
        // Add Circle Collider 2D if missing
        CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = prefab.AddComponent<CircleCollider2D>();
            Debug.Log($"  ✓ Added CircleCollider2D");
        }
        collider.radius = colliderRadius;
        collider.isTrigger = true;
        
        // Add BiomassEnergy if missing
        BiomassEnergy biomass = prefab.GetComponent<BiomassEnergy>();
        if (biomass == null)
        {
            biomass = prefab.AddComponent<BiomassEnergy>();
            Debug.Log($"  ✓ Added BiomassEnergy component");
        }
        
        // Configure BiomassEnergy
        biomass.entityType = type;
        
        switch (type)
        {
            case BiomassEnergy.EntityType.Plant:
                biomass.maxEnergy = 100f;
                biomass.currentEnergy = 100f;
                biomass.respirationLossRate = 0.1f;  // Slower drain for balance
                biomass.photosynthesisGainRate = 0.8f; // Moderate gain
                Debug.Log($"  ✓ Configured as Plant (biomass: 100, respiration: -0.3/s, photosynthesis: +0.8/s)");
                break;
                
            case BiomassEnergy.EntityType.Herbivore:
                biomass.maxEnergy = 100f;
                biomass.currentEnergy = 80f;
                biomass.respirationLossRate = 0.2f;
                biomass.hungerThreshold = 50f;
                biomass.searchRadius = 4f;
                biomass.searchInterval = 2f;
                biomass.trophicEfficiency = 0.18f; // 18% efficiency
                Debug.Log($"  ✓ Configured as Herbivore (health: 80, drain: -0.2/s, search: 4 units)");
                break;
                
            case BiomassEnergy.EntityType.Carnivore:
                biomass.maxEnergy = 100f;
                biomass.currentEnergy = 70f;
                biomass.respirationLossRate = 0.3f;
                biomass.hungerThreshold = 60f;
                biomass.searchRadius = 6f;
                biomass.searchInterval = 3f;
                biomass.trophicEfficiency = 0.12f; // 12% efficiency
                Debug.Log($"  ✓ Configured as Carnivore (health: 70, drain: -0.3/s, search: 6 units)");
                break;
        }
        
        // Mark prefab as dirty to save changes
        EditorUtility.SetDirty(prefab);
    }
#endif
}
