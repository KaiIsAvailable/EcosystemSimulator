# Quick Tooltip Setup - 3 Steps

## Problem: Tooltip Not Appearing

The tooltip system requires 3 things to work:
1. ✅ **EntityTooltip.cs** script (created)
2. ❌ **UI Canvas with tooltip panel** (missing)
3. ❌ **Circle Colliders on entity prefabs** (missing)

## Solution: Automated Setup

### Step 1: Add TooltipSetup Script to Scene
1. In Unity, go to **Hierarchy** window
2. Right-click → **Create Empty** → Name it "TooltipSetup"
3. Select the TooltipSetup GameObject
4. In **Inspector**, click **Add Component**
5. Search for "**TooltipSetup**" and add it
6. ✅ The script will auto-create the UI when you enter Play Mode!

### Step 2: Setup Entity Prefabs (Automatic)
1. In Unity, click menu: **Tools → Setup Entity Tooltip System**
2. Wait for console message: "✅ Setup Complete! 4/4 prefabs configured"
3. ✅ This automatically adds:
   - BiomassEnergy component to all prefabs
   - Circle Collider 2D to all prefabs
   - Correct energy/health values

### Step 3: Test in Play Mode
1. Press **Play** button in Unity
2. Move your mouse cursor over any entity (tree, grass, animal, human)
3. ✅ Tooltip should appear showing health/biomass!

---

## Alternative: Manual Setup (if automated fails)

### Manual Step 1: Create UI Canvas
1. Hierarchy → Right-click → **UI → Canvas**
2. Name it "TooltipCanvas"
3. Add Component → **EntityTooltip** script
4. Create child: Right-click Canvas → **UI → Panel** (name: "TooltipPanel")
5. Create child: Right-click Panel → **UI → Text** (name: "TooltipText")
6. Drag references in EntityTooltip Inspector:
   - Tooltip Text → drag TooltipText
   - Tooltip Panel → drag TooltipPanel
7. Set TooltipPanel active = false (uncheck in Inspector)

### Manual Step 2: Add Colliders to Prefabs
For each prefab (Tree, Grass, Animal, Human):
1. In **Project** window → Assets/Prefabs
2. Select prefab
3. In **Inspector**, click **Add Component**
4. Search "**Circle Collider 2D**"
5. Set **Radius**: 0.3 (adjust to sprite size)
6. Check ✅ **Is Trigger**
7. Click **Add Component** again
8. Search "**BiomassEnergy**"
9. Set **Entity Type**: Plant/Herbivore/Carnivore
10. Click **Apply** at top of Inspector

---

## Troubleshooting

### Still No Tooltip After Setup?

**Check 1: Is TooltipCanvas created?**
```
Look in Hierarchy for "TooltipCanvas" GameObject
If missing → Run Play Mode with TooltipSetup script attached
```

**Check 2: Do prefabs have colliders?**
```
Project → Assets/Prefabs → Select Tree.prefab
Inspector → Look for "Circle Collider 2D" component
If missing → Use menu: Tools → Setup Entity Tooltip System
```

**Check 3: Do entities have BiomassEnergy?**
```
Play Mode → Hierarchy → Expand any entity (Tree, Animal, etc)
Inspector → Look for "BiomassEnergy" component
If missing → Prefabs not configured, re-run Setup
```

**Check 4: Is hover distance too small?**
```
Select TooltipCanvas → EntityTooltip component
Change "Hover Distance" from 0.5 → 1.0
```

**Check 5: Camera setup**
```
Main Camera tag must be "MainCamera"
Select camera → Inspector → Tag dropdown → MainCamera
```

### Console Errors?

**"BiomassEnergy not found"**
- Run: Tools → Setup Entity Tooltip System

**"timeController is null"**
- EntityTooltip needs SunMoonController in scene
- Should auto-find, but verify it exists in Hierarchy

**"Physics2D.OverlapCircleAll returns nothing"**
- Entities need Circle Collider 2D with Is Trigger enabled
- Check prefabs have colliders

---

## Expected Result

When working correctly, hovering over:

**Tree**: 
```
Tree5
🌿 Plant
Biomass: 95.3/100 (95%) - Healthy
```

**Animal**:
```
Animal3
🐰 Herbivore
Health: 42.7/100 (43%) - Good
```

**Human**:
```
Human0
🧑 Carnivore
Health: 67.2/100 (67%) - Good
```

---

## Quick Test Checklist

After setup, verify:
- [ ] TooltipCanvas exists in Hierarchy
- [ ] TooltipPanel is child of TooltipCanvas
- [ ] TooltipText is child of TooltipPanel
- [ ] EntityTooltip script on TooltipCanvas has references set
- [ ] Tree.prefab has Circle Collider 2D + BiomassEnergy
- [ ] Grass.prefab has Circle Collider 2D + BiomassEnergy
- [ ] Animal.prefab has Circle Collider 2D + BiomassEnergy
- [ ] Human.prefab has Circle Collider 2D + BiomassEnergy
- [ ] All colliders have "Is Trigger" checked
- [ ] Play Mode → Hover over entity → Tooltip appears

---

**If still not working after all steps, check Unity Console for error messages and share them!**
