# Atmosphere UI Enhancement - Mole Display

## What Changed

The `AtmosphereUI` now displays **both percentage AND mole amounts** for each gas.

## New Display Format

**Before** (percentage only):
```
H₂O: 2.000%
N₂: 76.520%
O₂: 20.530%
Ar: 0.915%
CO₂: 0.041%
```

**After** (percentage + moles):
```
H₂O: 2.000% / 20000 mol
N₂: 76.520% / 765200 mol
O₂: 20.530% / 205300 mol
Ar: 0.915% / 9150 mol
CO₂: 0.041% / 410 mol
```

## Benefits

### 1. **Direct Visual Feedback**
You can immediately see if gases are increasing or decreasing in absolute terms:

```
Day 1:  CO₂: 0.041% / 410 mol
Day 2:  CO₂: 0.045% / 450 mol  ← +40 mol (increasing!)
Day 3:  CO₂: 0.050% / 500 mol  ← +50 mol (still rising)
```

### 2. **Easier to Track Changes**
Mole amounts change more noticeably than percentages:

**Example - Ocean CO₂ Absorption:**
```
Before ocean: CO₂: 0.041% / 410 mol
After 1 day:  CO₂: 0.040% / 400 mol  ← Ocean absorbed 10 mol ✓
```

The percentage changes by 0.001%, which is hard to notice.  
But **410 → 400 mol** is immediately obvious! 

### 3. **Match Debug Logs**
The console logs show mol/day rates:
```
[Atmosphere] Net → O₂: 0.0 mol/day, CO₂: -10.0 mol/day
```

Now the UI shows the same units (moles), making it easy to verify:
- Ocean absorbs 10 mol/day
- CO₂ should decrease by ~10 mol each day
- UI shows: 450 → 440 → 430 mol ✓

## Calculation

The mole amount is calculated from the percentage:

```csharp
float moles = (percentage / 100f) * totalAtmosphereMoles;

// Example for CO₂:
// 0.041% of 1,000,000 total moles = 410 mol
```

## Display Format

```csharp
textElement.text = $"{gasName}: {percentage:F3}% / {moles:F0} mol";
```

- **Percentage**: 3 decimal places (0.041%)
- **Moles**: No decimals, rounded (410 mol)

## Example Scenarios

### Scenario 1: Daytime (Plants Producing O₂)
```
Start: O₂: 20.530% / 205300 mol
After 1 day: O₂: 20.530% / 205300 mol  ← Balanced!
```

### Scenario 2: Nighttime (Plants Consuming O₂)
```
Start: O₂: 20.530% / 205300 mol
After night: O₂: 20.500% / 205000 mol  ← Lost 300 mol
```

### Scenario 3: CO₂ Accumulation (Without Ocean)
```
Day 1: CO₂: 0.041% / 410 mol
Day 2: CO₂: 0.059% / 590 mol  ← +180 mol
Day 3: CO₂: 0.077% / 770 mol  ← +180 mol (consistent rate)
```

### Scenario 4: CO₂ with Ocean Absorption
```
Day 1: CO₂: 0.041% / 410 mol
Day 2: CO₂: 0.048% / 480 mol  ← +70 mol (ocean reduced from +180)
Day 3: CO₂: 0.055% / 550 mol  ← +70 mol (ocean helping!)
```

## Visual Benefits

### Quick Increase/Decrease Detection

**Increasing** (Bad for CO₂):
```
410 → 450 → 500 → 550 mol  ← Clearly rising!
```

**Decreasing** (Good for CO₂):
```
550 → 540 → 530 → 520 mol  ← Ocean working!
```

**Stable** (Balanced):
```
205300 → 205300 → 205300 mol  ← Perfect balance
```

### Large Numbers vs Small Changes

**Nitrogen** (very stable, large amount):
```
N₂: 76.520% / 765200 mol
N₂: 76.520% / 765200 mol  ← Barely changes (as expected)
```

**CO₂** (volatile, small amount):
```
CO₂: 0.041% / 410 mol
CO₂: 0.045% / 450 mol  ← 40 mol change is 10% relative increase!
```

## Testing the Display

To verify it's working:

1. **Start the game**
2. **Check CO₂ display**: Should show something like `CO₂: 0.041% / 410 mol`
3. **Wait 1 game-day** (2 minutes at ×1 speed, 10 seconds at ×12)
4. **Check CO₂ again**: Number should have changed
5. **Observe the mole amount**: Easier to see than percentage!

## Expected Values at Start

Based on default settings (1,000,000 total moles):

| Gas | Initial % | Initial Moles |
|-----|-----------|---------------|
| H₂O | 2.000% | 20,000 mol |
| N₂ | 76.520% | 765,200 mol |
| O₂ | 20.530% | 205,300 mol |
| Ar | 0.915% | 9,150 mol |
| CO₂ | 0.041% | 410 mol |

## Customization

If you want different formatting, you can adjust:

```csharp
// Show more decimal places in moles
textElement.text = $"{gasName}: {percentage:F3}% / {moles:F1} mol";
// Output: "CO₂: 0.041% / 410.0 mol"

// Use scientific notation for large numbers
textElement.text = $"{gasName}: {percentage:F3}% / {moles:E2} mol";
// Output: "N₂: 76.520% / 7.65E+05 mol"

// Add thousands separators
textElement.text = $"{gasName}: {percentage:F3}% / {moles:N0} mol";
// Output: "N₂: 76.520% / 765,200 mol"
```

## Summary

✅ **Shows both percentage and moles** for all gases  
✅ **Easier to spot changes** at a glance  
✅ **Matches console log units** (mol/day)  
✅ **Direct feedback** on gas exchange effectiveness  
✅ **More intuitive** for tracking ecosystem health  

Now you can immediately see if CO₂ is increasing (bad), decreasing (ocean working), or stable! 🌊📊
