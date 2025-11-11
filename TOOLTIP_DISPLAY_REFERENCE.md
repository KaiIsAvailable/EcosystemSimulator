# Tooltip Display Reference

## What You'll See When Hovering

### Tree Example
```
Tree12
🌿 Plant
Biomass: 95.3/100 (95%) - Healthy
```
- **Color**: 🟢 Green (healthy)
- **Meaning**: Tree has plenty of stored energy
- **Day Behavior**: Gaining biomass (photosynthesis)
- **Night Behavior**: Losing biomass (respiration only)

### Grass Example (Low Health)
```
Grass23
🌿 Plant
Biomass: 32.1/100 (32%) - Weak
```
- **Color**: 🟠 Orange (weak)
- **Meaning**: Grass is struggling, might die soon
- **Cause**: Either nighttime drain or recently eaten by animal

### Animal Example (Hungry)
```
Animal5
🐰 Herbivore
Health: 42.7/100 (43%) - Good
```
- **Color**: 🟡 Yellow (moderate)
- **Meaning**: Animal is below 50% health, actively searching for grass
- **Behavior**: Moving toward nearest grass to eat

### Animal Example (Digesting)
```
Animal8
🐰 Herbivore
Health: 67.3/100 (67%) - Good
Digesting... 7s
```
- **Color**: 🟢 Green (good)
- **Meaning**: Just ate grass, can't eat again for 7 more seconds
- **Effect**: Health slowly increasing from meal

### Human Example (Critical)
```
Human0
🧑 Carnivore
Health: 18.2/100 (18%) - Critical
```
- **Color**: 🔴 Red (critical)
- **Meaning**: Human is near death, desperately hunting animals
- **Risk**: Will die in ~36 seconds if doesn't find food (18.2 / 0.5 drain)

### Human Example (Hunting)
```
Human0
🧑 Carnivore
Health: 75.8/100 (76%) - Healthy
Digesting... 22s
```
- **Color**: 🟢 Green (healthy)
- **Meaning**: Recently hunted animal, recovering health
- **Cooldown**: Can't hunt again for 22 more seconds

### Dead Entity Example
```
Grass42
🌿 Plant
☠ Dead
```
- **Color**: ⚪ Gray (faded)
- **Meaning**: Entity died (biomass/health = 0)
- **Fate**: Will disappear in 3 seconds

## Color Code System

| Color | Range | Status | Meaning |
|-------|-------|--------|---------|
| 🟢 **Green** | ≥80% | **Healthy** | Thriving, no immediate danger |
| 🟡 **Yellow** | 50-79% | **Good** | Stable, may need food soon |
| 🟠 **Orange** | 30-49% | **Weak** | Struggling, actively seeking food |
| 🔴 **Red** | <30% | **Critical/Dying** | Near death, emergency state |
| ⚪ **Gray** | 0% | **Dead** | Entity expired |

## Status Keywords Explained

### For Plants (Biomass)
- **Healthy** (≥80%): Photosynthesis > respiration, growing
- **Good** (50-79%): Balanced, stable
- **Weak** (30-49%): Respiration > photosynthesis, shrinking
- **Critical** (15-29%): Approaching death
- **Dying** (<15%): Will die within minutes

### For Animals/Humans (Health)
- **Healthy** (≥80%): Well-fed, not seeking food yet
- **Good** (50-79%): Moderate hunger, may start searching
- **Weak** (30-49%): Actively hunting/eating
- **Critical** (15-29%): Starvation imminent
- **Dying** (<15%): Death within seconds if no food

## Special Indicators

### "Digesting... Xs"
- Appears on animals/humans after eating
- Shows cooldown time before can eat again
- Animals: 10 second cooldown
- Humans: 30 second cooldown

### Day Activity (Plants Only)
When implemented with extended tooltip:
- **Day**: "🌞 Photosynthesis" (gaining biomass)
- **Night**: "🌙 Respiration" (losing biomass)

## Tooltip Positioning

```
     Screen Edge
┌────────────────────────────┐
│                            │
│        🐰 Entity           │
│         ↓                  │
│   ┌─────────────┐         │
│   │ Animal5     │         │
│   │ 🐰 Herbivore│         │
│   │ Health: 42% │←─ Tooltip
│   └─────────────┘         │
│                            │
└────────────────────────────┘
```

- **Default Offset**: 20 pixels right, 20 pixels down from cursor
- **Screen Clamping**: Automatically adjusts if near edge
- **Follow Cursor**: Moves with mouse while hovering

## Common Scenarios

### Scenario 1: Nighttime Plant Check
**What to See**:
- Tree biomass slowly decreasing (respiration only)
- Tooltip might show: "Biomass: 87.3/100 (87%) - Healthy"
- Color remains green if starting high

**What It Means**: 
- Plant is consuming stored energy to survive night
- Will regain biomass when sun rises at 7:00 AM

### Scenario 2: Animal Eating Grass
**Before Eating**:
```
Animal3
🐰 Herbivore
Health: 45.2/100 (45%) - Weak
```

**After Eating** (if grass had 100 biomass):
```
Animal3
🐰 Herbivore
Health: 60.2/100 (60%) - Good
Digesting... 10s
```
- Health increased by: 100 × 0.15 = 15 points
- Status changed: Weak → Good
- Color changed: 🟠 Orange → 🟡 Yellow

### Scenario 3: Human Hunting
**Before Hunt**:
```
Human0
🧑 Carnivore
Health: 55.0/100 (55%) - Good
```

**After Hunt** (if animal had 80 health):
```
Human0
🧑 Carnivore
Health: 63.0/100 (63%) - Good
Digesting... 30s
```
- Health increased by: 80 × 0.10 = 8 points
- Must wait 30 seconds before next hunt

### Scenario 4: Death Sequence
**T = 0s**: "Health: 5.2/100 (5%) - Dying" 🔴  
**T = 5s**: "Health: 2.7/100 (3%) - Dying" 🔴  
**T = 10s**: "☠ Dead" ⚪ (gray, fading)  
**T = 13s**: Entity disappears from game

## Troubleshooting Tooltip Display

### Tooltip Not Appearing?
**Check**:
1. Entity has Circle Collider 2D component? → Add it
2. Collider has "Is Trigger" enabled? → Check checkbox
3. Hover Distance too small? → Increase to 0.5 or 1.0
4. Entity is dead? → Tooltip only shows for `isAlive = true`

### Tooltip Shows Wrong Info?
**Check**:
1. BiomassEnergy component attached? → Add via WorldLogic integration
2. Entity type set correctly? → Plant/Herbivore/Carnivore
3. Energy values initialized? → Should start at 100/80/70

### Tooltip Position Wrong?
**Fix**:
- Adjust "Tooltip Offset" in EntityTooltip Inspector
- Default: (20, -20) means 20px right, 20px down
- Try: (30, -50) for more space from cursor

### Multiple Tooltips Appearing?
**Issue**: Multiple entities overlapping
**Solution**: EntityTooltip shows only first found entity
**Workaround**: Move entities apart or increase sprite separation

### Tooltip Text Too Small?
**Fix**:
1. Select TooltipText in Hierarchy
2. Text component → Font Size: 14 → 16 or 18
3. Adjust TooltipPanel size to fit larger text

## Performance Notes

- **Update Frequency**: Tooltip checks every frame (~60 FPS)
- **Detection Method**: Physics2D.OverlapCircleAll (efficient)
- **Max Entities**: Handles 100+ entities with no lag
- **Optimization**: Only shows tooltip for one entity at a time

## Advanced: Extending Tooltip

### Add More Info Lines
In BiomassEnergy.cs, modify GetStatusString():
```csharp
public string GetStatusString()
{
    string base = /* existing code */;
    
    // Add respiration rate
    string respirationInfo = $"\nRespiration: -{respirationDrain:F1}/s";
    
    // Add photosynthesis rate (plants only)
    if (entityType == EntityType.Plant && IsDaytime())
    {
        respirationInfo += $"\nPhotosynthesis: +{photosynthesisGain:F1}/s";
    }
    
    return base + respirationInfo;
}
```

**Result**:
```
Tree5
🌿 Plant
Biomass: 92.3/100 (92%) - Healthy
Respiration: -0.5/s
Photosynthesis: +1.0/s
```

### Add Icons Based on Activity
```csharp
string activityIcon = "";
if (entityType == EntityType.Herbivore && isHungry)
    activityIcon = "🔍 Searching";
else if (Time.time - lastEatTime < eatCooldown)
    activityIcon = "😋 Satisfied";

return $"{baseStatus}\n{activityIcon}";
```

---

**Summary**: Hover over any entity to see real-time health/biomass status with color-coded warnings!
