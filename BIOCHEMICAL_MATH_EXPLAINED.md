# 🧮 Biochemical Gas Exchange - Math Explained

## 📐 The Core Equation:

### **Moles to Percentage Conversion:**

```
Percentage Change = (Moles Changed / Total Atmosphere Moles) × 100%
```

**Example:**
- Total atmosphere = 1,000,000 moles
- Tree produces = +5 moles O₂/day
- Percentage change = (5 / 1,000,000) × 100% = **0.0005%** per tree per day

---

## 🌳 Example Calculation (1 Day Cycle):

### **Your Default Setup:**
- 10 Trees
- 40 Grass (3-5 per tree × 10 trees)
- 1 Human
- 10 Animals
- Ocean: 10 mol CO₂ sink/day

### **Daytime (12 hours):**

**O₂ Production:**
```
Trees:  10 × 5.0 = +50 mol O₂
Grass:  40 × 1.0 = +40 mol O₂
Total Production = +90 mol O₂
```

**O₂ Consumption:**
```
Human:   1 × 25.0 = -25 mol O₂
Animals: 10 × 2.5 = -25 mol O₂
Total Consumption = -50 mol O₂
```

**Net O₂ (Daytime):**
```
Net = +90 - 50 = +40 mol O₂
Percentage = (40 / 1,000,000) × 100% = +0.004%
```

### **Nighttime (12 hours):**

**O₂ Production:**
```
Trees:  0 (no photosynthesis)
Grass:  0 (no photosynthesis)
Total Production = 0 mol O₂
```

**O₂ Consumption:**
```
Human:   1 × 25.0 = -25 mol O₂
Animals: 10 × 2.5 = -25 mol O₂
Total Consumption = -50 mol O₂
```

**Net O₂ (Nighttime):**
```
Net = 0 - 50 = -50 mol O₂
Percentage = (-50 / 1,000,000) × 100% = -0.005%
```

---

## 📊 Full 24-Hour Cycle:

### **Average Over 24 Hours:**

Since your simulation runs 120 seconds = 1 day, and photosynthesis occurs during ~12 hours:

```
Daytime O₂:  +40 mol (12 hours)
Nighttime O₂: -50 mol (12 hours)
─────────────────────────────────
Net 24h:     -10 mol O₂/day
```

**This means your ecosystem is slightly O₂-negative!**

### **To Balance:**

**Option 1: Add more trees/grass**
```
Add 1 more tree: +5 mol O₂ (daytime)
Net becomes: -10 + 2.5 = -7.5 mol/day (better!)
```

**Option 2: Reduce animals**
```
Remove 2 animals: +5 mol O₂ saved
Net becomes: -10 + 5 = -5 mol/day
```

**Option 3: Adjust rates**
```
Increase tree O₂ rate to 6.0 mol/day:
10 trees × 6.0 = +60 mol (daytime)
Net becomes: +60 - 50 = +10 mol/day ✅ Balanced!
```

---

## 🌊 Ocean CO₂ Sink Calculation:

### **Why Ocean Matters:**

Without ocean, CO₂ would accumulate infinitely:
```
Day 1:  CO₂ = 0.041%
Day 10: CO₂ = 0.045% (keeps rising)
Day 100: CO₂ = 0.100% (doubled!)
```

**With Ocean Sink (10 mol/day):**
```
Respiration produces: +50 mol CO₂/day
Plants consume: -45 mol CO₂/day (daytime average)
Ocean absorbs: -10 mol CO₂/day
─────────────────────────────────────
Net CO₂: +50 - 45 - 10 = -5 mol/day (stable!)
```

---

## 🎯 Target Balance for Earth-like Atmosphere:

### **Goal:**
```
O₂:  20.53% (stable ±0.01%)
CO₂: 0.041% (stable ±0.001%)
```

### **Required Conditions:**

**For O₂ Stability:**
```
(Daytime O₂ Production × 12h) ≈ (24h O₂ Consumption)

Example:
(90 mol × 0.5) = 45 mol ≈ 50 mol consumption
→ Need ~10% more plants or reduce consumers
```

**For CO₂ Stability:**
```
Total Respiration - Photosynthesis - Ocean Sink ≈ 0

Example:
50 - 45 - 10 = -5 mol/day (CO₂ decreases slightly)
```

---

## 🧪 Experiment: Tree Deforestation

### **What Happens When 5 Trees Die:**

**Immediate CO₂ Spike:**
```
Each tree releases: 10 × 5 = 50 mol CO₂
5 trees: 5 × 50 = 250 mol CO₂
Percentage spike: (250 / 1,000,000) × 100% = +0.025%

New CO₂: 0.041% + 0.025% = 0.066% 🚨 (61% increase!)
```

**Long-term Effect:**
```
Lost O₂ production: 5 × 5 = -25 mol/day
New net O₂: -10 - 12.5 = -22.5 mol/day (worse!)

After 100 days:
O₂ loss = (-22.5 × 100) / 1,000,000 × 100% = -0.225%
New O₂: 20.53% - 0.225% = 20.305% ⚠️
```

---

## 📈 Population Scaling:

### **To Support 1 Human (25 mol O₂/day):**

**Minimum Trees Needed:**
```
1 Human needs: 25 mol O₂/day
1 Tree produces: 5 mol O₂/day (daytime only = 2.5 avg)
Trees needed: 25 / 2.5 = 10 trees minimum
```

**With Grass Helping:**
```
10 Trees: 10 × 2.5 = 25 mol O₂/day (avg)
40 Grass: 40 × 0.5 = 20 mol O₂/day (avg)
Total: 45 mol O₂/day
Can support: 45 / 25 = 1.8 humans (round to 1)
```

### **To Support 10 Animals (25 mol O₂/day total):**

Already balanced with current setup!

---

## 🔢 Formula Reference:

### **1. Percentage to Moles:**
```
Moles = (Percentage / 100) × Total_Atmosphere_Moles
```

### **2. Moles to Percentage:**
```
Percentage = (Moles / Total_Atmosphere_Moles) × 100
```

### **3. Daily Change:**
```
New_Percentage = Old_Percentage + Daily_Change
where Daily_Change = (Net_Moles_Per_Day / Total_Moles) × 100
```

### **4. Entity Count for Balance:**
```
Required_Producers = (Total_O2_Consumption) / (Producer_O2_Rate × 0.5)
                                                       ↑
                                        (0.5 = daytime fraction)
```

---

## 💡 Quick Balance Tips:

### **If O₂ Drops:**
1. Add more trees/grass
2. Reduce animals/humans
3. Increase `oxygenRate` for plants

### **If CO₂ Rises:**
1. Increase `oceanAbsorptionRate`
2. Add more trees
3. Reduce respiring entities

### **If Changes Too Fast:**
1. Increase `totalAtmosphereMoles` (more mass = slower change)
2. Increase `secondsPerDay` (slower simulation)

### **If Changes Too Slow:**
1. Decrease `totalAtmosphereMoles`
2. Decrease `secondsPerDay`
3. Increase exchange rates

---

## 🎓 Real-World Context:

**Earth's Actual Atmosphere:**
- Total mass: ~5.15 × 10¹⁸ kg
- Total moles: ~1.77 × 10²⁰ moles
- Your simulation: 1 × 10⁶ moles (scaled down for gameplay)

**This scale factor makes changes visible in your game while maintaining realistic proportions!**

---

**Use these formulas to design your perfect ecosystem!** 🌍🧮✨
