# Biomass Energy Flow System - Complete Summary

## Overview
A complete trophic energy flow system has been implemented for your ecosystem simulation. Entities now have health/biomass that changes based on photosynthesis, respiration, and eating behavior, creating a realistic food chain.

## System Architecture

```
┌─────────────────────────────────────────────────┐
│         ATMOSPHERIC SYSTEM (Existing)          │
│  GasExchanger.cs + AtmosphereManager.cs        │
│  • O₂/CO₂ exchange (molar calculations)       │
│  • Day/night photosynthesis                    │
│  • Ocean CO₂ absorption                        │
└─────────────────┬───────────────────────────────┘
                  │ Works alongside
┌─────────────────┴───────────────────────────────┐
│        BIOMASS/HEALTH SYSTEM (New)             │
│            BiomassEnergy.cs                    │
│  • Plant biomass (photosynthesis gain)         │
│  • Animal/Human health (eating behavior)       │
│  • Respiration energy drain                    │
│  • Trophic efficiency (10-20% transfer)        │
│  • Death conditions                            │
└─────────────────┬───────────────────────────────┘
                  │ Displayed by
┌─────────────────┴───────────────────────────────┐
│          TOOLTIP UI SYSTEM (New)               │
│           EntityTooltip.cs                     │
│  • Mouse hover detection                       │
│  • Health/Biomass display                      │
│  • Color-coded status                          │
│  • Hunger/Digestion indicators                 │
└─────────────────────────────────────────────────┘
```

## Energy Flow (Trophic Levels)

### Level 1: Primary Producers (Plants)
**Trees & Grass**
- **Biomass Storage**: 100 units max
- **Photosynthesis**: +1.0 biomass/sec (day only)
- **Respiration**: -0.5 biomass/sec (24/7)
- **Net Day Rate**: +0.5 biomass/sec (gaining)
- **Net Night Rate**: -0.5 biomass/sec (losing)
- **Death**: Biomass ≤ 0 (withered)
- **Can be eaten**: Yes (by herbivores)

### Level 2: Primary Consumers (Herbivores)
**Animals**
- **Health Storage**: 100 units max
- **Respiration**: -0.3 health/sec (24/7)
- **Eating**: Searches for grass when health < 50
- **Trophic Efficiency**: 15% (if grass has 100 biomass → animal gains 15 health)
- **Search Radius**: 3 units
- **Eat Cooldown**: 10 seconds (digestion time)
- **Death**: Health ≤ 0 (starved)
- **Can be hunted**: Yes (by carnivores)

### Level 3: Secondary Consumers (Carnivores)
**Humans**
- **Health Storage**: 100 units max
- **Respiration**: -0.5 health/sec (higher metabolic rate)
- **Hunting**: Searches for animals when health < 60
- **Trophic Efficiency**: 10% (if animal has 80 health → human gains 8 health)
- **Search Radius**: 5 units (larger hunting range)
- **Eat Cooldown**: 30 seconds (longer digestion)
- **Death**: Health ≤ 0 (starved)

## Energy Transfer Example

```
DAY 1 (12 hours day):
Tree: 100 biomass
  + Photosynthesis: +1.0 × 43,200s = +43,200
  - Respiration: -0.5 × 43,200s = -21,600
  = Net: +21,600 biomass (grows)

NIGHT 1 (12 hours):
Tree: 121,600 biomass
  - Respiration: -0.5 × 43,200s = -21,600
  = Net: 100,000 biomass (back to starting)

EATING CASCADE:
Grass (100 biomass) → eaten by Animal
  → Animal gains: 100 × 0.15 = 15 health

Animal (80 health) → hunted by Human
  → Human gains: 80 × 0.10 = 8 health
```

## Files Created

### 1. BiomassEnergy.cs (229 lines)
**Location**: `Assets/Scripts/BiomassEnergy.cs`

**Key Features**:
- Entity type enum (Plant, Herbivore, Carnivore)
- Biomass/Health variables with max values
- Respiration drain (constant energy loss)
- Photosynthesis gain (plants only, day only)
- Eating behavior (FindNearbyFood, ConsumeFood)
- Death conditions and cleanup
- UI helper methods (GetStatusString, GetHealthColor)

**Public Variables** (Inspector-configurable):
```csharp
float maxEnergy = 100f;           // Max biomass/health
float currentEnergy = 100f;       // Current biomass/health
float respirationDrain = 0.5f;    // Energy loss per second
float photosynthesisGain = 1.0f;  // Biomass gain per second (plants)
float hungerThreshold = 50f;      // Start searching for food
float searchRadius = 3f;          // How far to search
float eatCooldown = 10f;          // Time between meals
float trophicEfficiency = 0.15f;  // Energy transfer % (0.1 = 10%)
```

### 2. EntityTooltip.cs (117 lines)
**Location**: `Assets/Scripts/EntityTooltip.cs`

**Key Features**:
- Mouse raycast detection (Physics2D.OverlapCircleAll)
- Entity hover detection via Circle Collider 2D
- Dynamic tooltip positioning (follows cursor)
- Screen boundary clamping (stays visible)
- Color-coded health display
- Digestion cooldown indicator

**Public Variables** (Inspector-configurable):
```csharp
Text tooltipText;              // Reference to UI Text component
GameObject tooltipPanel;       // Reference to background panel
Vector2 tooltipOffset = (20, -20);  // Cursor offset
float hoverDistance = 0.5f;    // Detection radius
```

## Documentation Created

### 1. TOOLTIP_UI_SETUP.md
Step-by-step Unity Editor setup:
- Create UI Canvas with tooltip panel
- Add EntityTooltip script
- Configure text styling and positioning
- Add Circle Collider 2D to entity prefabs
- Testing and troubleshooting

### 2. BIOMASS_INTEGRATION_GUIDE.md
Code integration instructions:
- Modify WorldLogic.AddGasExchanger() method
- Add BiomassEnergy component to spawned entities
- Configure energy rates per entity type
- Balance testing guidelines
- Expected behavior patterns

### 3. BIOMASS_ENERGY_SYSTEM_SUMMARY.md (this file)
Complete system overview and usage reference

## Integration Checklist

### Unity Editor Setup
- [ ] Create UI Canvas → TooltipCanvas
- [ ] Add Panel → TooltipPanel (semi-transparent black)
- [ ] Add Text → TooltipText (white, size 14)
- [ ] Add EntityTooltip script to canvas
- [ ] Drag text/panel references in Inspector
- [ ] Add Circle Collider 2D to all entity prefabs:
  - [ ] Tree prefab (radius ~0.3, Is Trigger ✅)
  - [ ] Grass prefab (radius ~0.2, Is Trigger ✅)
  - [ ] Animal prefab (radius ~0.3, Is Trigger ✅)
  - [ ] Human prefab (radius ~0.3, Is Trigger ✅)

### Code Integration
- [ ] Open WorldLogic.cs
- [ ] Add `entityType` parameter to AddGasExchanger()
- [ ] Add BiomassEnergy component creation
- [ ] Create ConfigureBiomass() method
- [ ] Update SpawnTree() calls
- [ ] Update SpawnGrass() calls
- [ ] Update SpawnAnimal() calls
- [ ] Update SpawnHuman() calls
- [ ] Test in Play Mode

## Expected Behavior

### Startup (Day 1, 7:00 AM)
- 10 trees spawn with 100 biomass each
- 50 grass spawn with 100 biomass each
- 10 animals spawn with 80 health each (slightly hungry)
- 1 human spawns with 70 health (moderately hungry)

### First Hour
- **Plants**: Gaining biomass (photosynthesis > respiration)
- **Animals**: Losing health slowly, searching for grass
- **Human**: Losing health, searching for animals
- **Tooltip**: Hover shows "Health: 79.5/100 (80%) - Good"

### First Day/Night Cycle
- **Day (7:00-19:00)**: Plants grow, animals eat grass
- **Night (19:00-7:00)**: Plants shrink, animals still eat
- **Population**: Stable if rates balanced

### Long-Term Patterns
- **Grass Overpopulation**: Animals thrive, human well-fed
- **Grass Depletion**: Animals starve, human struggles
- **Animal Extinction**: Human starves (no food source)
- **Atmospheric Cascade**: Less plants → O₂ drops → environment degrades

## Balancing Guidelines

### If Plants Die Too Quickly:
```csharp
// In ConfigureBiomass(), Plant case:
biomass.respirationDrain = 0.3f;      // Was 0.5f
biomass.photosynthesisGain = 1.5f;    // Was 1.0f
```

### If Animals Starve Constantly:
```csharp
// In ConfigureBiomass(), Herbivore case:
biomass.respirationDrain = 0.2f;      // Was 0.3f
biomass.trophicEfficiency = 0.20f;    // Was 0.15f
biomass.searchRadius = 5f;            // Was 3f
biomass.eatCooldown = 5f;             // Was 10f
```

### If Human Dies Too Fast:
```csharp
// In ConfigureBiomass(), Carnivore case:
biomass.respirationDrain = 0.3f;      // Was 0.5f
biomass.trophicEfficiency = 0.15f;    // Was 0.10f
biomass.searchRadius = 8f;            // Was 5f
```

### If Ecosystem Collapses (All Die):
```csharp
// In WorldLogic.cs Start():
public int grassCount = 100;          // Was 50 (double grass)
```

## Debug Monitoring

### Console Logs to Watch
```
[BiomassEnergy] Animal3 ate Grass12, gained 15.0 health
[BiomassEnergy] Human0 hunted Animal5, gained 8.0 health
[BiomassEnergy] Grass8 died (withered)! Energy: 0.0
[BiomassEnergy] Animal2 died (starved)! Energy: 0.0
```

### Good Signs:
- Plants die occasionally (natural cycle)
- Animals eat grass regularly (every 10-20s)
- Human hunts occasionally (every 30-60s)
- Population fluctuates but stabilizes

### Bad Signs:
- All grass dies within first day → increase photosynthesisGain
- All animals dead within hours → increase trophicEfficiency
- Human starves immediately → increase animal count
- No deaths ever → increase respirationDrain

## Visual Indicators

The BiomassEnergy component already changes sprite color on death:
```csharp
sprite.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);  // Gray & transparent
```

You can extend this to show health while alive:
```csharp
// In BiomassEnergy.Update(), add:
SpriteRenderer sprite = GetComponent<SpriteRenderer>();
if (sprite != null && isAlive)
{
    sprite.color = Color.Lerp(Color.red, Color.white, currentEnergy / maxEnergy);
}
```

## Atmospheric Integration

The BiomassEnergy system works alongside (not replacing) the atmospheric system:

### Parallel Systems:
1. **GasExchanger**: Handles O₂/CO₂ exchange with atmosphere
2. **BiomassEnergy**: Handles entity survival and energy flow

### When Entity Dies:
```csharp
// BiomassEnergy.Die() calls:
if (gasExchanger != null)
{
    gasExchanger.Die();  // Stops atmospheric contribution
}
Destroy(gameObject, 3f);  // Removes from world
```

### Atmospheric Impact:
- Plant dies → O₂ production stops → atmospheric O₂ drops
- Animal dies → O₂ consumption stops → atmospheric O₂ rises slightly
- Human dies → massive O₂ consumption stops → O₂ recovers

## Testing Protocol

### 1. Initial Spawn Test (1 minute)
- ✅ All entities spawn with correct types
- ✅ Tooltip shows correct biomass/health values
- ✅ Plants show "🌞 Photosynthesis" during day

### 2. Day/Night Cycle Test (24 in-game hours)
- ✅ Plant biomass increases during day
- ✅ Plant biomass decreases during night
- ✅ Net plant biomass stable over 24h cycle

### 3. Eating Behavior Test (5 minutes)
- ✅ Animals search for nearby grass when health < 50
- ✅ Animals consume grass and gain health
- ✅ Tooltip shows "Digesting... Xs" after eating
- ✅ Grass disappears after being eaten

### 4. Hunting Behavior Test (5 minutes)
- ✅ Human searches for animals when health < 60
- ✅ Human hunts animal successfully
- ✅ Animal dies and disappears
- ✅ Human gains health from hunt

### 5. Death Cascade Test (speed up time ×12)
- ✅ Plant dies when biomass = 0
- ✅ Animal dies when health = 0 (no food)
- ✅ Human dies when health = 0 (no prey)
- ✅ Dead entities turn gray and fade out

### 6. Population Stability Test (10 in-game days)
- ✅ Population fluctuates but doesn't collapse
- ✅ Grass regrows (respawns?) when depleted
- ✅ Animals maintain ~5-10 population
- ✅ Atmospheric O₂ remains stable

## Known Limitations

### No Grass Respawn
- Grass doesn't regrow after being eaten
- Solution: Add grass respawn system or increase initial count

### No Animal Reproduction
- Animals don't reproduce
- Solution: Add breeding when health > 80%

### No Corpse Decomposition
- Dead entities just disappear after 3s
- Solution: Convert to "Dead" state that releases CO₂

### No Seasonal Variations
- Photosynthesis rate constant year-round
- Solution: Add seasonal multipliers to photosynthesisGain

## Future Enhancements

### Population Manager
```csharp
// Respawn grass periodically
if (grassCount < 50)
{
    SpawnGrass();
}
```

### Breeding System
```csharp
// In BiomassEnergy.cs, Herbivore section:
if (currentEnergy > 80f && Time.time - lastBreedTime > breedCooldown)
{
    Reproduce();
}
```

### Corpse System
```csharp
// Replace Die() instant destroy with:
public void Die()
{
    isAlive = false;
    StartCoroutine(Decompose());
}

IEnumerator Decompose()
{
    // Release stored CO₂ back to atmosphere
    float co2ToRelease = currentEnergy * 0.5f;
    AtmosphereManager.Instance.AddCO2(co2ToRelease);
    
    yield return new WaitForSeconds(10f);
    Destroy(gameObject);
}
```

### Health Bar UI
```csharp
// Add world-space health bar above each entity
Canvas worldCanvas;
Slider healthBar;

void Update()
{
    healthBar.value = currentEnergy / maxEnergy;
}
```

---

## Quick Start Summary

1. **Add Colliders**: Circle Collider 2D to all entity prefabs (Is Trigger ✅)
2. **Setup UI**: Create Canvas → Panel → Text, add EntityTooltip script
3. **Integrate Code**: Modify WorldLogic.AddGasExchanger() to add BiomassEnergy
4. **Test**: Play mode, hover entities, watch eating/hunting behavior
5. **Balance**: Adjust energy rates if entities die too fast/slow

**Result**: Complete trophic energy flow with hover tooltips showing real-time health/biomass status!

---
**System Status**: ✅ Complete - Ready for Integration  
**Files**: BiomassEnergy.cs, EntityTooltip.cs, 3 documentation files  
**Next Steps**: Follow BIOMASS_INTEGRATION_GUIDE.md to integrate into WorldLogic
