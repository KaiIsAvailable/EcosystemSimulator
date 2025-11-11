# BiomassEnergy Integration Guide

## Overview
This guide shows how to integrate the `BiomassEnergy.cs` component into your existing entity spawning system (`WorldLogic.cs`).

## Current Spawn System

Your entities currently get the `GasExchanger` component added via `AddGasExchanger()`:

```csharp
// In WorldLogic.cs
GasExchanger AddGasExchanger(GameObject go, string name, ...)
{
    go.name = name;
    var ge = go.AddComponent<GasExchanger>();
    ge.oxygenRate = o2Rate;
    ge.co2Rate = co2Rate;
    // ... etc
    return ge;
}
```

## Integration Steps

### 1. Update AddGasExchanger() Method

Add BiomassEnergy component alongside GasExchanger:

```csharp
GasExchanger AddGasExchanger(GameObject go, string name, float o2Rate, float co2Rate, 
    float respRate, float respCO2Rate, BiomassEnergy.EntityType entityType = BiomassEnergy.EntityType.Plant)
{
    go.name = name;
    
    // Add GasExchanger component (atmospheric system)
    var ge = go.AddComponent<GasExchanger>();
    ge.oxygenRate = o2Rate;
    ge.co2Rate = co2Rate;
    ge.respirationRate = respRate;
    ge.respirationCO2Rate = respCO2Rate;
    ge.atmosphereManager = AtmosphereManager.Instance;
    
    // Add BiomassEnergy component (survival/health system)
    var biomass = go.AddComponent<BiomassEnergy>();
    biomass.entityType = entityType;
    
    // Configure based on entity type
    ConfigureBiomass(biomass, entityType);
    
    return ge;
}

void ConfigureBiomass(BiomassEnergy biomass, BiomassEnergy.EntityType type)
{
    switch (type)
    {
        case BiomassEnergy.EntityType.Plant:
            // Trees & Grass
            biomass.maxEnergy = 100f;
            biomass.currentEnergy = 100f;
            biomass.respirationDrain = 0.5f;  // Slow drain
            biomass.photosynthesisGain = 1.0f;  // Day gain
            break;
            
        case BiomassEnergy.EntityType.Herbivore:
            // Animals (eat grass)
            biomass.maxEnergy = 100f;
            biomass.currentEnergy = 80f;  // Start slightly hungry
            biomass.respirationDrain = 0.3f;
            biomass.hungerThreshold = 50f;
            biomass.searchRadius = 3f;
            biomass.eatCooldown = 10f;
            biomass.trophicEfficiency = 0.15f;  // 15% energy transfer
            break;
            
        case BiomassEnergy.EntityType.Carnivore:
            // Humans (hunt animals)
            biomass.maxEnergy = 100f;
            biomass.currentEnergy = 70f;
            biomass.respirationDrain = 0.5f;  // Higher drain
            biomass.hungerThreshold = 60f;
            biomass.searchRadius = 5f;  // Larger hunting range
            biomass.eatCooldown = 30f;  // Longer digestion
            biomass.trophicEfficiency = 0.10f;  // 10% energy transfer
            break;
    }
}
```

### 2. Update SpawnTree() Calls

```csharp
void SpawnTree(Vector3 pos)
{
    var go = Instantiate(treePrefab, pos, Quaternion.identity);
    
    AddGasExchanger(go, $"Tree{treeCount++}", 
        5.0f,    // oxygenRate (photosynthesis)
        -5.0f,   // co2Rate (photosynthesis)
        -0.5f,   // respirationRate (24/7)
        0.5f,    // respirationCO2Rate (24/7)
        BiomassEnergy.EntityType.Plant);  // ← Add this
}
```

### 3. Update SpawnGrass() Calls

```csharp
void SpawnGrass()
{
    for (int i = 0; i < grassCount; i++)
    {
        if (TryGetFreePos(out Vector2 pos))
        {
            var go = Instantiate(grassPrefab, pos, Quaternion.identity);
            
            AddGasExchanger(go, $"Grass{i}", 
                1.1f,    // oxygenRate
                -1.1f,   // co2Rate
                -0.1f,   // respirationRate
                0.1f,    // respirationCO2Rate
                BiomassEnergy.EntityType.Plant);  // ← Add this
        }
    }
}
```

### 4. Update SpawnAnimal() Calls

```csharp
void SpawnAnimal(Vector3 pos)
{
    var go = Instantiate(animalPrefab, pos, Quaternion.identity);
    
    AddGasExchanger(go, $"Animal{animalCount++}", 
        -2.5f,   // oxygenRate (respiration only)
        2.5f,    // co2Rate
        0f,      // no separate respiration
        0f,
        BiomassEnergy.EntityType.Herbivore);  // ← Add this
}
```

### 5. Update SpawnHuman() Calls

```csharp
void SpawnHuman(Vector3 pos)
{
    var go = Instantiate(humanPrefab, pos, Quaternion.identity);
    
    AddGasExchanger(go, $"Human{humanCount++}", 
        -25.0f,  // oxygenRate (high respiration)
        25.0f,   // co2Rate
        0f,
        0f,
        BiomassEnergy.EntityType.Carnivore);  // ← Add this
}
```

## Complete Modified WorldLogic.cs Section

Here's the full modified spawning section:

```csharp
public class WorldLogic : MonoBehaviour
{
    // ... existing prefabs and variables ...
    
    void Start()
    {
        // Spawn ocean (no BiomassEnergy needed)
        SpawnOcean();
        
        // Spawn trees with BiomassEnergy
        for (int i = 0; i < treeCount; i++)
        {
            if (TryGetFreePos(out Vector2 pos))
            {
                SpawnTree(pos);
            }
        }
        
        // Spawn grass with BiomassEnergy
        SpawnGrass();
        
        // Spawn animals with BiomassEnergy
        for (int i = 0; i < animalCount; i++)
        {
            if (TryGetFreePos(out Vector2 pos))
            {
                SpawnAnimal(pos);
            }
        }
        
        // Spawn human with BiomassEnergy
        if (TryGetFreePos(out Vector2 humanPos))
        {
            SpawnHuman(humanPos);
        }
    }
    
    GasExchanger AddGasExchanger(GameObject go, string name, float o2Rate, float co2Rate, 
        float respRate, float respCO2Rate, BiomassEnergy.EntityType entityType = BiomassEnergy.EntityType.Plant)
    {
        go.name = name;
        
        // Add GasExchanger (atmospheric system)
        var ge = go.AddComponent<GasExchanger>();
        ge.oxygenRate = o2Rate;
        ge.co2Rate = co2Rate;
        ge.respirationRate = respRate;
        ge.respirationCO2Rate = respCO2Rate;
        ge.atmosphereManager = AtmosphereManager.Instance;
        
        // Add BiomassEnergy (survival system)
        var biomass = go.AddComponent<BiomassEnergy>();
        biomass.entityType = entityType;
        ConfigureBiomass(biomass, entityType);
        
        return ge;
    }
    
    void ConfigureBiomass(BiomassEnergy biomass, BiomassEnergy.EntityType type)
    {
        switch (type)
        {
            case BiomassEnergy.EntityType.Plant:
                biomass.maxEnergy = 100f;
                biomass.currentEnergy = 100f;
                biomass.respirationDrain = 0.5f;
                biomass.photosynthesisGain = 1.0f;
                break;
                
            case BiomassEnergy.EntityType.Herbivore:
                biomass.maxEnergy = 100f;
                biomass.currentEnergy = 80f;
                biomass.respirationDrain = 0.3f;
                biomass.hungerThreshold = 50f;
                biomass.searchRadius = 3f;
                biomass.eatCooldown = 10f;
                biomass.trophicEfficiency = 0.15f;
                break;
                
            case BiomassEnergy.EntityType.Carnivore:
                biomass.maxEnergy = 100f;
                biomass.currentEnergy = 70f;
                biomass.respirationDrain = 0.5f;
                biomass.hungerThreshold = 60f;
                biomass.searchRadius = 5f;
                biomass.eatCooldown = 30f;
                biomass.trophicEfficiency = 0.10f;
                break;
        }
    }
    
    void SpawnTree(Vector3 pos)
    {
        var go = Instantiate(treePrefab, pos, Quaternion.identity);
        AddGasExchanger(go, $"Tree{treeCount++}", 5.0f, -5.0f, -0.5f, 0.5f, 
            BiomassEnergy.EntityType.Plant);
    }
    
    void SpawnAnimal(Vector3 pos)
    {
        var go = Instantiate(animalPrefab, pos, Quaternion.identity);
        AddGasExchanger(go, $"Animal{animalCount++}", -2.5f, 2.5f, 0f, 0f, 
            BiomassEnergy.EntityType.Herbivore);
    }
    
    void SpawnHuman(Vector3 pos)
    {
        var go = Instantiate(humanPrefab, pos, Quaternion.identity);
        AddGasExchanger(go, $"Human{humanCount++}", -25.0f, 25.0f, 0f, 0f, 
            BiomassEnergy.EntityType.Carnivore);
    }
    
    void SpawnGrass()
    {
        for (int i = 0; i < grassCount; i++)
        {
            if (TryGetFreePos(out Vector2 pos))
            {
                var go = Instantiate(grassPrefab, pos, Quaternion.identity);
                AddGasExchanger(go, $"Grass{i}", 1.1f, -1.1f, -0.1f, 0.1f, 
                    BiomassEnergy.EntityType.Plant);
            }
        }
    }
}
```

## Testing After Integration

### What to Observe

1. **Plants (Trees & Grass)**:
   - Start with 100 biomass
   - Gain biomass during daytime (photosynthesis)
   - Lose biomass at night (respiration only)
   - Die if biomass reaches 0

2. **Animals (Herbivores)**:
   - Start with 80 health
   - Lose health constantly (respiration)
   - Search for grass when health < 50
   - Eat grass to regain health (~15% efficiency)
   - Die if health reaches 0

3. **Humans (Carnivores)**:
   - Start with 70 health
   - Lose health faster (higher respiration)
   - Search for animals when health < 60
   - Hunt animals to regain health (~10% efficiency)
   - Die if health reaches 0

### Debug Console Logs

You'll see messages like:
```
[BiomassEnergy] Animal5 ate Grass23, gained 15.0 health
[BiomassEnergy] Human0 hunted Animal2, gained 8.0 health
[BiomassEnergy] Grass12 died (withered)! Energy: 0.0
[BiomassEnergy] Animal8 died (starved)! Energy: 0.0
```

### Hover Tooltip Shows

When hovering over entities:
- **Tree**: "Biomass: 95.3/100 (95%) - Healthy"
- **Animal**: "Health: 42.7/100 (43%) - Good"
- **Human**: "Health: 67.2/100 (67%) - Good\nDigesting... 15s"

## Balancing Energy Rates

If entities die too quickly/slowly, adjust these values:

### Plants Die Too Fast?
- ↓ Decrease `respirationDrain` (0.5 → 0.3)
- ↑ Increase `photosynthesisGain` (1.0 → 1.5)

### Animals Starve Too Fast?
- ↓ Decrease `respirationDrain` (0.3 → 0.2)
- ↑ Increase `trophicEfficiency` (0.15 → 0.20)
- ↑ Increase `searchRadius` (3.0 → 5.0)
- ↓ Decrease `eatCooldown` (10s → 5s)

### Humans Starve Too Fast?
- ↓ Decrease `respirationDrain` (0.5 → 0.3)
- ↑ Increase `trophicEfficiency` (0.10 → 0.15)
- ↑ Increase `searchRadius` (5.0 → 8.0)

### Ecosystem Collapses?
If all grass dies → animals starve → humans starve:
- ↑ Increase `grassCount` (50 → 100)
- ↑ Increase grass `photosynthesisGain`
- ↓ Decrease animal eating frequency

## Expected Trophic Cascade

With proper balancing, you should observe:

1. **Stable Day**: Plants gain biomass, animals eat moderately
2. **Stable Night**: Plants lose biomass slowly, animals still eat
3. **Population Cycles**: 
   - Too many animals → grass depletes → animals starve
   - Fewer animals → grass recovers
   - Human hunts animals → animal population drops
4. **Atmospheric Impact**:
   - More plants → O₂ increases
   - Plants die → O₂ decreases
   - More animals → O₂ decreases faster

---
**Dependencies**: BiomassEnergy.cs, GasExchanger.cs  
**Files to Modify**: WorldLogic.cs  
**Next**: Add visual health indicators (sprite color tinting)
