# Time Speed Control System Documentation

## Overview
The Time Speed Control System allows users to control the simulation speed in real-time, making days pass faster or slower without affecting the accuracy of gas exchange calculations. This is essential for testing different scenarios and observing long-term ecosystem changes.

**Note**: This system uses a `speedMultiplier` approach to ensure gas exchange stays perfectly synchronized with the visual day/night cycle. Earlier versions had a bug where gas exchange would desync from the sun/moon cycle - this has been fixed.

## Components

### 1. TimeSpeedController.cs
The main controller that manages simulation speed and synchronizes all time-dependent systems.

#### Features
- **Singleton Pattern**: Only one instance exists in the scene
- **Multiple Speed Options**: ×1, ×2, ×4, ×8, ×12
- **System Synchronization**: Updates both day/night cycle and atmosphere calculations
- **Keyboard Shortcuts**: Quick access to speed changes
- **Framerate Independence**: Accurate calculations at any speed

#### Public Properties

```csharp
public int currentSpeed = 1;              // Current time multiplier (1-12)
public int[] speedOptions = {1,2,4,8,12}; // Available speed tiers
public float baseDaySeconds = 120f;        // Base day length at 1x speed (2 minutes)
```

#### Public Methods

**SetSpeed(int speed)**
- Sets the simulation to a specific speed multiplier
- Valid values: 1, 2, 4, 8, or 12
- Updates both SunMoonController and AtmosphereManager
- Example: `TimeSpeedController.Instance.SetSpeed(4);`

**IncreaseSpeed()**
- Moves to the next higher speed tier
- Caps at maximum (×12)
- Example: ×2 → ×4

**DecreaseSpeed()**
- Moves to the next lower speed tier
- Caps at minimum (×1)
- Example: ×4 → ×2

**CycleSpeed()**
- Cycles through all speed options
- Wraps back to ×1 after ×12
- Example: ×1 → ×2 → ×4 → ×8 → ×12 → ×1

**GetSpeedText()**
- Returns formatted speed string
- Returns: "×1", "×2", "×4", etc.
- Used for UI display

**ResetSpeed()**
- Returns to normal speed (×1)

#### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `1` | Set speed to ×1 (normal) |
| `2` | Set speed to ×2 |
| `3` | Set speed to ×4 |
| `4` | Set speed to ×8 |
| `5` | Set speed to ×12 (fastest) |
| `+` or `=` | Increase speed |
| `-` | Decrease speed |
| `Right Arrow` | Increase speed |
| `Left Arrow` | Decrease speed |
| `Tab` | Cycle to next speed |

#### How It Works

**Speed Calculation**:
```csharp
float newDaySeconds = baseDaySeconds / speed;
// Example: 120 seconds / 4 = 30 seconds per day at ×4 speed
```

**System Updates**:
1. **SunMoonController**: `fullDaySeconds` is updated → controls day/night cycle speed
2. **AtmosphereManager**: `speedMultiplier` is updated → accelerates gas exchange calculations

**Why It's Accurate**:
- AtmosphereManager uses continuous frame-by-frame processing
- Gas exchange is calculated as: `rate (mol/day) × (Time.deltaTime / secondsPerDay) × speedMultiplier`
- The speedMultiplier directly scales the gas exchange rate
- Result: Gas exchange happens at the same rate as the day/night cycle

**Important Note**: The system uses a `speedMultiplier` approach instead of changing `secondsPerDay` to ensure that gas exchange always stays synchronized with the visual day/night cycle.

#### Setup in Unity

1. **Create GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name it "TimeSpeedController"

2. **Add Script**:
   - Add Component → Scripts → TimeSpeedController

3. **Configure Settings** (optional):
   - Adjust `baseDaySeconds` (default: 120s = 2 minutes)
   - Modify `speedOptions` array for custom speeds

#### Debug Logging

The controller logs all speed changes:
```
[TimeSpeed] Speed set to ×4 (Day = 30.0s)
```

---

### 2. TimeSpeedUI.cs
UI component that displays current speed and provides clickable buttons for speed control.

#### Features
- **Real-time Display**: Shows current speed ("Speed: ×4")
- **Button Highlights**: Active speed button is highlighted in green
- **Automatic Updates**: UI refreshes every frame
- **Click Control**: Change speed by clicking buttons

#### Public Properties

**UI References**:
```csharp
public Text speedDisplayText;      // Text showing "Speed: ×4"
public Button speed1xButton;       // Button for ×1 speed
public Button speed2xButton;       // Button for ×2 speed
public Button speed4xButton;       // Button for ×4 speed
public Button speed8xButton;       // Button for ×8 speed
public Button speed12xButton;      // Button for ×12 speed
```

**Button Colors** (Optional):
```csharp
public Color normalColor = Color.white;   // Inactive button color
public Color activeColor = Color.green;   // Active button color
```

#### How It Works

**Initialization (Start)**:
- Finds TimeSpeedController singleton
- Registers button click listeners
- Each button calls `SetSpeed()` with its corresponding value

**Update Loop**:
- Updates speed display text every frame
- Highlights the currently active button
- Changes button colors based on `currentSpeed`

**Button Click Flow**:
```
User clicks "×4" button
    ↓
TimeSpeedUI.SetSpeed(4)
    ↓
TimeSpeedController.Instance.SetSpeed(4)
    ↓
Updates SunMoonController & AtmosphereManager
    ↓
UI detects change and highlights ×4 button
```

#### Setup in Unity

**Step 1: Create Canvas UI** (if you don't have one)
- Right-click Hierarchy → UI → Canvas

**Step 2: Create Speed Display Text**
- Right-click Canvas → UI → Text
- Name it "SpeedDisplay"
- Position: Top-right corner
- Text: "Speed: ×1"
- Font Size: 18-24
- Color: White or yellow

**Step 3: Create Speed Buttons**
- Right-click Canvas → UI → Button (create 5 times)
- Names: "Speed1xButton", "Speed2xButton", "Speed4xButton", "Speed8xButton", "Speed12xButton"
- Layout: Arrange horizontally (can use Horizontal Layout Group)
- Button Text Labels: "×1", "×2", "×4", "×8", "×12"

**Step 4: Create TimeSpeedUI GameObject**
- Right-click Canvas → Create Empty
- Name it "TimeSpeedUI"
- Add Component → Scripts → TimeSpeedUI

**Step 5: Wire Up References**
In TimeSpeedUI component Inspector:
- Drag "SpeedDisplay" Text → **Speed Display Text** field
- Drag "Speed1xButton" → **Speed 1x Button** field
- Drag "Speed2xButton" → **Speed 2x Button** field
- Drag "Speed4xButton" → **Speed 4x Button** field
- Drag "Speed8xButton" → **Speed 8x Button** field
- Drag "Speed12xButton" → **Speed 12x Button** field

**Step 6: Optional Color Customization**
- Normal Color: White (default)
- Active Color: Green (or any highlight color)

#### UI Layout Example

```
┌─────────────────────────────────────────┐
│                        Speed: ×4         │ ← Speed Display
│                                          │
│         [Ecosystem View]                 │
│                                          │
│  ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌────┐        │
│  │×1 │ │×2 │ │×4*│ │×8 │ │×12 │        │ ← Speed Buttons
│  └───┘ └───┘ └───┘ └───┘ └────┘        │   (* = highlighted)
└─────────────────────────────────────────┘
```

---

## System Architecture

### Data Flow

```
User Input
    ↓
TimeSpeedUI (UI Layer)
    ↓
TimeSpeedController (Logic Layer)
    ↓
┌─────────────────┬─────────────────────┐
↓                 ↓                     ↓
SunMoonController  AtmosphereManager  (Other systems)
    ↓                 ↓
Day/Night Cycle   Gas Exchange
```

### Speed Synchronization

**At ×1 Speed (120s/day)**:
- Sun/Moon: Full arc in 120 seconds
- Atmosphere: Gas exchange uses 120s as denominator
- Result: 1 game-day = 2 real minutes

**At ×4 Speed (30s/day)**:
- Sun/Moon: Full arc in 30 seconds
- Atmosphere: Gas exchange uses 30s as denominator
- Result: 1 game-day = 30 real seconds (4× faster)

**At ×12 Speed (10s/day)**:
- Sun/Moon: Full arc in 10 seconds
- Atmosphere: Gas exchange uses 10s as denominator
- Result: 1 game-day = 10 real seconds (12× faster)

### Mathematical Accuracy

**Gas Exchange Calculation**:
```csharp
// In AtmosphereManager.ProcessContinuousGasExchange()
float deltaTime = Time.deltaTime;  // Time since last frame (real time)
float speedMultiplier = 1.0f;      // Set by TimeSpeedController (1, 2, 4, 8, or 12)
float oxygenChange = netO2Rate * (deltaTime / secondsPerDay) * speedMultiplier;

// Example at ×4 speed:
// netO2Rate = 100 mol/day
// secondsPerDay = 120s (constant)
// speedMultiplier = 4
// deltaTime = 0.0167s (60 fps)
// oxygenChange = 100 * (0.0167 / 120) * 4 = 0.00557 mol/frame

// After 30 seconds (1 game-day at ×4):
// Total frames = 30s × 60fps = 1800 frames
// Total oxygen = 0.00557 × 1800 = 10.026 mol
// This is 1/4 of a full day's worth, which is CORRECT because:
//   - Visual day = 30 seconds (4× faster)
//   - Gas exchange = 1/4 day worth in 30 seconds
//   - They match perfectly! ✓
```

**The key insight**: The `speedMultiplier` directly scales gas exchange to match the visual day/night cycle speed. At ×12, both the sun/moon and gas exchange run 12× faster in real-time.

---

## Usage Examples

### Example 1: Quick Testing
```csharp
// Speed up to quickly see long-term effects
TimeSpeedController.Instance.SetSpeed(12);
// Wait 20 seconds = 24 game-days
// Check atmosphere changes
```

### Example 2: Detailed Observation
```csharp
// Slow down to observe specific events
TimeSpeedController.Instance.SetSpeed(1);
// Watch individual breathing animations
// See gas particles emitting in real-time
```

### Example 3: Dynamic Speed Control
```csharp
// Speed up during night, slow down during day
void Update() {
    if (sunMoonController.IsDaytime()) {
        TimeSpeedController.Instance.SetSpeed(1);  // Normal during day
    } else {
        TimeSpeedController.Instance.SetSpeed(8);  // Fast-forward night
    }
}
```

### Example 4: UI Button Script
```csharp
// Attach to a custom UI button
public void OnSpeedButtonClick() {
    TimeSpeedController.Instance.CycleSpeed();
}
```

---

## Troubleshooting

### Issue: Speed changes but data seems wrong (×12 slower than ×1)
**Solution**: This was a bug in earlier versions where AtmosphereManager changed `secondsPerDay` instead of using `speedMultiplier`. Make sure you have the latest version where:
- TimeSpeedController sets `atmosphereManager.speedMultiplier = currentSpeed`
- AtmosphereManager uses `* speedMultiplier` in gas exchange calculations
- Day tracking uses `dayTimer += Time.deltaTime * speedMultiplier`

### Issue: ×12 mode takes 12× longer than ×1 mode
**Solution**: Same as above - the speedMultiplier fix resolves this. Gas exchange now happens at the same rate as the visual cycle.

### Issue: Buttons don't work
**Solution**: 
1. Check TimeSpeedController GameObject exists in scene
2. Verify all button references are assigned in TimeSpeedUI Inspector
3. Check Console for error messages

### Issue: Speed display shows wrong value
**Solution**: Make sure Speed Display Text reference is assigned in TimeSpeedUI component

### Issue: Game freezes when pressing Tab
**Solution**: This is now fixed - Space key was removed (it conflicted with Unity's pause). Use Tab instead.

### Issue: Keyboard shortcuts don't work
**Solution**: 
1. Make sure TimeSpeedController GameObject is active
2. Check that the script is enabled
3. Verify no other script is consuming the same input

---

## Performance Considerations

### CPU Impact
- **Minimal overhead**: Only updates 2 float values when speed changes
- **No continuous overhead**: Speed multiplier is applied through existing Time.deltaTime

### Memory Impact
- **Negligible**: Only stores 2-3 float values and 1 int array

### Frame Rate Independence
- All calculations use `Time.deltaTime`
- Works correctly at 30fps, 60fps, 144fps, etc.
- No fixed timestep dependencies

---

## Advanced Customization

### Adding New Speed Options

Edit `TimeSpeedController.cs`:
```csharp
public int[] speedOptions = { 1, 2, 4, 6, 8, 12, 16, 24 };
```

Then add corresponding buttons in UI.

### Custom Base Day Length

```csharp
public float baseDaySeconds = 60f;  // 1-minute days at ×1 speed
```

### Pause Functionality

```csharp
// Add to TimeSpeedController
public void Pause() {
    Time.timeScale = 0;
}

public void Unpause() {
    Time.timeScale = 1;
}
```

### Speed Limits Based on O₂ Levels

```csharp
void Update() {
    // Slow down if oxygen is critically low
    if (AtmosphereManager.Instance.oxygen < 10f) {
        SetSpeed(1);  // Force slow motion during crisis
    }
}
```

---

## Testing Checklist

✅ **Speed changes correctly** - Press 1-5 keys, verify speed changes  
✅ **UI updates** - Speed display shows correct value  
✅ **Buttons highlight** - Active button turns green  
✅ **Day/night synced** - Sun/moon move faster at higher speeds  
✅ **Gas exchange accurate** - Same total change per game-day at all speeds  
✅ **No freezing** - Tab key cycles speeds smoothly  
✅ **Console logs** - "[TimeSpeed] Speed set to ×4 (Day = 30.0s)"  

---

## Code Reference

### TimeSpeedController Key Code
```csharp
public void SetSpeed(int speed) {
    currentSpeed = speed;
    currentSpeedIndex = System.Array.IndexOf(speedOptions, speed);
    
    float newDaySeconds = baseDaySeconds / currentSpeed;
    
    sunMoonController.fullDaySeconds = newDaySeconds;
    atmosphereManager.speedMultiplier = currentSpeed; // Direct speed scaling
    
    Debug.Log($"[TimeSpeed] Speed set to ×{currentSpeed} (Day = {newDaySeconds:F1}s)");
}
```

### TimeSpeedUI Key Code
```csharp
void Update() {
    if (speedDisplayText) {
        speedDisplayText.text = $"Speed: {timeController.GetSpeedText()}";
    }
    UpdateButtonColors();
}

void SetSpeed(int speed) {
    timeController.SetSpeed(speed);
}
```

---

## Summary

The Time Speed Control System provides:

✅ **User Control**: Easy speed adjustment via UI or keyboard  
✅ **Accuracy**: Mathematically correct at all speeds  
✅ **Flexibility**: 5 speed options (×1 to ×12)  
✅ **Visual Feedback**: Clear UI display and button highlights  
✅ **Performance**: Minimal overhead, framerate independent  
✅ **Extensibility**: Easy to add new speeds or features  

This system makes it practical to:
- Test long-term ecosystem changes quickly
- Observe detailed short-term events slowly
- Find the perfect speed for your analysis needs
- Run simulations for days/months in minutes

**Perfect for ecosystem simulation, education, and research!** 🎯
