# Entity Tooltip UI Setup Guide

## Overview
The tooltip system shows entity health/biomass details when hovering your mouse over them. It uses the `EntityTooltip.cs` component attached to a UI Canvas.

## Files Created
1. **EntityTooltip.cs** - Main tooltip controller (uses mouse raycast to detect entities)
2. **BiomassEnergy.cs** - Already has helper methods:
   - `GetStatusString()` - Returns formatted health/biomass info
   - `GetHealthColor()` - Returns color based on health percentage
   - `isAlive` - Flag to show/hide tooltip for dead entities

## Unity Setup Steps

### 1. Create UI Canvas
1. In Unity Hierarchy, right-click → **UI → Canvas**
2. Name it "TooltipCanvas"
3. Set **Canvas** component settings:
   - Render Mode: **Screen Space - Overlay**
   - Pixel Perfect: ✅ (optional)

### 2. Create Tooltip Panel
1. Right-click TooltipCanvas → **UI → Panel**
2. Name it "TooltipPanel"
3. Configure **RectTransform**:
   - Anchor: Bottom-left corner (X: 0, Y: 0)
   - Pivot: (0, 0)
   - Width: 250
   - Height: 100
4. Configure **Image** component:
   - Color: Black with alpha ~200 (semi-transparent)

### 3. Create Tooltip Text
1. Right-click TooltipPanel → **UI → Text**
2. Name it "TooltipText"
3. Configure **Text** component:
   - Font Size: 14
   - Color: White
   - Alignment: Left & Top
   - Vertical Overflow: Overflow
   - Horizontal Overflow: Wrap
4. Configure **RectTransform**:
   - Anchor: Stretch both axes
   - Left: 10, Right: -10, Top: -10, Bottom: 10

### 4. Attach EntityTooltip Script
1. Select **TooltipCanvas** in Hierarchy
2. Add Component → **EntityTooltip** script
3. In Inspector, drag references:
   - **Tooltip Text** → TooltipText object
   - **Tooltip Panel** → TooltipPanel object
4. Configure settings:
   - Tooltip Offset: (20, -20) - keeps tooltip near cursor
   - Hover Distance: 0.5 - detection radius in world units

### 5. Add Colliders to Entities (IMPORTANT!)
The tooltip uses `Physics2D.OverlapCircleAll()` to detect entities, so they need colliders:

1. Select **Tree prefab**:
   - Add Component → **Circle Collider 2D**
   - Radius: ~0.3 (adjust to sprite size)
   - Is Trigger: ✅ (prevents physics collisions)

2. Select **Grass prefab**:
   - Add Component → **Circle Collider 2D**
   - Radius: ~0.2
   - Is Trigger: ✅

3. Select **Animal prefab**:
   - Add Component → **Circle Collider 2D**
   - Radius: ~0.3
   - Is Trigger: ✅

4. Select **Human prefab**:
   - Add Component → **Circle Collider 2D**
   - Radius: ~0.3
   - Is Trigger: ✅

## How It Works

### Detection System
```csharp
// EntityTooltip.cs Update() method:
Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
Collider2D[] colliders = Physics2D.OverlapCircleAll(mouseWorldPos, hoverDistance);

// Find first entity with BiomassEnergy component
foreach (Collider2D col in colliders)
{
    BiomassEnergy biomass = col.GetComponent<BiomassEnergy>();
    if (biomass != null && biomass.isAlive)
    {
        ShowTooltip(biomass);
        break;
    }
}
```

### Tooltip Content
The tooltip displays:
- **Entity Name** (from GameObject.name)
- **Type Icon**: 🌿 Plant / 🐰 Herbivore / 🧑 Carnivore
- **Health/Biomass Bar**: Current / Max (Percentage%)
- **Status**: Healthy / Good / Weak / Critical / Dying
- **Color-coded** based on health:
  - 🟢 Green: ≥80%
  - 🟡 Yellow: 50-79%
  - 🟠 Orange: 30-49%
  - 🔴 Red: <30%

### Example Tooltip Output
```
Tree(Clone)
🌿 Plant
Biomass: 95.3/100 (95%) - Healthy

Rabbit(Clone)
🐰 Herbivore
Health: 42.7/100 (43%) - Good
Digesting... 8s
```

## Testing

1. **Play Mode** in Unity
2. Hover mouse over any entity (tree, grass, animal, human)
3. Tooltip should appear showing:
   - Biomass (for plants)
   - Health (for animals/humans)
   - Current status
4. Move mouse away → tooltip disappears

## Troubleshooting

### Tooltip Not Appearing?
- ✅ Check entities have **Circle Collider 2D** components
- ✅ Verify colliders have **Is Trigger** enabled
- ✅ Check **BiomassEnergy** component is attached to entities
- ✅ Ensure **Hover Distance** (0.5) matches entity size
- ✅ Verify **Main Camera** is tagged "MainCamera"

### Tooltip Position Wrong?
- Adjust **Tooltip Offset** in EntityTooltip component
- Check Canvas **Render Mode** is "Screen Space - Overlay"

### Tooltip Too Small?
- Increase **TooltipPanel** Width/Height
- Increase **TooltipText** Font Size

### Performance Issues?
- Reduce **Hover Distance** (fewer colliders checked per frame)
- Add layer masks to `Physics2D.OverlapCircleAll()` for filtering

## Integration with BiomassEnergy

The BiomassEnergy component already provides these methods:

```csharp
// Single-line status string for tooltip
public string GetStatusString()
{
    // Returns: "Health: 75.5/100 (76%) - Good"
}

// Health-based color (green → yellow → orange → red)
public Color GetHealthColor()
{
    // Returns color based on currentEnergy / maxEnergy
}

// Alive/dead flag
public bool isAlive;
```

EntityTooltip calls these methods when hovering:
```csharp
tooltipText.text = $"<b>{entityName}</b>\n{typeLabel}\n{statusInfo}";
tooltipText.color = entity.GetHealthColor();
```

## Next Steps

After UI is set up:
1. Integrate **BiomassEnergy** into WorldLogic spawning (see next guide)
2. Balance energy rates (photosynthesis gain vs respiration drain)
3. Test trophic energy flow (plants → animals → humans)
4. Add visual health indicators (sprite tinting based on health)

---
**File Created**: EntityTooltip.cs (Assets/Scripts/)  
**Dependencies**: BiomassEnergy.cs, Circle Collider 2D on all entities  
**Testing**: Hover mouse over entities to see health/biomass details
