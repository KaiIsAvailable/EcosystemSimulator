using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the Help Panel buttons and content display.
/// Each button shows different help text when clicked.
/// </summary>
public class HelpPanelToggleController : MonoBehaviour
{
    [Header("Help Buttons")]
    public Button helpBtn1;
    public Button helpBtn2;
    public Button helpBtn3;
    public Button helpBtn4;
    public Button helpBtn5;
    
    [Header("Help Content Display")]
    public Text helpContentText;
    
    [Header("Scroll View (Optional - for auto-scroll to top)")]
    public ScrollRect scrollRect;
    
    // Track currently selected button
    private Button selectedButton;
    
    // References to game systems
    private AtmosphereManager atmosphere;
    private SunMoonController sunMoon;
    
    void Start()
    {
        Debug.Log("[HelpPanelToggleController] Start() called");
        
        // Get references to game systems
        atmosphere = AtmosphereManager.Instance;
        sunMoon = FindAnyObjectByType<SunMoonController>();
        
        if (atmosphere == null)
        {
            Debug.LogError("[HelpPanelToggleController] AtmosphereManager not found!");
        }
        
        if (sunMoon == null)
        {
            Debug.LogError("[HelpPanelToggleController] SunMoonController not found!");
        }
        
        // Auto-find ScrollRect if not assigned
        if (scrollRect == null && helpContentText != null)
        {
            scrollRect = helpContentText.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                Debug.Log("[HelpPanelToggleController] ScrollRect found automatically");
            }
        }
        
        // Setup all buttons with hover effects and click handlers
        SetupHelpButton(helpBtn1, 1);
        SetupHelpButton(helpBtn2, 2);
        SetupHelpButton(helpBtn3, 3);
        SetupHelpButton(helpBtn4, 4);
        SetupHelpButton(helpBtn5, 5);
        
        // Select button 1 as default
        if (helpBtn1 != null)
        {
            selectedButton = helpBtn1;
            SetButtonAlpha(helpBtn1, 1.0f);
            OnHelpButtonClicked(1);
        }
        
        Debug.Log("[HelpPanelToggleController] Setup complete!");
    }
    
    void Update()
    {
        // Update analysis every second when button 1 is selected
        if (selectedButton == helpBtn1 && Time.frameCount % 60 == 0)
        {
            UpdateCurrentActivityAnalysis();
        }
    }
    
    /// <summary>
    /// Sets up a help button with hover effect and click handler
    /// </summary>
    void SetupHelpButton(Button btn, int buttonNumber)
    {
        if (btn == null) return;
        
        // Add EventTrigger component if not present
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btn.gameObject.AddComponent<EventTrigger>();
        }
        
        // Clear existing triggers to avoid duplicates
        trigger.triggers.Clear();
        
        // Hover Enter - Set alpha to 1.0
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { 
            SetButtonAlpha(btn, 1.0f);
        });
        trigger.triggers.Add(pointerEnter);
        
        // Hover Exit - Reset alpha to 0.5 only if not selected
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { 
            // Only reset alpha if this button is not currently selected
            if (selectedButton != btn)
            {
                SetButtonAlpha(btn, 0.5f);
            }
        });
        trigger.triggers.Add(pointerExit);
        
        // Click - Update text content and select button
        btn.onClick.AddListener(() => { 
            OnHelpButtonClicked(buttonNumber);
            SelectButton(btn);
        });
        
        Debug.Log($"[HelpPanelToggleController] Setup button {buttonNumber}");
    }
    
    /// <summary>
    /// Called when a help button is clicked
    /// </summary>
    void OnHelpButtonClicked(int buttonNumber)
    {
        if (helpContentText == null) return;
        
        // Update text based on button clicked
        switch (buttonNumber)
        {
            case 1:
                UpdateCurrentActivityAnalysis();
                break;
            case 2:
                ShowAppPurpose();
                break;
            case 3:
                ShowDefaultSettings();
                break;
            case 4:
                ShowLearningOutcomes();
                break;
            case 5:
                ShowScientificReferences();
                break;
        }
        
        // Update Content size to fit all text
        UpdateContentSize();
        
        // Reset scroll to top AFTER updating size
        ResetScrollToTop();
        
        Debug.Log($"[HelpPanelToggleController] Button {buttonNumber} clicked, text updated");
    }
    
    /// <summary>
    /// Updates the current activity analysis (Button 1)
    /// </summary>
    void UpdateCurrentActivityAnalysis()
    {
        if (helpContentText == null || atmosphere == null) return;
        
        // Get current ecosystem data
        int trees, grass, animals, humans;
        float totalO2_molPerSec, totalCO2_molPerSec;
        float plantPhotosynthesisO2, plantRespirationO2;
        float animalO2, animalCO2;
        float humanO2, humanCO2;
        float oceanCO2;
        
        atmosphere.GetEcosystemStatsWithPlantAgents(
            out trees, out grass, out animals, out humans,
            out totalO2_molPerSec, out totalCO2_molPerSec,
            out plantPhotosynthesisO2, out plantRespirationO2,
            out animalO2, out animalCO2,
            out humanO2, out humanCO2,
            out oceanCO2
        );
        
        // Convert to mol/day for better readability
        float totalO2_molPerDay = totalO2_molPerSec * atmosphere.secondsPerDay;
        float totalCO2_molPerDay = totalCO2_molPerSec * atmosphere.secondsPerDay;
        
        // Get current atmosphere percentages
        float o2Percent = atmosphere.oxygen;
        float co2Percent = atmosphere.carbonDioxide;
        
        // Build analysis text
        string analysis = "<b>=== ECOSYSTEM ANALYSIS ===</b>\n\n";
        
        // Population Summary
        analysis += "<b>POPULATION:</b>\n";
        analysis += $"Trees: {trees} | Grass: {grass}\n";
        analysis += $"Animals: {animals} | Humans: {humans}\n\n";
        
        // Atmosphere Status
        analysis += "<b>ATMOSPHERE:</b>\n";
        analysis += $"O₂: {o2Percent:F3}% | CO₂: {co2Percent:F4}%\n";
        analysis += $"Status: {atmosphere.environmentalStatus}\n\n";
        
        // Insights - Point form analysis
        analysis += "<b>WHAT'S HAPPENING:</b>\n";
        
        // O2 analysis
        if (o2Percent < 15f)
        {
            analysis += $"• O₂ critically low at {o2Percent:F2}% - ecosystem in danger\n";
        }
        else if (o2Percent < 18f)
        {
            analysis += $"• O₂ below safe levels at {o2Percent:F2}% - need more plants\n";
        }
        else if (o2Percent > 21f)
        {
            analysis += $"• O₂ abundant at {o2Percent:F2}% - healthy plant production\n";
        }
        else
        {
            analysis += $"• O₂ stable at {o2Percent:F2}% - balanced ecosystem\n";
        }
        
        // CO2 analysis
        if (co2Percent > 0.5f)
        {
            analysis += $"• CO₂ critically high at {co2Percent:F3}% because animals have {animals} and humans have {humans}, tree O₂ cannot afford the consumption\n";
        }
        else if (co2Percent > 0.1f)
        {
            analysis += $"• CO₂ elevated at {co2Percent:F3}% due to high animal ({animals}) and human ({humans}) respiration\n";
        }
        else if (co2Percent < 0.02f)
        {
            analysis += $"• CO₂ very low at {co2Percent:F4}% - plants may struggle without enough CO₂\n";
        }
        else
        {
            analysis += $"• CO₂ normal at {co2Percent:F4}% - good balance for photosynthesis\n";
        }
        
        // Population dynamics
        if (animals == 0 && humans > 0)
        {
            analysis += "• All animals are dead - humans may be hungry\n";
        }
        else if (animals < 5 && humans > 10)
        {
            analysis += $"• Only {animals} animals left because humans are hunting them\n";
        }
        else if (animals > 50)
        {
            analysis += $"• Animal population high at {animals} - may need more plants for food\n";
        }
        else if (animals > 20 && trees < 10)
        {
            analysis += $"• {animals} animals but only {trees} trees - animals may starve soon\n";
        }
        
        if (humans > 20)
        {
            analysis += $"• Human population very high at {humans} - consuming lots of resources\n";
        }
        else if (humans == 0 && animals > 30)
        {
            analysis += "• No humans remain - animals may overpopulate\n";
        }
        
        if (trees == 0)
        {
            analysis += "• No trees left - O₂ production stopped, ecosystem collapsing\n";
        }
        else if (trees < 5 && (animals + humans) > 20)
        {
            analysis += $"• Only {trees} trees for {animals + humans} consumers - not enough O₂ production\n";
        }
        else if (trees > 50)
        {
            analysis += $"• Forest thriving with {trees} trees - excellent O₂ production\n";
        }
        
        // Gas flow trends
        if (totalO2_molPerDay < -50f)
        {
            analysis += $"• O₂ dropping fast ({totalO2_molPerDay:F0} mol/day) - add more trees immediately\n";
        }
        else if (totalO2_molPerDay > 50f)
        {
            analysis += $"• O₂ rising quickly ({totalO2_molPerDay:F0} mol/day) - plants are thriving\n";
        }
        
        if (totalCO2_molPerDay > 50f)
        {
            analysis += $"• CO₂ rising fast ({totalCO2_molPerDay:F0} mol/day) - too many consumers\n";
        }
        else if (totalCO2_molPerDay < -50f)
        {
            analysis += $"• CO₂ dropping rapidly ({totalCO2_molPerDay:F0} mol/day) - plants absorbing well\n";
        }
        
        // Temperature effects
        if (sunMoon != null)
        {
            float temp = sunMoon.currentTemperature;
            if (temp > 35f)
            {
                analysis += $"• Temperature very high at {temp:F1}°C - animals and humans respiring more\n";
            }
            else if (temp < 10f)
            {
                analysis += $"• Temperature cold at {temp:F1}°C - slower biological processes\n";
            }
        }
        
        // Environmental status warnings
        if (atmosphere.environmentalStatus == AtmosphereManager.EnvironmentalStatus.Critical)
        {
            analysis += "• CRITICAL STATUS - immediate action required to save ecosystem\n";
        }
        else if (atmosphere.environmentalStatus == AtmosphereManager.EnvironmentalStatus.Danger)
        {
            analysis += "• DANGER STATUS - ecosystem needs attention soon\n";
        }
        else if (atmosphere.environmentalStatus == AtmosphereManager.EnvironmentalStatus.Healthy)
        {
            analysis += "• Healthy ecosystem - all systems balanced\n";
        }
        
        helpContentText.text = analysis;
    }
    
    /// <summary>
    /// Shows the purpose and overview of the app (Button 2)
    /// </summary>
    void ShowAppPurpose()
    {
        if (helpContentText == null) return;
        
        string purpose = "";
        
        purpose += "<b>===PURPOSE OF THIS APPLICATION===</b>\n\n";
        
        purpose += "This Ecosystem Simulator is an educational tool designed to help you understand the delicate balance of nature and how different organisms interact within an environment.\n\n";
        
        purpose += "<b>WHAT YOU CAN LEARN:</b>\n\n";
        
        purpose += "<b>1. Gas Exchange & Atmosphere</b>\n";
        purpose += "Watch in real-time how plants produce oxygen (O₂) through photosynthesis and consume carbon dioxide (CO₂). See how animals and humans do the opposite - consuming O₂ and producing CO₂ through respiration. The atmosphere composition changes dynamically based on the population balance.\n\n";
        
        purpose += "<b>2. Population Dynamics</b>\n";
        purpose += "Observe how populations grow, shrink, and interact. Trees and grass provide food and oxygen. Animals hunt for food and reproduce. Humans hunt animals for survival. Each species depends on others, creating a complex web of life.\n\n";
        
        purpose += "<b>3. Environmental Impact</b>\n";
        purpose += "Understand how overpopulation of one species can destabilize the entire ecosystem. Too many consumers without enough producers leads to oxygen depletion and CO₂ buildup. The simulator shows you the consequences of imbalance.\n\n";
        
        purpose += "<b>4. Temperature Effects</b>\n";
        purpose += "Experience how day-night cycles and temperature changes affect biological processes. Higher temperatures increase respiration rates, while cooler temperatures slow down activity.\n\n";
        
        purpose += "<b>WHY THIS MATTERS:</b>\n\n";
        
        purpose += "In the real world, our Earth's ecosystem works the same way. Forests produce the oxygen we breathe. Animals maintain balance by controlling plant populations. Humans have a massive impact on this balance.\n\n";
        
        purpose += "By experimenting with this simulator, you can:\n";
        purpose += "• See what happens when forests are destroyed\n";
        purpose += "• Understand why biodiversity is important\n";
        purpose += "• Learn how overpopulation affects resources\n";
        purpose += "• Appreciate the interconnectedness of all life\n\n";
        
        purpose += "<b>YOUR ROLE:</b>\n\n";
        purpose += "You control the ecosystem by adding or removing organisms. Try different scenarios - create a balanced paradise or watch what happens when you add too many of one species. Every action has consequences.\n\n";
        
        purpose += "<b>USER INTERACTIONS (DISRUPTIONS):</b>\n\n";
        
        purpose += "<b>Adding Organisms:</b>\n";
        purpose += "• Add Humans: Click the +Human button to increase population. Each human consumes 30 mol O₂/day and produces 30 mol CO₂/day. Humans hunt animals for food.\n";
        purpose += "• Add Animals: Click the +Animal button to increase animal population. Each animal consumes 12 mol O₂/day and produces 12 mol CO₂/day. Animals eat grass and plants.\n";
        purpose += "• Add Trees: Click the +Tree button to increase oxygen production. Each tree produces net +43.9 mol O₂/day.\n";
        purpose += "• Add Grass: Click the +Grass button to provide food for animals and produce oxygen. Each grass produces net +5.48 mol O₂/day.\n\n";
        
        purpose += "<b>Hunting & Removal:</b>\n";
        purpose += "• Hunt Animals: Humans automatically hunt nearby animals when hungry. Hunting reduces animal population and provides food for humans.\n";
        purpose += "• Natural Death: All organisms have lifespans and die naturally, returning nutrients to the ecosystem.\n";
        purpose += "• Starvation: Without enough food sources, animals and humans will starve, causing population decline.\n\n";
        
        purpose += "<b>Ecosystem Disruption Examples:</b>\n";
        purpose += "• Add 50 humans → Rapid O₂ depletion, CO₂ spike, animal extinction from overhunting\n";
        purpose += "• Add 100 animals → Plants get eaten too quickly, oxygen production drops, starvation\n";
        purpose += "• Remove all trees → Oxygen runs out, all animals and humans suffocate\n";
        purpose += "• Add 200 trees → Excess oxygen, CO₂ drops too low, plants struggle to photosynthesize\n\n";
        
        purpose += "This simulator is a window into understanding our planet's fragile balance and the importance of environmental conservation.\n";
        
        helpContentText.text = purpose;
    }
    
    /// <summary>
    /// Shows normal environment flow and user interaction disruptions (Button 3)
    /// </summary>
    void ShowDefaultSettings()
    {
        if (helpContentText == null) return;
        
        string settings = "<b>═══════════════════════════════</b>\n";
        settings += "<b>2.1 NORMAL ENVIRONMENT (Default Flow)</b>\n";
        settings += "<b>═══════════════════════════════</b>\n\n";
        
        // Get current population (not dynamic - snapshot)
        int trees = 0, grass = 0, animals = 0, humans = 0;
        if (atmosphere != null)
        {
            atmosphere.GetEcosystemStatsWithPlantAgents(
                out trees, out grass, out animals, out humans,
                out _, out _, out _, out _, out _, out _, out _, out _, out _
            );
        }
        
        settings += "<b>CURRENT POPULATION:</b>\n";
        settings += $"• Trees: {trees}\n";
        settings += $"• Grass: {grass}\n";
        settings += $"• Animals: {animals}\n";
        settings += $"• Humans: {humans}\n\n";
        
        settings += "<b>EXPECTED O₂ & CO₂ FLOW (Per Day):</b>\n\n";
        
        settings += "<b>🌳 TREES:</b>\n";
        settings += $"   Count: {trees} trees\n";
        settings += "   Per Tree: +43.9 mol O₂/day, -43.9 mol CO₂/day\n";
        settings += $"   Total: +{trees * 43.9f:F1} mol O₂/day, -{trees * 43.9f:F1} mol CO₂/day\n\n";
        
        settings += "<b>🌱 GRASS:</b>\n";
        settings += $"   Count: {grass} grass\n";
        settings += "   Per Grass: +5.48 mol O₂/day, -5.48 mol CO₂/day\n";
        settings += $"   Total: +{grass * 5.48f:F1} mol O₂/day, -{grass * 5.48f:F1} mol CO₂/day\n\n";
        
        settings += "<b>🦌 ANIMALS:</b>\n";
        settings += $"   Count: {animals} animals\n";
        settings += "   Per Animal: -12.0 mol O₂/day, +12.0 mol CO₂/day\n";
        settings += $"   Total: -{animals * 12f:F1} mol O₂/day, +{animals * 12f:F1} mol CO₂/day\n";
        settings += "   Eating Behavior: Eat grass every 30-60 seconds\n";
        settings += "   Food Need: ~2-3 grass per day to survive\n\n";
        
        settings += "<b>👤 HUMANS:</b>\n";
        settings += $"   Count: {humans} humans\n";
        settings += "   Per Human: -30.0 mol O₂/day, +30.0 mol CO₂/day\n";
        settings += $"   Total: -{humans * 30f:F1} mol O₂/day, +{humans * 30f:F1} mol CO₂/day\n";
        settings += "   Hunting Behavior: Hunt animals every 40-80 seconds\n";
        settings += "   Food Need: ~1-2 animals per day to survive\n\n";
        
        settings += "<b>🌊 OCEAN CO₂ SINK:</b>\n";
        settings += "   Absorption: -200 mol CO₂/day (natural buffer)\n\n";
        
        // Calculate balance
        float totalO2Production = trees * 43.9f + grass * 5.48f;
        float totalO2Consumption = animals * 12f + humans * 30f;
        float netO2Flow = totalO2Production - totalO2Consumption;
        
        float totalCO2Consumption = trees * 43.9f + grass * 5.48f + 200f;
        float totalCO2Production = animals * 12f + humans * 30f;
        float netCO2Flow = totalCO2Production - totalCO2Consumption;
        
        settings += "<b>NET ECOSYSTEM BALANCE:</b>\n";
        settings += $"• O₂: {netO2Flow:+F1;-F1} mol/day ";
        
        if (netO2Flow > 50)
            settings += "(✅ Healthy)\n";
        else if (netO2Flow > 0)
            settings += "(⚠️ Balanced)\n";
        else
            settings += "(❌ Danger)\n";
        
        settings += $"• CO₂: {netCO2Flow:+F1;-F1} mol/day\n\n";
        
        settings += "<b>═══════════════════════════════</b>\n";
        settings += "<b>2.2 DISRUPTION (User Interaction)</b>\n";
        settings += "<b>═══════════════════════════════</b>\n\n";
        
        settings += "<b>WHAT YOU CAN DO:</b>\n\n";
        
        settings += "<b>➕ ADD ORGANISMS:</b>\n";
        settings += "• <b>+Human Button:</b> Add 1 human\n";
        settings += "   → -30 mol O₂/day, +30 mol CO₂/day per human\n";
        settings += "   → Hunts animals for food\n";
        settings += "   → High oxygen demand\n\n";
        
        settings += "• <b>+Animal Button:</b> Add 1 animal\n";
        settings += "   → -12 mol O₂/day, +12 mol CO₂/day per animal\n";
        settings += "   → Eats grass/plants\n";
        settings += "   → Can be hunted by humans\n\n";
        
        settings += "• <b>+Tree Button:</b> Add 1 tree\n";
        settings += "   → +43.9 mol O₂/day, -43.9 mol CO₂/day per tree\n";
        settings += "   → Major oxygen producer\n";
        settings += "   → Helps balance ecosystem\n\n";
        
        settings += "• <b>+Grass Button:</b> Add 1 grass\n";
        settings += "   → +5.48 mol O₂/day, -5.48 mol CO₂/day per grass\n";
        settings += "   → Food source for animals\n";
        settings += "   → Small oxygen contributor\n\n";
        
        settings += "<b>🎯 HUNTING & INTERACTIONS:</b>\n";
        settings += "• <b>Humans Hunt Animals:</b>\n";
        settings += "   → Automatic when human is hungry\n";
        settings += "   → Reduces animal population\n";
        settings += "   → Happens every 40-80 seconds\n\n";
        
        settings += "• <b>Animals Eat Grass:</b>\n";
        settings += "   → Automatic when animal is hungry\n";
        settings += "   → Reduces grass population\n";
        settings += "   → Happens every 30-60 seconds\n\n";
        
        settings += "<b>⚠️ DISRUPTION EXAMPLES:</b>\n\n";
        
        settings += "<b>Scenario 1: Add 50 Humans</b>\n";
        settings += "• O₂ Consumption: -1,500 mol/day (massive drain!)\n";
        settings += "• Animals get overhunted → extinction\n";
        settings += "• Humans starve without food\n";
        settings += "• Oxygen levels crash → suffocation\n\n";
        
        settings += "<b>Scenario 2: Add 100 Animals</b>\n";
        settings += "• Grass gets eaten too fast → plant extinction\n";
        settings += "• O₂ Production drops (no plants left)\n";
        settings += "• Animals starve to death\n";
        settings += "• Ecosystem collapse\n\n";
        
        settings += "<b>Scenario 3: Remove All Trees</b>\n";
        settings += "• O₂ Production drops drastically\n";
        settings += "• CO₂ builds up in atmosphere\n";
        settings += "• All animals/humans suffocate\n";
        settings += "• Total ecosystem failure\n\n";
        
        settings += "<b>Scenario 4: Add 200 Trees</b>\n";
        settings += "• Excess O₂ production (+8,780 mol/day!)\n";
        settings += "• CO₂ drops too low for photosynthesis\n";
        settings += "• Plants struggle to grow\n";
        settings += "• Imbalanced but survivable\n\n";
        
        settings += "<b>💡 KEY TAKEAWAYS:</b>\n";
        settings += "• Every organism affects O₂/CO₂ balance\n";
        settings += "• Overpopulation = resource depletion\n";
        settings += "• Predators need prey to survive\n";
        settings += "• Plants are the foundation of life\n";
        settings += "• Balance is crucial for survival\n\n";
        
        settings += "Experiment and see how your actions affect the ecosystem!\n";
        
        helpContentText.text = settings;
    }
    
    /// <summary>
    /// Shows learning outcomes and scenarios (Button 4)
    /// </summary>
    void ShowLearningOutcomes()
    {
        if (helpContentText == null) return;
        
        string learning = "<b>═══════════════════════════════</b>\n";
        learning += "<b>3.0 WHAT YOU CAN LEARN FROM THIS SIMULATOR</b>\n";
        learning += "<b>═══════════════════════════════</b>\n\n";
        
        learning += "<b>EDUCATIONAL GOALS:</b>\n\n";
        
        learning += "• <b>Ecosystem Balance:</b> Understand how populations must remain balanced for survival\n";
        learning += "• <b>Gas Cycles:</b> Learn how oxygen and carbon dioxide flow through living systems\n";
        learning += "• <b>Food Chains:</b> See how energy transfers from plants → animals → humans\n";
        learning += "• <b>Cause & Effect:</b> Every action has consequences that ripple through the environment\n";
        learning += "• <b>Sustainability:</b> Discover why biodiversity and conservation matter\n\n";
        
        learning += "<b>═══════════════════════════════</b>\n";
        learning += "<b>SCENARIO-BASED LEARNING</b>\n";
        learning += "<b>═══════════════════════════════</b>\n\n";
        
        // Scenario 1: Too Many Humans
        learning += "<b>3.1 SCENARIO: TOO MANY HUMANS</b>\n";
        learning += "<b>Action:</b> Add 50+ humans to the ecosystem\n\n";
        
        learning += "<b>Expected Environmental Changes:</b>\n\n";
        
        learning += "<b>Phase 1: Immediate Impact (First 1-2 days)</b>\n";
        learning += "• O₂ Consumption SKYROCKETS: -1,500 mol/day or more\n";
        learning += "• CO₂ Production SURGES: +1,500 mol/day\n";
        learning += "• Oxygen % drops rapidly from 20.95% toward 18%\n";
        learning += "• CO₂ % rises from 0.04% toward 0.1%+\n";
        learning += "• Environmental Status: Healthy → Danger → Critical\n\n";
        
        learning += "<b>Phase 2: Animal Extinction (Days 2-4)</b>\n";
        learning += "• Humans hunt animals aggressively (every 40-80 seconds)\n";
        learning += "• Animal population crashes to near zero\n";
        learning += "• Grass population increases (no animals eating them)\n";
        learning += "• But grass O₂ production can't keep up with human demand\n\n";
        
        learning += "<b>Phase 3: Mass Starvation (Days 4-7)</b>\n";
        learning += "• No animals left to hunt → humans starve\n";
        learning += "• Human population begins declining from starvation\n";
        learning += "• Oxygen continues depleting despite fewer consumers\n";
        learning += "• Trees can't photosynthesize fast enough to recover O₂\n\n";
        
        learning += "<b>Phase 4: Ecosystem Collapse (Week 2+)</b>\n";
        learning += "• O₂ drops below survivable levels (<15%)\n";
        learning += "• Remaining organisms suffocate even if food is available\n";
        learning += "• CO₂ buildup creates toxic atmosphere (>1%)\n";
        learning += "• Complete ecosystem failure - all life dies\n\n";
        
        learning += "<b>🎓 LESSON LEARNED:</b>\n";
        learning += "• Overpopulation exhausts resources faster than they can regenerate\n";
        learning += "• Predator overpopulation destroys prey populations\n";
        learning += "• Without prey, predators starve despite plenty of producers\n";
        learning += "• Real-world parallel: Human overpopulation and resource depletion\n\n";
        
        learning += "─────────────────────────────────\n\n";
        
        // Scenario 2: Too Many Animals
        learning += "<b>3.2 SCENARIO: TOO MANY ANIMALS</b>\n";
        learning += "<b>Action:</b> Add 100+ animals to the ecosystem\n\n";
        
        learning += "<b>Expected Environmental Changes:</b>\n\n";
        
        learning += "<b>Phase 1: Plant Destruction (First 1-2 days)</b>\n";
        learning += "• Animals eat grass every 30-60 seconds\n";
        learning += "• 100 animals need ~200-300 grass per day\n";
        learning += "• Grass population depletes within hours\n";
        learning += "• O₂ Production drops as plants disappear\n";
        learning += "• O₂ Consumption increases: -1,200 mol/day from animals alone\n\n";
        
        learning += "<b>Phase 2: Oxygen Crisis (Days 2-3)</b>\n";
        learning += "• With grass gone, O₂ production drops drastically\n";
        learning += "• Only trees remain as O₂ producers\n";
        learning += "• Net O₂ balance becomes severely negative\n";
        learning += "• Oxygen % starts dropping toward 18%\n";
        learning += "• CO₂ accumulates rapidly (0.04% → 0.08%)\n\n";
        
        learning += "<b>Phase 3: Mass Starvation (Days 3-5)</b>\n";
        learning += "• Animals can't find food (all grass eaten)\n";
        learning += "• Animal population crashes from starvation\n";
        learning += "• Dead animals decompose (more CO₂ released)\n";
        learning += "• Humans may starve too (hunting dead/weak animals first)\n\n";
        
        learning += "<b>Phase 4: Recovery or Death (Week 2+)</b>\n";
        learning += "• If trees survive: Ecosystem might slowly recover\n";
        learning += "• Grass regrows when animal population drops\n";
        learning += "• But O₂ levels may have dropped too low already\n";
        learning += "• Remaining organisms suffocate before recovery\n\n";
        
        learning += "<b>🎓 LESSON LEARNED:</b>\n";
        learning += "• Herbivore overpopulation destroys plant foundation\n";
        learning += "• Without producers, entire ecosystem collapses\n";
        learning += "• Food chain breaks when primary consumers overpopulate\n";
        learning += "• Real-world parallel: Invasive species destroying habitats\n\n";
        
        learning += "─────────────────────────────────\n\n";
        
        // Scenario 3: Deforestation
        learning += "<b>3.3 SCENARIO: DEFORESTATION (Remove All Trees)</b>\n";
        learning += "<b>Action:</b> Delete/remove all trees from ecosystem\n\n";
        
        learning += "<b>Expected Environmental Changes:</b>\n\n";
        
        learning += "• Lose ~17,560 mol O₂ production per day (400 trees × 43.9)\n";
        learning += "• Only grass remains as O₂ source (~300 mol/day)\n";
        learning += "• Net O₂ balance: Severely negative (-1,700+ mol/day)\n";
        learning += "• Oxygen drops from 20.95% to <15% within 3-5 days\n";
        learning += "• CO₂ rises from 0.04% to >0.5% (lethal levels)\n\n";
        
        learning += "<b>Outcome:</b>\n";
        learning += "• All animals suffocate within 1 week\n";
        learning += "• Humans die from oxygen deprivation\n";
        learning += "• Grass survives temporarily but ecosystem is dead\n";
        learning += "• No recovery possible without replanting trees\n\n";
        
        learning += "<b>🎓 LESSON LEARNED:</b>\n";
        learning += "• Trees are PRIMARY oxygen producers\n";
        learning += "• Deforestation = removing Earth's \"lungs\"\n";
        learning += "• Real-world parallel: Amazon rainforest destruction\n\n";
        
        learning += "─────────────────────────────────\n\n";
        
        // Scenario 4: Excess Trees
        learning += "<b>3.4 SCENARIO: EXCESSIVE FORESTATION (Add 200+ Trees)</b>\n";
        learning += "<b>Action:</b> Add 200 additional trees (600 total)\n\n";
        
        learning += "<b>Expected Environmental Changes:</b>\n\n";
        
        learning += "• O₂ Production: +26,340 mol/day (600 trees × 43.9)\n";
        learning += "• CO₂ Consumption: -26,340 mol/day (plants + ocean)\n";
        learning += "• Oxygen % rises toward 22-25% (hyperoxia)\n";
        learning += "• CO₂ drops below 0.01% (too low for photosynthesis)\n";
        learning += "• Net balance: Extreme O₂ surplus (+25,000+ mol/day)\n\n";
        
        learning += "<b>Outcome:</b>\n";
        learning += "• Consumers (animals/humans) thrive temporarily\n";
        learning += "• But CO₂ becomes scarce for photosynthesis\n";
        learning += "• Trees struggle to grow without enough CO₂\n";
        learning += "• Ecosystem becomes imbalanced but survivable\n";
        learning += "• High O₂ increases fire risk and oxidative stress\n\n";
        
        learning += "<b>🎓 LESSON LEARNED:</b>\n";
        learning += "• Too much of a good thing can still cause problems\n";
        learning += "• Plants need CO₂ to survive (not just animals)\n";
        learning += "• Balance is key - extremes in either direction are bad\n\n";
        
        learning += "─────────────────────────────────\n\n";
        
        // Scenario 5: Predator Extinction
        learning += "<b>3.5 SCENARIO: PREDATOR EXTINCTION (Remove All Humans)</b>\n";
        learning += "<b>Action:</b> Remove all humans from ecosystem\n\n";
        
        learning += "<b>Expected Environmental Changes:</b>\n\n";
        
        learning += "• Save -30 mol O₂/day per human removed\n";
        learning += "• Animal population grows unchecked (no predators)\n";
        learning += "• Animals overbreed and overpopulate\n";
        learning += "• Grass gets overeaten (herbivore explosion)\n";
        learning += "• Eventually leads to Scenario 3.2 (too many animals)\n\n";
        
        learning += "<b>Outcome:</b>\n";
        learning += "• Short-term: Ecosystem improves (less O₂ drain)\n";
        learning += "• Long-term: Animal overpopulation destroys plants\n";
        learning += "• Without predators, herbivores destroy foundation\n\n";
        
        learning += "<b>🎓 LESSON LEARNED:</b>\n";
        learning += "• Predators control prey populations\n";
        learning += "• Removing predators causes prey explosion\n";
        learning += "• Real-world parallel: Wolf reintroduction in Yellowstone\n\n";
        
        learning += "─────────────────────────────────\n\n";
        
        // Scenario 6: Balanced Growth
        learning += "<b>3.6 SCENARIO: BALANCED GROWTH (Add Proportionally)</b>\n";
        learning += "<b>Action:</b> Add organisms in balanced ratios\n";
        learning += "(e.g., +10 trees, +5 grass, +1 animal, +1 human)\n\n";
        
        learning += "<b>Expected Environmental Changes:</b>\n\n";
        
        learning += "• O₂ Production: +466.4 mol/day (10 trees + 5 grass)\n";
        learning += "• O₂ Consumption: -42 mol/day (1 animal + 1 human)\n";
        learning += "• Net O₂: +424.4 mol/day (healthy surplus)\n";
        learning += "• CO₂ balanced by ocean sink\n";
        learning += "• All organisms have adequate food sources\n\n";
        
        learning += "<b>Outcome:</b>\n";
        learning += "• Ecosystem remains stable and healthy\n";
        learning += "• Populations grow sustainably\n";
        learning += "• Oxygen and CO₂ levels stay in safe ranges\n";
        learning += "• Long-term survival for all species\n\n";
        
        learning += "<b>🎓 LESSON LEARNED:</b>\n";
        learning += "• Sustainable growth requires balanced expansion\n";
        learning += "• Must add producers before adding consumers\n";
        learning += "• Ratio of producers:consumers determines survival\n";
        learning += "• Real-world parallel: Sustainable development goals\n\n";
        
        learning += "<b>═══════════════════════════════</b>\n";
        learning += "<b>KEY TAKEAWAYS FROM ALL SCENARIOS</b>\n";
        learning += "<b>═══════════════════════════════</b>\n\n";
        
        learning += "✓ <b>Foundation First:</b> Plants must be established before consumers\n";
        learning += "✓ <b>Population Control:</b> Predators prevent herbivore overpopulation\n";
        learning += "✓ <b>Resource Limits:</b> Finite resources limit maximum population\n";
        learning += "✓ <b>Cascading Effects:</b> One species affects all others\n";
        learning += "✓ <b>Recovery Time:</b> Damage happens fast, recovery takes much longer\n";
        learning += "✓ <b>Biodiversity Matters:</b> Multiple species create stable systems\n";
        learning += "✓ <b>Balance Over Extremes:</b> Moderate populations are most stable\n\n";
        
        learning += "<b>🌍 REAL-WORLD APPLICATION:</b>\n\n";
        
        learning += "This simulator mirrors Earth's ecosystems:\n";
        learning += "• Human population growth → resource depletion\n";
        learning += "• Deforestation → oxygen loss & climate change\n";
        learning += "• Overfishing → marine ecosystem collapse\n";
        learning += "• Invasive species → native species extinction\n";
        learning += "• Climate change → habitat destruction\n\n";
        
        learning += "Understanding these patterns helps us make better decisions for our planet's future!\n";
        
        helpContentText.text = learning;
    }
    
    /// <summary>
    /// Shows scientific references and biological foundations (Button 5)
    /// </summary>
    void ShowScientificReferences()
    {
        if (helpContentText == null) return;
        
        string references = "<b>═══════════════════════════════</b>\n";
        references += "<b>SCIENTIFIC REFERENCES & BIOLOGICAL FOUNDATIONS</b>\n";
        references += "<b>═══════════════════════════════</b>\n\n";
        
        references += "This simulator is built on established scientific principles and research in ecology, biology, and environmental science. Below are the key references that validate the simulation mechanics.\n\n";
        
        references += "<b>═══════════════════════════════</b>\n\n";
        
        // 1. Atmospheric System
        references += "<b>1. ATMOSPHERIC SYSTEM (AtmosphereManager.cs)</b>\n\n";
        
        references += "This system manages the global gas composition, simulating the Earth's \"Source of Truth\" for respiration and photosynthesis.\n\n";
        
        references += "<b>Atmospheric Composition:</b>\n";
        references += "• The simulation initializes with the standard molar ratios of Earth's dry atmosphere (78% N₂, 21% O₂, 0.93% Ar, 0.04% CO₂)\n";
        references += "• <b>Source:</b> \"Earth's atmosphere: Facts about our planet's protective blanket\" (Space.com/NASA)\n";
        references += "• <b>Link:</b> https://nssdc.gsfc.nasa.gov/planetary/factsheet/earthfact.html\n\n";
        
        references += "<b>Gas Exchange Stoichiometry:</b>\n";
        references += "• The 1:1 molar exchange (consuming 1 mole of O₂ produces 1 mole of CO₂) is based on the balanced equation for aerobic respiration:\n";
        references += "• C₆H₁₂O₆ + 6O₂ → 6CO₂ + 6H₂O\n";
        references += "• <b>Source:</b> \"Photosynthesis: Equation, Formula & Products\" (ChemTalk)\n";
        references += "• <b>Link:</b> https://chemistrytalk.org/photosynthesis-equation-formula-products/\n\n";
        
        references += "<b>Ocean Carbon Sink:</b>\n";
        references += "• The removal of CO₂ without adding O₂ simulates the physical dissolution of gas into seawater, a major planetary buffer\n";
        references += "• <b>Source:</b> \"Ocean Carbon & Biogeochemistry\" (NOAA)\n";
        references += "• <b>Link:</b> https://globalocean.noaa.gov/the-ocean/ocean-carbon-biogeochemistry/\n\n";
        
        references += "<b>Safety Thresholds:</b>\n";
        references += "• The warning (19%) and critical (<10%) oxygen levels match OSHA safety standards for human hypoxia\n";
        references += "• <b>Source:</b> \"Oxygen Deficient Atmosphere Hazards\" (OSHA Guidelines)\n";
        references += "• <b>Link:</b> https://www.co2meter.com/blogs/news/oxygen-deficient-atmosphere-hazards\n\n";
        
        references += "<b>Carbon Dioxide Toxicity:</b>\n";
        references += "• CO₂ levels above 1% become toxic to humans (matching simulator critical thresholds)\n";
        references += "• <b>Source:</b> \"Carbon Dioxide Toxicity\" (CDC/NIOSH)\n";
        references += "• <b>Link:</b> https://www.cdc.gov/niosh/idlh/124389.html\n\n";
        
        references += "─────────────────────────────────\n\n";
        
        // 2. Biological Metabolism
        references += "<b>2. BIOLOGICAL METABOLISM (AnimalMetabolism.cs, HumanMetabolism.cs)</b>\n\n";
        
        references += "These scripts calculate energy expenditure based on mass, temperature, and activity.\n\n";
        
        references += "<b>Metabolic Scaling (Kleiber's Law):</b>\n";
        references += "• The formula M_base = BMR × biomass uses the principle that metabolic rate scales to the ¾ power of body mass (Mass^0.75)\n";
        references += "• Explains the difference between human and animal base rates\n";
        references += "• <b>Source:</b> \"Body size and metabolism\" (Kleiber, 1932)\n";
        references += "• <b>Link:</b> https://scispace.com/pdf/body-size-and-metabolism-1rtj2yc7oh.pdf\n\n";
        
        references += "<b>Temperature Sensitivity (Q10):</b>\n";
        references += "• The formula Mathf.Pow(Q10_factor, deltaT / 10f) is the Van 't Hoff equation\n";
        references += "• Models how biological rates roughly double for every 10°C increase\n";
        references += "• <b>Source:</b> \"Temperature coefficient (Q10) and its applications\"\n";
        references += "• <b>Link:</b> https://www.researchgate.net/publication/341991878_Temperature_coefficient_Q10_and_its_applications_in_biological_systems_Beyond_the_Arrhenius_theory\n\n";
        
        references += "<b>Metabolic Rate Overview:</b>\n";
        references += "• Comprehensive resource on metabolic rate principles\n";
        references += "• <b>Source:</b> \"Metabolic Rate\" (Nature Education)\n";
        references += "• <b>Link:</b> https://www.nature.com/scitable/knowledge/library/metabolic-rate-15822369/\n\n";
        
        references += "<b>Trophic Efficiency (10% Rule):</b>\n";
        references += "• The logic where animals consume 10kg of food to gain only a fraction of that mass is based on Lindeman's Efficiency\n";
        references += "• Describes energy loss up the food chain\n";
        references += "• <b>Source:</b> \"The Trophic-Dynamic Aspect of Ecology\" (Lindeman, 1942)\n";
        references += "• <b>Link:</b> https://www.ebsco.com/research-starters/history/lindemans-trophic-dynamic-aspect-ecology-published\n\n";
        
        references += "<b>Thermoregulation:</b>\n";
        references += "• Increasing hunger burn when the temperature deviates from the \"Comfort Zone\" simulates the metabolic cost of maintaining homeostasis (shivering or panting)\n";
        references += "• <b>Source:</b> \"Heat Regulation in Some Arctic and Tropical Mammals\" (Scholander et al., 1950)\n";
        references += "• <b>Link:</b> https://pubmed.ncbi.nlm.nih.gov/14791422/\n\n";
        
        references += "─────────────────────────────────\n\n";
        
        // 3. Population & World Logic
        references += "<b>3. POPULATION & WORLD LOGIC (WorldLogic.cs)</b>\n\n";
        
        references += "This script manages the initial state and spatial rules of the ecosystem.\n\n";
        
        references += "<b>Ecological Pyramids:</b>\n";
        references += "• The ratio of 400 trees to 15 animals (~26:1) follows the \"Pyramid of Numbers\"\n";
        references += "• Primary producers must vastly outnumber consumers to support them\n";
        references += "• <b>Source:</b> \"Ecological Pyramids\" (Nature Education)\n";
        references += "• <b>Link:</b> https://www.nature.com/scitable/knowledge/library/ecological-pyramids-17095478/\n\n";
        
        references += "<b>Energy Flow Through Ecosystems:</b>\n";
        references += "• Explains primary productivity and energy transfer between trophic levels\n";
        references += "• <b>Source:</b> \"Energy Flow\" (Khan Academy)\n";
        references += "• <b>Link:</b> https://www.khanacademy.org/science/biology/ecology/energy-flow-through-ecosystems/a/energy-flow-primary-productivity\n\n";
        
        references += "<b>Carrying Capacity:</b>\n";
        references += "• Limiting spawn attempts (maxTriesPerSpawn) simulates the environment's finite Carrying Capacity (K)\n";
        references += "• A habitat can only support a specific density of life\n";
        references += "• <b>Source:</b> \"Carrying Capacity\" (Nature Education)\n";
        references += "• <b>Link:</b> https://www.nature.com/scitable/knowledge/library/carrying-capacity-the-concept-and-its-ecological-15643906/\n\n";
        
        references += "─────────────────────────────────\n\n";
        
        // 4. Environmental Physics
        references += "<b>4. ENVIRONMENTAL PHYSICS (SunMoonController.cs)</b>\n\n";
        
        references += "This script drives the energy input (Sun) that powers the entire system.\n\n";
        
        references += "<b>Solar Position (Celestial Mechanics):</b>\n";
        references += "• The sine wave path of the sun (Mathf.Sin(t × Mathf.PI)) approximates the sun's daily arc across the sky relative to a fixed observer\n";
        references += "• <b>Source:</b> \"Solar Calculation Details\" (NOAA)\n";
        references += "• <b>Link:</b> https://gml.noaa.gov/grad/solcalc/solareqns.PDF\n\n";
        
        references += "<b>Solar Irradiance (PAR):</b>\n";
        references += "• The logic that photosynthesis efficiency peaks at noon and drops to zero at night reflects Photosynthetically Active Radiation (PAR) curves\n";
        references += "• <b>Source:</b> \"Introduction to Photosynthesis and PAR\" (LI-COR)\n";
        references += "• <b>Link:</b> https://www.licor.com/env/support/LI-190R/topics/what-is-par.html\n\n";
        
        references += "<b>Thermal Lag:</b>\n";
        references += "• The calculation that places the daily temperature peak (e.g., 3:00 PM) after solar noon (12:00 PM) simulates the Earth's thermal inertia\n";
        references += "• The ground continues to heat up even as the sun lowers\n";
        references += "• <b>Source:</b> \"Diurnal Variation of Air Temperature\" (Hong Kong Observatory)\n";
        references += "• <b>Link:</b> https://www.hko.gov.hk/en/education/weather/temperature/00295-diurnal-variation-of-air-temperature.html\n\n";
        
        references += "─────────────────────────────────\n\n";
        
        // 5. Animal Behavior
        references += "<b>5. ANIMAL BEHAVIOR (AnimalWander.cs)</b>\n\n";
        
        references += "<b>Random Walk Movement:</b>\n";
        references += "• The movement logic using random vectors to find new targets implements a Correlated Random Walk (CRW)\n";
        references += "• A standard model for animal foraging when no food is visible\n";
        references += "• <b>Source:</b> \"Random walk models in biology\" (Codling et al., 2008)\n";
        references += "• <b>Link:</b> https://royalsocietypublishing.org/doi/10.1098/rsif.2008.0014\n\n";
        
        references += "<b>Optimal Foraging:</b>\n";
        references += "• The logic in FindNearestPlant (maximizing gain/minimizing distance) is a simplified Optimal Foraging Theory\n";
        references += "• <b>Source:</b> \"Optimal Foraging Theory: A Critical Review\" (Pyke, 1984)\n";
        references += "• <b>Link:</b> https://www.researchgate.net/publication/229190360_Optimal_Foraging_Theory_A_Critical_Review\n\n";
        
        references += "<b>Biomass Estimation:</b>\n";
        references += "• Tree and grass biomass calculations based on forestry and agriculture research\n";
        references += "• <b>Tree Biomass Source:</b> USDA Forest Service\n";
        references += "• <b>Link:</b> https://www.fs.usda.gov/treesearch/pubs/19539\n";
        references += "• <b>Grass/Forage Source:</b> NDSU Agriculture\n";
        references += "• <b>Link:</b> https://www.ag.ndsu.edu/publications/livestock/determining-carrying-capacity-and-stocking-rates-for-range-and-pasture\n\n";
        
        references += "<b>═══════════════════════════════</b>\n";
        references += "<b>FULL CITATIONS</b>\n";
        references += "<b>═══════════════════════════════</b>\n\n";
        
        references += "1. <b>Atmospheric Composition:</b> Space.com/NASA. \"Earth's atmosphere: Facts about our planet's protective blanket.\"\n\n";
        
        references += "2. <b>Gas Exchange:</b> ChemTalk. \"Photosynthesis: Equation, Formula & Products.\"\n\n";
        
        references += "3. <b>Ocean Carbon Sink:</b> NOAA. \"Ocean Carbon & Biogeochemistry.\"\n\n";
        
        references += "4. <b>Safety Thresholds:</b> OSHA. \"Oxygen Deficient Atmosphere Hazards.\"\n\n";
        
        references += "5. <b>Metabolic Scaling:</b> Kleiber, M. (1932). Body size and metabolism. <i>Hilgardia</i>, 6(11), 315-353.\n\n";
        
        references += "6. <b>Q10 Temperature:</b> Temperature coefficient (Q10) and its applications in biological systems. ResearchGate.\n\n";
        
        references += "7. <b>Trophic Efficiency:</b> Lindeman, R. L. (1942). The trophic-dynamic aspect of ecology. <i>Ecology</i>, 23(4), 399-417.\n\n";
        
        references += "8. <b>Thermoregulation:</b> Scholander, P. F., et al. (1950). Heat regulation in some arctic and tropical mammals and birds. <i>The Biological Bulletin</i>, 99(2), 237-258.\n\n";
        
        references += "9. <b>Ecological Pyramids:</b> Nature Education. \"Ecological Pyramids.\"\n\n";
        
        references += "10. <b>Spatial Patterns:</b> Dale, M. R. T. (1999). Spatial patterns in plant communities. Cambridge University Press.\n\n";
        
        references += "11. <b>Carrying Capacity:</b> Nature Education. \"Carrying Capacity.\"\n\n";
        
        references += "12. <b>Solar Calculations:</b> NOAA. \"Solar Calculation Details\" (PDF).\n\n";
        
        references += "13. <b>PAR:</b> LI-COR. \"Introduction to Photosynthesis and PAR.\"\n\n";
        
        references += "14. <b>Thermal Lag:</b> Hong Kong Observatory. \"Diurnal Variation of Air Temperature.\"\n\n";
        
        references += "15. <b>Random Walk:</b> Codling, E. A., et al. (2008). Random walk models in biology. <i>Journal of the Royal Society Interface</i>, 5(25), 813-834.\n\n";
        
        references += "16. <b>Optimal Foraging:</b> Pyke, G. H. (1984). Optimal foraging theory: A critical review. <i>Annual Review of Ecology and Systematics</i>, 15, 523-575.\n\n";
        
        references += "<b>═══════════════════════════════</b>\n\n";
        
        references += "<b>🎓 EDUCATIONAL VALIDATION:</b>\n\n";
        
        references += "This simulator is not just a game—it's an educational tool grounded in decades of peer-reviewed scientific research. Every mechanic reflects real biological and physical principles:\n\n";
        
        references += "• <b>Atmospheric Chemistry:</b> Accurate gas ratios and exchange equations\n";
        references += "• <b>Metabolic Physics:</b> Kleiber's Law, Q10 coefficients, thermoregulation\n";
        references += "• <b>Ecological Theory:</b> Trophic pyramids, carrying capacity, spatial distribution\n";
        references += "• <b>Environmental Science:</b> Solar cycles, thermal dynamics, carbon sinks\n";
        references += "• <b>Animal Behavior:</b> Optimal foraging, random walk models\n\n";
        
        references += "By experimenting with this simulator, you're learning the same concepts that ecologists and environmental scientists use to:\n";
        references += "• Predict ecosystem collapse\n";
        references += "• Manage wildlife populations\n";
        references += "• Design conservation strategies\n";
        references += "• Understand climate change impacts\n";
        references += "• Model planetary life support systems\n\n";
        
        references += "The accuracy of these models makes this simulator a valuable learning resource for students, educators, and anyone interested in environmental science.\n";
        
        helpContentText.text = references;
    }
    
    /// <summary>
    /// Updates the Content RectTransform height to fit all text
    /// </summary>
    void UpdateContentSize()
    {
        if (helpContentText == null) return;
        
        // Force text to recalculate its preferred height
        Canvas.ForceUpdateCanvases();
        
        // Get the Text component's RectTransform
        RectTransform textRect = helpContentText.GetComponent<RectTransform>();
        if (textRect == null) return;
        
        // Calculate the preferred height of the text
        float preferredHeight = helpContentText.preferredHeight;
        
        // Add padding (optional - adjust as needed)
        float padding = 20f;
        float totalHeight = preferredHeight + padding;
        
        // Get the Content RectTransform (parent of the text)
        RectTransform contentRect = textRect.parent as RectTransform;
        if (contentRect != null)
        {
            // Set the Content height to accommodate all text
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
            
            Debug.Log($"[HelpPanel] Content height updated to {totalHeight:F0}px (text needs {preferredHeight:F0}px)");
        }
        else
        {
            Debug.LogWarning("[HelpPanel] Could not find Content RectTransform!");
        }
    }
    
    /// <summary>
    /// Resets scroll position to top
    /// </summary>
    void ResetScrollToTop()
    {
        if (scrollRect != null)
        {
            // Set vertical scroll to top (1.0 = top, 0.0 = bottom)
            scrollRect.verticalNormalizedPosition = 1.0f;
        }
    }
    
    /// <summary>
    /// Selects a button and deselects all others
    /// </summary>
    void SelectButton(Button btn)
    {
        if (btn == null) return;
        
        // Deselect previous button
        if (selectedButton != null && selectedButton != btn)
        {
            SetButtonAlpha(selectedButton, 0.5f);
        }
        
        // Select new button
        selectedButton = btn;
        SetButtonAlpha(btn, 1.0f);
        
        Debug.Log($"[HelpPanelToggleController] Button selected: {btn.name}");
    }
    
    /// <summary>
    /// Sets the alpha value of a button's image
    /// </summary>
    void SetButtonAlpha(Button btn, float alpha)
    {
        if (btn == null) return;
        
        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
        {
            Color color = btnImage.color;
            color.a = alpha;
            btnImage.color = color;
        }
    }
}
