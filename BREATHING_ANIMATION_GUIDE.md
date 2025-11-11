# Breathing Animation System

## Overview
Added visual breathing animations to all living organisms (trees, grass, animals, humans) to show they are actively exchanging O₂ and CO₂ with the atmosphere.

## Changes Made

### 1. AnimalAgent.cs
- **Added `canMove` toggle**: Set to `false` by default - animals now stay in place
- **Added breathing parameters**:
  - `showBreathing = true`: Enable breathing animation
  - `breathingSpeed = 1.5f`: Breaths per second
  - `breathingAmount = 0.05f`: 5% scale expansion/contraction
- **Breathing animation**: Uses sine wave to smoothly expand/contract the sprite
- **Randomized phase**: Each animal starts at a different point in the breathing cycle

### 2. HumanAgent.cs
- **Added `canMove` toggle**: Set to `false` by default - humans stay in place
- **Added breathing parameters**:
  - `showBreathing = true`: Enable breathing animation
  - `breathingSpeed = 1.2f`: Slightly slower than animals
  - `breathingAmount = 0.06f`: 6% scale change (more visible)
- **Breathing animation**: Same sine wave system as animals
- **Randomized phase**: Each human breathes at their own pace

### 3. BreathingAnimation.cs (NEW)
A reusable component for any GameObject that needs breathing animation:

**Features**:
- `isBreathing`: Toggle on/off
- `breathingSpeed`: Adjustable breathing rate
- `breathingAmount`: Adjustable expansion amount
- `randomizePhase`: Prevents synchronized breathing
- `StopBreathing()`: Can stop animation (e.g., when entity dies)
- `StartBreathing()`: Resume animation
- `SetBreathingSpeed(float)`: Dynamic speed changes (e.g., stress response)

**How it works**:
```csharp
breathingTimer += Time.deltaTime * breathingSpeed;
float breathScale = 1f + Mathf.Sin(breathingTimer) * breathingAmount;
transform.localScale = originalScale * breathScale;
```

### 4. WorldLogic.cs
Updated to automatically add breathing animations to all entities:

- **Trees**: `AddBreathingAnimation(tree, 0.8f, 0.03f)` - Slow, subtle (3% scale)
- **Grass**: `AddBreathingAnimation(grass, 1.2f, 0.04f)` - Faster, slightly visible (4% scale)
- **Animals**: `AddBreathingAnimation(animal, 1.5f, 0.05f)` - Active breathing (5% scale)
- **Humans**: `AddBreathingAnimation(human, 1.2f, 0.06f)` - Slow, visible (6% scale)

## Breathing Rates Summary

| Entity Type | Speed (breaths/sec) | Amount (%) | Visual Effect |
|-------------|---------------------|------------|---------------|
| Tree        | 0.8                 | 3%         | Very subtle, like swaying |
| Grass       | 1.2                 | 4%         | Gentle pulsing |
| Animal      | 1.5                 | 5%         | Clear breathing |
| Human       | 1.2                 | 6%         | Most visible breathing |

## Visual Representation of Gas Exchange

The breathing animation serves multiple purposes:

1. **Shows "life"**: All organisms appear alive and active
2. **Gas exchange indicator**: Breathing = O₂ consumption and CO₂ production
3. **System activity**: Easy to see the ecosystem is functioning
4. **Educational**: Clear connection between breathing and atmosphere changes

## Enabling Movement (Optional)

If you want entities to move again later:

**In Unity Inspector**:
- Select an Animal or Human GameObject
- Find `AnimalAgent` or `HumanAgent` component
- Check the `Can Move` checkbox

**Or in code**:
```csharp
GetComponent<AnimalWander>().canMove = true;  // Enable animal movement
GetComponent<HumanAgent>().canMove = true;    // Enable human movement
```

## Testing the Breathing Animation

1. **Start the game** - all entities should stay in place
2. **Watch for breathing**:
   - Trees/grass should gently pulse
   - Animals should breathe visibly
   - Humans should have the most visible breathing
3. **Check atmosphere UI** - even while stationary, O₂ and CO₂ should change
4. **Verify gas exchange** - Animals/humans consume O₂, plants produce O₂ during day

## Performance Notes

- Breathing animation uses `Update()` for smooth visuals
- Very lightweight: just one sine calculation per entity per frame
- No physics calculations involved
- Minimal performance impact even with 100+ entities

## Future Enhancements

Possible additions:
- **Breathing rate changes**: Faster breathing when O₂ is low
- **Death animation**: Stop breathing when an entity dies
- **Stress response**: Rapid breathing during adverse conditions
- **Sleep mode**: Slower breathing at night for animals
- **Color changes**: Tint sprite based on O₂ availability

## Summary

✅ **Animals and humans are now stationary** (`canMove = false`)  
✅ **All entities show breathing animation** (expand/contract)  
✅ **Breathing rates vary by entity type** (realistic variation)  
✅ **Phase randomization** (not synchronized)  
✅ **Visual confirmation of gas exchange** (breathing = O₂/CO₂ exchange)

The breathing animation makes it immediately clear that:
- Plants are "breathing" (photosynthesis + respiration)
- Animals are consuming O₂ and producing CO₂
- Humans are actively respiring
- The entire ecosystem is alive and interconnected
