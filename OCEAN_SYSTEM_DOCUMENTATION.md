# Ocean Water System Documentation

## Overview
The ocean water system creates a single, connected body of water at the bottom of the map, covering approximately 20% of the total map height. The ocean serves as a CO₂ sink in the atmospheric simulation and prevents entities from spawning in the water.

## Features

### Single Connected Ocean
- **One unified ocean**: Not separated tiles, but a single connected body of water
- **Bottom placement**: Ocean positioned at the bottom of the map
- **Behind ground layer**: Renders at sortingOrder = -10 (background)
- **Configurable height**: Default 20% of map height
- **Full width**: Spans the entire width of the map
- **No-spawn zone**: Trees, grass, animals, and humans cannot spawn on ocean

### CO₂ Absorption
- Ocean acts as a carbon sink in `AtmosphereManager`
- Default absorption: 10 mol CO₂/day
- Helps balance ecosystem CO₂ production
- Configurable via `oceanAbsorptionRate` setting

## Configuration

### In WorldLogic.cs

```csharp
[Header("Prefabs")]
public GameObject oceanWaterPrefab;  // Assign your OceanWater prefab here

[Header("Ocean Settings")]
public bool spawnOcean = true;                      // Enable/disable ocean
public float oceanHeightPercent = 0.2f;             // Ocean height (0.2 = 20% of map)
```

### In AtmosphereManager.cs

```csharp
[Header("Ocean CO₂ Sink (Optional)")]
public float oceanAbsorptionRate = 10f;  // mol CO₂/day
```

## How the Ocean Works

### Ocean Dimensions

```csharp
// Ocean spans full map width
float oceanWidth = mapWidth;  // 100% of map width

// Ocean height is percentage of total map height
float oceanHeight = mapHeight × oceanHeightPercent;  // e.g., 20% of map height

// Ocean positioned at bottom
float oceanY = bottomOfMap + (oceanHeight / 2);
```

### No-Spawn Zone

The ocean creates a restricted zone where entities cannot spawn:

```csharp
// Calculate ocean top boundary
oceanTopY = oceanY + (oceanHeight / 2);

// When spawning entities
if (candidate.y < oceanTopY) {
    // Too close to ocean, try different position
    continue;
}
```

**Result**: All trees, grass, animals, and humans spawn only on land (above ocean level).

### Example Calculation

**Map Setup**:
- Camera orthographicSize: 5 units
- Aspect ratio: 16:9
- Map width: 17.8 units (full width)
- Map height: 10 units
- Total area: 178 square units

**Ocean Dimensions** (20% height):
- Ocean width: 17.8 units (100% of map width)
- Ocean height: 10 × 0.2 = 2 units (20% of map height)
- Ocean area: 17.8 × 2 = **35.6 square units**
- Coverage: 35.6 ÷ 178 = **20% of total map** ✓

**Ocean Position**:
- Bottom of map: Y = -5
- Ocean center: Y = -5 + 1 = -4
- Ocean top (land boundary): Y = -4 + 1 = **-3**
- **All entities spawn above Y = -3**

### Visual Layout

```
┌─────────────────────────────────────┐ ← Top of map (Y = +5)
│         🌙                          │
│    🌳   🌿  🌿                      │
│  🌿 🌿     🦌                       │   Land Area
│        🌳  🌿 🌿                    │   (80% of map)
│   🦌      🌿  🌳                    │   Entities spawn here
│              🌿 🌿  👤              │
├─────────────────────────────────────┤ ← oceanTopY (Y = -3)
│ ≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈ │
│ ≈≈≈≈  Ocean Water (Connected) ≈≈≈≈ │   Ocean Area
│ ≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈ │   (20% of map)
└─────────────────────────────────────┘ ← Bottom of map (Y = -5)
```

## Setup in Unity Editor

### Step 1: Assign Ocean Prefab
1. Select your main **WorldLogic** GameObject
2. In the Inspector, find **World Logic (Script)**
3. Under **Prefabs** section
4. Drag your **OceanWater** prefab → **Ocean Water Prefab** field

### Step 2: Configure Ocean Settings
In the **Ocean Settings** section:
- **Spawn Ocean**: Check to enable ocean (default: true)
- **Ocean Height Percent**: Slider 0-0.5 (default: 0.2 for 20%)

### Step 3: Configure CO₂ Absorption
1. Find the **AtmosphereManager** GameObject
2. In **Ocean CO₂ Sink** section
3. Set **Ocean Absorption Rate** (default: 10 mol/day)

### Step 4: Test
- Press Play
- Check Console for: `[WorldLogic] Spawned ocean: width=X, height=Y, top Y=Z`
- Ocean should appear at **bottom of map** as a connected body of water
- Verify **no entities spawn in the ocean**
- Trees, grass, animals should all be on land (above ocean)

## Ocean Tile Layering

### Sorting Order Hierarchy

| Object | Sorting Order | Layer |
|--------|--------------|-------|
| Ocean Water | -10 | Background (bottom) |
| Ground | -1 | Base layer |
| Grass | 0 | Ground level |
| Trees | 0 | Ground level |
| Animals | 1 | Above ground |
| Humans | 1 | Above ground |
| Gas Particles | 2 | Effects layer |
| Sun/Moon | 10 | Sky layer |
| UI | 100+ | Top layer |

Ocean renders **below everything** so it appears as a background water layer.

## Ocean's Role in Ecosystem

### CO₂ Sink Function

```csharp
// In AtmosphereManager.LogDailyStats()
if (oceanAbsorptionRate > 0f)
{
    netCO2Rate -= oceanAbsorptionRate;  // Reduces CO₂ by 10 mol/day
}
```

### Impact on Gas Balance

**Without Ocean** (oceanAbsorptionRate = 0):
```
Daytime CO₂: -10.0 mol/day
Nighttime CO₂: +55.0 mol/day
24h Average: +22.5 mol/day ❌ (CO₂ accumulates!)
```

**With Ocean** (oceanAbsorptionRate = 10):
```
Daytime CO₂: -10.0 - 10.0 = -20.0 mol/day
Nighttime CO₂: +55.0 - 10.0 = +45.0 mol/day
24h Average: +12.5 mol/day ⚠️ (Still accumulates, but slower)
```

### Real Ocean CO₂ Absorption

Oceans absorb ~25% of human CO₂ emissions in real life. In this simulation:
- Default 10 mol/day ≈ 20% of animal+human CO₂ production (50 mol/day)
- Can increase to 25 mol/day for stronger carbon sink
- Set to 0 to disable ocean absorption (e.g., for testing)

## Customization Options

### 1. Increase Ocean Coverage

```csharp
// Cover 30% of map height instead of 20%
oceanHeightPercent = 0.3f;
```

### 2. Decrease Ocean Coverage

```csharp
// Cover only 10% of map height
oceanHeightPercent = 0.1f;
```

### 3. Disable Ocean

```csharp
// Remove ocean entirely
spawnOcean = false;
// OR uncheck "Spawn Ocean" in Inspector
```

### 4. Stronger CO₂ Sink

```csharp
// In AtmosphereManager
oceanAbsorptionRate = 25f;  // Absorbs 50% of animal+human CO₂
```

### 5. Ocean at Top Instead of Bottom

```csharp
// In SpawnOcean() method
float oceanY = areaCenter.y + halfExtents.y - (oceanHeight / 2f);  // Top instead
oceanTopY = oceanY - (oceanHeight / 2f);  // Entities spawn BELOW this

// Update TryGetFreePos to check:
if (candidate.y > oceanTopY) continue;  // Change < to >
```

## Troubleshooting

### Issue: Ocean not visible
**Solutions**:
1. Check oceanWaterPrefab is assigned in Inspector
2. Verify SpriteRenderer exists on prefab
3. Check sortingOrder is -10 (background)
4. Ensure camera can see the bottom of the map
5. Verify `spawnOcean = true`

### Issue: Ocean covers entire map
**Solutions**:
1. Reduce `oceanHeightPercent` (try 0.1 or 0.2)
2. Check console logs for ocean dimensions
3. Ensure oceanHeightPercent is between 0 and 0.5

### Issue: Entities spawning in ocean
**Solutions**:
1. Check `oceanTopY` value in console logs
2. Verify `TryGetFreePos()` checks `candidate.y < oceanTopY`
3. Increase `maxTriesPerSpawn` if map is too crowded

### Issue: Ocean doesn't absorb CO₂
**Solutions**:
1. Verify `oceanAbsorptionRate > 0` in AtmosphereManager
2. Check `useBiochemicalModel = true`
3. Look for ocean absorption in console debug logs

### Issue: No entities spawn at all
**Solutions**:
1. Ocean might be too large, leaving no land
2. Reduce `oceanHeightPercent` to 0.15 or less
3. Check `oceanTopY` - should leave at least 50% of map for land

## Advanced Features

### Visual Effects on Ocean Prefab

Add components to enhance the ocean:
- **Animator**: Gentle wave animation
- **Particle System**: Bubbles, foam, or ripples
- **Shader**: Reflective water shader with transparency
- **Script**: Animated UV scrolling for wave effect

### Dynamic Ocean Level (Climate Change)

```csharp
// Expand ocean based on CO₂ levels (simulating sea level rise)
void Update() {
    if (AtmosphereManager.Instance.carbonDioxide > 0.05f) {
        // Global warming → sea level rise
        oceanHeightPercent = Mathf.Min(0.3f, oceanHeightPercent + Time.deltaTime * 0.001f);
        UpdateOceanSize();
    }
}

void UpdateOceanSize() {
    // Rescale ocean GameObject
    float newHeight = halfExtents.y * 2f * oceanHeightPercent;
    ocean.transform.localScale = new Vector3(oceanWidth, newHeight, 1f);
    
    // Update no-spawn boundary
    oceanTopY = oceanY + (newHeight / 2f);
}
```

### Ocean Life (Algae/Plankton)

```csharp
// Add photosynthetic ocean organisms
void SpawnOceanLife() {
    // Calculate ocean bounds
    float oceanLeft = areaCenter.x - halfExtents.x;
    float oceanRight = areaCenter.x + halfExtents.x;
    float oceanBottom = areaCenter.y - halfExtents.y;
    
    // Spawn algae in ocean zone
    for (int i = 0; i < 10; i++) {
        float x = Random.Range(oceanLeft, oceanRight);
        float y = Random.Range(oceanBottom, oceanTopY);
        
        GameObject algae = Instantiate(algaePrefab, new Vector3(x, y, 0), Quaternion.identity);
        AddGasExchanger(algae, GasExchanger.EntityType.Grass);  // Similar to grass
        AddBreathingAnimation(algae, 1.5f, 0.05f);
    }
}
```

## Performance Notes

- **Single GameObject**: Ocean is just 1 object (not multiple tiles)
- **Minimal Overhead**: One SpriteRenderer with simple scaling
- **No Physics**: Ocean is purely visual (no colliders needed)
- **Memory**: Single ocean ≈ negligible memory impact
- **Batching**: Automatically batched with other sprites

## Summary

✅ **Single connected ocean** at bottom of map  
✅ **Spans full width** of the map  
✅ **Covers 20% of map height** (configurable 0-50%)  
✅ **Renders behind ground** (sortingOrder = -10)  
✅ **No-spawn zone**: Entities only spawn on land (above ocean)  
✅ **Absorbs 10 mol CO₂/day** (configurable)  
✅ **Realistic representation** of ocean as carbon sink  

The ocean system creates a **unified, connected body of water** that adds both **visual realism** and **ecological accuracy** to your ecosystem simulator! 🌊
