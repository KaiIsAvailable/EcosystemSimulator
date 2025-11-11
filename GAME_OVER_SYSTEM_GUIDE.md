# Game Over System Setup Guide

## What It Does
- Automatically detects when the human dies
- Shows "GAME OVER" screen with survival time
- Provides RESTART and QUIT buttons
- Pauses the game when game over occurs

## Installation (2 Steps)

### Step 1: Add GameOverSetup to Scene
1. In Unity **Hierarchy**, right-click → **Create Empty**
2. Name it "**GameOverSetup**"
3. Select it → **Inspector** → **Add Component**
4. Search for "**GameOverSetup**" and add it
5. ✅ Done! It will auto-create the UI when you press Play

### Step 2: Test
1. Press **Play**
2. Wait for human to die (health reaches 0)
3. **Game Over screen appears** automatically with:
   - "GAME OVER" title (red, 60pt font)
   - Death message and survival time
   - Green RESTART button
   - Red QUIT button

---

## How It Works

### Automatic Detection
The `GameOverManager` checks every 0.5 seconds:
```csharp
// Finds all BiomassEnergy components
// Looks for Carnivore type (Human)
// If human.isAlive == false → Trigger Game Over
```

### When Human Dies
1. Game pauses (`Time.timeScale = 0`)
2. Game Over UI appears (center screen)
3. Shows survival time (minutes:seconds)
4. Buttons become clickable:
   - **RESTART** → Reloads scene, resumes game
   - **QUIT** → Exits game (or stops Play Mode in editor)

---

## Customization

### Change Font Sizes
Select **GameOverSetup** GameObject → Inspector:
- **Title Font Size**: 60 (default) - "GAME OVER" text
- **Message Font Size**: 30 (default) - survival time text
- **Button Font Size**: 24 (default) - button labels

### Change Messages
After first run, select **GameOverCanvas** → **GameOverManager**:
- **Game Over Message**: "GAME OVER" (title)
- **Human Died Message**: Death description
- **Check Interval**: 0.5s (how often to check)

### Adjust UI Position/Size
After first run, in Hierarchy:
- **GameOverCanvas → GameOverPanel**: Center panel (600x400)
  - Change size: Inspector → Rect Transform → Width/Height
  - Change position: Adjust anchor points
- **GameOverTitle**: Red "GAME OVER" text
- **MessageText**: White survival time text
- **RestartButton**: Green button (left)
- **QuitButton**: Red button (right)

---

## Manual Setup (Alternative)

If auto-setup fails, create manually:

### 1. Create Canvas
- Hierarchy → UI → Canvas
- Name: "GameOverCanvas"
- Render Mode: Screen Space - Overlay
- Sorting Order: 9999

### 2. Create Panel
- Right-click Canvas → UI → Panel
- Name: "GameOverPanel"
- Anchor: Center
- Size: 600 x 400
- Color: Dark gray (10, 10, 10, 240)

### 3. Add Title Text
- Right-click Panel → UI → Text
- Name: "GameOverTitle"
- Text: "GAME OVER"
- Font Size: 60
- Color: Red
- Alignment: Center

### 4. Add Message Text
- Right-click Panel → UI → Text
- Name: "MessageText"
- Text: "Human died!\nSurvival Time: 0m 0s"
- Font Size: 30
- Color: White
- Alignment: Center

### 5. Add Restart Button
- Right-click Panel → UI → Button
- Name: "RestartButton"
- Color: Green
- Text: "RESTART"
- Font Size: 24

### 6. Add Quit Button
- Right-click Panel → UI → Button
- Name: "QuitButton"
- Color: Red
- Text: "QUIT"
- Font Size: 24

### 7. Attach GameOverManager
- Select GameOverCanvas
- Add Component → GameOverManager
- Drag references:
  - Game Over Panel → GameOverPanel
  - Game Over Text → GameOverTitle
  - Survival Time Text → MessageText
  - Restart Button → RestartButton
  - Quit Button → QuitButton

### 8. Wire Button Events
- Select RestartButton → Inspector → On Click()
  - Add: GameOverCanvas → GameOverManager.RestartGame
- Select QuitButton → Inspector → On Click()
  - Add: GameOverCanvas → GameOverManager.QuitGame

---

## Troubleshooting

### Game Over Doesn't Appear When Human Dies?

**Check 1: Is GameOverCanvas created?**
```
Hierarchy → Look for "GameOverCanvas"
If missing → Add GameOverSetup component and press Play
```

**Check 2: Is BiomassEnergy on Human?**
```
Play Mode → Hierarchy → Expand Human
Inspector → Look for "BiomassEnergy" component
Entity Type should be: Carnivore
```

**Check 3: Is human actually dying?**
```
Hover over human → Check health in tooltip
Should decrease over time due to respiration
If not decreasing → Check respirationLossRate > 0
```

**Check 4: Check Console for errors**
```
Window → General → Console
Look for GameOverManager errors
```

### Buttons Don't Work?

**Restart Button:**
- Check button has onClick event wired to GameOverManager.RestartGame

**Quit Button:**
- Check button has onClick event wired to GameOverManager.QuitGame
- In editor, will stop Play Mode
- In build, will exit application

### Game Doesn't Pause?

**Check:**
- GameOverManager.TriggerGameOver() sets Time.timeScale = 0
- If other scripts use Time.deltaTime, they'll pause
- If scripts use Time.unscaledDeltaTime, they won't pause

---

## Expected Behavior

### Game Running:
- Human starts with 70 health
- Loses health over time (respirationLossRate)
- Hunts animals to regain health
- If no animals available → health reaches 0

### Human Dies:
1. BiomassEnergy.isAlive becomes false
2. GameOverManager detects death (within 0.5s)
3. Game Over UI fades in (center screen)
4. Game pauses (Time.timeScale = 0)
5. Shows survival time (e.g., "5m 32s")
6. Buttons become clickable

### Click RESTART:
1. Time.timeScale resets to 1
2. Scene reloads (fresh start)
3. All entities respawn
4. Timer resets

### Click QUIT:
1. In Unity Editor: Stops Play Mode
2. In Build: Closes application window

---

## Testing Checklist

- [ ] GameOverSetup component added to scene
- [ ] Press Play → GameOverCanvas created automatically
- [ ] Human spawns with BiomassEnergy (Carnivore type)
- [ ] Human health decreases over time (check tooltip)
- [ ] When health reaches 0 → Game Over appears
- [ ] "GAME OVER" title visible (red, center)
- [ ] Survival time displays correctly
- [ ] RESTART button works (reloads scene)
- [ ] QUIT button works (stops Play Mode)
- [ ] Game is paused (entities stop moving)

---

## Quick Start Summary

1. **Add GameOverSetup** component to empty GameObject
2. **Press Play**
3. **Wait for human to die** (or speed up time ×12)
4. **Game Over screen appears** automatically!

That's it! The system handles everything else automatically. 🎮💀

---

**Files Created:**
- GameOverManager.cs (detects death, manages UI)
- GameOverSetup.cs (auto-creates UI)

**Next Steps:**
- Adjust human starting health to test faster
- Balance respiration rates so human survives longer
- Add more carnivores for variety
