using UnityEngine;
using UnityEngine.UI;

public class AtmosphereUI : MonoBehaviour
{
    [Header("Gas Percentage UI References")]
    public Text waterVaporText;
    public Text nitrogenText;
    public Text oxygenText;
    public Text argonText;
    public Text carbonDioxideText;
    public Text totalText;

    [Header("Ecosystem Stats UI (Optional)")]
    public Text ecosystemStatsText;
    
    [Header("Environmental Status UI (Optional)")]
    public Text environmentalStatusText;

    [Header("Color Coding (Optional)")]
    public bool useColorCoding = true;
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [Header("Warning Thresholds")]
    [Tooltip("Oxygen below this % shows warning")]
    public float oxygenWarningThreshold = 18f;
    [Tooltip("Oxygen below this % shows danger")]
    public float oxygenDangerThreshold = 15f;
    
    [Tooltip("CO₂ above this % shows warning")]
    public float co2WarningThreshold = 0.1f;
    [Tooltip("CO₂ above this % shows danger")]
    public float co2DangerThreshold = 0.5f;

    private AtmosphereManager atmosphere;

    void Start()
    {
        atmosphere = AtmosphereManager.Instance;
        
        if (!atmosphere)
        {
            Debug.LogError("[AtmosphereUI] No AtmosphereManager found in scene!");
        }
    }

    void Update()
    {
        if (!atmosphere) return;

        // Update all gas displays (with mole amounts)
        UpdateGasDisplay(waterVaporText, "H₂O", atmosphere.waterVapor, Color.cyan);
        UpdateGasDisplay(nitrogenText, "N₂", atmosphere.nitrogen, normalColor);
        UpdateGasDisplay(oxygenText, "O₂", atmosphere.oxygen, GetOxygenColor());
        UpdateGasDisplay(argonText, "Ar", atmosphere.argon, normalColor);
        UpdateGasDisplay(carbonDioxideText, "CO₂", atmosphere.carbonDioxide, GetCO2Color());

        // Update total
        if (totalText)
        {
            float total = atmosphere.GetTotalPercentage();
            totalText.text = $"Total: {total:F2}%";
            totalText.color = Mathf.Abs(total - 100f) < 0.1f ? Color.green : Color.red;
        }

        // Update ecosystem stats (optional)
        if (ecosystemStatsText)
        {
            atmosphere.GetEcosystemStats(out int trees, out int grass, out int animals, out int humans,
                                          out float totalO2, out float totalCO2);
            
            ecosystemStatsText.text = $"Trees: {trees}  Grass: {grass}\n" +
                                      $"Animals: {animals}  Humans: {humans}\n" +
                                      $"Net O₂: {totalO2:F1} mol/day\n" +
                                      $"Net CO₂: {totalCO2:F1} mol/day";
        }
        
        // Update environmental status (optional)
        if (environmentalStatusText)
        {
            environmentalStatusText.text = atmosphere.GetEnvironmentalStatusMessage();
            
            // Color based on status
            switch (atmosphere.environmentalStatus)
            {
                case AtmosphereManager.EnvironmentalStatus.Healthy:
                    environmentalStatusText.color = Color.green;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Warning:
                    environmentalStatusText.color = warningColor;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Danger:
                    environmentalStatusText.color = dangerColor;
                    break;
                case AtmosphereManager.EnvironmentalStatus.Critical:
                    environmentalStatusText.color = Color.red;
                    break;
            }
        }
    }

    void UpdateGasDisplay(Text textElement, string gasName, float percentage, Color color)
    {
        if (!textElement) return;

        // Get actual moles directly from AtmosphereManager (source of truth)
        float moles = 0f;
        switch (gasName)
        {
            case "H₂O": moles = atmosphere.waterVaporMoles; break;
            case "N₂": moles = atmosphere.nitrogenMoles; break;
            case "O₂": moles = atmosphere.oxygenMoles; break;
            case "Ar": moles = atmosphere.argonMoles; break;
            case "CO₂": moles = atmosphere.carbonDioxideMoles; break;
        }
        
        // Format: "CO₂: 0.041% / 410 mol"
        textElement.text = $"{gasName}: {percentage:F3}% / {moles:F0} mol";
        
        if (useColorCoding)
        {
            textElement.color = color;
        }
    }

    Color GetOxygenColor()
    {
        if (!useColorCoding) return normalColor;
        
        if (atmosphere.oxygen < oxygenDangerThreshold)
            return dangerColor;
        else if (atmosphere.oxygen < oxygenWarningThreshold)
            return warningColor;
        else
            return normalColor;
    }

    Color GetCO2Color()
    {
        if (!useColorCoding) return normalColor;
        
        if (atmosphere.carbonDioxide > co2DangerThreshold)
            return dangerColor;
        else if (atmosphere.carbonDioxide > co2WarningThreshold)
            return warningColor;
        else
            return normalColor;
    }
}
